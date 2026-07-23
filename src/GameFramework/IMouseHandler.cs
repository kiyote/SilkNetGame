namespace GameFramework;

public interface IMouseHandler {

	bool MouseDown( Coordinate position, int button );
	bool MouseUp( Coordinate position, int button );

	bool MouseMove( Coordinate position );
}
