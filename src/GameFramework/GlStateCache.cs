using Silk.NET.OpenGL;

namespace GameFramework;

/// <summary>
/// Shadows frequently-set OpenGL state so redundant driver/marshalling calls can be
/// skipped. A single instance is shared across every object that touches the tracked
/// state for one GL context. This type is intentionally not thread-safe: all access is
/// expected on the render thread.
///
/// Correctness rule: every mutation of tracked state MUST flow through this cache (or be
/// followed by <see cref="Invalidate"/>). Any raw GL call that changes the current
/// program, framebuffer, scissor, or texture binding without going through here will
/// desync the shadow and produce incorrect skips.
/// </summary>
internal sealed class GlStateCache {

	// A sentinel that can never equal a real GL name (0 is a valid "unbound" name),
	// used to force the first call of each kind to issue through to GL.
	private const uint UNKNOWN = uint.MaxValue;
	private const int UNKNOWN_UNIT = -1;

	private readonly GL _gl;

	private uint _program = UNKNOWN;
	private uint _framebuffer = UNKNOWN;

	private bool _scissorInitialized;
	private bool _scissorEnabled;
	private int _scissorX;
	private int _scissorY;
	private uint _scissorWidth;
	private uint _scissorHeight;

	private int _activeUnit = UNKNOWN_UNIT;
	// Bound texture id per texture unit. Small fixed set is plenty for a 2D sprite engine.
	private readonly uint[] _boundTextures;

	private bool _blendInitialized;
	private bool _blendEnabled;
	private BlendingFactor _blendSrc;
	private BlendingFactor _blendDst;

	public GlStateCache(
		GL gl
	) {
		_gl = gl;
		_boundTextures = new uint[32];
		Array.Fill( _boundTextures, UNKNOWN );
	}

	public void UseProgram(
		uint program
	) {
		if( _program == program ) {
			return;
		}
		_gl.UseProgram( program );
		_program = program;
	}

	public void BindFramebuffer(
		uint framebuffer
	) {
		if( _framebuffer == framebuffer ) {
			return;
		}
		_gl.BindFramebuffer( FramebufferTarget.Framebuffer, framebuffer );
		_framebuffer = framebuffer;
	}

	public bool IsFramebufferBound(
		uint framebuffer
	) {
		return _framebuffer == framebuffer;
	}

	public void SetScissor(
		bool enabled,
		int x,
		int y,
		uint width,
		uint height
	) {
		if( !enabled ) {
			if( !_scissorInitialized || _scissorEnabled ) {
				_gl.Disable( EnableCap.ScissorTest );
				_scissorEnabled = false;
				_scissorInitialized = true;
			}
			return;
		}

		if( !_scissorInitialized || !_scissorEnabled ) {
			_gl.Enable( EnableCap.ScissorTest );
			_scissorEnabled = true;
		}

		if( !_scissorInitialized
			|| _scissorX != x
			|| _scissorY != y
			|| _scissorWidth != width
			|| _scissorHeight != height
		) {
			_gl.Scissor( x, y, width, height );
			_scissorX = x;
			_scissorY = y;
			_scissorWidth = width;
			_scissorHeight = height;
		}

		_scissorInitialized = true;
	}

	public void BindTexture(
		int unit,
		uint texture
	) {
		if( unit >= 0 && unit < _boundTextures.Length && _boundTextures[unit] == texture ) {
			return;
		}

		if( _activeUnit != unit ) {
			_gl.ActiveTexture( (TextureUnit)( (int)TextureUnit.Texture0 + unit ) );
			_activeUnit = unit;
		}

		_gl.BindTexture( TextureTarget.Texture2D, texture );

		if( unit >= 0 && unit < _boundTextures.Length ) {
			_boundTextures[unit] = texture;
		}
	}

	// Reports whether the shadowed binding for the given unit already matches the
	// requested texture. Returns false for out-of-range units and until the first
	// bind on that unit, so the caller always issues a deterministic first bind.
	public bool IsTextureBound(
		int unit,
		uint texture
	) {
		if( unit < 0 || unit >= _boundTextures.Length ) {
			return false;
		}
		return _boundTextures[unit] == texture;
	}

	// Programs the blend state, skipping the driver calls when the shadowed state
	// already matches. When disabling, the source/destination factors are ignored.
	public void SetBlend(
		bool enabled,
		BlendingFactor src,
		BlendingFactor dst
	) {
		if( !enabled ) {
			if( !_blendInitialized || _blendEnabled ) {
				_gl.Disable( EnableCap.Blend );
				_blendEnabled = false;
				_blendInitialized = true;
			}
			return;
		}

		if( !_blendInitialized || !_blendEnabled ) {
			_gl.Enable( EnableCap.Blend );
			_blendEnabled = true;
		}

		if( !_blendInitialized || _blendSrc != src || _blendDst != dst ) {
			_gl.BlendFunc( src, dst );
			_blendSrc = src;
			_blendDst = dst;
		}

		_blendInitialized = true;
	}

	// Reports whether the shadowed blend state already matches the requested state.
	// Returns false until the first SetBlend has run, so the caller always programs
	// a deterministic state at least once. When disabling, only the enabled flag is
	// considered; the source/destination factors are irrelevant.
	public bool IsBlend(
		bool enabled,
		BlendingFactor src,
		BlendingFactor dst
	) {
		if( !_blendInitialized ) {
			return false;
		}

		if( !enabled ) {
			return !_blendEnabled;
		}

		return _blendEnabled && _blendSrc == src && _blendDst == dst;
	}

	/// <summary>
	/// Drops all shadowed state so the next call of each kind is forced through to GL.
	/// Call this after any code path mutates tracked GL state without going through the
	/// cache (for example diagnostic tooling that binds textures directly).
	/// </summary>
	public void Invalidate() {
		_program = UNKNOWN;
		_framebuffer = UNKNOWN;
		_scissorInitialized = false;
		_activeUnit = UNKNOWN_UNIT;
		Array.Fill( _boundTextures, UNKNOWN );
		_blendInitialized = false;
	}
}
