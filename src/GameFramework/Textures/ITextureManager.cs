namespace GameFramework.Textures;

public interface ITextureManager : IDisposable {

	ITexture this[string id] { get; }

	ITexture Load(
		string id,
		string textureFile,
		bool premultiplyAlpha = true,
		TextureFilter filter = TextureFilter.Linear
	);

	void Clear();
}
