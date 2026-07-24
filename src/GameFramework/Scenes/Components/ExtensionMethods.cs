using GameFramework.Fonts;
using GameFramework.Textures;

namespace GameFramework.Scenes.Components;

public static class ExtensionMethods {

	public static SceneNode AddButton(
		this SceneNode node,
		Coordinate position,
		Dimension size,
		ITextureAtlas ui,
		string up,
		string down,
		int pressHeight = 1,
		uint unfocusedColour = 0xFFFFFFFFu,
		uint focusedColour = 0xFFFFFFFFu
	) {
		NinePatchButton button = new NinePatchButton( ui, up, down, pressHeight, unfocusedColour, focusedColour );
		return node.AddChild( position, size, button, button );
	}

	public static SceneNode AddLabelButton(
		this SceneNode node,
		Action clickHandler,
		Coordinate position,
		Dimension size,
		ITextureAtlas ui,
		string up,
		string down,
		IFont font,
		string text,
		int pressHeight = 1,
		uint unfocusedColour = 0xFFFFFFFFu,
		uint focusedColour = 0xFFFFFFFFu,
		uint textColour = 0xFFFFFFFFu,
		uint outlineColour = 0x000000FFu,
		int outlineThickness = 1
	) {
		NinePatchLabelButton button = new NinePatchLabelButton( clickHandler, ui, up, down, font, text, pressHeight, unfocusedColour, focusedColour, textColour, outlineColour, outlineThickness );
		return node.AddChild( position, size, button, button );
	}

	public static SceneNode AddPanel(
		this SceneNode node,
		Coordinate position,
		Dimension size,
		ITextureAtlas ui,
		string panel,
		bool draggable = false
	) {
		NinePatchPanel panelComponent = new NinePatchPanel( ui, panel, draggable );
		return node.AddChild( position, size, panelComponent, panelComponent );
	}

	public static SceneNode AddLabel(
		this SceneNode node,
		Coordinate position,
		ITextureAtlas ui,
		string id,
		IFont font,
		string text
	) {
		Dimension size = font.MeasureText( text, 0 );
		Label label = new Label( ui, id, font, text );
		return node.AddChild( position, size, ISceneMouseHandler.None, label );
	}
}
