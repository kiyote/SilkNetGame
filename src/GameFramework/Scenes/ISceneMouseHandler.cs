namespace GameFramework.Scenes;

public interface ISceneMouseHandler {
	// A no-op mouse handler that ignores all input. Use as the default when a node
	// has no mouse behaviour of its own.
	static ISceneMouseHandler None => NullSceneMouseHandler.Instance;

	// Default implementations for actions require no code block body
	void OnMousePressed( SceneNode node, int button ) { }
	void OnMouseReleased( SceneNode node, int button ) { }
	void OnMouseMoved( SceneNode node, Coordinate coordinate ) { }
	void OnMouseEntered( SceneNode node ) { }
	void OnMouseExited( SceneNode node ) { }

	// Default implementations for functions return the original baseline state
	bool OnMouseDown( SceneNode node, Coordinate coordinate, int button, bool isHandled ) => false;
	bool OnMouseUp( SceneNode node, Coordinate coordinate, int button, bool isHandled ) => false;
	bool OnMouseMove( SceneNode node, Coordinate coordinate, bool isHandled ) => false;
}
