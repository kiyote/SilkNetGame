using GameFramework.Textures;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace GameFramework.Sprites.Tests;

// End-to-end colour correctness for the sprite pipeline.
//
//   1. The public API convention: callers pass a packed colour as 0xRRGGBBAA.
//   2. SpriteBatchPMO.Draw byte-swaps the packed colour (BinaryPrimitives.Reverse-
//      Endianness) so the four normalized vertex bytes are read as R,G,B,A on a
//      little-endian host.
//   3. The fragment shader premultiplies the straight-alpha tint: rgb' = rgb * a.
//
// A unit test over any one stage would have passed while the composed pipeline was
// wrong. So each test renders a known white texture tinted with a known packed
// colour into a RenderTexture and reads the pixels back off the GPU, asserting the
// bytes that actually landed in the framebuffer.
//
// The render target, white source texture, and batch are created ONCE and reused,
// mirroring how the engine actually uses these long-lived GPU resources. Creating
// and disposing a RenderTexture per test recycles framebuffer ids underneath the
// shared GlStateCache, which desynchronises its shadowed bind state -- so the tests
// intentionally avoid that churn.
//
// BlendMode.None is used for the channel-order/premultiply assertions so the source
// colour is written verbatim (dst factor Zero), isolating the tint maths from any
// destination blending.
[TestFixture]
internal sealed class SpriteBatchPMORgbaTests {

	private IWindow _window;
	private GL _gl;
	private GlStateCache _stateCache;

	private IRenderTexture _target;
	private ITexture _white;
	private ISpriteBatch _batch;

	private const int TargetSize = 16;

	[OneTimeSetUp]
	public void OneTimeSetUp() {
		WindowOptions options = WindowOptions.Default;
		options.IsVisible = false;
		_window = Window.Create( options );
		_window.Initialize();
		_gl = GL.GetApi( _window.GLContext );
		_stateCache = new GlStateCache( _gl );

		_target = new RenderTexture( _gl, _stateCache, new Dimension( TargetSize, TargetSize ), TextureFilter.Nearest );

		// A fully opaque white texture: the tint is the only thing that colours the
		// output, so whatever lands in the framebuffer is purely a function of the
		// packed colour flowing through the pipeline.
		_white = new Textures.Texture( _gl, _stateCache, new Dimension( 4, 4 ), TextureFilter.Nearest );
		_white.Clear( new Coordinate( 0, 0 ), new Dimension( 4, 4 ), 0xFFFFFFFFu );

		_batch = new SpriteBatchPMO( _gl, _stateCache );
	}

	[OneTimeTearDown]
	public void OneTimeTearDown() {
		_batch.Dispose();
		_white.Dispose();
		_target.Dispose();
		_gl.Dispose();
		_window.Dispose();
	}

	// Renders a single full-target sprite of the given colour/blend mode over a known
	// cleared background and returns the RGBA bytes at the centre pixel (well away
	// from any edge sampling).
	private (byte R, byte G, byte B, byte A) RenderCentrePixel(
		uint colour,
		BlendMode blendMode,
		uint clearColour = 0x00000000u
	) {
		_target.Bind();
		_target.Clear( clearColour );

		_batch.Start( _target );
		_batch.Draw( _white.Id, 0, 0, TargetSize, TargetSize, 0, 0, 1, 1, colour, blendMode );
		_batch.Finish();

		_gl.Finish();

		byte[] pixels = new byte[TargetSize * TargetSize * 4];
		_target.Bind();
		_gl.ReadPixels( 0, 0, TargetSize, TargetSize, PixelFormat.Rgba, PixelType.UnsignedByte, (Span<byte>)pixels );

		int centre = ( (  TargetSize / 2  * TargetSize ) + ( TargetSize / 2 ) ) * 4;
		return (pixels[centre + 0], pixels[centre + 1], pixels[centre + 2], pixels[centre + 3]);
	}

	[Test]
	public void Draw_OpaqueRedTint_WritesRedChannelNotBlue() {
		// 0xRRGGBBAA = solid red. If the byte-swap were missing/wrong the red and
		// blue channels would be transposed, so this fails loudly on a regression.
		(byte R, byte G, byte B, byte A) = RenderCentrePixel( 0xFF0000FFu, BlendMode.None );

		using( Assert.EnterMultipleScope() ) {
			Assert.That( R, Is.EqualTo( 255 ) );
			Assert.That( G, Is.Zero );
			Assert.That( B, Is.Zero );
			Assert.That( A, Is.EqualTo( 255 ) );
		}
	}

	[Test]
	public void Draw_OpaqueGreenTint_WritesGreenChannel() {
		(byte R, byte G, byte B, byte A) = RenderCentrePixel( 0x00FF00FFu, BlendMode.None );

		using( Assert.EnterMultipleScope() ) {
			Assert.That( R, Is.Zero );
			Assert.That( G, Is.EqualTo( 255 ) );
			Assert.That( B, Is.Zero );
			Assert.That( A, Is.EqualTo( 255 ) );
		}
	}

	[Test]
	public void Draw_OpaqueBlueTint_WritesBlueChannelNotRed() {
		(byte R, byte G, byte B, byte A) = RenderCentrePixel( 0x0000FFFFu, BlendMode.None );

		using( Assert.EnterMultipleScope() ) {
			Assert.That( R, Is.Zero );
			Assert.That( G, Is.Zero );
			Assert.That( B, Is.EqualTo( 255 ) );
			Assert.That( A, Is.EqualTo( 255 ) );
		}
	}

	[Test]
	public void Draw_HalfAlphaWhiteTint_PremultipliesRgbByAlpha() {
		// Straight-alpha white at ~50% alpha. The shader premultiplies, so the
		// framebuffer must hold roughly (a, a, a, a) rather than (255,255,255,a).
		// 0x80 = 128/255 -> 255 * (128/255) ~= 128 after premultiply.
		(byte R, byte G, byte B, byte A) = RenderCentrePixel( 0xFFFFFF80u, BlendMode.None );

		using( Assert.EnterMultipleScope() ) {
			// Allow +/-1 for GPU rounding of the normalized byte maths.
			Assert.That( R, Is.EqualTo( 128 ).Within( 1 ) );
			Assert.That( G, Is.EqualTo( 128 ).Within( 1 ) );
			Assert.That( B, Is.EqualTo( 128 ).Within( 1 ) );
			Assert.That( A, Is.EqualTo( 128 ).Within( 1 ) );
			// The key premultiply invariant: RGB is scaled down with alpha.
			Assert.That( R, Is.LessThan( 200 ) );
		}
	}

	[Test]
	public void Draw_FullyTransparentWhiteTint_ContributesNothing() {
		// This is the exact case MapNodeRenderer fades toward: fully transparent
		// white must premultiply to (0,0,0,0) and therefore leave the cleared
		// target untouched -- not pulse to an opaque colour.
		(byte R, byte G, byte B, byte A) = RenderCentrePixel( 0xFFFFFF00u, BlendMode.Premultiplied, 0x00000000u );

		using( Assert.EnterMultipleScope() ) {
			Assert.That( R, Is.Zero );
			Assert.That( G, Is.Zero );
			Assert.That( B, Is.Zero );
			Assert.That( A, Is.Zero );
		}
	}

	[Test]
	public void Draw_OpaqueWhiteTint_PreservesTextureColourExactly() {
		// A fully opaque white tint is the identity case: the white texture must
		// come through as pure white with no channel drift.
		(byte R, byte G, byte B, byte A) = RenderCentrePixel( 0xFFFFFFFFu, BlendMode.None );

		using( Assert.EnterMultipleScope() ) {
			Assert.That( R, Is.EqualTo( 255 ) );
			Assert.That( G, Is.EqualTo( 255 ) );
			Assert.That( B, Is.EqualTo( 255 ) );
			Assert.That( A, Is.EqualTo( 255 ) );
		}
	}
}
