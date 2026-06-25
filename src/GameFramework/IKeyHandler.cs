using Silk.NET.Input;

namespace GameFramework;

public interface IKeyHandler {

	bool KeyUp( Key key );
	bool KeyDown( Key key );
}
