using GameFramework.Sprites;

namespace GameFramework.Textures;

public interface INinePatch {
	string Name { get; }

	// Renders this nine-patch into the target rectangle using the given fill mode,
	// issuing draws through the supplied sprite batch. A holder needs only this
	// nine-patch and a sprite batch to render it.
	void Draw( ISpriteBatch spriteBatch, float x, float y, float width, float height, uint colour = 0xFFFFFFFF, NinePatchFill fill = NinePatchFill.Tile, BlendMode blendMode = BlendMode.Premultiplied );

	void Draw( ISpriteBatch spriteBatch, Coordinate position, Dimension size, uint colour = 0xFFFFFFFF, NinePatchFill fill = NinePatchFill.Tile, BlendMode blendMode = BlendMode.Premultiplied );
}
