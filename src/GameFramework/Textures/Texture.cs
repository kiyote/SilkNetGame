using System.Globalization;
using Silk.NET.OpenGL;
using StbImageSharp;

namespace GameFramework.Textures;

internal sealed class Texture : ITexture {
	private readonly GL _gl;
	private readonly int _width;
	private readonly int _height;

	private readonly uint _id;
	private readonly string _name;
	private bool _isDisposed;

	private readonly float _halfX;
	private readonly float _halfY;

	private readonly TextureFilter _filter;

	internal Texture(
		GL gl,
		string textureFile,
		bool premultiplyAlpha = true,
		TextureFilter filter = TextureFilter.Linear
	) {
		if( filter == TextureFilter.Unknown ) {
			throw new ArgumentException( "Specify a value texture filter", nameof( filter ) );
		}
		_filter = filter;
		_gl = gl;

		ImageResult image = ImageLoader.Load(
			textureFile,
			premultiplyAlpha
		);

		_width = image.Width;
		_height = image.Height;
		_halfX = 0.5f / _width;
		_halfY = 0.5f / _height;

		_id = _gl.GenTexture();
		_name = _id.ToString( CultureInfo.InvariantCulture );
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
		_filter = filter;

		_width = width;
		_height = height;
		_halfX = 0.5f / _width;
		_halfY = 0.5f / _height;
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
		_name = _id.ToString( CultureInfo.InvariantCulture );

	}

	uint ITexture.Id => _id;

	int ITexture.TextureWidth => _width;

	int ITexture.TextureHeight => _height;

	string ISubTexture.Name => _name;

	int ISubTexture.Left => 0;

	int ISubTexture.Top => 0;

	int ISubTexture.Width => _width;

	int ISubTexture.Height => _height;

	int ISubTexture.AllocatedWidth => _width;

	int ISubTexture.AllocatedHeight => _height;

	float ISubTexture.U1 => 0.0f;

	float ISubTexture.V1 => 0.0f;

	float ISubTexture.U2 => 1.0f;

	float ISubTexture.V2 => 1.0f;

	float ITexture.HalfX => _halfX;

	float ITexture.HalfY => _halfY;

	TextureFilter ITexture.Filter => _filter;

	ITexture ISubTexture.Texture => this;

	public ITextureAtlas CreateAtlas() {
		return new TextureAtlas( this );
	}

	public ISubTexture CreateSubTexture(
		string name,
		int left,
		int top,
		int width,
		int height
	) {
		return new SubTexture( name, this, left, top, width, height, _width, _height );
	}

	public void Copy(
		int x,
		int y,
		ITexture source,
		int sourceX,
		int sourceY,
		int sourceWidth,
		int sourceHeight
	) {
		_gl.CopyImageSubData(
			srcName: source.Id,          // ID of the source texture
			srcTarget: (GLEnum)TextureTarget.Texture2D, // Source target type
			srcLevel: 0,                       // Source mipmap level
			srcX: sourceX,                          // Starting X coordinate in source
			srcY: sourceY,                          // Starting Y coordinate in source
			srcZ: 0,                           // Starting Z coordinate (0 for 2D)

			dstName: _id,            // ID of the destination texture
			dstTarget: (GLEnum)TextureTarget.Texture2D, // Destination target type
			dstLevel: 0,                       // Destination mipmap level
			dstX: x,                         // Insertion X coordinate in destination
			dstY: y,                         // Insertion Y coordinate in destination
			dstZ: 0,                           // Insertion Z coordinate (0 for 2D)

			srcWidth: (uint)sourceWidth,                     // Width of the portion to copy
			srcHeight: (uint)sourceHeight,                    // Height of the portion to copy
			srcDepth: 1                        // Depth of the portion to copy (1 for 2D)
		);
	}

	void ITexture.Bind(
		int textureUnit
	) {
		if( !_isDisposed ) {
			_gl.ActiveTexture( (TextureUnit)( (int)TextureUnit.Texture0 + textureUnit ) );
			_gl.BindTexture( TextureTarget.Texture2D, _id );
		} else {
			throw new ObjectDisposedException( nameof( Texture ) );
		}
	}

	void IDisposable.Dispose() {
		if( !_isDisposed ) {
			if( _id != 0 ) {
				_gl.DeleteTexture( _id );
			}
			_isDisposed = true;
		}
		GC.SuppressFinalize( this );
	}

	void ISubTexture.Update(
		int left,
		int top,
		int width,
		int height
	) {
		// Do nothing, as this is a full texture and cannot be updated like a subtexture.
	}

	bool IEquatable<ISubTexture>.Equals( ISubTexture? other ) {
		if( other is null ) {
			return false;
		}
		return other.Name == _name && other.Left == 0 && other.Top == 0 && other.Width == _width && other.Height == _height;
	}

	~Texture() {
		if( !_isDisposed ) {
			System.Diagnostics.Debug.Fail( $"Warning: Texture was not disposed properly!" );
		}
	}
}
