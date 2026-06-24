using System.Drawing;
using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Game.Framework;

internal sealed class Display : IDisplay {

	private readonly IWindow _window;
	private readonly GL _gl;
	private uint _width;
	private uint _height;
	private Matrix4x4 _projection;

	public Display(
		IWindow window,
		GL gl,
		int width,
		int height
	) {
		_gl = gl;
		_window = window;
		_window.FramebufferResize += FramebufferResize;

		SetSize( (uint)width, (uint)height );
	}

	void IRenderTarget.Clear(
		Color colour
	) {
		DoBind();
		_gl.ClearColor( colour );
		_gl.Clear( ClearBufferMask.ColorBufferBit );
	}

	void IRenderTarget.Bind() {
		DoBind();
	}

	Matrix4x4 IRenderTarget.Projection => _projection;

	uint IRenderTarget.Width => _width;

	uint IRenderTarget.Height => _height;

	private void DoBind() {
		_gl.BindFramebuffer( FramebufferTarget.Framebuffer, 0 );
		_gl.Viewport( 0, 0, _width, _height );
	}

	private void FramebufferResize(
		Vector2D<int> size
	) {
		SetSize( (uint)size.X, (uint)size.Y );
	}

	private void SetSize(
		uint width,
		uint height
	) {
		_width = width;
		_height = height;
		_gl.Viewport( 0, 0, _width, _height );
		_projection = Matrix4x4.CreateOrthographicOffCenter(
			left: 0.0f,
			right: width,
			bottom: height,
			top: 0.0f,
			zNearPlane: -1.0f,
			zFarPlane: 1.0f
		);
	}

	void IDisposable.Dispose() {
	}
}
