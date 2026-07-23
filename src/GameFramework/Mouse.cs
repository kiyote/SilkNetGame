using System.Numerics;
using Silk.NET.Input;

namespace GameFramework;

public sealed class Mouse : IDisposable {

	private readonly IInputContext _context;
	private readonly List<IMouseHandler> _handlers = [];

	public Mouse(
		IInputContext context
	) {
		_context = context;
		foreach( IMouse mouse in _context.Mice ) {
			mouse.MouseDown += OnMouseDown;
			mouse.MouseUp += OnMouseUp;
			mouse.MouseMove += OnMouseMove;
		}
	}

	public void AddHandler(
		IMouseHandler mouseHandler
	) {
		_handlers.Add( mouseHandler );
	}

	private void OnMouseDown(
		IMouse mouse,
		MouseButton button
	) {
		Coordinate position = new Coordinate( (int)mouse.Position.X, (int)mouse.Position.Y );
		foreach( IMouseHandler handler in _handlers ) {
			if( handler.MouseDown( position, (int)button ) ) {
				break;
			}
		}
	}

	private void OnMouseUp(
		IMouse mouse,
		MouseButton button	
	) {
		Coordinate position = new Coordinate( (int)mouse.Position.X, (int)mouse.Position.Y );
		foreach( IMouseHandler handler in _handlers ) {
			if( handler.MouseUp( position, (int)button ) ) {
				break;
			}
		}
	}

	private void OnMouseMove(
		IMouse mouse,
		Vector2 vector
	) {
		Coordinate position = new Coordinate( (int)vector.X, (int)vector.Y );
		foreach( IMouseHandler handler in _handlers ) {
			if( handler.MouseMove( position ) ) {
				break;
			}
		}
	}

	public void Dispose() {
		_handlers.Clear();
	}
}
