using GameFramework.Fonts;

namespace GameFramework.Textures;

public interface ITextureAtlas : IDisposable {
	ISubTexture Create( string id, IFont font, ReadOnlySpan<byte> text, uint colour = uint.MaxValue );
	ISubTexture Create( string id, IFont font, string text, uint colour = uint.MaxValue );
	ISubTexture Create( string id, IFont font, ReadOnlySpan<byte> text, uint textColour = uint.MaxValue, uint outlineColour = 255, int outlineWidth = 1 );
	ISubTexture Create( string id, ITexture source, Coordinate sourcePosition, Dimension sourceSize );

	void Update( string id, IFont font, ReadOnlySpan<byte> text, uint colour = uint.MaxValue );
	void Update( string id, IFont font, string text, uint colour = uint.MaxValue );
	void Update( string id, IFont font, ReadOnlySpan<byte> text, uint textColour = uint.MaxValue, uint outlineColour = 255, int outlineWidth = 1 );
	void Update( string id, IFont font, string text, uint textColour = uint.MaxValue, uint outlineColour = 255, int outlineWidth = 1 );
	void Update( string id, ITexture source, Coordinate sourcePosition, Dimension sourceSize );

	ITexture Texture { get; }

}
