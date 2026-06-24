using Game.Framework.Textures;

namespace Game.Framework.Sprites;

public interface ISpriteBatch: IDisposable {
	void Start( IRenderTarget renderTarget, ITexture texture );
	void Finish();
	void Draw( float x, float y, float width, float height, float u1, float v1, float u2, float v2, uint colour );
	void Draw( float x, float y, float width, float height, Sprite sprite, uint colour ) => Draw( x, y, width, height, sprite.u1, sprite.v1, sprite.u2, sprite.v2, colour );
	void Draw( float x, float y, Sprite sprite, uint colour ) => Draw( x, y, sprite.Width, sprite.Height, sprite.u1, sprite.v1, sprite.u2, sprite.v2, colour );
}
