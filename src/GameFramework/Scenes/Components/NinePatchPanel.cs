using GameFramework.Sprites;
using GameFramework.Textures;

namespace GameFramework.Scenes.Components;

public sealed class NinePatchPanel : ISceneRenderHandler, ISceneMouseHandler {

	private readonly ITextureAtlas _ui;
	private readonly string _panel;

	private readonly bool _draggable;
	private bool _dragging;
	private bool _dragStarted;
	private Coordinate _dragOrigin;

	public NinePatchPanel(
		ITextureAtlas ui,
		string panel,
		bool draggable = false
	) {
		_ui = ui;
		_panel = panel;
		_draggable = draggable;
	}

	void ISceneRenderHandler.OnRender(
		SceneNode node,
		ISpriteBatch spriteBatch
	) {
		_ui.NinePatch( _panel ).Draw( spriteBatch, node.Clip.Position, node.Clip.Size );
	}

	void ISceneRenderHandler.OnUpdate(
		SceneNode node,
		double deltaTime
	) {
		// Do nothing
	}

	bool ISceneMouseHandler.OnMouseDown(
		SceneNode node,
		Coordinate coordinate,
		int button,
		bool isHandled
	) {
		if (_draggable) {
			_dragging = true;
			_dragStarted = false;
			return true;
		}
		return false;
	}

	void ISceneMouseHandler.OnMouseMoved(
		SceneNode node,
		Coordinate coordinate
	) {
		if (_dragging) {
			if (!_dragStarted) {
				_dragOrigin = coordinate;
				_dragStarted = true;
				return;
			}
			Coordinate delta = coordinate.Subtract( _dragOrigin );
			Coordinate target = node.Position.Add( delta );
			// Never allow the panel to be dragged above or left of the origin.
			node.Position = new Coordinate( Math.Max( 0, target.X ), Math.Max( 0, target.Y ) );
			_dragOrigin = coordinate;
		}
	}

	void ISceneMouseHandler.OnMouseReleased(
		SceneNode node,
		int button
	) {
		_dragging = false;
	}
}
