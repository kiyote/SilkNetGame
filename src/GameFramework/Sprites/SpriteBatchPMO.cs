using System.Drawing;
using System.Runtime.CompilerServices;
using GameFramework.Textures;
using Silk.NET.OpenGL;

namespace GameFramework.Sprites;

internal unsafe sealed class SpriteBatchPMO : ISpriteBatch {
	private const int TEXTURE_UNIT_0 = 0;

	private const int POSITION_OFFSET = 0;
	private const int TEXTURE_OFFSET = 8;
	private const int COLOUR_OFFSET = 16;
	private const int VERTICES_PER_SPRITE = 4;

	// Per-segment capacity and fence-ring depth are configured through
	// SpriteBatchOptions (see that type for the semantics and tuning guidance).
	private static readonly uint STRIDE = (uint)Unsafe.SizeOf<SpriteVertex>();

	private readonly int _maxSpritesPerFrame;
	private readonly int _numSegments;
	private readonly uint _frameMaxBytes;
	private readonly uint _totalBufferBytes;

	private readonly GL _gl;
	private readonly SpriteBatchShader _shader;

	private uint _vao;
	private uint _vbo;
	private uint _ibo;

	private void* _mappedVboPtr = null;
	private readonly nint[] _fences;
	private int _currentSegment;

	private int _bufferedSprites;
	private bool _isBatching;
	private bool _isDisposed;
	private Rectangle? _clip;
	private ITexture? _texture;
	private IRenderTarget? _renderTarget;
	private BlendMode _blendMode;

	public SpriteBatchPMO(
		GL gl,
		GlStateCache stateCache,
		SpriteBatchOptions? options = null
	) {
		options ??= new SpriteBatchOptions();

		// SpriteBatchOptions validates its own values on assignment, so by the time
		// they reach here they are guaranteed to be within the supported ranges.
		_maxSpritesPerFrame = options.MaxSpritesPerFrame;
		_numSegments = options.NumSegments;
		_frameMaxBytes = (uint)( _maxSpritesPerFrame * VERTICES_PER_SPRITE * STRIDE );
		_totalBufferBytes = _frameMaxBytes * (uint)_numSegments;
		_fences = new nint[_numSegments];

		_gl = gl;
		_shader = new SpriteBatchShader( gl, stateCache );
		Initialize();
	}

	private void Initialize() {
		_vao = _gl.GenVertexArray();
		_gl.BindVertexArray( _vao );

		_vbo = _gl.GenBuffer();
		_gl.BindBuffer( BufferTargetARB.ArrayBuffer, _vbo );

		BufferStorageMask storageFlags =
				BufferStorageMask.MapWriteBit
				| BufferStorageMask.MapPersistentBit;

		_gl.BufferStorage( GLEnum.ArrayBuffer, _totalBufferBytes, null, storageFlags );

		MapBufferAccessMask mapFlags =
			MapBufferAccessMask.WriteBit
			| MapBufferAccessMask.PersistentBit
			| MapBufferAccessMask.FlushExplicitBit;

		_mappedVboPtr = _gl.MapBufferRange( GLEnum.ArrayBuffer, 0, _totalBufferBytes, mapFlags );

		if( _mappedVboPtr == null ) {
			throw new InvalidOperationException( $"OpenGL failed to map the buffer! GL Error: {_gl.GetError()}" );
		}

		_gl.EnableVertexAttribArray( 0 );
		_gl.VertexAttribPointer( 0, 2, VertexAttribPointerType.Float, false, STRIDE, (void*)POSITION_OFFSET );

		_gl.EnableVertexAttribArray( 1 );
		_gl.VertexAttribPointer( 1, 2, VertexAttribPointerType.Float, false, STRIDE, (void*)TEXTURE_OFFSET );

		_gl.EnableVertexAttribArray( 2 );
		_gl.VertexAttribPointer( 2, 4, VertexAttribPointerType.UnsignedByte, true, STRIDE, (void*)COLOUR_OFFSET );

		ushort[] indices = new ushort[_maxSpritesPerFrame * 6];
		for( int i = 0; i < _maxSpritesPerFrame; i++ ) {
			int indexOffset = i * 6;
			int vertexOffset = i * 4;

			indices[indexOffset + 0] = (ushort)( vertexOffset + 0 );
			indices[indexOffset + 1] = (ushort)( vertexOffset + 1 );
			indices[indexOffset + 2] = (ushort)( vertexOffset + 2 );
			indices[indexOffset + 3] = (ushort)( vertexOffset + 2 );
			indices[indexOffset + 4] = (ushort)( vertexOffset + 3 );
			indices[indexOffset + 5] = (ushort)( vertexOffset + 0 );
		}

		_ibo = _gl.GenBuffer();
		_gl.BindBuffer( BufferTargetARB.ElementArrayBuffer, _ibo );

		fixed( ushort* ptr = indices ) {
			_gl.BufferData( BufferTargetARB.ElementArrayBuffer, (nuint)( indices.Length * sizeof( ushort ) ), ptr, BufferUsageARB.StaticDraw );
		}

		_gl.BindBuffer( BufferTargetARB.ElementArrayBuffer, 0 );
		_gl.BindVertexArray( 0 );
	}

	void ISpriteBatch.Start(
		IRenderTarget renderTarget,
		ITexture texture,
		BlendMode blendMode,
		Rectangle? clip
	) {
		if( _isBatching ) {
			return;
		}
		_isBatching = true;
		_bufferedSprites = 0;

		// No sync needed here: FlushBatch() always leaves the current segment
		// waited-on and ready to write (see the sync-on-advance there), and a
		// freshly constructed batch has no pending fences.
		_shader.Bind( renderTarget );
		texture.Bind( TEXTURE_UNIT_0 );
		_texture = texture;
		_renderTarget = renderTarget;
		_blendMode = blendMode;
		_gl.BindVertexArray( _vao );

		// The clip is batch-scoped. Always set it explicitly (or clear it) so the
		// batch starts in a deterministic state rather than inheriting whatever
		// scissor a previous batch left on the target.
		if( clip.HasValue ) {
			renderTarget.SetClip( clip.Value );
		} else {
			renderTarget.ClearClip();
		}
		_clip = clip;

		switch( blendMode ) {
			case BlendMode.None:
				_gl.Disable( EnableCap.Blend );
				break;
			case BlendMode.Premultiplied:
				_gl.Enable( EnableCap.Blend );
				_gl.BlendFunc( BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha );
				break;
			case BlendMode.Additive:
				_gl.Enable( EnableCap.Blend );
				_gl.BlendFunc( BlendingFactor.One, BlendingFactor.One );
				break;
		}
	}

	// Reserves the slot for the next sprite in the current segment and returns a
	// pointer to its first vertex. Guards capacity *before* writing: if the current
	// segment is full it flushes, which rotates to a fresh, already-synced segment.
	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	private SpriteVertex* ReserveSprite() {
		if( !_isBatching ) {
			throw new InvalidOperationException( "Cannot render before calling Start." );
		}

		// A segment holds at most _maxSpritesPerFrame sprites; flush before the
		// write that would overflow it so a slot is always valid.
		if( _bufferedSprites >= _maxSpritesPerFrame ) {
			FlushBatch();
		}

		uint segmentOffsetBytes = (uint)( _currentSegment * _frameMaxBytes );
		uint spriteOffsetBytes = (uint)( _bufferedSprites * VERTICES_PER_SPRITE * STRIDE );
		return (SpriteVertex*)( (byte*)_mappedVboPtr + segmentOffsetBytes + spriteOffsetBytes );
	}

	public void Draw(
		float x,
		float y,
		float width,
		float height,
		float u1,
		float v1,
		float u2,
		float v2,
		uint colour
	) {
		// Reserve a slot (guards capacity and rotates segments if needed) and copy
		// the sprite vertex data directly into the persistent mapping.
		SpriteVertex* basePtr = ReserveSprite();

		// Top-Left
		basePtr[0].X = x;
		basePtr[0].Y = y;
		basePtr[0].U = u1;
		basePtr[0].V = v1;
		basePtr[0].Color = colour;

		// Bottom-Left
		basePtr[1].X = x;
		basePtr[1].Y = y + height;
		basePtr[1].U = u1;
		basePtr[1].V = v2;
		basePtr[1].Color = colour;

		// Bottom-Right
		basePtr[2].X = x + width;
		basePtr[2].Y = y + height;
		basePtr[2].U = u2;
		basePtr[2].V = v2;
		basePtr[2].Color = colour;

		// Top-Right
		basePtr[3].X = x + width;
		basePtr[3].Y = y;
		basePtr[3].U = u2;
		basePtr[3].V = v1;
		basePtr[3].Color = colour;

		_bufferedSprites++;
	}

	public void Draw(
		float x,
		float y,
		float width,
		float height,
		float u1,
		float v1,
		float u2,
		float v2,
		float rotation,
		float originX,
		float originY,
		uint colour
	) {
		SpriteVertex* basePtr = ReserveSprite();

		// Pivot in world space, and the corner offsets relative to it (local space).
		float pivotX = x + originX;
		float pivotY = y + originY;

		float left = -originX;
		float right = width - originX;
		float top = -originY;
		float bottom = height - originY;

		float cos = MathF.Cos( rotation );
		float sin = MathF.Sin( rotation );

		// Top-Left
		basePtr[0].X = pivotX + ( ( left * cos ) - ( top * sin ) );
		basePtr[0].Y = pivotY + ( ( left * sin ) + ( top * cos ) );
		basePtr[0].U = u1;
		basePtr[0].V = v1;
		basePtr[0].Color = colour;

		// Bottom-Left
		basePtr[1].X = pivotX + ( ( left * cos ) - ( bottom * sin ) );
		basePtr[1].Y = pivotY + ( ( left * sin ) + ( bottom * cos ) );
		basePtr[1].U = u1;
		basePtr[1].V = v2;
		basePtr[1].Color = colour;

		// Bottom-Right
		basePtr[2].X = pivotX + ( ( right * cos ) - ( bottom * sin ) );
		basePtr[2].Y = pivotY + ( ( right * sin ) + ( bottom * cos ) );
		basePtr[2].U = u2;
		basePtr[2].V = v2;
		basePtr[2].Color = colour;

		// Top-Right
		basePtr[3].X = pivotX + ( ( right * cos ) - ( top * sin ) );
		basePtr[3].Y = pivotY + ( ( right * sin ) + ( top * cos ) );
		basePtr[3].U = u2;
		basePtr[3].V = v1;
		basePtr[3].Color = colour;

		_bufferedSprites++;
	}

	void ISpriteBatch.Finish() {
		if( !_isBatching ) {
			throw new InvalidOperationException( "Cannot call Finish without a matching call to Start." );
		}

		if( _bufferedSprites > 0 ) {
			FlushBatch();
		}

		_gl.BindVertexArray( 0 );
		_isBatching = false;
		_clip = null;
		_texture = null;
		_renderTarget = null;
	}

	bool ISpriteBatch.Ensure(
		IRenderTarget renderTarget,
		ITexture texture,
		BlendMode blendMode,
		Rectangle? clip
	) {
		ISpriteBatch self = this;
		if( _isBatching
			&& ReferenceEquals( _renderTarget, renderTarget )
			&& _texture is not null && _texture.Id == texture.Id
			&& _blendMode == blendMode
			&& _clip == clip ) {
			return false;
		}

		if( _isBatching ) {
			self.Finish();
		}
		self.Start( renderTarget, texture, blendMode, clip );
		return true;
	}

	private void FlushPending() {
		if( _bufferedSprites > 0 ) {
			FlushBatch();
		}
	}

	private void FlushBatch() {
		uint segmentOffsetBytes = (uint)( _currentSegment * _frameMaxBytes );
		uint totalVertices = (uint)( _bufferedSprites * VERTICES_PER_SPRITE );
		uint dataSizeInBytes = totalVertices * STRIDE;

		_gl.BindBuffer( BufferTargetARB.ArrayBuffer, _vbo );
		_gl.FlushMappedBufferRange( GLEnum.ArrayBuffer, (nint)segmentOffsetBytes, (nuint)dataSizeInBytes );

		_gl.BindBuffer( BufferTargetARB.ElementArrayBuffer, _ibo );

		_gl.MemoryBarrier( MemoryBarrierMask.ClientMappedBufferBarrierBit );

		int baseVertex = (int)( segmentOffsetBytes / STRIDE );
		uint indexCount = (uint)( _bufferedSprites * 6 );

		_gl.DrawElementsBaseVertex(
			PrimitiveType.Triangles,
			indexCount,
			DrawElementsType.UnsignedShort,
			null,
			baseVertex
		);

		if( _fences[_currentSegment] != 0 ) {
			_gl.DeleteSync( _fences[_currentSegment] );
			_fences[_currentSegment] = 0;
		}

		_fences[_currentSegment] = _gl.FenceSync( SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None );

		_bufferedSprites = 0;
		_currentSegment = ( _currentSegment + 1 ) % _numSegments;

		// Sync-on-advance: the segment we just rotated to may still be read by an
		// in-flight draw submitted NUM_SEGMENTS flushes ago. Wait on its fence now so
		// every subsequent write (including mid-batch overflow flushes) is safe. This
		// is the single point that keeps the current segment always ready to write.
		SyncCurrentSegment();
	}

	private void SyncCurrentSegment() {
		if( _fences[_currentSegment] == 0 ) {
			return;
		}

		while( true ) {
			GLEnum waitReturn = _gl.ClientWaitSync( _fences[_currentSegment], SyncObjectMask.Bit, 1_000_000_000 );
			if( waitReturn == GLEnum.AlreadySignaled || waitReturn == GLEnum.ConditionSatisfied ) {
				_gl.DeleteSync( _fences[_currentSegment] );
				_fences[_currentSegment] = 0;
				break;
			}
			if( waitReturn == GLEnum.WaitFailed ) {
				throw new InvalidOperationException( "OpenGL fence sync wait failed critically." );
			}
		}
	}

	void IDisposable.Dispose() {
		if( _isDisposed ) {
			return;
		}

		_shader.Dispose();

		if( _mappedVboPtr != null ) {
			_gl.BindBuffer( BufferTargetARB.ArrayBuffer, _vbo );
			_gl.UnmapBuffer( GLEnum.ArrayBuffer );
			_mappedVboPtr = null;
		}

		_gl.DeleteBuffer( _vbo );
		_gl.DeleteBuffer( _ibo );
		_gl.DeleteVertexArray( _vao );

		for( int i = 0; i < _numSegments; i++ ) {
			if( _fences[i] != 0 ) {
				_gl.DeleteSync( _fences[i] );
				_fences[i] = 0;
			}
		}

		_isDisposed = true;
		GC.SuppressFinalize( this );
	}

	~SpriteBatchPMO() {
		if( !_isDisposed ) {
			System.Diagnostics.Debug.Fail( "SpriteBatchPMO leaked. Dispose it on the render thread." );
		}

	}
}
