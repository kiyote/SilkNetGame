using GameFramework.Textures;

namespace GameFramework.Scenes.Components;

public static class ExtensionMethods {

	public static SceneNode AddButton(
		this SceneNode node,
		Coordinate position,
		Dimension size,
		INinePatch up,
		INinePatch down,
		int pressHeight = 1,
		uint unfocusedColour = 0xFFFFFFFFu,
		uint focusedColour = 0xFFFFFFFFu
	) {
		NinePatchButton button = new NinePatchButton( up, down, pressHeight, unfocusedColour, focusedColour );
		return node.AddChild( position, size, button, button );
	}

	public static SceneNode AddPanel(
		this SceneNode node,
		Coordinate position,
		Dimension size,
		INinePatch panel
	) {
		NinePatchPanel panelComponent = new NinePatchPanel( panel );
		return node.AddChild( position, size, ISceneMouseHandler.None, panelComponent );
	}
}
