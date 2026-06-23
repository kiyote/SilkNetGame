using System.Runtime.CompilerServices;
using Game.Framework.Textures;
using Silk.NET.OpenGL;

namespace Game.Framework.Sprites;

internal unsafe sealed class SpriteBatchPMO : ISpriteBatch {
	private const int TEXTURE_UNIT_0 = 0;

	private const int POSITION_OFFSET = 0;
	private const int TEXTURE_OFFSET = 8;
	private const int COLOUR_OFFSET = 16;
	private const int VERTICES_PER_SPRITE = 4;
	private const int MAX_SPRITES_PER_FRAME = 10_000;
	private const int NUM_SEGMENTS = 3; // Triple Buffering
	private static readonly uint STRIDE = (uint)Unsafe.SizeOf<SpriteVertex>();
	private static readonly uint FRAME_MAX_BYTES = ( MAX_SPRITES_PER_FRAME * VERTICES_PER_SPRITE * STRIDE );
	private static readonly uint TOTAL_BUFFER_BYTES = FRAME_MAX_BYTES * NUM_SEGMENTS;

	private readonly GL _gl;
	private readonly SpriteBatchShader _shader;

	private uint _vao;
	private uint _vbo;
	private uint _ibo;

	private void* _mappedVboPtr = null;
	private readonly nint[] _fences = new nint[NUM_SEGMENTS];
	private int _currentSegment = 0;

	private int _bufferedSprites = 0;
	private bool _isBatching = false;
	private bool _isDisposed = false;

	public SpriteBatchPMO(
		GL gl
	) {
		_gl = gl;
		_shader = new SpriteBatchShader( gl );
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

		_gl.BufferStorage( GLEnum.ArrayBuffer, TOTAL_BUFFER_BYTES, null, storageFlags );

		MapBufferAccessMask mapFlags =
			MapBufferAccessMask.WriteBit
			| MapBufferAccessMask.PersistentBit
			| MapBufferAccessMask.FlushExplicitBit;

		_mappedVboPtr = _gl.MapBufferRange( GLEnum.ArrayBuffer, 0, TOTAL_BUFFER_BYTES, mapFlags );

		if( _mappedVboPtr == null ) {
			throw new Exception( $"OpenGL failed to map the buffer! GL Error: {_gl.GetError()}" );
		}

		_gl.EnableVertexAttribArray( 0 );
		_gl.VertexAttribPointer( 0, 2, VertexAttribPointerType.Float, false, STRIDE, (void*)POSITION_OFFSET );

		_gl.EnableVertexAttribArray( 1 );
		_gl.VertexAttribPointer( 1, 2, VertexAttribPointerType.Float, false, STRIDE, (void*)TEXTURE_OFFSET );

		_gl.EnableVertexAttribArray( 2 );
		_gl.VertexAttribPointer( 2, 4, VertexAttribPointerType.UnsignedByte, true, STRIDE, (void*)COLOUR_OFFSET );

		ushort[] indices = new ushort[MAX_SPRITES_PER_FRAME * 6];
		for( ushort i = 0; i < MAX_SPRITES_PER_FRAME; i++ ) {
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

		_gl.BindVertexArray( 0 );
	}

	void ISpriteBatch.Begin(
		IRenderTarget renderTarget,
		ITexture texture
	) {
		if( _isBatching ) {
			return;
		}
		_isBatching = true;
		_bufferedSprites = 0;

		SyncCurrentSegment();

		_shader.Bind( renderTarget, TEXTURE_UNIT_0 );
		texture.Bind( TEXTURE_UNIT_0 );
		_gl.BindVertexArray( _vao );

		_gl.Enable( EnableCap.Blend );
		_gl.BlendFunc( BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha );
	}

	void ISpriteBatch.Draw(
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
		if( !_isBatching ) {
			throw new InvalidOperationException( "Cannot render before calling Begin." );
		}

		uint segmentOffsetBytes = (uint)( _currentSegment * FRAME_MAX_BYTES );
		uint spriteOffsetBytes = (uint)( _bufferedSprites * VERTICES_PER_SPRITE * STRIDE );

		// Get a direct reference to the active layout offset inside the persistent mapping
		SpriteVertex* basePtr = (SpriteVertex*)( (byte*)_mappedVboPtr + segmentOffsetBytes + spriteOffsetBytes );

		// Copy the sprite vertex data directly into the mapped buffer region for the current segment.

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

		if( _bufferedSprites >= MAX_SPRITES_PER_FRAME ) {
			FlushBatch();
		}
	}

	void ISpriteBatch.End() {
		if( !_isBatching ) {
			throw new InvalidOperationException( "Cannot call End without a matching call to Begin." );
		}

		if( _bufferedSprites > 0 ) {
			FlushBatch();
		}

		_gl.BindVertexArray( 0 );
		_isBatching = false;
	}

	private void FlushBatch() {
		uint segmentOffsetBytes = (uint)( _currentSegment * FRAME_MAX_BYTES );
		uint totalVertices = (uint)( _bufferedSprites * VERTICES_PER_SPRITE );
		uint dataSizeInBytes = totalVertices * STRIDE;

		_gl.BindBuffer( BufferTargetARB.ArrayBuffer, _vbo );
		_gl.FlushMappedBufferRange( GLEnum.ArrayBuffer, (nint)segmentOffsetBytes, (nuint)dataSizeInBytes );

		_gl.MemoryBarrier( MemoryBarrierMask.ClientMappedBufferBarrierBit );

		_gl.BindBuffer( BufferTargetARB.ElementArrayBuffer, _ibo );

		int baseVertex = (int)( segmentOffsetBytes / STRIDE );
		uint indexCount = (uint)( _bufferedSprites * 6 );

		_gl.DrawElementsBaseVertex(
			PrimitiveType.Triangles,
			indexCount,
			DrawElementsType.UnsignedShort,
			(void*)0,
			baseVertex
		);

		if( _fences[_currentSegment] != 0 ) {
			_gl.DeleteSync( _fences[_currentSegment] );
			_fences[_currentSegment] = 0;
		}

		_fences[_currentSegment] = _gl.FenceSync( SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None );

		_bufferedSprites = 0;
		_currentSegment = ( _currentSegment + 1 ) % NUM_SEGMENTS;
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
				throw new Exception( "OpenGL fence sync wait failed critically." );
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

		for( int i = 0; i < NUM_SEGMENTS; i++ ) {
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
