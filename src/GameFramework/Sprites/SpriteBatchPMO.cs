using System.Buffers.Binary;
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
	private readonly GlStateCache _stateCache;
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
	private readonly Stack<Bounds> _clipStack = new Stack<Bounds>();
	private int _clipDepthAtStart;
	private Bounds? _appliedClip;
	private IRenderTarget? _renderTarget;
	private long _flushCount;

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
		_stateCache = stateCache;
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
		IRenderTarget renderTarget
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
		_renderTarget = renderTarget;
		_gl.BindVertexArray( _vao );

		// Texture and blend state are owned by GlStateCache, which shadows the real GL
		// state across batches. The first Draw programs each through the cache (see
		// BindTexture / ApplyBlendMode); a redundant program is skipped when the cache
		// already reflects the requested state.

		// Adopt whatever clip is currently on top of the stack (which may have been
		// pushed before Start) so the batch begins in a deterministic scissor state
		// rather than inheriting whatever a previous batch left on the target. Record
		// the current depth so Finish can verify the stack was balanced.
		_clipDepthAtStart = _clipStack.Count;
		if( _clipStack.Count > 0 ) {
			Bounds clip = _clipStack.Peek();
			renderTarget.SetClip( clip );
			_appliedClip = clip;
		} else {
			renderTarget.ClearClip();
			_appliedClip = null;
		}
	}

	// Ensures the given blend mode is the one programmed for subsequent draws. The
	// mode maps to concrete GL blend factors that are compared against GlStateCache's
	// shadowed state; when they differ, any pending sprites are flushed (they belong
	// to the old blend state) before the new state is programmed through the cache.
	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	private void ApplyBlendMode(
		BlendMode blendMode
	) {
		bool enabled;
		BlendingFactor src;
		BlendingFactor dst;
		switch( blendMode ) {
			case BlendMode.None:
				enabled = false;
				src = BlendingFactor.One;
				dst = BlendingFactor.Zero;
				break;
			case BlendMode.Additive:
				enabled = true;
				src = BlendingFactor.One;
				dst = BlendingFactor.One;
				break;
			case BlendMode.Premultiplied:
			default:
				enabled = true;
				src = BlendingFactor.One;
				dst = BlendingFactor.OneMinusSrcAlpha;
				break;
		}

		if( _stateCache.IsBlend( enabled, src, dst ) ) {
			return;
		}

		if( _bufferedSprites > 0 ) {
			FlushBatch();
		}

		_stateCache.SetBlend( enabled, src, dst );
	}

	// Ensures the given texture is the one bound for subsequent draws. The bound
	// texture is compared against GlStateCache's shadowed state; when it differs, any
	// pending sprites are flushed (they belong to the old texture) before the new one
	// is bound through the cache.
	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	private void BindTexture(
		uint textureId
	) {
		if( _stateCache.IsTextureBound( TEXTURE_UNIT_0, textureId ) ) {
			return;
		}

		if( _bufferedSprites > 0 ) {
			FlushBatch();
		}

		_stateCache.BindTexture( TEXTURE_UNIT_0, textureId );
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
		uint textureId,
		float x,
		float y,
		float width,
		float height,
		float u1,
		float v1,
		float u2,
		float v2,
		uint colour = 0xFFFFFFFF,
		BlendMode blendMode = BlendMode.Premultiplied
	) {
		// Program blend state first; if it differs from the currently applied mode
		// this flushes the pending sprites and reprograms GL blend state.
		ApplyBlendMode( blendMode );

		// Switch to the requested texture first; if it differs from the currently
		// bound one this flushes the pending sprites and rebinds.
		BindTexture( textureId );

		// Colours are authored as 0xRRGGBBAA, but the vertex attribute reads the four
		// bytes in memory order. On a little-endian host that order is reversed, so
		// byte-swap to [RR,GG,BB,AA] so the shader sees true (R,G,B,A).
		colour = BinaryPrimitives.ReverseEndianness( colour );

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
		uint textureId,
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
		uint colour = 0xFFFFFFFF,
		BlendMode blendMode = BlendMode.Premultiplied
	) {
		ApplyBlendMode( blendMode );

		BindTexture( textureId );

		// See the non-rotated Draw: byte-swap the 0xRRGGBBAA colour so the little-endian
		// vertex attribute presents true (R,G,B,A) to the shader.
		colour = BinaryPrimitives.ReverseEndianness( colour );

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

		// The clip stack must be balanced: every ReplaceClip issued after Start must
		// have a matching RestoreClip, leaving the stack at the depth it had when
		// Start was called. A mismatch means a clip scope was leaked.
		if( _clipStack.Count != _clipDepthAtStart ) {
			throw new InvalidOperationException(
				$"Clip stack is unbalanced: expected depth {_clipDepthAtStart} at Finish but found {_clipStack.Count}. Every ReplaceClip after Start must have a matching RestoreClip."
			);
		}

		if( _bufferedSprites > 0 ) {
			FlushBatch();
		}

		_gl.BindVertexArray( 0 );
		_isBatching = false;
		_renderTarget = null;
	}

	void ISpriteBatch.ReplaceClip(
		Bounds clip
	) {
		_clipStack.Push( clip );
		ApplyCurrentClip();
	}

	void ISpriteBatch.RestoreClip() {
		// While a batch is running, RestoreClip must not pop below the depth the
		// stack had at Start: doing so would mutate a clip the caller seeded before
		// Start (or that a parent scope owns) rather than one this batch pushed.
		if( _isBatching && _clipStack.Count <= _clipDepthAtStart ) {
			throw new InvalidOperationException(
				$"RestoreClip has no matching ReplaceClip in this batch: stack depth {_clipStack.Count} is at or below the depth {_clipDepthAtStart} recorded at Start."
			);
		}

		if( _clipStack.Count == 0 ) {
			return;
		}

		_clipStack.Pop();
		ApplyCurrentClip();
	}

	// Applies the clip currently on top of the stack (or clears the scissor when the
	// stack is empty) to the render target. Only touches GL state -- and flushes the
	// pending sprites -- while a batch is running; before Start it simply tracks the
	// stack so the eventual Start can adopt the right clip.
	private void ApplyCurrentClip() {
		if( !_isBatching ) {
			return;
		}

		Bounds? desired = _clipStack.Count > 0 ? _clipStack.Peek() : null;

		// Nothing to do if the effective scissor is unchanged -- avoids flushing a
		// batch when a duplicate clip is pushed or popped.
		if( desired == _appliedClip ) {
			return;
		}

		// A scissor change affects subsequent draws, so any sprites already buffered
		// under the previous clip must be flushed first.
		if( _bufferedSprites > 0 ) {
			FlushBatch();
		}

		if( desired.HasValue ) {
			_renderTarget!.SetClip( desired.Value );
		} else {
			_renderTarget!.ClearClip();
		}
		_appliedClip = desired;
	}

	long ISpriteBatch.FlushCount => _flushCount;

	private void FlushPending() {
		if( _bufferedSprites > 0 ) {
			FlushBatch();
		}
	}

	private void FlushBatch() {
		_flushCount++;
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
