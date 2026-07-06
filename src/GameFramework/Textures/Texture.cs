using Silk.NET.OpenGL;
using StbImageSharp;

namespace GameFramework.Textures;

internal sealed class Texture : ITexture {
	private readonly GL _gl;
	private readonly int _width;
	private readonly int _height;

	private uint _id;
	private bool _isDisposed;

	internal Texture(
		GL gl,
		string textureFile,
		bool premultiplyAlpha = true,
		TextureFilter filter = TextureFilter.Linear
	) {
		if (filter == TextureFilter.Unknown) {
			throw new ArgumentException( "Specify a value texture filter", nameof( filter ) );
		}
		_gl = gl;

		ImageResult image = ImageLoader.Load(
			textureFile,
			premultiplyAlpha
		);

		_width = image.Width;
		_height = image.Height;

		_id = _gl.GenTexture();
		_gl.ActiveTexture( TextureUnit.Texture0 );
		_gl.BindTexture( TextureTarget.Texture2D, _id );
		_gl.PixelStore( PixelStoreParameter.UnpackAlignment, 1 );

		unsafe {
			fixed( byte* ptr = image.Data ) {
				_gl.TexImage2D(
					TextureTarget.Texture2D,
					0,
					InternalFormat.Rgba8,
					(uint)image.Width,
					(uint)image.Height,
					0,
					PixelFormat.Rgba,
					PixelType.UnsignedByte,
					ptr
				);
			}
		}

		_gl.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge );
		_gl.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge );
		_gl.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)filter );
		_gl.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)filter );

		_gl.BindTexture( TextureTarget.Texture2D, 0 );	
	}

	internal Texture(
		GL gl,
		int width,
		int height,
		TextureFilter filter
	) {
		if( filter == TextureFilter.Unknown ) {
			throw new ArgumentException( "Specify a value texture filter", nameof( filter ) );
		}

		_width = width;
		_height = height;
		uint texture = gl.GenTexture();

		gl.ActiveTexture( TextureUnit.Texture0 );
		gl.BindTexture( TextureTarget.Texture2D, texture );

		unsafe {
			gl.TexImage2D(
				TextureTarget.Texture2D,
				0,
				InternalFormat.Rgba8,
				(uint)width,
				(uint)height,
				0,
				PixelFormat.Rgba,
				PixelType.UnsignedByte,
				null
			);
		}

		gl.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge );
		gl.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge );
		gl.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)filter );
		gl.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)filter );

		gl.BindTexture( TextureTarget.Texture2D, 0 );

		_gl = gl;
		_id = texture;

	}

	uint ITexture.Id => _id;

	int ITexture.TextureWidth => _width;

	int ITexture.TextureHeight => _height;

	void ITexture.Bind(
		int textureUnit
	) {
		if( !_isDisposed ) {
			_gl.ActiveTexture( (TextureUnit)((int)TextureUnit.Texture0 + textureUnit) );
			_gl.BindTexture( TextureTarget.Texture2D, _id );
		} else {
			throw new ObjectDisposedException( nameof( Texture ) );
		}
	}

	void IDisposable.Dispose() {
		if( !_isDisposed ) {
			if( _id != 0 ) {
				_gl.DeleteTexture( _id );
				_id = 0;
			}
			_isDisposed = true;
		}
		GC.SuppressFinalize( this );
	}

	~Texture() {
		if( !_isDisposed ) {
			System.Diagnostics.Debug.Fail( $"Warning: Texture was not disposed properly!" );
		}
	}
}
