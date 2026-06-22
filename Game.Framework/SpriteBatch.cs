namespace Game.Framework;

public abstract class SpriteBatch: IDisposable {

	public abstract void Begin(
		RenderTarget renderTarget,
		Texture texture
	);

	public abstract void End();

	public abstract void Sprite(
		float x,
		float y,
		float width,
		float height,
		float u1,
		float v1,
		float u2,
		float v2,
		uint colour
	);

	public void Sprite(
		float x,
		float y,
		float width,
		float height,
		Sprite sprite,
		uint colour
	) {
		Sprite( x, y, width, height, sprite.u1, sprite.v1, sprite.u2, sprite.v2, colour );
	}

	public void Sprite(
		float x,
		float y,
		Sprite sprite,
		uint colour
	) {
		Sprite( x, y, sprite.Width, sprite.Height, sprite.u1, sprite.v1, sprite.u2, sprite.v2, colour );
	}

	public abstract void Dispose();
}
