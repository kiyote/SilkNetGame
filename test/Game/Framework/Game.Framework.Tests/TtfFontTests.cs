using Game.Framework.Fonts;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Game.Framework.Tests;

internal sealed class TtfFontTests {

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

	[Test]
	public void Ctor_ValidFont_FontLoaded() {
		using TtfFont font = new TtfFont( _gl, "Roboto-Regular.ttf", 16 );
	}
}
