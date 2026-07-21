namespace GameFramework;

public record struct Coordinate( int X, int Y ) {

	public readonly Coordinate Subtract( in Coordinate other ) {
		return new Coordinate( X - other.X, Y - other.Y );
	}

	public readonly Coordinate Subtract( int x, int y ) {
		return new Coordinate( X - x, Y - y );
	}

	public readonly Coordinate Subtract( in Bounds bounds ) {
		return new Coordinate( X - bounds.X, Y - bounds.Y );
	}

	public readonly Coordinate Add( int x, int y ) {
		return new Coordinate( X + x, Y + y );
	}
}
