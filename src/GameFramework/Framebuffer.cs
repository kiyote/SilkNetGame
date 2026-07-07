using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using GameFramework.Textures;
using Silk.NET.OpenGL;

namespace GameFramework;

internal sealed class Framebuffer : IFramebuffer {
	private readonly GL _gl;
	private readonly Matrix4x4 _projection;
	private readonly int _width;
	private readonly int _height;
	private readonly uint _framebuffer;
	private readonly ITexture _texture;

	private bool _isDisposed;
	private Rectangle? _clip;

	public Framebuffer(
		GL gl,
		int width,
		int height,
		TextureFilter filter
	) {
		_gl = gl;
		_width = width;
		_height = height;

		_texture = new Textures.Texture( gl, width, height, filter );

		_framebuffer = _gl.GenFramebuffer();
		_gl.BindFramebuffer( FramebufferTarget.Framebuffer, _framebuffer );

		_gl.FramebufferTexture2D(
			FramebufferTarget.Framebuffer,
			FramebufferAttachment.ColorAttachment0,
			TextureTarget.Texture2D,
			Texture.Id,
			0
		);

		GLEnum status = _gl.CheckFramebufferStatus( FramebufferTarget.Framebuffer );
		if( status != GLEnum.FramebufferComplete ) {
			_gl.BindFramebuffer( FramebufferTarget.Framebuffer, 0 );
			Texture.Dispose();
			_gl.DeleteFramebuffer( _framebuffer );
			_framebuffer = 0;
			throw new InvalidOperationException( $"Framebuffer is incomplete: {status}" );
		}

		_gl.BindFramebuffer( FramebufferTarget.Framebuffer, 0 );

		_projection = Matrix4x4.CreateOrthographicOffCenter(
			left: 0.0f,
			right: width,
			bottom: 0.0f,
			top: height,
			zNearPlane: -1.0f,
			zFarPlane: 1.0f
		);
	}

	public ITexture Texture => _texture;

	void IRenderTarget.Clear(
		Color colour
	) {
		DoBind();
		_gl.ClearColor( colour );
		_gl.Clear( ClearBufferMask.ColorBufferBit );
	}

	void IRenderTarget.Clear(
		uint colour
	) {
		DoBind();
		(float R, float G, float B, float A) = DecomposeRgba( colour );
		_gl.ClearColor( R, G, B, A );
		_gl.Clear( ClearBufferMask.ColorBufferBit );
	}

	Matrix4x4 IRenderTarget.Projection => _projection;

	int IRenderTarget.Width => _width;

	int IRenderTarget.Height => _height;

	void IRenderTarget.Bind() {
		DoBind();
	}

	void IRenderTarget.SetClip(
		Rectangle clip
	) {
		_clip = clip;
		if( IsBound() ) {
			ApplyClip();
		}
	}

	void IRenderTarget.SetClip(
		int x,
		int y,
		int w,
		int h
	) {
		if( _clip is null
			|| _clip.Value.X != x
			|| _clip.Value.Y != y
			|| _clip.Value.Width != w
			|| _clip.Value.Height != h
		) {
			_clip = new Rectangle( x, y, w, h );
		}
		if( IsBound() ) {
			ApplyClip();
		}
	}

	void IRenderTarget.ClearClip() {
		_clip = null;
		if( IsBound() ) {
			ApplyClip();
		}
	}

	int ITexture.TextureWidth => _texture.TextureWidth;

	int ITexture.TextureHeight => _texture.TextureHeight;

	uint ITexture.Id => _texture.Id;

	void ITexture.Bind( int textureUnit ) {
		_texture.Bind( textureUnit );
	}

	void IDisposable.Dispose() {
		if( _isDisposed ) {
			return;
		}

		_texture.Dispose();

		_gl.DeleteFramebuffer( _framebuffer );

		_isDisposed = true;
		GC.SuppressFinalize( this );
	}

	private void DoBind() {
		_gl.BindFramebuffer( FramebufferTarget.Framebuffer, _framebuffer );
		_gl.Viewport( 0, 0, (uint)_width, (uint)_height );
		ApplyClip();
	}

	private void ApplyClip() {
		if( _clip is Rectangle clip ) {
			// The framebuffer projection already has its origin at the
			// bottom-left, matching the OpenGL scissor box, so no Y flip.
			_gl.Enable( EnableCap.ScissorTest );
			_gl.Scissor( clip.X, clip.Y, (uint)clip.Width, (uint)clip.Height );
		} else {
			_gl.Disable( EnableCap.ScissorTest );
		}
	}

	private bool IsBound() {
		_gl.GetInteger( GLEnum.FramebufferBinding, out int bound );
		return (uint)bound == _framebuffer;
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

	~Framebuffer() {
		if( !_isDisposed ) {
			System.Diagnostics.Debug.Fail( "Framebuffer leaked. Dispose it on the render thread." );
		}
	}
}
