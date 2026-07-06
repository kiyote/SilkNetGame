using System.Drawing;
using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Rectangle = System.Drawing.Rectangle;

namespace GameFramework;

internal sealed class Display : IDisplay {

	private readonly IWindow _window;
	private readonly GL _gl;
	private int _width;
	private int _height;
	private Matrix4x4 _projection;
	private Rectangle? _clip;

	public Display(
		IWindow window,
		GL gl,
		int width,
		int height
	) {
		_gl = gl;
		_window = window;
		_window.FramebufferResize += FramebufferResize;

		SetSize( width, height );
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
		if (_clip is null
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

	Matrix4x4 IRenderTarget.Projection => _projection;

	int IRenderTarget.Width => _width;

	int IRenderTarget.Height => _height;

	private void DoBind() {
		_gl.BindFramebuffer( FramebufferTarget.Framebuffer, 0 );
		_gl.Viewport( 0, 0, (uint)_width, (uint)_height );
		ApplyClip();
	}

	private void ApplyClip() {
		if( _clip is Rectangle clip ) {
			// The display projection has its origin at the top-left, but the
			// OpenGL scissor box is measured from the bottom-left, so flip Y.
			int y = (int)_height - ( clip.Y + clip.Height );
			_gl.Enable( EnableCap.ScissorTest );
			_gl.Scissor( clip.X, y, (uint)clip.Width, (uint)clip.Height );
		} else {
			_gl.Disable( EnableCap.ScissorTest );
		}
	}

	private bool IsBound() {
		_gl.GetInteger( GLEnum.FramebufferBinding, out int bound );
		return bound == 0;
	}

	private void FramebufferResize(
		Vector2D<int> size
	) {
		SetSize( size.X, size.Y );
	}

	private void SetSize(
		int width,
		int height
	) {
		_width = width;
		_height = height;
		_gl.Viewport( 0, 0, (uint)_width, (uint)_height );
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
