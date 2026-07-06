using GameFramework.Fonts;
using GameFramework.Sprites;
using GameFramework.Textures;
using Microsoft.Extensions.DependencyInjection;

namespace GameFramework;

public interface IDevice {
	IDevice ConfigureServices(
		Action<IServiceCollection> configure
	);

	IFramebuffer CreateFramebuffer(
		int width,
		int height,
		TextureFilter filter = TextureFilter.Nearest
	);

	void Terminate();

	IFont LoadTtfFont(
		string fontFile,
		float fontSize
	);

	ITexture LoadTexture(
		string textureFile,
		bool premultiplyAlpha = true,
		TextureFilter filter = TextureFilter.Linear
	);

	ISpriteAtlas CreateSpriteAtlas(
		ITexture texture,
		ISpriteBatch spriteBatch
	);

	void Run();
}

