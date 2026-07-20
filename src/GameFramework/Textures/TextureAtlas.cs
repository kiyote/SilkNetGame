
using GameFramework.Fonts;
using StbRectPackSharp;

namespace GameFramework.Textures;

internal sealed class TextureAtlas : ITextureAtlas {

	public const int GutterSize = 2;

	private readonly Packer _packer;
	private readonly Dictionary<string, SubTexture> _registry;

	private readonly ITexture _texture;

	private bool _isDisposed;

	public TextureAtlas(
		ITexture texture
	) {
		_texture = texture;
		_packer = new Packer(
			texture.TextureSize.Width,
			texture.TextureSize.Height
		);
		_registry = [];
	}

	ITexture ITextureAtlas.Texture => _texture;

	public ISubTexture Create(
		string id,
		IFont font,
		ReadOnlySpan<byte> text,
		uint colour = uint.MaxValue
	) {
		Dimension size = font.MeasureText( text, 0 );
		SubTexture subTexture = Insert( id, size );
		font.DrawText( _texture, text, subTexture.Position, colour );
		ExtrudeGutter( subTexture );

		return subTexture;
	}

	public ISubTexture Create(
		string id,
		IFont font,
		string text,
		uint colour = uint.MaxValue
	) {
		Dimension size = font.MeasureText( text, 0 );
		SubTexture subTexture = Insert( id, size );
		font.DrawText( _texture, text, subTexture.Position, colour );
		ExtrudeGutter( subTexture );

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
		Dimension size = font.MeasureText( text, outlineWidth );
		SubTexture subTexture = Insert( id, size );
		font.DrawOutlinedText( _texture, text, subTexture.Position, textColour, outlineColour, outlineWidth );
		ExtrudeGutter( subTexture );

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
		Dimension size = font.MeasureText( text, outlineWidth );
		SubTexture subTexture = Insert( id, size );
		font.DrawOutlinedText( _texture, text, subTexture.Position, textColour, outlineColour, outlineWidth );
		ExtrudeGutter( subTexture );

		return subTexture;
	}

	public ISubTexture Create(
		string id,
		ITexture source,
		Coordinate sourcePosition,
		Dimension sourceSize
	) {
		SubTexture subTexture = Insert( id, sourceSize );
		_texture.Copy( subTexture.Position, source, sourcePosition, sourceSize );
		ExtrudeGutter( subTexture );
		return subTexture;
	}

	public void Update(
		string id,
		IFont font,
		ReadOnlySpan<byte> text,
		uint colour = uint.MaxValue
	) {
		Dimension size = font.MeasureText( text, 0 );
		SubTexture subTexture = _registry[id];
		PrepareUpdate( subTexture, size );
		font.DrawText( _texture, text, subTexture.Position, colour );
		ExtrudeGutter( subTexture );
	}

	public void Update(
		string id,
		IFont font,
		string text,
		uint colour = uint.MaxValue
	) {
		Dimension size = font.MeasureText( text, 0 );
		SubTexture subTexture = _registry[id];
		PrepareUpdate( subTexture, size );
		font.DrawText( _texture, text, subTexture.Position, colour );
		ExtrudeGutter( subTexture );
	}

	public void Update(
		string id,
		IFont font,
		ReadOnlySpan<byte> text,
		uint textColour = uint.MaxValue,
		uint outlineColour = 255,
		int outlineWidth = 1
	) {
		Dimension size = font.MeasureText( text, outlineWidth );
		SubTexture subTexture = _registry[id];
		PrepareUpdate( subTexture, size );
		font.DrawOutlinedText( _texture, text, subTexture.Position, textColour, outlineColour, outlineWidth );
		ExtrudeGutter( subTexture );
	}

	public void Update(
		string id,
		IFont font,
		string text,
		uint textColour = uint.MaxValue,
		uint outlineColour = 255,
		int outlineWidth = 1
	) {
		Dimension size = font.MeasureText( text, outlineWidth );
		SubTexture subTexture = _registry[id];
		PrepareUpdate( subTexture, size );
		font.DrawOutlinedText( _texture, text, subTexture.Position, textColour, outlineColour, outlineWidth );
		ExtrudeGutter( subTexture );
	}

	public void Update(
		string id,
		ITexture source,
		Coordinate sourcePosition,
		Dimension sourceSize
	) {
		SubTexture subTexture = _registry[id];
		PrepareUpdate( subTexture, sourceSize );
		_texture.Copy( subTexture.Position, source, sourcePosition, sourceSize );
		ExtrudeGutter( subTexture );
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
		Dimension size
	) {
		if( _registry.TryGetValue( name, out SubTexture? cached ) ) {
			return cached;
		}

		Dimension storedSize = new(
			size.Width + ( GutterSize * 2 ),
			size.Height + ( GutterSize * 2 )
		);
		PackerRectangle result = AllocateSpace( storedSize );

		SubTexture subTexture = new SubTexture(
			name,
			_texture,
			new Coordinate( result.X + GutterSize, result.Y + GutterSize ),
			size,
			new Coordinate( result.X, result.Y ),
			new Dimension( result.Width, result.Height )
		);

		_registry.Add( name, subTexture );

		return subTexture;
	}

	private static void EnsureFitsStoredArea(
		SubTexture subTexture,
		Dimension size
	) {
		Dimension usableSize = new(
			subTexture.StoredSize.Width - ( GutterSize * 2 ),
			subTexture.StoredSize.Height - ( GutterSize * 2 )
		);
		if( size.Width > usableSize.Width || size.Height > usableSize.Height ) {
			throw new InvalidOperationException( "Updated texture dimensions exceed the stored texture area." );
		}
	}

	private void PrepareUpdate(
		SubTexture subTexture,
		Dimension size
	) {
		EnsureFitsStoredArea( subTexture, size );
		_texture.Clear( subTexture.StoredPosition, subTexture.StoredSize, 0 );
		subTexture.Update( subTexture.Position, size );
	}

	private void ExtrudeGutter(
		SubTexture subTexture
	) {
		if( _texture.Filter == TextureFilter.Nearest || subTexture.Size.Width <= 0 || subTexture.Size.Height <= 0 ) {
			return;
		}

		for( int offset = 1; offset <= GutterSize; offset++ ) {
			_texture.Copy( new Coordinate( subTexture.Position.X - offset, subTexture.Position.Y ), _texture, subTexture.Position, new Dimension( 1, subTexture.Size.Height ) );
			_texture.Copy( new Coordinate( subTexture.Position.X + subTexture.Size.Width - 1 + offset, subTexture.Position.Y ), _texture, new Coordinate( subTexture.Position.X + subTexture.Size.Width - 1, subTexture.Position.Y ), new Dimension( 1, subTexture.Size.Height ) );
		}

		Dimension extrudedRowSize = new( subTexture.Size.Width + ( GutterSize * 2 ), 1 );
		for( int offset = 1; offset <= GutterSize; offset++ ) {
			_texture.Copy( new Coordinate( subTexture.StoredPosition.X, subTexture.Position.Y - offset ), _texture, new Coordinate( subTexture.StoredPosition.X, subTexture.Position.Y ), extrudedRowSize );
			_texture.Copy( new Coordinate( subTexture.StoredPosition.X, subTexture.Position.Y + subTexture.Size.Height - 1 + offset ), _texture, new Coordinate( subTexture.StoredPosition.X, subTexture.Position.Y + subTexture.Size.Height - 1 ), extrudedRowSize );
		}
	}

	private PackerRectangle AllocateSpace(
		Dimension size
	) {
		PackerRectangle result = _packer.PackRect( size.Width, size.Height, null );
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
