using GameFramework.Sprites;

namespace GameFramework.Scenes;

public sealed class SceneManager : ISceneManager {

	private readonly SceneNode _root;
	private Coordinate _lastCoordinate = new( int.MinValue, int.MinValue );

	public SceneManager(
		Dimension surfaceSize
	) {
		_root = new SceneNode( surfaceSize );
	}

	SceneNode ISceneManager.Root => _root;

	void ISceneManager.Reparent(
		SceneNode node,
		SceneNode newParent
	) {
		ArgumentNullException.ThrowIfNull( node );
		ArgumentNullException.ThrowIfNull( newParent );
		node.Reparent( newParent );
	}

	void ISceneManager.Reorder(
		SceneNode node,
		int index
	) {
		ArgumentNullException.ThrowIfNull( node );
		node.Reorder( index );
	}

	void ISceneManager.ReorderBefore(
		SceneNode node,
		SceneNode before
	) {
		ArgumentNullException.ThrowIfNull( node );
		ArgumentNullException.ThrowIfNull( before );
		node.ReorderBefore( before );
	}

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

	void ISceneManager.Update(
		double deltaTime
	) {
		WalkUpdate( _root, deltaTime );
	}

	private static void WalkUpdate(
		SceneNode node,
		double deltaTime
	) {
		node.Update( deltaTime );
		foreach( SceneNode child in node.Children ) {
			WalkUpdate( child, deltaTime );
		}
	}

	void ISceneManager.Render(
		ISpriteBatch spriteBatch
	) {
		WalkRender( _root, spriteBatch );
	}

	private static void WalkRender(
		SceneNode node,
		ISpriteBatch spriteBatch
	) {
		node.Render( spriteBatch );
		foreach( SceneNode child in node.Children ) {
			WalkRender( child, spriteBatch );
		}
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
