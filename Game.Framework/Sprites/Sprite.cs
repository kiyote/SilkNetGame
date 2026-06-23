using Game.Framework.Textures;

namespace Game.Framework.Sprites;

public sealed class Sprite: IEquatable<Sprite> {

	public Sprite(
		ITexture texture,
		int x,
		int y,
		int width,
		int height
	) {
		X = x;
		Y = y;
		Width = width;
		Height = height;
		u1 = x / (float)texture.TextureHeight;
		v1 = y / (float)texture.TextureWidth;
		u2 = ( x + width ) / (float)texture.TextureHeight;
		v2 = ( y + height ) / (float)texture.TextureWidth;
	}

	internal int X;
	internal int Y;
	internal int Width;
	internal int Height;

	internal float u1;
	internal float v1;
	internal float u2;
	internal float v2;

	public bool Equals( Sprite? other ) {
		if (other is null) {
			return false;
		}
		return other.u1 == u1 && other.v1 == v1 && other.u2 == u2 && other.v2 == v2;
	}
}
