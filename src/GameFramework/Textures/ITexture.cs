namespace GameFramework.Textures;

public interface ITexture: IDisposable {
	uint TextureWidth { get; }
	uint TextureHeight { get; }
	uint Id { get; }

	void Bind( int textureUnit = 0 );
}
