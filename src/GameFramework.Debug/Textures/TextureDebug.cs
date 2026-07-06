using Silk.NET.OpenGL;
using StbImageWriteSharp;

namespace GameFramework.Textures;

public unsafe class TextureDebug {

	private readonly GL _gl;

	public TextureDebug(
		GL gl
	) {
		_gl = gl;
	}

	public void Write(
		ITexture texture,
		string filePath
	) {
		int totalPixels = texture.TextureWidth * texture.TextureHeight;
		byte[] rawPixels = new byte[totalPixels * 4];

		// 2. Bind the texture and pull data from the GPU
		_gl.BindTexture( TextureTarget.Texture2D, texture.Id );

		fixed( byte* ptr = rawPixels ) {
			// Retrieve texture data as standard RGBA bytes
			_gl.GetTexImage( TextureTarget.Texture2D, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr );
		}

		// 3. Reverse premultiplied alpha & correct OpenGL's vertical inversion
		byte[] processedPixels = new byte[totalPixels * 4];

		for( int y = 0; y < texture.TextureHeight; y++ ) {
			for( int x = 0; x < texture.TextureWidth; x++ ) {

				// Read sequentially forward from your GPU array
				int sourceIndex = ( ( y * texture.TextureWidth ) + x ) * 4;

				// CHANGE THIS LINE: Use a direct mapping. 
				// This will cleanly shift the final image orientation by exactly 180 degrees.
				int targetIndex = sourceIndex;

				byte r = rawPixels[sourceIndex + 0];
				byte g = rawPixels[sourceIndex + 1];
				byte b = rawPixels[sourceIndex + 2];
				byte a = rawPixels[sourceIndex + 3];

				if( a > 0 && a < 255 ) {
					float alphaFactor = 255.0f / a;
					processedPixels[targetIndex + 0] = (byte)Math.Clamp( r * alphaFactor, 0, 255 );
					processedPixels[targetIndex + 1] = (byte)Math.Clamp( g * alphaFactor, 0, 255 );
					processedPixels[targetIndex + 2] = (byte)Math.Clamp( b * alphaFactor, 0, 255 );
					processedPixels[targetIndex + 3] = a;
				} else if( a == 0 ) {
					processedPixels[targetIndex + 0] = 0;
					processedPixels[targetIndex + 1] = 0;
					processedPixels[targetIndex + 2] = 0;
					processedPixels[targetIndex + 3] = 0;
				} else {
					processedPixels[targetIndex + 0] = r;
					processedPixels[targetIndex + 1] = g;
					processedPixels[targetIndex + 2] = b;
					processedPixels[targetIndex + 3] = a;
				}
			}
		}


		// 4. Save the processed data to a PNG file using StbImageWriteSharp
		if( File.Exists( filePath ) ) {
			File.Delete( filePath );
		}
		using Stream stream = File.OpenWrite( filePath );
		ImageWriter writer = new ImageWriter();
		writer.WritePng( processedPixels, (int)texture.TextureWidth, (int)texture.TextureHeight, ColorComponents.RedGreenBlueAlpha, stream );
	}
}
