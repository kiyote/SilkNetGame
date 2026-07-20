using GameFramework.Fonts;
using GameFramework.Textures;
using Microsoft.Extensions.DependencyInjection;

namespace GameFramework;

public interface IDevice {
	IDevice ConfigureServices(
		Action<IServiceCollection> configure
	);

	void Terminate();

	IFont LoadTtfFont(
		string fontFile,
		int fontHeightInPixels
	);

	ITexture LoadTexture(
		string textureFile,
		bool premultiplyAlpha = true,
		TextureFilter filter = TextureFilter.Linear
	);

	IRenderTexture CreateRenderTexture(
		Dimension size,
		TextureFilter filter = TextureFilter.Nearest
	);

	ITextureAtlas CreateTextureAtlas(
		ITexture texture
	);

	ITextureAtlas CreateTextureAtlas(
		Dimension size,
		TextureFilter filter = TextureFilter.Nearest
	);

	void Run();
}

