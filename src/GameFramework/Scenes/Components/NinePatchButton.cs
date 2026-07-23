using GameFramework.Sprites;
using GameFramework.Textures;

namespace GameFramework.Scenes.Components;

public sealed class NinePatchButton : ISceneMouseHandler, ISceneRenderHandler {

	private readonly INinePatch _up;
	private readonly INinePatch _down;
	private readonly uint _unfocusedColour;
	private readonly uint _focusedColour;
	private readonly int _pressHeight;

	private uint _colour;
	private bool _pressed;


	public NinePatchButton(
		INinePatch up,
		INinePatch down,
		int pressHeight = 1,
		uint unfocusedColour = 0xFFFFFFFFu,
		uint focusedColour = 0xFFFFFFFFu
	) {
		_up = up;
		_down = down;
		_unfocusedColour = unfocusedColour;
		_focusedColour = focusedColour;
		_pressHeight = pressHeight;

		_colour = unfocusedColour;
		_pressed = false;
	}

	void ISceneRenderHandler.OnRender(
		SceneNode node,
		ISpriteBatch spriteBatch
	) {
		if ( _pressed ) {
			Coordinate pressedCoordinate = node.Clip.Position.Add( 0, _pressHeight );
			Dimension pressedSize = node.Clip.Size.Subtract( 0, _pressHeight );
			spriteBatch.Draw( _down, pressedCoordinate, pressedSize, _colour );
		} else {
			spriteBatch.Draw( _up, node.Clip.Position, node.Clip.Size, _colour );
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
