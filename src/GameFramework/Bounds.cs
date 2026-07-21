namespace GameFramework;

public record struct Bounds( Coordinate Position, Dimension Size ) {

	public Bounds( int x, int y, int width, int height )
		: this( new Coordinate( x, y ), new Dimension( width, height ) ) {
	}

	public readonly int X => Position.X;

	public readonly int Y => Position.Y;

	public readonly int Width => Size.Width;

	public readonly int Height => Size.Height;

	public readonly int Left => Position.X;

	public readonly int Top => Position.Y;

	public readonly int Right => Position.X + Size.Width;

	public readonly int Bottom => Position.Y + Size.Height;

	public readonly bool Contains(
		int x,
		int y
	) {
		return x >= Left
			&& x < Right
			&& y >= Top
			&& y < Bottom;
	}

	public readonly bool Contains(
		in Coordinate coordinate
	) {
		return Contains( coordinate.X, coordinate.Y );
	}

	public static Bounds Intersect(
		in Bounds a,
		in Bounds b
	) {
		int left = Math.Max( a.Left, b.Left );
		int top = Math.Max( a.Top, b.Top );
		int right = Math.Min( a.Right, b.Right );
		int bottom = Math.Min( a.Bottom, b.Bottom );

		if( right >= left && bottom >= top ) {
			return new Bounds( left, top, right - left, bottom - top );
		}

		return default;
	}
}
