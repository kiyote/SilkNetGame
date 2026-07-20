using GameFramework.Sprites;

namespace GameFramework.Scenes;

internal sealed class SceneManager : ISceneManager {

	private readonly SceneNode _root;
	private Coordinate _lastCoordinate = new( int.MinValue, int.MinValue );

	public SceneManager(
		Dimension surfaceSize
	) {
		_root = new SceneNode( surfaceSize );
	}

	SceneNode ISceneManager.Root => _root;

	bool ISceneManager.MouseDown(
		Coordinate coordinate,
		int button
	) {
		return WalkMouseDown( _root, coordinate, button, false );
	}

	bool ISceneManager.MouseUp(
		Coordinate coordinate,
		int button
	) {
		return WalkMouseUp( _root, coordinate, button, false );
	}

	bool ISceneManager.MouseMove(
		Coordinate coordinate
	) {
		bool result = WalkMouseMove( _root, coordinate, false );
		_lastCoordinate = coordinate;
		return result;
	}

	int ISceneManager.Render(
		ISpriteBatch spriteBatch
	) {
		return WalkRender( _root, spriteBatch );
	}

	private static int WalkRender(
		SceneNode node,
		ISpriteBatch spriteBatch
	) {
		int flushes = node.Render( spriteBatch ) ? 1 : 0;

		// Then descend into children so they paint over their parent.
		foreach( SceneNode child in node.Children ) {
			flushes += WalkRender( child, spriteBatch );
		}

		return flushes;
	}

	private bool WalkMouseMove(
		SceneNode node,
		Coordinate coordinate,
		bool isHandled
	) {
		foreach( SceneNode child in node.Children ) {
			isHandled |= WalkMouseMove( child, coordinate, isHandled );
		}

		bool containsMouse = node.Clip.Contains( coordinate.X, coordinate.Y );
		bool previouslyContainedMouse = node.Clip.Contains( _lastCoordinate.X, _lastCoordinate.Y );

		if( containsMouse && !previouslyContainedMouse ) {
			node.MouseEntered();
		}
		node.MouseMoved( coordinate );
		if( containsMouse ) {
			isHandled |= node.MouseMove( coordinate.Subtract( node.Clip ), isHandled );
		}
		if( !containsMouse && previouslyContainedMouse ) {
			node.MouseExited();
		}

		return isHandled;
	}

	private static bool WalkMouseDown(
		SceneNode node,
		Coordinate coordinate,
		int button,
		bool isHandled
	) {
		foreach( SceneNode child in node.Children ) {
			isHandled |= WalkMouseDown( child, coordinate, button, isHandled );
		}

		node.MousePressed( button );
		if( node.Clip.Contains( coordinate.X, coordinate.Y ) ) {
			isHandled |= node.MouseDown( coordinate.Subtract( node.Clip ), button, isHandled );
		}

		return isHandled;
	}

	private static bool WalkMouseUp(
		SceneNode node,
		Coordinate coordinate,
		int button,
		bool isHandled
	) {
		foreach( SceneNode child in node.Children ) {
			isHandled |= WalkMouseUp( child, coordinate, button, isHandled );
		}

		node.MouseReleased( button );
		if( node.Clip.Contains( coordinate.X, coordinate.Y ) ) {
			isHandled |= node.MouseUp( coordinate.Subtract( node.Clip ), button, isHandled );
		}

		return isHandled;
	}

}
