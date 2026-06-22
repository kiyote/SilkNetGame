using Silk.NET.Input;

namespace Game.Framework;

public sealed class Keyboard: IDisposable {

	private readonly IInputContext _context;
	private readonly List<IKeyHandler> _handlers = [];

	public Keyboard(
		IInputContext context
	) {
		_context = context;
		foreach( IKeyboard keyboard in _context.Keyboards ) {
			keyboard.KeyDown += OnKeyDown;
			keyboard.KeyUp += OnKeyUp;
		}
	}

	public void AddHandler(
		IKeyHandler keyHandler
	) {
		_handlers.Add( keyHandler );
	}

	private void OnKeyDown(
		IKeyboard keyboard,
		Key key,
		int scancode
	) {
		foreach( IKeyHandler handler in _handlers ) {
			if( handler.KeyDown( key ) ) {
				break;
			}
		}
	}

	private void OnKeyUp(
		IKeyboard keyboard,
		Key key,
		int scancode
	) {
		foreach( IKeyHandler handler in _handlers ) {
			if( handler.KeyUp( key ) ) {
				break;
			}
		}
	}

	public void Dispose() {
		_handlers.Clear();
	}
}
