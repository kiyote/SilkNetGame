using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using GameFramework.Textures;
using Silk.NET.OpenGL;

namespace GameFramework;

internal sealed class RenderTexture : IRenderTexture {
	private readonly GL _gl;
	private readonly GlStateCache _stateCache;
	private readonly Matrix4x4 _projection;
	private readonly Dimension _size;
	private readonly uint _framebuffer;
	private readonly ITexture _texture;

	private bool _isDisposed;
	private Rectangle? _clip;

	public RenderTexture(
		GL gl,
		GlStateCache stateCache,
		Dimension size,
		TextureFilter filter
	) {
		ArgumentOutOfRangeException.ThrowIfLessThan( size.Width, 1 );
		ArgumentOutOfRangeException.ThrowIfLessThan( size.Height, 1 );

		_gl = gl;
		_stateCache = stateCache;
		_size = size;

		_texture = new Textures.Texture( gl, stateCache, size, filter );

		_framebuffer = _gl.GenFramebuffer();
		_stateCache.BindFramebuffer( _framebuffer );

		_gl.FramebufferTexture2D(
			FramebufferTarget.Framebuffer,
			FramebufferAttachment.ColorAttachment0,
			TextureTarget.Texture2D,
			Texture.Id,
			0
		);

		GLEnum status = _gl.CheckFramebufferStatus( FramebufferTarget.Framebuffer );
		if( status != GLEnum.FramebufferComplete ) {
			_stateCache.BindFramebuffer( 0 );
			Texture.Dispose();
			_gl.DeleteFramebuffer( _framebuffer );
			_framebuffer = 0;
			throw new InvalidOperationException( $"Framebuffer is incomplete: {status}" );
		}

		_stateCache.BindFramebuffer( 0 );

		_projection = Matrix4x4.CreateOrthographicOffCenter(
			left: 0.0f,
			right: size.Width,
			bottom: 0.0f,
			top: size.Height,
			zNearPlane: -1.0f,
			zFarPlane: 1.0f
		);
	}

	public ITexture Texture => _texture;

	void IRenderTarget.Clear(
		Color colour
	) {
		ThrowIfDisposed();
		DoBind();
		_gl.ClearColor( colour );
		_gl.Clear( ClearBufferMask.ColorBufferBit );
	}

	void IRenderTarget.Clear(
		uint colour
	) {
		ThrowIfDisposed();
		DoBind();
		(float R, float G, float B, float A) = DecomposeRgba( colour );
		_gl.ClearColor( R, G, B, A );
		_gl.Clear( ClearBufferMask.ColorBufferBit );
	}

	Matrix4x4 IRenderTarget.Projection => _projection;

	Dimension IRenderTarget.Size => _size;

	void IRenderTarget.Bind() {
		ThrowIfDisposed();
		DoBind();
	}

	void IRenderTarget.SetClip(
		Rectangle clip
	) {
		ThrowIfDisposed();
		_clip = clip;
		if( IsBound() ) {
			ApplyClip();
		}
	}

	void IRenderTarget.SetClip(
		Coordinate position,
		Dimension size
	) {
		ThrowIfDisposed();
		if( _clip is null
			|| _clip.Value.X != position.X
			|| _clip.Value.Y != position.Y
			|| _clip.Value.Width != size.Width
			|| _clip.Value.Height != size.Height
		) {
			_clip = new Rectangle( position.X, position.Y, size.Width, size.Height );
		}
		if( IsBound() ) {
			ApplyClip();
		}
	}

	void IRenderTarget.ClearClip() {
		ThrowIfDisposed();
		_clip = null;
		if( IsBound() ) {
			ApplyClip();
		}
	}

	Rectangle? IRenderTarget.Clip => _clip;

	Dimension ITexture.TextureSize => _texture.TextureSize;

	uint ITexture.Id => _texture.Id;

	float ITexture.HalfX => _texture.HalfX;

	float ITexture.HalfY => _texture.HalfY;

	TextureFilter ITexture.Filter => _texture.Filter;

	void ITexture.Clear(
		Coordinate position,
		Dimension size,
		uint colour
	) {
		ThrowIfDisposed();
		_texture.Clear( position, size, colour );
	}

	string ISubTexture.Name => _texture.Name;

	Coordinate ISubTexture.Position => default;

	Dimension ISubTexture.Size => _size;

	Coordinate ISubTexture.StoredPosition => default;

	Dimension ISubTexture.StoredSize => _size;

	ITexture ISubTexture.Texture => this;

	float ISubTexture.U1 => 0.0f;

	float ISubTexture.V1 => 0.0f;

	float ISubTexture.U2 => 1.0f;

	float ISubTexture.V2 => 1.0f;

	ITextureAtlas ITexture.CreateAtlas() {
		ThrowIfDisposed();
		return new TextureAtlas( this );
	}

	ISubTexture ITexture.CreateSubTexture(
		string name,
		Coordinate position,
		Dimension size
	) {
		ThrowIfDisposed();
		return new SubTexture( name, this, position, size, position, size );
	}

	void ITexture.Bind( int textureUnit ) {
		ThrowIfDisposed();
		_texture.Bind( textureUnit );
	}

	void ISubTexture.Update(
		Coordinate position,
		Dimension size
	) {
	}

	bool IEquatable<ISubTexture>.Equals(
		ISubTexture? other
	) {
		if( other is null ) {
			return false;
		}
		return other.Name == _texture.Name
			&& other.Position == default
			&& other.Size == _size;
	}

	void IDisposable.Dispose() {
		if( _isDisposed ) {
			return;
		}

		_gl.DeleteFramebuffer( _framebuffer );
		_texture.Dispose();

		_isDisposed = true;
		GC.SuppressFinalize( this );
	}

	private void DoBind() {
		_stateCache.BindFramebuffer( _framebuffer );
		_gl.Viewport( 0, 0, (uint)_size.Width, (uint)_size.Height );
		ApplyClip();
	}

	private void ApplyClip() {
		if( _clip is Rectangle clip ) {
			// The framebuffer projection already has its origin at the
			// bottom-left, matching the OpenGL scissor box, so no Y flip.
			_stateCache.SetScissor( true, clip.X, clip.Y, (uint)clip.Width, (uint)clip.Height );
		} else {
			_stateCache.SetScissor( false, 0, 0, 0, 0 );
		}
	}

	private bool IsBound() {
		return _stateCache.IsFramebufferBound( _framebuffer );
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	private static (float R, float G, float B, float A) DecomposeRgba( uint rgba ) {
		const float ToFloat = 1.0f / 255.0f;
		return (
			( ( rgba >> 24 ) & 0xFF ) * ToFloat,
			( ( rgba >> 16 ) & 0xFF ) * ToFloat,
			( ( rgba >> 8 ) & 0xFF ) * ToFloat,
			( rgba & 0xFF ) * ToFloat
		);
	}

	public void Copy(
		Coordinate position,
		ITexture source,
		Coordinate sourcePosition,
		Dimension sourceSize
	) {
		ThrowIfDisposed();
		_texture.Copy( position, source, sourcePosition, sourceSize );
	}

	private void ThrowIfDisposed() {
		ObjectDisposedException.ThrowIf( _isDisposed, this );
	}

	~RenderTexture() {
		if( !_isDisposed ) {
			System.Diagnostics.Debug.Fail( "Framebuffer leaked. Dispose it on the render thread." );
		}
	}
}
