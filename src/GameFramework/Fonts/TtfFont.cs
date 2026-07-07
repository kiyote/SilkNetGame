using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using GameFramework.Textures;
using HarfBuzzSharp;
using Silk.NET.OpenGL;
using StbTrueTypeSharp;
using Buffer = HarfBuzzSharp.Buffer;

namespace GameFramework.Fonts;

internal sealed unsafe class TtfFont : IFont {
	// Largest UTF-8 byte count we encode on the stack before falling back to the pool.
	private const int MaxStackTextBytes = 512;

	private readonly GL _gl;
	private byte[]? _fontData;
	private StbTrueType.stbtt_fontinfo? _fontInfo;
	private readonly Blob _blob;
	private readonly Face _face;
	private readonly Font _font;
	private readonly float _scale;
	private bool _isDisposed;

	private readonly Dictionary<uint, GlyphMetrics> _glyphCache = [];

	private readonly Buffer _buffer = new Buffer();
	private readonly List<(GlyphMetrics Metrics, Vector2 FinalPos)> _renderedGlyphs = [];
	private byte[] _compositePixels = [];

	private struct GlyphMetrics {
		public byte[] Pixels;    // Raw R8 grayscale data
		public int Width;        // Pixel width of the bitmap
		public int Height;       // Pixel height of the bitmap
		public int XOffset;      // Left bearing (horizontal offset)
		public int YOffset;      // Top bearing (vertical baseline shift)
	}

	public TtfFont(
		GL gl,
		string fontPath,
		float fontHeightInPixels
	) {
		_gl = gl;
		_fontData = File.ReadAllBytes( fontPath );

		_fontInfo = new StbTrueType.stbtt_fontinfo();
		fixed( byte* ptr = _fontData ) {
			int result = StbTrueType.stbtt_InitFont( _fontInfo, ptr, 0 );
			if( result != 1 ) {
				throw new InvalidOperationException( "Failed to initialize font data." );
			}
		}
		_scale = StbTrueType.stbtt_ScaleForMappingEmToPixels( _fontInfo, fontHeightInPixels );

		MemoryStream ms = new MemoryStream( _fontData );
		_blob = Blob.FromStream( ms );
		_face = new Face( _blob, 0 );
		_font = new Font( _face );

		int hbScale = (int)MathF.Round( _scale * _face.UnitsPerEm * 64.0f );
		_font.SetScale( hbScale, hbScale );
	}

	private void ShapeText(
		ReadOnlySpan<byte> text
	) {
		_buffer.ClearContents();
		_buffer.AddUtf8( text );
		_buffer.GuessSegmentProperties();
		_font.Shape( _buffer );
	}

	private byte[] RentCompositeBuffer(
		int byteCount
	) {
		if( _compositePixels.Length < byteCount ) {
			_compositePixels = new byte[byteCount];
		} else {
			Array.Clear( _compositePixels, 0, byteCount );
		}
		return _compositePixels;
	}

	public void DrawText(
		ITexture texture,
		ReadOnlySpan<byte> text,
		int x,
		int y,
		uint colour = 0xFFFFFFFF
	) {
		ObjectDisposedException.ThrowIf( _isDisposed, this );

		// Text with no outline is just the outlined path with a zero-width stroke.
		byte[] textureData = GenerateTextureData( text, colour, 0u, 0, out int width, out int height );

		UploadClipped( texture, textureData, x, y, width, height );
	}

	public void DrawOutlinedText(
		ITexture texture,
		ReadOnlySpan<byte> text,
		int x,
		int y,
		uint textColour = 0xFFFFFFFF,
		uint outlineColour = 0x000000FF,
		int outlineThickness = 1
	) {
		ObjectDisposedException.ThrowIf( _isDisposed, this );

		byte[] textureData = GenerateTextureData( text, textColour, outlineColour, outlineThickness, out int width, out int height );

		UploadClipped( texture, textureData, x, y, width, height );
	}

	private void UploadClipped(
		ITexture texture,
		byte[] textureData,
		int x,
		int y,
		int width,
		int height
	) {
		// Nothing was rendered (empty / whitespace string).
		if( width <= 0 || height <= 0 || textureData.Length == 0 ) {
			return;
		}

		int texWidth = (int)texture.TextureWidth;
		int texHeight = (int)texture.TextureHeight;

		int destX = x;
		int destY = y;
		int srcSkipX = 0;
		int srcSkipY = 0;
		int copyWidth = width;
		int copyHeight = height;

		// Clip against the left / top edges (negative destination offsets):
		// move the destination back to 0 and skip the hidden source columns/rows.
		if( destX < 0 ) {
			srcSkipX = -destX;
			copyWidth += destX;
			destX = 0;
		}
		if( destY < 0 ) {
			srcSkipY = -destY;
			copyHeight += destY;
			destY = 0;
		}

		// Clip against the right / bottom edges (overflow past the texture).
		if( destX + copyWidth > texWidth ) {
			copyWidth = texWidth - destX;
		}
		if( destY + copyHeight > texHeight ) {
			copyHeight = texHeight - destY;
		}

		// The text is entirely outside the texture; nothing to upload.
		if( copyWidth <= 0 || copyHeight <= 0 ) {
			return;
		}

		_gl.BindTexture( TextureTarget.Texture2D, texture.Id );

		// Describe the real stride of the source buffer and the sub-rect to read,
		// so a left/top clip reads from the correct offset instead of repacking.
		_gl.PixelStore( PixelStoreParameter.UnpackRowLength, width );
		_gl.PixelStore( PixelStoreParameter.UnpackSkipPixels, srcSkipX );
		_gl.PixelStore( PixelStoreParameter.UnpackSkipRows, srcSkipY );

		fixed( byte* ptr = textureData ) {
			_gl.TexSubImage2D(
				TextureTarget.Texture2D,
				0,                      // Mipmap level
				destX,                  // Clipped X destination offset inside the texture
				destY,                  // Clipped Y destination offset inside the texture
				(uint)copyWidth,        // Clipped width of the sub-image
				(uint)copyHeight,       // Clipped height of the sub-image
				PixelFormat.Rgba,       // Pixel format of the CPU byte array
				PixelType.UnsignedByte, // Data type per channel
				ptr                     // Pointer to raw bytes
			);
		}

		// Restore default unpack state so unrelated uploads are unaffected.
		_gl.PixelStore( PixelStoreParameter.UnpackRowLength, 0 );
		_gl.PixelStore( PixelStoreParameter.UnpackSkipPixels, 0 );
		_gl.PixelStore( PixelStoreParameter.UnpackSkipRows, 0 );
	}

	public void MeasureText(
		ReadOnlySpan<byte> text,
		int outlineWidth,
		out int width,
		out int height
	) {
		LayoutGlyphs( text, outlineWidth, out width, out height, out _, out _ );
	}

	public void DrawText(
		ITexture texture,
		string text,
		int x,
		int y,
		uint colour = 0xFFFFFFFF
	) {
		int maxBytes = Encoding.UTF8.GetMaxByteCount( text.Length );

		if (maxBytes <= MaxStackTextBytes) {
			Span<byte> buffer = stackalloc byte[MaxStackTextBytes];
			int written = Encoding.UTF8.GetBytes( text, buffer );
			DrawText( texture, buffer[..written], x, y, colour );
		} else {
			byte[] rented = ArrayPool<byte>.Shared.Rent( maxBytes );
			Span<byte> buffer = rented;
			int written = Encoding.UTF8.GetBytes( text, buffer );
			DrawText( texture, buffer[..written], x, y, colour );
			ArrayPool<byte>.Shared.Return( rented );
		}
	}

	public void DrawOutlinedText(
		ITexture texture,
		string text,
		int x,
		int y,
		uint textColour = 0xFFFFFFFF,
		uint outlineColour = 0x000000FF,
		int outlineThickness = 1
	) {
		// Check if the string length itself safely fits in the stack allocation.
		// Since 1 char is at most 4 UTF-8 bytes, text.Length * 4 is a safe boundary.
		if( text.Length * 4 <= MaxStackTextBytes ) {
			Span<byte> buffer = stackalloc byte[MaxStackTextBytes];
			int written = Encoding.UTF8.GetBytes( text, buffer );
			DrawOutlinedText( texture, buffer[..written], x, y, textColour, outlineColour, outlineThickness );
		} else {
			// Only calculate the exact max bytes if we absolutely must fall back to the pool
			int maxBytes = Encoding.UTF8.GetMaxByteCount( text.Length );
			byte[] rented = ArrayPool<byte>.Shared.Rent( maxBytes );

			// Explicitly casting the array to Span ensures the [..written] syntax remains a cheap slice
			Span<byte> buffer = rented;
			int written = Encoding.UTF8.GetBytes( text, buffer );

			DrawOutlinedText( texture, buffer[..written], x, y, textColour, outlineColour, outlineThickness );
			ArrayPool<byte>.Shared.Return( rented );
		}
	}


	public void MeasureText(
		string text,
		int outlineWidth,
		out int width,
		out int height
	) {
		// 1 char is at most 4 UTF-8 bytes. 
		// Checking text.Length * 4 ensures the stack buffer will never overflow.
		if( text.Length * 4 <= MaxStackTextBytes ) {
			Span<byte> buffer = stackalloc byte[MaxStackTextBytes];
			int written = Encoding.UTF8.GetBytes( text, buffer );
			MeasureText( buffer[..written], outlineWidth, out width, out height );
		} else {
			// Fall back to the ArrayPool only for long strings
			int maxBytes = Encoding.UTF8.GetMaxByteCount( text.Length );
			byte[] rented = ArrayPool<byte>.Shared.Rent( maxBytes );

			Span<byte> buffer = rented;
			int written = Encoding.UTF8.GetBytes( text, buffer );
			MeasureText( buffer[..written], outlineWidth, out width, out height );

			ArrayPool<byte>.Shared.Return( rented );
		}
	}

	// Shapes 'text', caches each glyph bitmap, records the visible glyphs into the
	// reusable _renderedGlyphs scratch list, and reports the padded pixel footprint.
	// 'padding' expands the bounds on every side (used for outline spread).
	// Returns false (with a zero-sized footprint) when nothing visible was produced.
	private bool LayoutGlyphs(
		ReadOnlySpan<byte> text,
		int padding,
		out int outputWidth,
		out int outputHeight,
		out float minX,
		out float minY
	) {
		// Process text through HarfBuzz to get glyph IDs and layout tracking metrics.
		ShapeText( text );

		ReadOnlySpan<GlyphInfo> infos = _buffer.GetGlyphInfoSpan();
		ReadOnlySpan<GlyphPosition> positions = _buffer.GetGlyphPositionSpan();
		int glyphCount = _buffer.Length;

		_renderedGlyphs.Clear();
		float penX = 0f, penY = 0f;
		minX = float.MaxValue;
		minY = float.MaxValue;
		float maxX = float.MinValue, maxY = float.MinValue;

		for( int i = 0; i < glyphCount; i++ ) {
			uint glyphId = infos[i].Codepoint;
			GlyphMetrics glyph = GetOrCacheGlyph( glyphId );

			// Convert fractional HarfBuzz metrics back to pixel space.
			float hbXOffset = positions[i].XOffset / 64.0f;
			float hbYOffset = positions[i].YOffset / 64.0f;
			float hbXAdvance = positions[i].XAdvance / 64.0f;
			float hbYAdvance = positions[i].YAdvance / 64.0f;

			float xPos = penX + glyph.XOffset + hbXOffset;
			float yPos = penY + glyph.YOffset + hbYOffset;

			if( glyph.Width > 0 && glyph.Height > 0 ) {
				_renderedGlyphs.Add( (glyph, new Vector2( xPos, yPos )) );

				// Expand the bounding box, accounting for any outline spread.
				minX = Math.Min( minX, xPos - padding );
				minY = Math.Min( minY, yPos - padding );
				maxX = Math.Max( maxX, xPos + glyph.Width + padding );
				maxY = Math.Max( maxY, yPos + glyph.Height + padding );
			}

			// Move the pen cursor ahead based on HarfBuzz spacing definitions.
			penX += hbXAdvance;
			penY += hbYAdvance;
		}

		// Empty or completely whitespace string: report a zero-sized footprint.
		if( _renderedGlyphs.Count == 0 ) {
			outputWidth = 0;
			outputHeight = 0;
			minX = 0f;
			minY = 0f;
			return false;
		}

		outputWidth = (int)Math.Ceiling( maxX - minX );
		outputHeight = (int)Math.Ceiling( maxY - minY );
		return true;
	}

	private byte[] GenerateTextureData(
		ReadOnlySpan<byte> text,
		uint textColour,
		uint outlineColour,
		int outlineThickness,
		out int outputWidth,
		out int outputHeight
	) {
		(float textR, float textG, float textB, float textA) = DecomposeRgba( textColour );
		(float outR, float outG, float outB, float outA) = DecomposeRgba( outlineColour );

		outlineThickness = Math.Max( 0, outlineThickness );

		// Shape + lay out the glyphs; the bounds are padded by the outline thickness.
		if( !LayoutGlyphs( text, outlineThickness, out outputWidth, out outputHeight, out float minX, out float minY ) ) {
			return [];
		}

		byte[] compositePixels = RentCompositeBuffer( outputWidth * outputHeight * 4 );

		// 3. Composite Blitting Loop
		foreach( (GlyphMetrics glyph, Vector2 position) in _renderedGlyphs ) {
			// Align coordinates relative to our zero-indexed canvas surface
			int startX = (int)Math.Round( position.X - minX );
			int startY = (int)Math.Round( position.Y - minY );

			// ====================================================================
			// LAYER A: Generate and blend the outline first
			// ====================================================================
			if( outlineThickness > 0 ) {
				for( int row = 0; row < glyph.Height; row++ ) {
					for( int col = 0; col < glyph.Width; col++ ) {
						byte sourceAlpha = glyph.Pixels[( row * glyph.Width ) + col];
						if( sourceAlpha == 0 ) {
							continue;
						}

						// Spread this pixel value out into a radial circle matching the stroke thickness
						for( int dy = -outlineThickness; dy <= outlineThickness; dy++ ) {
							for( int dx = -outlineThickness; dx <= outlineThickness; dx++ ) {
								// Circular constraint equation creates smooth rounded outline edges
								if( ( dx * dx ) + ( dy * dy ) > outlineThickness * outlineThickness ) {
									continue;
								}

								int targetX = startX + col + dx;
								int targetY = startY + row + dy;

								if( targetX < 0 || targetX >= outputWidth || targetY < 0 || targetY >= outputHeight ) {
									continue;
								}

								int targetIndex = ( ( targetY * outputWidth ) + targetX ) * 4;

								// Calculate target visibility weight
								float currentOutlineAlpha = sourceAlpha / 255f * outA;
								float existingAlpha = compositePixels[targetIndex + 3] / 255f;

								// If multiple pixels bleed into this outline region, preserve the strongest one
								float newAlpha = Math.Max( existingAlpha, currentOutlineAlpha );

								// Apply Pre-Multiplied Alpha formatting directly to the byte structure
								compositePixels[targetIndex + 0] = (byte)( outR * newAlpha * 255f );
								compositePixels[targetIndex + 1] = (byte)( outG * newAlpha * 255f );
								compositePixels[targetIndex + 2] = (byte)( outB * newAlpha * 255f );
								compositePixels[targetIndex + 3] = (byte)( newAlpha * 255f );
							}
						}
					}
				}
			}
		}
		foreach( (GlyphMetrics glyph, Vector2 position) in _renderedGlyphs ) {
			// Align coordinates relative to our zero-indexed canvas surface
			int startX = (int)Math.Round( position.X - minX );
			int startY = (int)Math.Round( position.Y - minY );

			// ====================================================================
			// LAYER B: Overlay the primary font content cleanly on top
			// ====================================================================
			for( int row = 0; row < glyph.Height; row++ ) {
				int targetY = startY + row;
				if( targetY < 0 || targetY >= outputHeight ) {
					continue;
				}

				for( int col = 0; col < glyph.Width; col++ ) {
					int targetX = startX + col;
					if( targetX < 0 || targetX >= outputWidth ) {
						continue;
					}

					byte glyphAlphaBytes = glyph.Pixels[( row * glyph.Width ) + col];
					if( glyphAlphaBytes == 0 ) {
						continue;
					}

					float glyphAlpha = glyphAlphaBytes / 255f * textA;
					int targetIndex = ( ( targetY * outputWidth ) + targetX ) * 4;

					// Load any background pixels already processed (i.e. our outline layer)
					float bgR = compositePixels[targetIndex + 0] / 255f;
					float bgG = compositePixels[targetIndex + 1] / 255f;
					float bgB = compositePixels[targetIndex + 2] / 255f;
					float bgA = compositePixels[targetIndex + 3] / 255f;

					// Standard Pre-Multiplied Alpha manual composition formula:
					// Color = SrcColor + DstColor * (1.0 - SrcAlpha)
					float invGlyphAlpha = 1.0f - glyphAlpha;
					float outR_P = ( textR * glyphAlpha ) + ( bgR * invGlyphAlpha );
					float outG_P = ( textG * glyphAlpha ) + ( bgG * invGlyphAlpha );
					float outB_P = ( textB * glyphAlpha ) + ( bgB * invGlyphAlpha );
					float outA_P = glyphAlpha + ( bgA * invGlyphAlpha );

					compositePixels[targetIndex + 0] = (byte)Math.Clamp( outR_P * 255f, 0f, 255f );
					compositePixels[targetIndex + 1] = (byte)Math.Clamp( outG_P * 255f, 0f, 255f );
					compositePixels[targetIndex + 2] = (byte)Math.Clamp( outB_P * 255f, 0f, 255f );
					compositePixels[targetIndex + 3] = (byte)Math.Clamp( outA_P * 255f, 0f, 255f );
				}
			}
		}

		return compositePixels;
	}

	private GlyphMetrics GetOrCacheGlyph(
		uint glyphId
	) {
		if( _glyphCache.TryGetValue( glyphId, out GlyphMetrics metrics ) ) {
			return metrics;
		}

		int w, h, xOff, yOff;
		StbTrueType.stbtt_GetGlyphBitmapBox( _fontInfo, (int)glyphId, _scale, _scale, &xOff, &yOff, &w, &h );

		int width = w - xOff;
		int height = h - yOff;
		byte[] pixels = [];

		if( width > 0 && height > 0 ) {
			pixels = new byte[width * height];
			fixed( byte* pFont = _fontData )
			fixed( byte* pPixels = pixels ) {
				StbTrueType.stbtt_MakeGlyphBitmap( _fontInfo, pPixels, width, height, width, _scale, _scale, (int)glyphId );
			}
		}

		metrics = new GlyphMetrics {
			Pixels = pixels,
			Width = width,
			Height = height,
			XOffset = xOff,
			YOffset = yOff
		};

		_glyphCache[glyphId] = metrics;
		return metrics;
	}

	public void Dispose() {
		if( !_isDisposed ) {
			_isDisposed = true;
			_glyphCache.Clear();
			_renderedGlyphs.Clear();
			_compositePixels = [];
			_buffer.Dispose();
			_font.Dispose();
			_face.Dispose();
			_blob.Dispose();
			_fontInfo?.Dispose();
			_fontInfo = null;
			_fontData = null;
		}
		GC.SuppressFinalize( this );
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

	~TtfFont() {
		if( !_isDisposed ) {
			System.Diagnostics.Debug.Fail( $"Warning: TtfFont was not disposed properly!" );
		}
	}
}
