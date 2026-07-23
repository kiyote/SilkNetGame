using GameFramework.Sprites;
using GameFramework.Textures;

namespace GameFramework.Scenes.Components;

public sealed class NinePatchPanel : ISceneRenderHandler {

	private readonly INinePatch _panel;

	public NinePatchPanel(
		INinePatch panel
	) {
		_panel = panel;
	}

	void ISceneRenderHandler.OnRender(
		SceneNode node,
		ISpriteBatch spriteBatch
	) {
		_panel.Draw( spriteBatch, node.Clip.Position, node.Clip.Size );
	}

	void ISceneRenderHandler.OnUpdate(
		SceneNode node,
		double deltaTime
	) {
		// Do nothing
	}
}
