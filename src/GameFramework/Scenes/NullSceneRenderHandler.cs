using GameFramework.Sprites;

namespace GameFramework.Scenes;

internal class NullSceneRenderHandler: ISceneRenderHandler {

	public static readonly NullSceneRenderHandler Instance = new NullSceneRenderHandler();

	private NullSceneRenderHandler() { }

	public bool OnRender( SceneNode node, ISpriteBatch spriteBatch ) { return false; }
}
