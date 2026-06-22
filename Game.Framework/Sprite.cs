namespace Game.Framework;

public sealed class Sprite {

	public Sprite(
		Texture texture,
		int x,
		int y,
		int width,
		int height
	) {
		Width = width;
		Height = height;
		u1 = x / (float)texture.Width;
		v1 = y / (float)texture.Height;
		u2 = ( x + width ) / (float)texture.Width;
		v2 = ( y + height ) / (float)texture.Height;
	}

	internal float Width;
	internal float Height;

	internal float u1;
	internal float v1;
	internal float u2;
	internal float v2;
}
