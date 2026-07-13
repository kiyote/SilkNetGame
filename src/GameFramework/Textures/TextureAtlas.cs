
using GameFramework.Fonts;
using StbRectPackSharp;

namespace GameFramework.Textures;


internal sealed class TextureAtlas : ITextureAtlas {

	public const int MinBlockSize = 48;

	private readonly Packer _packer;
	private readonly Dictionary<string, SubTexture> _registry;

	private readonly ITexture _texture;

	private bool _isDisposed;

	public TextureAtlas(
		ITexture texture
	) {
		_texture = texture;
		_packer = new Packer(
			texture.TextureWidth,
			texture.TextureHeight
		);
		_registry = [];
	}

	public ISubTexture Create(
		string id,
		IFont font,
		ReadOnlySpan<byte> text,
		uint colour = uint.MaxValue
	) {
		font.MeasureText( text, 0, out int width, out int height );
		SubTexture subTexture = Insert( id, width, height );
		font.DrawText( _texture, text, subTexture.Left, subTexture.Top, colour );

		return subTexture;
	}

	public ISubTexture Create(
		string id,
		IFont font,
		string text,
		uint colour = uint.MaxValue
	) {
		font.MeasureText( text, 0, out int width, out int height );
		SubTexture subTexture = Insert( id, width, height );
		font.DrawText( _texture, text, subTexture.Left, subTexture.Top, colour );

		return subTexture;
	}

	public ISubTexture Create(
		string id,
		IFont font,
		ReadOnlySpan<byte> text,
		uint textColour = uint.MaxValue,
		uint outlineColour = 255,
		int outlineWidth = 1
	) {
		font.MeasureText( text, outlineWidth, out int width, out int height );
		ISubTexture subTexture = Insert( id, width, height );
		font.DrawOutlinedText( _texture, text, subTexture.Left, subTexture.Top, textColour, outlineColour, outlineWidth );

		return subTexture;
	}

	public ISubTexture Create(
		string id,
		IFont font,
		string text,
		uint textColour = uint.MaxValue,
		uint outlineColour = 255,
		int outlineWidth = 1
	) {
		font.MeasureText( text, outlineWidth, out int width, out int height );
		ISubTexture subTexture = Insert( id, width, height );
		font.DrawOutlinedText( _texture, text, subTexture.Left, subTexture.Top, textColour, outlineColour, outlineWidth );

		return subTexture;
	}

	public ISubTexture Create(
		string id,
		ITexture source,
		int sourceX,
		int sourceY,
		int sourceWidth,
		int sourceHeight
	) {
		ISubTexture subTexture = Insert( id, sourceWidth, sourceHeight );
		_texture.Copy( subTexture.Left, subTexture.Top, source, sourceX, sourceY, sourceWidth, sourceHeight );
		return subTexture;
	}

	public void Update(
		string id,
		IFont font,
		ReadOnlySpan<byte> text,
		uint colour = uint.MaxValue
	) {
		font.MeasureText( text, 0, out int width, out int height );
		SubTexture subTexture = _registry[id];
		if( width > subTexture.AllocatedWidth || height > subTexture.AllocatedHeight ) {
			throw new InvalidOperationException( "Updated text dimensions exceed the allocated sprite dimensions." );
		}
		subTexture.Update( subTexture.Left, subTexture.Top, width, height );
		font.DrawText( _texture, text, subTexture.Left, subTexture.Top, colour );
	}

	public void Update(
		string id,
		IFont font,
		string text,
		uint colour = uint.MaxValue
	) {
		font.MeasureText( text, 0, out int width, out int height );
		SubTexture subTexture = _registry[id];
		if( width > subTexture.AllocatedWidth || height > subTexture.AllocatedHeight ) {
			throw new InvalidOperationException( "Updated text dimensions exceed the allocated sprite dimensions." );
		}
		subTexture.Update( subTexture.Left, subTexture.Top, width, height );
		font.DrawText( _texture, text, subTexture.Left, subTexture.Top, colour );
	}

	public void Update(
		string id,
		IFont font,
		ReadOnlySpan<byte> text,
		uint textColour = uint.MaxValue,
		uint outlineColour = 255,
		int outlineWidth = 1
	) {
		font.MeasureText( text, outlineWidth, out int width, out int height );
		SubTexture subTexture = _registry[id];
		if( width > subTexture.AllocatedWidth || height > subTexture.AllocatedHeight ) {
			throw new InvalidOperationException( "Updated text dimensions exceed the allocated sprite dimensions." );
		}

		font.DrawOutlinedText( _texture, text, subTexture.Left, subTexture.Top, textColour, outlineColour, outlineWidth );
	}

	public void Update(
		string id,
		IFont font,
		string text,
		uint textColour = uint.MaxValue,
		uint outlineColour = 255,
		int outlineWidth = 1
	) {
		font.MeasureText( text, outlineWidth, out int width, out int height );
		SubTexture subTexture = _registry[id];
		if( width > subTexture.AllocatedWidth || height > subTexture.AllocatedHeight ) {
			throw new InvalidOperationException( "Updated text dimensions exceed the allocated sprite dimensions." );
		}

		font.DrawOutlinedText( _texture, text, subTexture.Left, subTexture.Top, textColour, outlineColour, outlineWidth );
	}

	public void Update(
		string id,
		ITexture source,
		int sourceX,
		int sourceY,
		int sourceW,
		int sourceH
	) {
		throw new NotImplementedException();
	}

	public void Dispose() {
		if( !_isDisposed ) {
			_isDisposed = true;
			_registry.Clear();
			_packer.Dispose();
			_texture.Dispose();
		}
		GC.SuppressFinalize( this );
	}

	private SubTexture Insert(
		string name,
		int width,
		int height
	) {
		if( _registry.TryGetValue( name, out SubTexture? cached ) ) {
			return cached;
		}

		// Quantize size to reduce fragmentation
		int allocatedWidth = ( width + MinBlockSize - 1 ) / MinBlockSize * MinBlockSize;
		int allocatedHeight = ( height + MinBlockSize - 1 ) / MinBlockSize * MinBlockSize;

		PackerRectangle result = AllocateSpace( allocatedWidth, allocatedHeight );

		SubTexture subTexture = new SubTexture(
			name,
			_texture,
			result.X,
			result.Y,
			width,
			height,
			result.Width,
			result.Height
		);

		_registry.Add( name, subTexture );

		return subTexture;
	}

	private PackerRectangle AllocateSpace(
		int width,
		int height
	) {
		PackerRectangle result = _packer.PackRect( width, height, null );
		if( result != null ) {
			return result;
		}

		throw new InvalidOperationException( "Texture is too large to fit in remaining atlas space." );
	}

	~TextureAtlas() {
		if( !_isDisposed ) {
			System.Diagnostics.Debug.Fail( $"Warning: TextureAtlas was not disposed properly!" );
		}
	}
}
