using GameFramework.Fonts;
using GameFramework.Sprites;
using GameFramework.Textures;

namespace GameFramework.Scenes.Components;

public sealed class NinePatchLabelButton : ISceneMouseHandler, ISceneRenderHandler {

	private readonly ITextureAtlas _ui;
	private readonly string _up;
	private readonly string _down;
	private readonly uint _unfocusedColour;
	private readonly uint _focusedColour;
	private readonly int _pressHeight;
	private readonly string _label;
	private readonly Action _clickHandler;
	
	private uint _colour;
	private bool _pressed;


	public NinePatchLabelButton(
		Action clickHandler,
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
		_clickHandler = clickHandler;
		_ui = ui;
		_up = up;
		_down = down;
		_unfocusedColour = unfocusedColour;
		_focusedColour = focusedColour;
		_pressHeight = pressHeight;

		_colour = unfocusedColour;
		_pressed = false;

		_label = $"{Guid.NewGuid():N}";
		_ui.Create( _label, font, text, textColour, outlineColour, outlineThickness );
	}

	void ISceneRenderHandler.OnRender(
		SceneNode node,
		ISpriteBatch spriteBatch
	) {
		if( _pressed ) {
			Coordinate pressedCoordinate = node.Clip.Position.Add( 0, _pressHeight );
			Dimension pressedSize = node.Clip.Size.Subtract( 0, _pressHeight );
			_ui.NinePatch( _down ).Draw( spriteBatch, pressedCoordinate, pressedSize, _colour );

			ISubTexture subLabel = _ui.SubTexture( _label );
			Coordinate labelCoord = pressedCoordinate.Add(
				( pressedSize.Width - subLabel.Size.Width ) / 2,
				(( pressedSize.Height - subLabel.Size.Height ) / 2) - 1
			);
			spriteBatch.Draw( subLabel, labelCoord );

		} else {
			_ui.NinePatch( _up ).Draw( spriteBatch, node.Clip.Position, node.Clip.Size, _colour );

			ISubTexture subLabel = _ui.SubTexture( _label );
			Coordinate labelCoord = node.Clip.Position.Add(
				( node.Clip.Size.Width - subLabel.Size.Width ) / 2,
				(( node.Clip.Size.Height - subLabel.Size.Height ) / 2) - 1
			);
			spriteBatch.Draw( subLabel, labelCoord );
		}
	}

	void ISceneRenderHandler.OnUpdate(
		SceneNode node,
		double deltaTime
	) {
		// Nothing
	}

	void ISceneMouseHandler.OnMouseEntered(
		SceneNode node
	) {
		_colour = _focusedColour;
	}

	void ISceneMouseHandler.OnMouseExited(
		SceneNode node
	) {
		_colour = _unfocusedColour;
	}

	bool ISceneMouseHandler.OnMouseDown(
		SceneNode node,
		Coordinate coordinate,
		int button,
		bool isHandled
	) {
		if( button == 0
			&& !isHandled
		) {
			_pressed = true;
			return true;
		}
		return false;
	}

	bool ISceneMouseHandler.OnMouseUp(
		SceneNode node,
		Coordinate coordinate,
		int button,
		bool isHandled
	) {
		if ( button == 0
			&& !isHandled
		) {
			_clickHandler();
			return true;
		}
		return false;			
	}

	void ISceneMouseHandler.OnMouseReleased(
		SceneNode node,
		int button
	) {
		if( button == 0
			&& _pressed
		) {
			_pressed = false;
		}
	}
}
