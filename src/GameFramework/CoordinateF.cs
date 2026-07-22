namespace GameFramework;

public record struct CoordinateF( float X, float Y ) {

	public readonly CoordinateF Subtract( in CoordinateF other ) {
		return new CoordinateF( X - other.X, Y - other.Y );
	}

	public readonly CoordinateF Subtract( float x, float y ) {
		return new CoordinateF( X - x, Y - y );
	}

	public readonly CoordinateF Subtract( in BoundsF bounds ) {
		return new CoordinateF( X - bounds.X, Y - bounds.Y );
	}

	public readonly CoordinateF Add( float x, float y ) {
		return new CoordinateF( X + x, Y + y );
	}
}
