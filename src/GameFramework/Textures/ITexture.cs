namespace GameFramework.Textures;

public interface ITexture: IDisposable {
	int TextureWidth { get; }
	int TextureHeight { get; }
	uint Id { get; }

	void Bind( int textureUnit = 0 );
}
