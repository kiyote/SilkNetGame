using System.Drawing;
using System.Numerics;
using Silk.NET.OpenGL;

namespace Game.Framework;

public sealed class Framebuffer : RenderTarget, IDisposable {
	private readonly GL _gl;
	private readonly Matrix4x4 _projection;
	private readonly uint _width;
	private readonly uint _height;
	private readonly uint _framebuffer;

	private bool _isDisposed = false;

	public Framebuffer(
		GL gl,
		uint width,
		uint height
	) {
		_gl = gl;
		_width = width;
		_height = height;

		Texture = new Texture( gl, width, height );

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

	public Texture Texture { get; }

	public void Clear(
		Color colour
	) {
		Bind();
		_gl.ClearColor( colour );
		_gl.Clear( ClearBufferMask.ColorBufferBit );
	}

	internal override Matrix4x4 Projection => _projection;

	internal override uint Width => _width;

	internal override uint Height => _height;

	internal override void Bind() {
		_gl.BindFramebuffer( FramebufferTarget.Framebuffer, _framebuffer );
		_gl.Viewport( 0, 0, _width, _height );
	}

	public void Dispose() {
		if( _isDisposed ) {
			return;
		}

		Texture.Dispose();

		_gl.DeleteFramebuffer( _framebuffer );

		_isDisposed = true;
		GC.SuppressFinalize( this );
	}

	~Framebuffer() {
		if( !_isDisposed ) {
			System.Diagnostics.Debug.Fail( "Framebuffer leaked. Dispose it on the render thread." );
		}
	}
}
