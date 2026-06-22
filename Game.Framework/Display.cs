using System.Drawing;
using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Game.Framework;

public sealed class Display : RenderTarget {

	private readonly IWindow _window;
	private readonly GL _gl;
	private uint _width;
	private uint _height;
	private Matrix4x4 _projection;

	// *******************************************************
	// These are for handling disabling updating when resizing
	private bool _isResizing = false;
	private CancellationTokenSource? _debounceCts;
	private Vector2D<int> _targetSize;
	private bool _lastVSync;
	// *******************************************************

	internal Display(
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

	public bool IsResizing => _isResizing;

	public void Clear(
		Color colour
	) {
		Bind();
		_gl.ClearColor( colour );
		_gl.Clear( ClearBufferMask.ColorBufferBit );
	}

	internal override void Bind() {
		_gl.BindFramebuffer( FramebufferTarget.Framebuffer, 0 );
		_gl.Viewport( 0, 0, _width, _height );
	}

	internal override Matrix4x4 Projection => _projection;

	internal override uint Width => _width;

	internal override uint Height => _height;

	private void FramebufferResize(
		Vector2D<int> size
	) {
		_targetSize = size;

		if( !_isResizing ) {
			_isResizing = true;
			_lastVSync = _window.VSync;
			_window.VSync = false;
		}

		_debounceCts?.Cancel();
		_debounceCts = new CancellationTokenSource();

		Task.Delay( 150, _debounceCts.Token ).ContinueWith( t => {
			if( t.IsCompletedSuccessfully ) {
				EndResize();
			}
		} );
	}

	private void EndResize() {
		_window.VSync = _lastVSync;
		_isResizing = false;
		_debounceCts = null;
		GC.Collect(); // Now is an opportune time since the user will be not expecting perfection after a resize

		SetSize( (uint)_targetSize.X, (uint)_targetSize.Y );
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
}
