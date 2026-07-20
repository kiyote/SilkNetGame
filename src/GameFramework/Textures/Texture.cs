using System.Globalization;
using Silk.NET.OpenGL;
using StbImageSharp;

namespace GameFramework.Textures;

internal sealed class Texture : ITexture {
	private readonly GL _gl;
	private readonly GlStateCache _stateCache;
	private readonly Dimension _size;

	private readonly uint _id;
	private readonly string _name;
	private bool _isDisposed;

	private readonly float _halfX;
	private readonly float _halfY;

	private readonly TextureFilter _filter;

	internal Texture(
		GL gl,
		GlStateCache stateCache,
		string textureFile,
		bool premultiplyAlpha = true,
		TextureFilter filter = TextureFilter.Linear
	) {
		if( filter == TextureFilter.Unknown ) {
			throw new ArgumentException( "Specify a value texture filter", nameof( filter ) );
		}
		_filter = filter;
		_gl = gl;
		_stateCache = stateCache;

		ImageResult image = ImageLoader.Load(
			textureFile,
			premultiplyAlpha
		);

		_size = new Dimension( image.Width, image.Height );
		_halfX = 0.5f / _size.Width;
		_halfY = 0.5f / _size.Height;

		_id = _gl.GenTexture();
		_name = _id.ToString( CultureInfo.InvariantCulture );
		_stateCache.BindTexture( 0, _id );
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

		_stateCache.BindTexture( 0, 0 );
	}

	internal Texture(
		GL gl,
		GlStateCache stateCache,
		Dimension size,
		TextureFilter filter
	) {
		if( filter == TextureFilter.Unknown ) {
			throw new ArgumentException( "Specify a value texture filter", nameof( filter ) );
		}
		_filter = filter;

		_size = size;
		_halfX = 0.5f / size.Width;
		_halfY = 0.5f / size.Height;
		uint texture = gl.GenTexture();

		stateCache.BindTexture( 0, texture );

		unsafe {
			gl.TexImage2D(
				TextureTarget.Texture2D,
				0,
				InternalFormat.Rgba8,
					(uint)size.Width,
					(uint)size.Height,
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

		stateCache.BindTexture( 0, 0 );

		_gl = gl;
		_stateCache = stateCache;
		_id = texture;
		_name = _id.ToString( CultureInfo.InvariantCulture );

	}

	uint ITexture.Id => _id;

	Dimension ITexture.TextureSize => _size;

	string ISubTexture.Name => _name;

	Coordinate ISubTexture.Position => default;

	Dimension ISubTexture.Size => _size;

	Coordinate ISubTexture.StoredPosition => default;

	Dimension ISubTexture.StoredSize => _size;

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
		Coordinate position,
		Dimension size
	) {
		return new SubTexture( name, this, position, size, position, size );
	}

	public void Clear(
		Coordinate position,
		Dimension size,
		uint colour = 0
	) {
		ObjectDisposedException.ThrowIf( _isDisposed, this );
		ArgumentOutOfRangeException.ThrowIfNegative( position.X );
		ArgumentOutOfRangeException.ThrowIfNegative( position.Y );
		ArgumentOutOfRangeException.ThrowIfNegative( size.Width );
		ArgumentOutOfRangeException.ThrowIfNegative( size.Height );

		if( position.X > _size.Width - size.Width || position.Y > _size.Height - size.Height ) {
			throw new ArgumentException( "The clear area exceeds the texture bounds." );
		}
		if( size.Width == 0 || size.Height == 0 ) {
			return;
		}

		unsafe {
			byte* clearColour = stackalloc byte[4];
			clearColour[0] = (byte)( colour >> 24 );
			clearColour[1] = (byte)( colour >> 16 );
			clearColour[2] = (byte)( colour >> 8 );
			clearColour[3] = (byte)colour;

			_gl.ClearTexSubImage(
				_id,
				0,
				position.X,
				position.Y,
				0,
				(uint)size.Width,
				(uint)size.Height,
				1,
				PixelFormat.Rgba,
				PixelType.UnsignedByte,
				clearColour
			);
		}
	}

	public void Copy(
		Coordinate position,
		ITexture source,
		Coordinate sourcePosition,
		Dimension sourceSize
	) {
		_gl.CopyImageSubData(
			srcName: source.Id,          // ID of the source texture
			srcTarget: (GLEnum)TextureTarget.Texture2D, // Source target type
			srcLevel: 0,                       // Source mipmap level
			srcX: sourcePosition.X,                          // Starting X coordinate in source
			srcY: sourcePosition.Y,                          // Starting Y coordinate in source
			srcZ: 0,                           // Starting Z coordinate (0 for 2D)

			dstName: _id,            // ID of the destination texture
			dstTarget: (GLEnum)TextureTarget.Texture2D, // Destination target type
			dstLevel: 0,                       // Destination mipmap level
			dstX: position.X,                         // Insertion X coordinate in destination
			dstY: position.Y,                         // Insertion Y coordinate in destination
			dstZ: 0,                           // Insertion Z coordinate (0 for 2D)

			srcWidth: (uint)sourceSize.Width,                     // Width of the portion to copy
			srcHeight: (uint)sourceSize.Height,                    // Height of the portion to copy
			srcDepth: 1                        // Depth of the portion to copy (1 for 2D)
		);
	}

	void ITexture.Bind(
		int textureUnit
	) {
		if( !_isDisposed ) {
			_stateCache.BindTexture( textureUnit, _id );
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
		Coordinate position,
		Dimension size
	) {
		// Do nothing, as this is a full texture and cannot be updated like a subtexture.
	}

	bool IEquatable<ISubTexture>.Equals( ISubTexture? other ) {
		if( other is null ) {
			return false;
		}
		return other.Name == _name && other.Position == default && other.Size == _size;
	}

	~Texture() {
		if( !_isDisposed ) {
			System.Diagnostics.Debug.Fail( $"Warning: Texture was not disposed properly!" );
		}
	}
}
