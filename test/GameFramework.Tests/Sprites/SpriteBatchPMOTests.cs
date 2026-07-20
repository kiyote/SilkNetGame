using System.Drawing;
using GameFramework.Sprites;
using GameFramework.Textures;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace GameFramework.Tests;

// Uses a real (hidden) GL context because Silk.NET's GL is a concrete type that
// cannot be cheaply mocked (same approach as GlStateCacheTests).
//
// Ensure()'s "no-op vs restart" contract is proven with the "mutate behind the
// batch's back" technique against the vertex-array binding: Start() binds the
// batch VAO and Finish() unbinds it, and the batch drives that binding directly
// (it is not routed through GlStateCache). So after establishing a batch we set
// the VAO binding to 0 ourselves, call Ensure() again, and observe whether the
// binding was restored -- a no-op leaves it at 0, a restart re-binds the VAO.
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

	private int VertexArrayBinding() {
		Span<int> data = stackalloc int[1];
		_gl.GetInteger( GLEnum.VertexArrayBinding, data );
		return data[0];
	}

	private ITexture CreateTexture() {
		return new Textures.Texture( _gl, _stateCache, new Dimension( 4, 4 ), TextureFilter.Linear );
	}

	private IRenderTarget CreateRenderTarget() {
		return new RenderTexture( _gl, _stateCache, new Dimension( 16, 16 ), TextureFilter.Linear );
	}

	[Test]
	public void Ensure_SameParameters_DoesNotRestartBatch() {
		IRenderTarget target = CreateRenderTarget();
		ITexture texture = CreateTexture();
		ISpriteBatch batch = new SpriteBatchPMO( _gl, _stateCache );
		Rectangle? clip = new Rectangle( 0, 0, 8, 8 );
		try {
			batch.Ensure( target, texture, BlendMode.Premultiplied, clip );

			// Drop the batch's VAO behind its back; a no-op Ensure must not restore it.
			_gl.BindVertexArray( 0 );
			Assert.That( VertexArrayBinding(), Is.EqualTo( 0 ) );

			bool restarted = batch.Ensure( target, texture, BlendMode.Premultiplied, clip );

			Assert.That( restarted, Is.False );
			Assert.That( VertexArrayBinding(), Is.EqualTo( 0 ) );
		} finally {
			batch.Dispose();
			texture.Dispose();
			target.Dispose();
		}
	}

	[Test]
	public void Ensure_DifferentClip_RestartsBatch() {
		IRenderTarget target = CreateRenderTarget();
		ITexture texture = CreateTexture();
		ISpriteBatch batch = new SpriteBatchPMO( _gl, _stateCache );
		try {
			batch.Ensure( target, texture, BlendMode.Premultiplied, new Rectangle( 0, 0, 8, 8 ) );

			_gl.BindVertexArray( 0 );

			bool restarted = batch.Ensure( target, texture, BlendMode.Premultiplied, new Rectangle( 1, 1, 4, 4 ) );

			Assert.That( restarted, Is.True );
			Assert.That( VertexArrayBinding(), Is.Not.EqualTo( 0 ) );
		} finally {
			batch.Dispose();
			texture.Dispose();
			target.Dispose();
		}
	}

	[Test]
	public void Ensure_DifferentTexture_RestartsBatch() {
		IRenderTarget target = CreateRenderTarget();
		ITexture texture = CreateTexture();
		ITexture otherTexture = CreateTexture();
		ISpriteBatch batch = new SpriteBatchPMO( _gl, _stateCache );
		try {
			batch.Ensure( target, texture, BlendMode.Premultiplied, null );

			_gl.BindVertexArray( 0 );

			bool restarted = batch.Ensure( target, otherTexture, BlendMode.Premultiplied, null );

			Assert.That( restarted, Is.True );
			Assert.That( VertexArrayBinding(), Is.Not.EqualTo( 0 ) );
		} finally {
			batch.Dispose();
			otherTexture.Dispose();
			texture.Dispose();
			target.Dispose();
		}
	}

	[Test]
	public void Ensure_DifferentBlendMode_RestartsBatch() {
		IRenderTarget target = CreateRenderTarget();
		ITexture texture = CreateTexture();
		ISpriteBatch batch = new SpriteBatchPMO( _gl, _stateCache );
		try {
			batch.Ensure( target, texture, BlendMode.Premultiplied, null );

			_gl.BindVertexArray( 0 );

			bool restarted = batch.Ensure( target, texture, BlendMode.Additive, null );

			Assert.That( restarted, Is.True );
			Assert.That( VertexArrayBinding(), Is.Not.EqualTo( 0 ) );
		} finally {
			batch.Dispose();
			texture.Dispose();
			target.Dispose();
		}
	}

	[Test]
	public void Ensure_DifferentRenderTarget_RestartsBatch() {
		IRenderTarget target = CreateRenderTarget();
		IRenderTarget otherTarget = CreateRenderTarget();
		ITexture texture = CreateTexture();
		ISpriteBatch batch = new SpriteBatchPMO( _gl, _stateCache );
		try {
			batch.Ensure( target, texture, BlendMode.Premultiplied, null );

			_gl.BindVertexArray( 0 );

			bool restarted = batch.Ensure( otherTarget, texture, BlendMode.Premultiplied, null );

			Assert.That( restarted, Is.True );
			Assert.That( VertexArrayBinding(), Is.Not.EqualTo( 0 ) );
		} finally {
			batch.Dispose();
			texture.Dispose();
			otherTarget.Dispose();
			target.Dispose();
		}
	}
}
