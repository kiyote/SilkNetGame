namespace GameFramework.Textures;

public interface ITexture : ISubTexture, IDisposable {
	Dimension TextureSize { get; }
	uint Id { get; }

	// The U offset for one-half pixel
	float HalfX { get; }
	// The V offset for one-half pixel
	float HalfY { get; }

	TextureFilter Filter { get; }

	void Clear(
		Coordinate position,
		Dimension size,
		uint colour = 0
	);

	void Copy(
		Coordinate position,
		ITexture source,
		Coordinate sourcePosition,
		Dimension sourceSize
	);

	ITextureAtlas CreateAtlas();

	ISubTexture CreateSubTexture(
		string name,
		Coordinate position,
		Dimension size
	);

	void Bind( int textureUnit = 0 );
}
