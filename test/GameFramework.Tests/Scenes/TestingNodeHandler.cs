namespace GameFramework.Scenes.Tests;

internal sealed class TestingNodeHandler : ISceneNodeHandler {

	public void OnMousePressed(
		SceneNode node,
		int button
	) {
		WasMousePressedCalled = true;
	}

	public bool OnMouseUp(
		SceneNode node,
		Coordinate coordinate,
		int button,
		bool isHandled
	) {
		WasMouseUpCalled = true;
		return true;
	}

	public bool OnMouseDown(
		SceneNode node,
		Coordinate coordinate,
		int button,
		bool isHandled
	) {
		WasMouseDownCalled = true;
		return true;
	}

	public void OnMouseReleased( SceneNode node, int button ) {
		WasMouseReleasedCalled = true;
	}

	public bool WasMousePressedCalled { get; set; }

	public bool WasMouseDownCalled { get; set; }

	public bool WasMouseReleasedCalled { get; set; }

	public bool WasMouseUpCalled { get; set; }
}
