using Silk.NET.Input;

namespace Game.Framework;

public interface IKeyHandler {

	bool KeyUp( Key key );
	bool KeyDown( Key key );
}
