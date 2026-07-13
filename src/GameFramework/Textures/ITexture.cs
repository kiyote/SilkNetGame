namespace GameFramework.Textures;

public interface ITexture : ISubTexture, IDisposable {
	int TextureWidth { get; }
	int TextureHeight { get; }
	uint Id { get; }

	float HalfX { get; }
	float HalfY { get; }

	TextureFilter Filter { get; }

	void Copy(
		int x,
		int y,
		ITexture source,
		int sourceX,
		int sourceY,
		int sourceWidth,
		int sourceHeight
	);

	ITextureAtlas CreateAtlas();

	ISubTexture CreateSubTexture(
		string name,
		int left,
		int top,
		int width,
		int height
	);

	void Bind( int textureUnit = 0 );
}
