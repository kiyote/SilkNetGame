using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace GameFramework.Fonts.Tests;

internal sealed class TtfFontTests {

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

	[Test]
	public void Ctor_ValidFont_FontLoaded() {
		using TtfFont font = new TtfFont( _gl, _stateCache, "Roboto-Regular.ttf", 16 );
	}

	[Test]
	public void Ctor_MissingFontFile_ThrowsFileNotFoundException() {
		Assert.Throws<FileNotFoundException>(
			() => new TtfFont( _gl, _stateCache, "does-not-exist.ttf", 16 )
		);
	}

	[Test]
	public void MeasureText_NonEmptyString_ReturnsPositiveDimension() {
		using TtfFont font = new TtfFont( _gl, _stateCache, "Roboto-Regular.ttf", 16 );

		Dimension size = font.MeasureText( "Hello", 0 );

		using( Assert.EnterMultipleScope() ) {
			Assert.That( size.Width, Is.GreaterThan( 0 ) );
			Assert.That( size.Height, Is.GreaterThan( 0 ) );
		}
	}

	[Test]
	public void MeasureText_LongerStringWiderThanShorter() {
		using TtfFont font = new TtfFont( _gl, _stateCache, "Roboto-Regular.ttf", 16 );

		Dimension shortText = font.MeasureText( "i", 0 );
		Dimension longText = font.MeasureText( "iiiiiiiiii", 0 );

		Assert.That( longText.Width, Is.GreaterThan( shortText.Width ) );
	}

	[Test]
	public void MeasureText_EmptyString_ReturnsZeroWidth() {
		using TtfFont font = new TtfFont( _gl, _stateCache, "Roboto-Regular.ttf", 16 );

		Dimension size = font.MeasureText( "", 0 );

		Assert.That( size.Width, Is.EqualTo( 0 ) );
	}
}
