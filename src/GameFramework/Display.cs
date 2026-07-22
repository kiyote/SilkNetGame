using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace GameFramework;

internal sealed class Display : IDisplay {

	private readonly IWindow _window;
	private readonly GL _gl;
	private readonly GlStateCache _stateCache;
	private Dimension _size;
	private Matrix4x4 _projection;
	private Bounds? _clip;

	public Display(
		IWindow window,
		GL gl,
		GlStateCache stateCache,
		Dimension size
	) {
		_gl = gl;
		_stateCache = stateCache;
		_window = window;
		_window.FramebufferResize += FramebufferResize;

		SetSize( size );
	}

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

	void IRenderTarget.Bind() {
		DoBind();
	}

	void IRenderTarget.SetClip(
		Bounds clip
	) {
		_clip = clip;
		if( IsBound() ) {
			ApplyClip();
		}
	}

	void IRenderTarget.SetClip(
		Coordinate position,
		Dimension size
	) {
		if( _clip is null
			|| _clip.Value.X != position.X
			|| _clip.Value.Y != position.Y
			|| _clip.Value.Width != size.Width
			|| _clip.Value.Height != size.Height
		) {
			_clip = new Bounds( position.X, position.Y, size.Width, size.Height );
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

	Bounds? IRenderTarget.Clip => _clip;

	Matrix4x4 IRenderTarget.Projection => _projection;

	Dimension IRenderTarget.Size => _size;

	private void DoBind() {
		_stateCache.BindFramebuffer( 0 );
		_gl.Viewport( 0, 0, (uint)_size.Width, (uint)_size.Height );
		ApplyClip();
	}

	private void ApplyClip() {
		if( _clip is Bounds clip ) {
			// The display projection has its origin at the top-left, but the
			// OpenGL scissor box is measured from the bottom-left, so flip Y.
			int clipX = clip.X;
			int clipY = clip.Y;
			int clipWidth = clip.Width;
			int clipHeight = clip.Height;
			int y = _size.Height - ( clipY + clipHeight );
			_stateCache.SetScissor( true, clipX, y, (uint)clipWidth, (uint)clipHeight );
		} else {
			_stateCache.SetScissor( false, 0, 0, 0, 0 );
		}
	}

	private bool IsBound() {
		return _stateCache.IsFramebufferBound( 0 );
	}

	private void FramebufferResize(
		Vector2D<int> size
	) {
		SetSize( new Dimension( size.X, size.Y ) );
	}

	private void SetSize(
		Dimension size
	) {
		_size = size;
		_gl.Viewport( 0, 0, (uint)size.Width, (uint)size.Height );
		_projection = Matrix4x4.CreateOrthographicOffCenter(
			left: 0.0f,
			right: size.Width,
			bottom: size.Height,
			top: 0.0f,
			zNearPlane: -1.0f,
			zFarPlane: 1.0f
		);
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

	void IDisposable.Dispose() {
	}
}
