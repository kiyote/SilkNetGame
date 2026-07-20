using GameFramework.Sprites;

namespace GameFramework.Scenes;

public interface ISceneRenderHandler {

	bool OnRender( SceneNode node, ISpriteBatch spriteBatch );
}
