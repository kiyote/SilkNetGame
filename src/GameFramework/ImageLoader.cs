using System.IO;
using StbImageSharp;

namespace GameFramework;

internal static class ImageLoader {

	public static ImageResult Load(
		string filePath,
		bool premultiplyAlpha
	) {
		string resolvedPath = ResolveAssetPath( filePath );

		if( !premultiplyAlpha ) {
			using FileStream stream = File.OpenRead( resolvedPath );
			ImageResult image = ImageResult.FromStream( stream, ColorComponents.RedGreenBlueAlpha );
			return image;
		}

		return LoadPremultiplied( resolvedPath );
	}

	private static ImageResult LoadPremultiplied( string filePath ) {
		string resolvedPath = ResolveAssetPath( filePath );

		// 1. Force StbImageSharp to load the image as 4-channel RGBA
		using FileStream stream = File.OpenRead( resolvedPath );
		ImageResult image = ImageResult.FromStream( stream, ColorComponents.RedGreenBlueAlpha );

		// 2. Safely obtain a span or reference to the raw byte data
		byte[] pixels = image.Data;

		// 3. Loop through every pixel (4 bytes per pixel: R, G, B, A)
		for( int i = 0; i < pixels.Length; i += 4 ) {
			byte alpha = pixels[i + 3];

			// Skip math entirely if the pixel is fully opaque (255)
			if( alpha < 255 ) {
				// Convert alpha to a float multiplier between 0.0 and 1.0
				float alphaMultiplier = alpha / 255f;

				// Multiply the color channels by the alpha factor
				pixels[i] = (byte)( pixels[i] * alphaMultiplier ); // Red
				pixels[i + 1] = (byte)( pixels[i + 1] * alphaMultiplier ); // Green
				pixels[i + 2] = (byte)( pixels[i + 2] * alphaMultiplier ); // Blue
			}
		}

		return image;
	}

	public static string ResolveAssetPath( string filePath ) {
		if( Path.IsPathRooted( filePath ) ) {
			return filePath;
		}

		string baseCandidate = Path.Combine( AppContext.BaseDirectory, filePath );
		if( File.Exists( baseCandidate ) ) {
			return baseCandidate;
		}

		if( File.Exists( filePath ) ) {
			return Path.GetFullPath( filePath );
		}

		return filePath;
	}
}
