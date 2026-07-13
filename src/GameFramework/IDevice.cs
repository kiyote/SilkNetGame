using GameFramework.Fonts;
using GameFramework.Sprites;
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

/*
	IFramebuffer CreateFramebuffer(
		int width,
		int height,
		TextureFilter filter = TextureFilter.Nearest
	);

	ISpriteAtlas CreateSpriteAtlas(
		ITexture texture,
		ISpriteBatch spriteBatch
	);
	*/

	void Run();
}

