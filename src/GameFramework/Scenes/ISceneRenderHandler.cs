using GameFramework.Sprites;

namespace GameFramework.Scenes;

public interface ISceneRenderHandler {

	// A no-op render handler that draws nothing. Use as the default when a node has
	// no rendering of its own.
	static ISceneRenderHandler None => NullSceneRenderHandler.Instance;

	void OnRender( SceneNode node, ISpriteBatch spriteBatch );
}
