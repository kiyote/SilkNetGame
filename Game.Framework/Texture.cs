using Silk.NET.OpenGL;
using StbImageSharp;

namespace Game.Framework;

public sealed class Texture : IDisposable {
	private readonly GL _gl;
	private readonly uint _width;
	private readonly uint _height;

	private uint _texture;
	private bool _isDisposed = false;

	internal Texture(
		GL gl,
		string textureFile,
		bool premultiplyAlpha = true
	) {
		_gl = gl;

		ImageResult image = ImageLoader.Load(
			textureFile,
			premultiplyAlpha
		);

		_width = (uint)image.Width;
		_height = (uint)image.Height;

		_texture = _gl.GenTexture();
		_gl.ActiveTexture( TextureUnit.Texture0 );
		_gl.BindTexture( TextureTarget.Texture2D, _texture );

		unsafe {
			fixed( byte* ptr = image.Data ) {
				_gl.TexImage2D(
					TextureTarget.Texture2D,
					0,
					InternalFormat.Rgba,
					(uint)image.Width,
					(uint)image.Height,
					0,
					PixelFormat.Rgba,
					PixelType.UnsignedByte,
					ptr
				);
			}
		}

		_gl.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat );
		_gl.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat );
		_gl.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest );
		_gl.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest );

		_gl.BindTexture( TextureTarget.Texture2D, 0 );
	}

	internal Texture(
		GL gl,
		uint width,
		uint height
	) {
		uint texture = gl.GenTexture();

		gl.ActiveTexture( TextureUnit.Texture0 );
		gl.BindTexture( TextureTarget.Texture2D, texture );

		unsafe {
			gl.TexImage2D(
				TextureTarget.Texture2D,
				0,
				InternalFormat.Rgba8,
				width,
				height,
				0,
				PixelFormat.Rgba,
				PixelType.UnsignedByte,
				null
			);
		}

		gl.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge );
		gl.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge );
		gl.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest );
		gl.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest );

		gl.BindTexture( TextureTarget.Texture2D, 0 );

		_gl = gl;
		_texture = texture;

	}

	internal uint Id => _texture;

	public uint Width => _width;

	public uint Height => _height;

	internal void Bind(
		int textureUnit
	) {
		if( !_isDisposed ) {
			_gl.ActiveTexture( TextureUnit.Texture0 + textureUnit );
			_gl.BindTexture( TextureTarget.Texture2D, _texture );
		} else {
			throw new ObjectDisposedException( nameof( Texture ) );
		}
	}

	public void Dispose() {
		if( !_isDisposed ) {
			if( _texture != 0 ) {
				_gl.DeleteTexture( _texture );
				_texture = 0;
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
