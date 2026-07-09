using GameFramework.Sprites;
using GameFramework.Textures;

namespace GameFramework.Scenes;

public interface ISceneNodeRenderer {

	void Prepare(
		ITextureManager textureManager,
		ISpriteAtlas spriteAtlas
	);

	void Render(
		ISpriteAtlas spriteAtlas
	);
}
