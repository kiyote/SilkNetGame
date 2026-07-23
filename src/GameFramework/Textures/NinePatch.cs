using GameFramework.Sprites;

namespace GameFramework.Textures;

internal sealed class NinePatch : INinePatch {

	private readonly ITextureAtlas _atlas;

	public NinePatch(
		string name,
		ITextureAtlas atlas,
		string topLeft,
		string topCenter,
		string topRight,
		string middleLeft,
		string middleCenter,
		string middleRight,
		string bottomLeft,
		string bottomCenter,
		string bottomRight
	) {
		Name = name;
		_atlas = atlas;
		TopLeftId = topLeft;
		TopCenterId = topCenter;
		TopRightId = topRight;
		MiddleLeftId = middleLeft;
		MiddleCenterId = middleCenter;
		MiddleRightId = middleRight;
		BottomLeftId = bottomLeft;
		BottomCenterId = bottomCenter;
		BottomRightId = bottomRight;
	}

	public string Name { get; }

	internal string TopLeftId { get; }
	internal string TopCenterId { get; }
	internal string TopRightId { get; }
	internal string MiddleLeftId { get; }
	internal string MiddleCenterId { get; }
	internal string MiddleRightId { get; }
	internal string BottomLeftId { get; }
	internal string BottomCenterId { get; }
	internal string BottomRightId { get; }

	private ISubTexture TopLeft => _atlas.SubTexture( TopLeftId );
	private ISubTexture TopCenter => _atlas.SubTexture( TopCenterId );
	private ISubTexture TopRight => _atlas.SubTexture( TopRightId );
	private ISubTexture MiddleLeft => _atlas.SubTexture( MiddleLeftId );
	private ISubTexture MiddleCenter => _atlas.SubTexture( MiddleCenterId );
	private ISubTexture MiddleRight => _atlas.SubTexture( MiddleRightId );
	private ISubTexture BottomLeft => _atlas.SubTexture( BottomLeftId );
	private ISubTexture BottomCenter => _atlas.SubTexture( BottomCenterId );
	private ISubTexture BottomRight => _atlas.SubTexture( BottomRightId );

	public void Draw(
		ISpriteBatch spriteBatch,
		Coordinate position,
		Dimension size,
		uint colour = 0xFFFFFFFF,
		NinePatchFill fill = NinePatchFill.Tile,
		BlendMode blendMode = BlendMode.Premultiplied
	) => Draw( spriteBatch, position.X, position.Y, size.Width, size.Height, colour, fill, blendMode );

	public void Draw(
		ISpriteBatch spriteBatch,
		float x,
		float y,
		float width,
		float height,
		uint colour = 0xFFFFFFFF,
		NinePatchFill fill = NinePatchFill.Tile,
		BlendMode blendMode = BlendMode.Premultiplied
	) {
		ArgumentNullException.ThrowIfNull( spriteBatch );

		ISubTexture topLeft = TopLeft;
		ISubTexture topRight = TopRight;
		ISubTexture bottomLeft = BottomLeft;

		float leftWidth = topLeft.Size.Width;
		float rightWidth = topRight.Size.Width;
		float topHeight = topLeft.Size.Height;
		float bottomHeight = bottomLeft.Size.Height;

		float centerWidth = width - leftWidth - rightWidth;
		float middleHeight = height - topHeight - bottomHeight;

		float x0 = x;
		float x1 = x + leftWidth;
		float x2 = x + width - rightWidth;
		float y0 = y;
		float y1 = y + topHeight;
		float y2 = y + height - bottomHeight;

		if( fill == NinePatchFill.Stretch ) {
			// Top row
			spriteBatch.Draw( topLeft, x0, y0, leftWidth, topHeight, colour, blendMode );
			spriteBatch.Draw( TopCenter, x1, y0, centerWidth, topHeight, colour, blendMode );
			spriteBatch.Draw( topRight, x2, y0, rightWidth, topHeight, colour, blendMode );

			// Middle row
			spriteBatch.Draw( MiddleLeft, x0, y1, leftWidth, middleHeight, colour, blendMode );
			spriteBatch.Draw( MiddleCenter, x1, y1, centerWidth, middleHeight, colour, blendMode );
			spriteBatch.Draw( MiddleRight, x2, y1, rightWidth, middleHeight, colour, blendMode );

			// Bottom row
			spriteBatch.Draw( bottomLeft, x0, y2, leftWidth, bottomHeight, colour, blendMode );
			spriteBatch.Draw( BottomCenter, x1, y2, centerWidth, bottomHeight, colour, blendMode );
			spriteBatch.Draw( BottomRight, x2, y2, rightWidth, bottomHeight, colour, blendMode );
			return;
		}

		// Repeats a tile across the given region, trimming the overflow of the last
		// tile in each row/column against the region bounds.
		void TileRegion( float rx, float ry, float rw, float rh, ISubTexture tile ) {
			if( rw <= 0.0f || rh <= 0.0f ) {
				return;
			}
			float tileWidth = tile.Size.Width;
			float tileHeight = tile.Size.Height;
			if( tileWidth <= 0.0f || tileHeight <= 0.0f ) {
				return;
			}

			Bounds clip = new Bounds( (int)rx, (int)ry, (int)rw, (int)rh );
			for( float ty = ry; ty < ry + rh; ty += tileHeight ) {
				for( float tx = rx; tx < rx + rw; tx += tileWidth ) {
					spriteBatch.Draw( tile, tx, ty, tileWidth, tileHeight, clip, colour, blendMode );
				}
			}
		}

		// Corners keep their intrinsic size.
		spriteBatch.Draw( topLeft, x0, y0, leftWidth, topHeight, colour, blendMode );
		spriteBatch.Draw( topRight, x2, y0, rightWidth, topHeight, colour, blendMode );
		spriteBatch.Draw( bottomLeft, x0, y2, leftWidth, bottomHeight, colour, blendMode );
		spriteBatch.Draw( BottomRight, x2, y2, rightWidth, bottomHeight, colour, blendMode );

		// Edges tile along their stretch axis.
		TileRegion( x1, y0, centerWidth, topHeight, TopCenter );
		TileRegion( x1, y2, centerWidth, bottomHeight, BottomCenter );
		TileRegion( x0, y1, leftWidth, middleHeight, MiddleLeft );
		TileRegion( x2, y1, rightWidth, middleHeight, MiddleRight );

		// Center tiles across both axes.
		TileRegion( x1, y1, centerWidth, middleHeight, MiddleCenter );
	}
}
