using GameFramework.Scenes;

namespace GameFramework.Components;

public sealed class ButtonBehaviour : ISceneNodeHandler {

	private bool _containsMouse;
	private State _currentState;

	public enum State {
		None,
		Highlighted,
		Pressed
	}

	public State CurrentState => _currentState;

	public void OnMouseEntered(
		SceneNode node
	) {
		_containsMouse = true;
		_currentState = State.Highlighted;
	}

	public void OnMouseExited(
		SceneNode node
	) {
		_containsMouse = false;
		_currentState = State.None;
	}

	public void OnMouseReleased(
		SceneNode node,
		int button
	) {
		_currentState = _containsMouse ? State.Highlighted : State.None;
	}

	public bool OnMouseDown(
		 SceneNode node,
		 Coordinate coordinate,
		 int button,
		 bool isHandled
	) {
		if( isHandled ) {
			return false;
		}
		_currentState = State.Pressed;
		return true;
	}
}
