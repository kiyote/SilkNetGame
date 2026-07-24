using GameFramework.Fonts;
using GameFramework.Sprites;
using GameFramework.Textures;

namespace GameFramework.Scenes.Components;

public sealed class Label : ISceneRenderHandler {

	private readonly ITextureAtlas _ui;
	private readonly string _id;

	public Label(
		ITextureAtlas ui,
		string id,
		IFont font,
		string text,
		uint colour = 0xFFFFFFFFu,
		uint outline = 0x000000FFu,
		int outlineWidth = 1
	) {
		_ui = ui;
		_id = id;
		_ui.Create( id, font, text, colour, outline, outlineWidth );		
	}

	public void OnRender(
		SceneNode node,
		ISpriteBatch spriteBatch
	) {
		spriteBatch.Draw( _ui.SubTexture( _id ), node.Clip.Position );
	}

	public void OnUpdate(
		SceneNode node,
		double deltaTime
	) {
		// Do nothing
	}
}
