using GameFramework.Textures;

namespace GameFramework.Sprites;


public interface ISpriteBatch : IDisposable {
	void Start( IRenderTarget renderTarget, ITexture texture );
	void Finish();

	// Existing fast path (unchanged)
	void Draw( float x, float y, float width, float height, float u1, float v1, float u2, float v2, uint colour );

	// New rotation-aware core. rotation is in radians; origin is the pivot
	// expressed in the sprite's local space (0,0 = top-left).
	void Draw( float x, float y, float width, float height, float u1, float v1, float u2, float v2, float rotation, float originX, float originY, uint colour );

	void Draw( float x, float y, float width, float height, Sprite sprite, uint colour ) => Draw( x, y, width, height, sprite.u1, sprite.v1, sprite.u2, sprite.v2, colour );
	void Draw( float x, float y, Sprite sprite, uint colour ) => Draw( x, y, sprite.Width, sprite.Height, sprite.u1, sprite.v1, sprite.u2, sprite.v2, colour );

	// New rotation convenience overloads (default pivot = sprite centre)
	void Draw( float x, float y, float width, float height, Sprite sprite, float rotation, uint colour ) => Draw( x, y, width, height, sprite.u1, sprite.v1, sprite.u2, sprite.v2, rotation, width * 0.5f, height * 0.5f, colour );
	void Draw( float x, float y, Sprite sprite, float rotation, uint colour ) => Draw( x, y, sprite.Width, sprite.Height, sprite.u1, sprite.v1, sprite.u2, sprite.v2, rotation, sprite.Width * 0.5f, sprite.Height * 0.5f, colour );

	// Rotation convenience overloads with an explicit pivot, expressed in the sprite's local space (0,0 = top-left).
	void Draw( float x, float y, float width, float height, Sprite sprite, float rotation, float originX, float originY, uint colour ) => Draw( x, y, width, height, sprite.u1, sprite.v1, sprite.u2, sprite.v2, rotation, originX, originY, colour );
	void Draw( float x, float y, Sprite sprite, float rotation, float originX, float originY, uint colour ) => Draw( x, y, sprite.Width, sprite.Height, sprite.u1, sprite.v1, sprite.u2, sprite.v2, rotation, originX, originY, colour );
}
