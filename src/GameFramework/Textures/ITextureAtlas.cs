using GameFramework.Fonts;

namespace GameFramework.Textures;

public interface ITextureAtlas : IDisposable {
	ISubTexture Create( string id, IFont font, ReadOnlySpan<byte> text, uint colour = uint.MaxValue );
	ISubTexture Create( string id, IFont font, string text, uint colour = uint.MaxValue );
	ISubTexture Create( string id, IFont font, ReadOnlySpan<byte> text, uint textColour = uint.MaxValue, uint outlineColour = 255, int outlineWidth = 1 );
	ISubTexture Create( string id, ITexture source, Coordinate sourcePosition, Dimension sourceSize );

	// Creates a nine-patch named 'id' by slicing nine regions out of 'source'
	// (position/size are pixel coordinates within 'source') and registering each
	// region in this atlas. The returned INinePatch can render itself given a
	// sprite batch.
	INinePatch Create( string id, ITexture source, Coordinate sourcePosition, Dimension sourceSize, int leftBorder, int rightBorder, int topBorder, int bottomBorder );

	void Update( string id, IFont font, ReadOnlySpan<byte> text, uint colour = uint.MaxValue );
	void Update( string id, IFont font, string text, uint colour = uint.MaxValue );
	void Update( string id, IFont font, ReadOnlySpan<byte> text, uint textColour = uint.MaxValue, uint outlineColour = 255, int outlineWidth = 1 );
	void Update( string id, IFont font, string text, uint textColour = uint.MaxValue, uint outlineColour = 255, int outlineWidth = 1 );
	void Update( string id, ITexture source, Coordinate sourcePosition, Dimension sourceSize );

	ITexture Texture { get; }

	ISubTexture SubTexture( string id );

	INinePatch NinePatch( string id );

}
