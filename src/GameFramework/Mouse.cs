using System.Numerics;
using Silk.NET.Input;

namespace GameFramework;

public sealed class Mouse : IDisposable {

	private readonly IInputContext _context;
	private readonly List<IMouseHandler> _handlers = [];
	private bool _suspended;

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
			if( _suspended
				|| handler.MouseDown( position, (int)button )
			) {
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
			if( _suspended
				|| handler.MouseUp( position, (int)button )
			) {
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
			if( _suspended
				|| handler.MouseMove( position )
			) {
				break;
			}
		}
	}

	public void Dispose() {
		_handlers.Clear();
	}

	public void Suspend() {
		_suspended = true;
	}

	public void Resume() {
		_suspended = false;
	}
}
