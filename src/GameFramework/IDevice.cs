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
		uint width,
		uint height
	);

	void Terminate();

	IFont LoadTtfFont(
		string fontFile,
		float fontSize
	);

	ITexture LoadTexture(
		string textureFile,
		bool premultiplyAlpha = true
	);

	ISpriteAtlas CreateSpriteAtlas(
		ITexture texture,
		ISpriteBatch spriteBatch
	);

	void Run();
}

