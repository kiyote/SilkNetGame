using Game.Framework.Fonts;
using Game.Framework.Sprites;
using Game.Framework.Textures;
using Microsoft.Extensions.DependencyInjection;

namespace Game.Framework;

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

