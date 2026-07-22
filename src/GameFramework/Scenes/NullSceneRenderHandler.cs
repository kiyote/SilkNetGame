using GameFramework.Sprites;

namespace GameFramework.Scenes;

internal class NullSceneRenderHandler: ISceneRenderHandler {

	public static readonly NullSceneRenderHandler Instance = new NullSceneRenderHandler();

	private NullSceneRenderHandler() { }

	public void OnRender( SceneNode node, ISpriteBatch spriteBatch ) { }

	public void OnUpdate( SceneNode node, double deltaTime ) { }
}
