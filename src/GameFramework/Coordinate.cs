using System.Drawing;

namespace GameFramework;

public readonly record struct Coordinate( int X, int Y ) {

	public Coordinate Subtract( in Coordinate other ) {
		return new Coordinate( X - other.X, Y - other.Y );
	}

	public Coordinate Subtract( int x, int y ) {
		return new Coordinate( X - x, Y - y );
	}

	public Coordinate Subtract( in Rectangle rectangle ) {
		return new Coordinate( X - rectangle.X, Y - rectangle.Y );
	}
}
