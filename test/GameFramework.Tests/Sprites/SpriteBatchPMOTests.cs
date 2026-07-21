using GameFramework.Sprites;
using GameFramework.Textures;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace GameFramework.Tests;

// Uses a real (hidden) GL context because Silk.NET's GL is a concrete type that
// cannot be cheaply mocked (same approach as GlStateCacheTests).
//
// The batch's flush behaviour is proven through the public FlushCount metric: it
// increments once per GPU submission (a texture change with pending sprites, a
// clip change with pending sprites, a capacity overflow, or Finish()). Buffering a
// sprite and then triggering a state change lets us assert whether a flush was
// forced.
[TestFixture]
internal sealed class SpriteBatchPMOTests {

	private IWindow _window;
	private GL _gl;
	private GlStateCache _stateCache;

	[OneTimeSetUp]
	public void OneTimeSetUp() {
		WindowOptions options = WindowOptions.Default;
		options.IsVisible = false;
		_window = Window.Create( options );
		_window.Initialize();
		_gl = GL.GetApi( _window.GLContext );
		_stateCache = new GlStateCache( _gl );
	}

	[OneTimeTearDown]
	public void OneTimeTearDown() {
		_gl.Dispose();
		_window.Dispose();
	}

	private ITexture CreateTexture() {
		return new Textures.Texture( _gl, _stateCache, new Dimension( 4, 4 ), TextureFilter.Linear );
	}

	private IRenderTarget CreateRenderTarget() {
		return new RenderTexture( _gl, _stateCache, new Dimension( 16, 16 ), TextureFilter.Linear );
	}

	[Test]
	public void Draw_SameTexture_DoesNotFlush() {
		IRenderTarget target = CreateRenderTarget();
		ITexture texture = CreateTexture();
		ISpriteBatch batch = new SpriteBatchPMO( _gl, _stateCache );
		try {
			batch.Start( target );

			batch.Draw( texture.Id, 0, 0, 4, 4, 0, 0, 1, 1, 0xFFFFFFFF );
			long before = batch.FlushCount;

			// Drawing again with the same texture id must not force a flush.
			batch.Draw( texture.Id, 4, 4, 4, 4, 0, 0, 1, 1, 0xFFFFFFFF );

			Assert.That( batch.FlushCount, Is.EqualTo( before ) );
		} finally {
			batch.Dispose();
			texture.Dispose();
			target.Dispose();
		}
	}

	[Test]
	public void Draw_DifferentTexture_FlushesPendingSprites() {
		IRenderTarget target = CreateRenderTarget();
		ITexture texture = CreateTexture();
		ITexture otherTexture = CreateTexture();
		ISpriteBatch batch = new SpriteBatchPMO( _gl, _stateCache );
		try {
			batch.Start( target );

			batch.Draw( texture.Id, 0, 0, 4, 4, 0, 0, 1, 1, 0xFFFFFFFF );
			long before = batch.FlushCount;

			// A different texture id with pending sprites forces a flush/rebind.
			batch.Draw( otherTexture.Id, 4, 4, 4, 4, 0, 0, 1, 1, 0xFFFFFFFF );

			Assert.That( batch.FlushCount, Is.EqualTo( before + 1 ) );
		} finally {
			batch.Dispose();
			otherTexture.Dispose();
			texture.Dispose();
			target.Dispose();
		}
	}

	[Test]
	public void Draw_DifferentTexture_NoPendingSprites_DoesNotFlush() {
		IRenderTarget target = CreateRenderTarget();
		ITexture texture = CreateTexture();
		ITexture otherTexture = CreateTexture();
		ISpriteBatch batch = new SpriteBatchPMO( _gl, _stateCache );
		try {
			batch.Start( target );

			// First draw binds the texture but there is nothing buffered before it,
			// so no flush is forced by the initial bind.
			long before = batch.FlushCount;
			batch.Draw( texture.Id, 0, 0, 4, 4, 0, 0, 1, 1, 0xFFFFFFFF );

			Assert.That( batch.FlushCount, Is.EqualTo( before ) );
		} finally {
			batch.Dispose();
			otherTexture.Dispose();
			texture.Dispose();
			target.Dispose();
		}
	}

	[Test]
	public void Draw_SameBlendMode_DoesNotFlush() {
		IRenderTarget target = CreateRenderTarget();
		ITexture texture = CreateTexture();
		ISpriteBatch batch = new SpriteBatchPMO( _gl, _stateCache );
		try {
			batch.Start( target );

			batch.Draw( texture.Id, 0, 0, 4, 4, 0, 0, 1, 1, 0xFFFFFFFF, BlendMode.Premultiplied );
			long before = batch.FlushCount;

			// Drawing again with the same blend mode must not force a flush.
			batch.Draw( texture.Id, 4, 4, 4, 4, 0, 0, 1, 1, 0xFFFFFFFF, BlendMode.Premultiplied );

			Assert.That( batch.FlushCount, Is.EqualTo( before ) );
		} finally {
			batch.Dispose();
			texture.Dispose();
			target.Dispose();
		}
	}

	[Test]
	public void Draw_DifferentBlendMode_FlushesPendingSprites() {
		IRenderTarget target = CreateRenderTarget();
		ITexture texture = CreateTexture();
		ISpriteBatch batch = new SpriteBatchPMO( _gl, _stateCache );
		try {
			batch.Start( target );

			batch.Draw( texture.Id, 0, 0, 4, 4, 0, 0, 1, 1, 0xFFFFFFFF, BlendMode.Premultiplied );
			long before = batch.FlushCount;

			// A different blend mode with pending sprites forces a flush/reprogram.
			batch.Draw( texture.Id, 4, 4, 4, 4, 0, 0, 1, 1, 0xFFFFFFFF, BlendMode.Additive );

			Assert.That( batch.FlushCount, Is.EqualTo( before + 1 ) );
		} finally {
			batch.Dispose();
			texture.Dispose();
			target.Dispose();
		}
	}

	[Test]
	public void ReplaceClip_DifferentClip_FlushesPendingSprites() {
		IRenderTarget target = CreateRenderTarget();
		ITexture texture = CreateTexture();
		ISpriteBatch batch = new SpriteBatchPMO( _gl, _stateCache );
		try {
			batch.Start( target );

			batch.Draw( texture.Id, 0, 0, 4, 4, 0, 0, 1, 1, 0xFFFFFFFF );
			long before = batch.FlushCount;

			batch.ReplaceClip( new Bounds( 1, 1, 4, 4 ) );

			Assert.That( batch.FlushCount, Is.EqualTo( before + 1 ) );

			batch.RestoreClip();
		} finally {
			batch.Dispose();
			texture.Dispose();
			target.Dispose();
		}
	}

	[Test]
	public void ReplaceClip_SameClip_DoesNotFlush() {
		IRenderTarget target = CreateRenderTarget();
		ITexture texture = CreateTexture();
		ISpriteBatch batch = new SpriteBatchPMO( _gl, _stateCache );
		try {
			batch.Start( target );

			Bounds clip = new Bounds( 1, 1, 4, 4 );
			batch.ReplaceClip( clip );
			batch.Draw( texture.Id, 0, 0, 4, 4, 0, 0, 1, 1, 0xFFFFFFFF );
			long before = batch.FlushCount;

			// Pushing the same clip again is effectively a no-op scissor change and
			// must not flush.
			batch.ReplaceClip( clip );

			Assert.That( batch.FlushCount, Is.EqualTo( before ) );

			batch.RestoreClip();
			batch.RestoreClip();
		} finally {
			batch.Dispose();
			texture.Dispose();
			target.Dispose();
		}
	}

	[Test]
	public void RestoreClip_WhenClipped_FlushesPendingSprites() {
		IRenderTarget target = CreateRenderTarget();
		ITexture texture = CreateTexture();
		ISpriteBatch batch = new SpriteBatchPMO( _gl, _stateCache );
		try {
			batch.Start( target );

			batch.ReplaceClip( new Bounds( 1, 1, 4, 4 ) );
			batch.Draw( texture.Id, 0, 0, 4, 4, 0, 0, 1, 1, 0xFFFFFFFF );
			long before = batch.FlushCount;

			batch.RestoreClip();

			Assert.That( batch.FlushCount, Is.EqualTo( before + 1 ) );
		} finally {
			batch.Dispose();
			texture.Dispose();
			target.Dispose();
		}
	}

	[Test]
	public void RestoreClip_BelowStartDepth_Throws() {
		IRenderTarget target = CreateRenderTarget();
		ITexture texture = CreateTexture();
		ISpriteBatch batch = new SpriteBatchPMO( _gl, _stateCache );
		try {
			batch.Start( target );

			batch.Draw( texture.Id, 0, 0, 4, 4, 0, 0, 1, 1, 0xFFFFFFFF );

			// Nothing was pushed after Start, so there is no clip this batch owns to
			// restore; popping below the start depth must throw.
			Assert.Throws<InvalidOperationException>( () => batch.RestoreClip() );
		} finally {
			batch.Dispose();
			texture.Dispose();
			target.Dispose();
		}
	}

	[Test]
	public void RestoreClip_BeforeStart_WhenStackEmpty_DoesNotThrow() {
		IRenderTarget target = CreateRenderTarget();
		ISpriteBatch batch = new SpriteBatchPMO( _gl, _stateCache );
		try {
			// Outside a batch there is no start depth to protect, so restoring an
			// empty stack is a silent no-op.
			Assert.DoesNotThrow( () => batch.RestoreClip() );
		} finally {
			batch.Dispose();
			target.Dispose();
		}
	}

	[Test]
	public void ReplaceClip_BeforeStart_IsRetainedThroughFinish() {
		IRenderTarget target = CreateRenderTarget();
		ITexture texture = CreateTexture();
		ISpriteBatch batch = new SpriteBatchPMO( _gl, _stateCache );
		try {
			// A clip pushed before Start must survive a full Start/Finish cycle.
			batch.ReplaceClip( new Bounds( 2, 2, 8, 8 ) );
			batch.Start( target );
			batch.Draw( texture.Id, 0, 0, 4, 4, 0, 0, 1, 1, 0xFFFFFFFF );

			Assert.DoesNotThrow( () => batch.Finish() );
		} finally {
			batch.Dispose();
			texture.Dispose();
			target.Dispose();
		}
	}

	[Test]
	public void Finish_UnbalancedClipStack_Throws() {
		IRenderTarget target = CreateRenderTarget();
		ITexture texture = CreateTexture();
		ISpriteBatch batch = new SpriteBatchPMO( _gl, _stateCache );
		try {
			batch.Start( target );

			// A clip pushed after Start without a matching RestoreClip leaks a scope.
			batch.ReplaceClip( new Bounds( 1, 1, 4, 4 ) );

			Assert.Throws<InvalidOperationException>( () => batch.Finish() );

			// Balance the stack so the batch can be disposed cleanly.
			batch.RestoreClip();
			batch.Finish();
		} finally {
			batch.Dispose();
			texture.Dispose();
			target.Dispose();
		}
	}

	[Test]
	public void Finish_WithPendingSprites_Flushes() {
		IRenderTarget target = CreateRenderTarget();
		ITexture texture = CreateTexture();
		ISpriteBatch batch = new SpriteBatchPMO( _gl, _stateCache );
		try {
			batch.Start( target );

			batch.Draw( texture.Id, 0, 0, 4, 4, 0, 0, 1, 1, 0xFFFFFFFF );
			long before = batch.FlushCount;

			batch.Finish();

			Assert.That( batch.FlushCount, Is.EqualTo( before + 1 ) );
		} finally {
			batch.Dispose();
			texture.Dispose();
			target.Dispose();
		}
	}
}

