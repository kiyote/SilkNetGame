using System.Drawing;
using System.Numerics;
using GameFramework.Textures;
using Silk.NET.OpenGL;

namespace GameFramework;

internal sealed class Framebuffer : IFramebuffer {
	private readonly GL _gl;
	private readonly Matrix4x4 _projection;
	private readonly uint _width;
	private readonly uint _height;
	private readonly uint _framebuffer;
	private readonly ITexture _texture;

	private bool _isDisposed;

	public Framebuffer(
		GL gl,
		uint width,
		uint height
	) {
		_gl = gl;
		_width = width;
		_height = height;

		_texture = new Textures.Texture( gl, width, height );

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

	Matrix4x4 IRenderTarget.Projection => _projection;

	uint IRenderTarget.Width => _width;

	uint IRenderTarget.Height => _height;

	void IRenderTarget.Bind() {
		DoBind();
	}

	uint ITexture.TextureWidth => _texture.TextureWidth;

	uint ITexture.TextureHeight => _texture.TextureHeight;

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
		_gl.Viewport( 0, 0, _width, _height );
	}

	~Framebuffer() {
		if( !_isDisposed ) {
			System.Diagnostics.Debug.Fail( "Framebuffer leaked. Dispose it on the render thread." );
		}
	}
}
