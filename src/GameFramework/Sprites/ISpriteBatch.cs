using System.Drawing;
using GameFramework.Textures;

namespace GameFramework.Sprites;


public interface ISpriteBatch : IDisposable {
	void Start( IRenderTarget renderTarget, ITexture texture );
	void Finish();

	// Methods that perform actual drawing
	// -----------------------------------
	void Draw( float x, float y, float width, float height, float u1, float v1, float u2, float v2, uint colour );
	void Draw( float x, float y, float width, float height, float u1, float v1, float u2, float v2, float rotation, float originX, float originY, uint colour );

	// Helper methods for drawing
	// --------------------------
	void Draw( float x, float y, float width, float height, Sprite sprite, uint colour ) => Draw( x, y, width, height, sprite.u1, sprite.v1, sprite.u2, sprite.v2, colour );
	void Draw( float x, float y, Sprite sprite, uint colour ) => Draw( x, y, sprite.Width, sprite.Height, sprite.u1, sprite.v1, sprite.u2, sprite.v2, colour );
	void Draw( float x, float y, float width, float height, Sprite sprite, float rotation, uint colour ) => Draw( x, y, width, height, sprite.u1, sprite.v1, sprite.u2, sprite.v2, rotation, width * 0.5f, height * 0.5f, colour );
	void Draw( float x, float y, Sprite sprite, float rotation, uint colour ) => Draw( x, y, sprite.Width, sprite.Height, sprite.u1, sprite.v1, sprite.u2, sprite.v2, rotation, sprite.Width * 0.5f, sprite.Height * 0.5f, colour );
	void Draw( float x, float y, float width, float height, Sprite sprite, float rotation, float originX, float originY, uint colour ) => Draw( x, y, width, height, sprite.u1, sprite.v1, sprite.u2, sprite.v2, rotation, originX, originY, colour );
	void Draw( float x, float y, Sprite sprite, float rotation, float originX, float originY, uint colour ) => Draw( x, y, sprite.Width, sprite.Height, sprite.u1, sprite.v1, sprite.u2, sprite.v2, rotation, originX, originY, colour );

	// Methods to draw clipped sprites
	// (Using these instead of setting the clip on the render target can be more efficient as it avoids flushing the batch if you need to change the clipping rect often.)
	// -------------------------------
	void Draw( float x, float y, float width, float height, Sprite sprite, Rectangle clip, uint colour ) {
		float left = MathF.Max( x, clip.Left );
		float top = MathF.Max( y, clip.Top );
		float right = MathF.Min( x + width, clip.Right );
		float bottom = MathF.Min( y + height, clip.Bottom );

		// Nothing of the sprite falls inside the clip rectangle.
		if( right <= left || bottom <= top ) {
			return;
		}

		// Convert the visible bounds into the sprite's UV space.
		float uPerPixel = ( sprite.u2 - sprite.u1 ) / width;
		float vPerPixel = ( sprite.v2 - sprite.v1 ) / height;

		float u1 = sprite.u1 + ( ( left - x ) * uPerPixel );
		float v1 = sprite.v1 + ( ( top - y ) * vPerPixel );
		float u2 = sprite.u1 + ( ( right - x ) * uPerPixel );
		float v2 = sprite.v1 + ( ( bottom - y ) * vPerPixel );

		Draw( left, top, right - left, bottom - top, u1, v1, u2, v2, colour );
	}

	void Draw( float x, float y, Sprite sprite, Rectangle clip, uint colour ) => Draw( x, y, sprite.Width, sprite.Height, sprite, clip, colour );

}
