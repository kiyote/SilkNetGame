using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace GameFramework.Tests;

// Uses a real (hidden) GL context because Silk.NET's GL is a concrete type that
// cannot be cheaply mocked. Correctness is asserted against observable GL state.
// The redundant-skip contract is proven with the "mutate behind the cache's back"
// technique: change GL state directly, then ask the cache for the value it already
// believes is current -- if the state stays as we mutated it, the call was skipped.
[TestFixture]
internal sealed class GlStateCacheTests {

	private static readonly int[] ScissorBox_10_20_30_40 = [10, 20, 30, 40];
	private static readonly int[] ScissorBox_1_2_3_4 = [1, 2, 3, 4];
	private static readonly int[] ScissorBox_5_6_7_8 = [5, 6, 7, 8];

	private IWindow _window;
	private GL _gl;

	[OneTimeSetUp]
	public void OneTimeSetUp() {
		WindowOptions options = WindowOptions.Default;
		options.IsVisible = false;
		_window = Window.Create( options );
		_window.Initialize();
		_gl = GL.GetApi( _window.GLContext );
	}

	[OneTimeTearDown]
	public void OneTimeTearDown() {
		_gl.Dispose();
		_window.Dispose();
	}

	private int GetInt(
		GLEnum pname
	) {
		Span<int> data = stackalloc int[1];
		_gl.GetInteger( pname, data );
		return data[0];
	}

	private uint CreateTrivialProgram() {
		uint vs = _gl.CreateShader( ShaderType.VertexShader );
		_gl.ShaderSource( vs, "#version 330 core\nvoid main(){ gl_Position = vec4( 0.0 ); }" );
		_gl.CompileShader( vs );

		uint fs = _gl.CreateShader( ShaderType.FragmentShader );
		_gl.ShaderSource( fs, "#version 330 core\nout vec4 c;\nvoid main(){ c = vec4( 1.0 ); }" );
		_gl.CompileShader( fs );

		uint program = _gl.CreateProgram();
		_gl.AttachShader( program, vs );
		_gl.AttachShader( program, fs );
		_gl.LinkProgram( program );

		_gl.GetProgram( program, ProgramPropertyARB.LinkStatus, out int linked );
		_gl.DeleteShader( vs );
		_gl.DeleteShader( fs );

		if( linked == 0 ) {
			_gl.DeleteProgram( program );
			Assert.Ignore( "GLSL program failed to link in this context; skipping program-dependent assertions." );
		}

		return program;
	}

	[Test]
	public void UseProgram_RepeatedSameProgram_SkipsRedundantBind() {
		GlStateCache cache = new GlStateCache( _gl );
		uint program = CreateTrivialProgram();
		try {
			cache.UseProgram( program );
			Assert.That( GetInt( GLEnum.CurrentProgram ), Is.EqualTo( (int)program ) );

			// Change the real state behind the cache's back.
			_gl.UseProgram( 0 );
			Assert.That( GetInt( GLEnum.CurrentProgram ), Is.EqualTo( 0 ) );

			// The cache still believes 'program' is current, so this must be skipped
			// and the raw-mutated state (0) must remain.
			cache.UseProgram( program );
			Assert.That( GetInt( GLEnum.CurrentProgram ), Is.EqualTo( 0 ) );

			// After invalidation the next call is forced through to GL.
			cache.Invalidate();
			cache.UseProgram( program );
			Assert.That( GetInt( GLEnum.CurrentProgram ), Is.EqualTo( (int)program ) );
		} finally {
			_gl.UseProgram( 0 );
			_gl.DeleteProgram( program );
		}
	}

	[Test]
	public void BindFramebuffer_RepeatedSameTarget_SkipsRedundantBindAndTracksState() {
		GlStateCache cache = new GlStateCache( _gl );
		uint fbo = _gl.GenFramebuffer();
		try {
			cache.BindFramebuffer( fbo );
			using( Assert.EnterMultipleScope() ) {
				Assert.That( GetInt( GLEnum.FramebufferBinding ), Is.EqualTo( (int)fbo ) );
				Assert.That( cache.IsFramebufferBound( fbo ), Is.True );
				Assert.That( cache.IsFramebufferBound( 0 ), Is.False );
			}

			// Mutate directly, then ask the cache for the value it thinks is current.
			_gl.BindFramebuffer( FramebufferTarget.Framebuffer, 0 );
			cache.BindFramebuffer( fbo );
			Assert.That( GetInt( GLEnum.FramebufferBinding ), Is.EqualTo( 0 ), "Redundant bind should have been skipped." );

			// A genuinely different target is issued.
			cache.BindFramebuffer( 0 );
			using( Assert.EnterMultipleScope() ) {
				Assert.That( GetInt( GLEnum.FramebufferBinding ), Is.EqualTo( 0 ) );
				Assert.That( cache.IsFramebufferBound( 0 ), Is.True );
			}
		} finally {
			_gl.BindFramebuffer( FramebufferTarget.Framebuffer, 0 );
			_gl.DeleteFramebuffer( fbo );
		}
	}

	[Test]
	public void SetScissor_FirstDisable_IssuesDisableEvenWhenUninitialized() {
		GlStateCache cache = new GlStateCache( _gl );
		_gl.Enable( EnableCap.ScissorTest ); // pretend a previous, untracked enable.

		cache.SetScissor( false, 0, 0, 0, 0 );

		Assert.That( _gl.IsEnabled( EnableCap.ScissorTest ), Is.False );
	}

	[Test]
	public void SetScissor_EnableThenSameBox_IssuesEnableOnceAndSkipsRedundantBox() {
		GlStateCache cache = new GlStateCache( _gl );
		try {
			cache.SetScissor( true, 10, 20, 30u, 40u );
			using( Assert.EnterMultipleScope() ) {
				Assert.That( _gl.IsEnabled( EnableCap.ScissorTest ), Is.True );
				Span<int> box = stackalloc int[4];
				_gl.GetInteger( GLEnum.ScissorBox, box );
				Assert.That( box.ToArray(), Is.EqualTo( ScissorBox_10_20_30_40 ) );
			}

			// Mutate the box directly; an identical request must be skipped.
			_gl.Scissor( 1, 2, 3u, 4u );
			cache.SetScissor( true, 10, 20, 30u, 40u );
			Span<int> after = stackalloc int[4];
			_gl.GetInteger( GLEnum.ScissorBox, after );
			Assert.That( after.ToArray(), Is.EqualTo( ScissorBox_1_2_3_4 ), "Redundant scissor box should have been skipped." );
		} finally {
			_gl.Disable( EnableCap.ScissorTest );
		}
	}

	[Test]
	public void SetScissor_SameEnabledDifferentBox_ReissuesBoxOnly() {
		GlStateCache cache = new GlStateCache( _gl );
		try {
			cache.SetScissor( true, 0, 0, 10u, 10u );
			cache.SetScissor( true, 5, 6, 7u, 8u );

			Span<int> box = stackalloc int[4];
			_gl.GetInteger( GLEnum.ScissorBox, box );
			using( Assert.EnterMultipleScope() ) {
				Assert.That( _gl.IsEnabled( EnableCap.ScissorTest ), Is.True );
				Assert.That( box.ToArray(), Is.EqualTo( ScissorBox_5_6_7_8 ) );
			}
		} finally {
			_gl.Disable( EnableCap.ScissorTest );
		}
	}

	[Test]
	public void BindTexture_RepeatedSameUnitAndId_SkipsRedundantBind() {
		GlStateCache cache = new GlStateCache( _gl );
		uint texture = _gl.GenTexture();
		try {
			cache.BindTexture( 0, texture );
			using( Assert.EnterMultipleScope() ) {
				Assert.That( GetInt( GLEnum.TextureBinding2D ), Is.EqualTo( (int)texture ) );
				Assert.That( GetInt( GLEnum.ActiveTexture ), Is.EqualTo( (int)TextureUnit.Texture0 ) );
			}

			// Mutate directly, then request the same id on the same unit -> skipped.
			_gl.BindTexture( TextureTarget.Texture2D, 0 );
			cache.BindTexture( 0, texture );
			Assert.That( GetInt( GLEnum.TextureBinding2D ), Is.EqualTo( 0 ), "Redundant texture bind should have been skipped." );
		} finally {
			_gl.BindTexture( TextureTarget.Texture2D, 0 );
			_gl.DeleteTexture( texture );
		}
	}

	[Test]
	public void BindTexture_DifferentUnit_SwitchesActiveTexture() {
		GlStateCache cache = new GlStateCache( _gl );
		uint texture0 = _gl.GenTexture();
		uint texture1 = _gl.GenTexture();
		uint texture2 = _gl.GenTexture();
		try {
			cache.BindTexture( 0, texture0 );
			cache.BindTexture( 1, texture1 );

			using( Assert.EnterMultipleScope() ) {
				Assert.That( GetInt( GLEnum.ActiveTexture ), Is.EqualTo( (int)TextureUnit.Texture1 ) );
				Assert.That( GetInt( GLEnum.TextureBinding2D ), Is.EqualTo( (int)texture1 ) );
			}

			// Rebinding unit 0 with the SAME id is redundant: the cache early-returns
			// without reactivating unit 0, so the active unit legitimately stays at 1.
			cache.BindTexture( 0, texture0 );
			Assert.That( GetInt( GLEnum.ActiveTexture ), Is.EqualTo( (int)TextureUnit.Texture1 ) );

			// Binding a NEW id on unit 0 is a genuine change, so unit 0 is reactivated.
			cache.BindTexture( 0, texture2 );
			using( Assert.EnterMultipleScope() ) {
				Assert.That( GetInt( GLEnum.ActiveTexture ), Is.EqualTo( (int)TextureUnit.Texture0 ) );
				Assert.That( GetInt( GLEnum.TextureBinding2D ), Is.EqualTo( (int)texture2 ) );
			}
		} finally {
			_gl.BindTexture( TextureTarget.Texture2D, 0 );
			_gl.DeleteTexture( texture0 );
			_gl.DeleteTexture( texture1 );
			_gl.DeleteTexture( texture2 );
		}
	}

	[Test]
	public void Invalidate_ForcesNextCallOfEachKindThroughToGl() {
		GlStateCache cache = new GlStateCache( _gl );
		uint texture = _gl.GenTexture();
		uint fbo = _gl.GenFramebuffer();
		try {
			cache.BindTexture( 0, texture );
			cache.BindFramebuffer( fbo );

			// Wipe the real state without telling the cache.
			_gl.BindTexture( TextureTarget.Texture2D, 0 );
			_gl.BindFramebuffer( FramebufferTarget.Framebuffer, 0 );

			// Invalidation drops the shadow, so the next calls must reissue.
			cache.Invalidate();
			cache.BindTexture( 0, texture );
			cache.BindFramebuffer( fbo );

			using( Assert.EnterMultipleScope() ) {
				Assert.That( GetInt( GLEnum.TextureBinding2D ), Is.EqualTo( (int)texture ) );
				Assert.That( GetInt( GLEnum.FramebufferBinding ), Is.EqualTo( (int)fbo ) );
			}
		} finally {
			_gl.BindTexture( TextureTarget.Texture2D, 0 );
			_gl.BindFramebuffer( FramebufferTarget.Framebuffer, 0 );
			_gl.DeleteTexture( texture );
			_gl.DeleteFramebuffer( fbo );
		}
	}

	[Test]
	public void IsTextureBound_TracksBindingsPerUnit() {
		GlStateCache cache = new GlStateCache( _gl );
		uint texture = _gl.GenTexture();
		try {
			using( Assert.EnterMultipleScope() ) {
				Assert.That( cache.IsTextureBound( 0, texture ), Is.False, "Nothing is bound before the first bind." );
				Assert.That( cache.IsTextureBound( -1, texture ), Is.False, "Out-of-range units report unbound." );
			}

			cache.BindTexture( 0, texture );

			using( Assert.EnterMultipleScope() ) {
				Assert.That( cache.IsTextureBound( 0, texture ), Is.True );
				Assert.That( cache.IsTextureBound( 1, texture ), Is.False, "A different unit is unaffected." );
				Assert.That( cache.IsTextureBound( 0, 0 ), Is.False, "A different id on the same unit is not bound." );
			}
		} finally {
			_gl.BindTexture( TextureTarget.Texture2D, 0 );
			_gl.DeleteTexture( texture );
		}
	}

	[Test]
	public void IsBlend_BeforeFirstSet_ReturnsFalse() {
		GlStateCache cache = new GlStateCache( _gl );

		Assert.That(
			cache.IsBlend( true, BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha ),
			Is.False,
			"An uninitialized cache must not claim any blend state is current."
		);
	}

	[Test]
	public void SetBlend_Enabled_ProgramsGlAndShadowsState() {
		GlStateCache cache = new GlStateCache( _gl );
		try {
			cache.SetBlend( true, BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha );

			using( Assert.EnterMultipleScope() ) {
				Assert.That( GetInt( GLEnum.Blend ), Is.Not.EqualTo( 0 ) );
				Assert.That( GetInt( GLEnum.BlendSrcRgb ), Is.EqualTo( (int)BlendingFactor.One ) );
				Assert.That( GetInt( GLEnum.BlendDstRgb ), Is.EqualTo( (int)BlendingFactor.OneMinusSrcAlpha ) );
				Assert.That( cache.IsBlend( true, BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha ), Is.True );
			}
		} finally {
			_gl.Disable( EnableCap.Blend );
		}
	}

	[Test]
	public void SetBlend_RepeatedSameState_SkipsRedundantProgramming() {
		GlStateCache cache = new GlStateCache( _gl );
		try {
			cache.SetBlend( true, BlendingFactor.One, BlendingFactor.One );

			// Mutate the factors directly behind the cache's back, then re-request the
			// same state: a skip leaves our mutation in place.
			_gl.BlendFunc( BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha );
			cache.SetBlend( true, BlendingFactor.One, BlendingFactor.One );

			using( Assert.EnterMultipleScope() ) {
				Assert.That( GetInt( GLEnum.BlendSrcRgb ), Is.EqualTo( (int)BlendingFactor.SrcAlpha ), "Redundant blend program should have been skipped." );
				Assert.That( GetInt( GLEnum.BlendDstRgb ), Is.EqualTo( (int)BlendingFactor.OneMinusSrcAlpha ) );
			}
		} finally {
			_gl.Disable( EnableCap.Blend );
		}
	}

	[Test]
	public void SetBlend_Disabled_TurnsBlendOff() {
		GlStateCache cache = new GlStateCache( _gl );
		try {
			cache.SetBlend( true, BlendingFactor.One, BlendingFactor.One );
			cache.SetBlend( false, BlendingFactor.One, BlendingFactor.Zero );

			using( Assert.EnterMultipleScope() ) {
				Assert.That( GetInt( GLEnum.Blend ), Is.EqualTo( 0 ) );
				Assert.That( cache.IsBlend( false, BlendingFactor.One, BlendingFactor.Zero ), Is.True );
				Assert.That( cache.IsBlend( true, BlendingFactor.One, BlendingFactor.One ), Is.False );
			}
		} finally {
			_gl.Disable( EnableCap.Blend );
		}
	}

	[Test]
	public void Invalidate_ForcesNextBlendThroughToGl() {
		GlStateCache cache = new GlStateCache( _gl );
		try {
			cache.SetBlend( true, BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha );

			// Wipe the real state without telling the cache.
			_gl.Disable( EnableCap.Blend );

			cache.Invalidate();
			Assert.That(
				cache.IsBlend( true, BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha ),
				Is.False,
				"After Invalidate the cache must not claim any blend state is current."
			);

			cache.SetBlend( true, BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha );
			Assert.That( GetInt( GLEnum.Blend ), Is.Not.EqualTo( 0 ) );
		} finally {
			_gl.Disable( EnableCap.Blend );
		}
	}
}
