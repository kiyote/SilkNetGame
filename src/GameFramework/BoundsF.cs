namespace GameFramework;

public record struct BoundsF( CoordinateF Position, DimensionF Size ) {

	public BoundsF( float x, float y, float width, float height )
		: this( new CoordinateF( x, y ), new DimensionF( width, height ) ) {
	}

	public readonly float X => Position.X;

	public readonly float Y => Position.Y;

	public readonly float Width => Size.Width;

	public readonly float Height => Size.Height;

	public readonly float Left => Position.X;

	public readonly float Top => Position.Y;

	public readonly float Right => Position.X + Size.Width;

	public readonly float Bottom => Position.Y + Size.Height;

	public readonly bool Contains(
		float x,
		float y
	) {
		return x >= Left
			&& x < Right
			&& y >= Top
			&& y < Bottom;
	}

	public readonly bool Contains(
		in CoordinateF coordinate
	) {
		return Contains( coordinate.X, coordinate.Y );
	}

	public static BoundsF Intersect(
		in BoundsF a,
		in BoundsF b
	) {
		float left = MathF.Max( a.Left, b.Left );
		float top = MathF.Max( a.Top, b.Top );
		float right = MathF.Min( a.Right, b.Right );
		float bottom = MathF.Min( a.Bottom, b.Bottom );

		if( right >= left && bottom >= top ) {
			return new BoundsF( left, top, right - left, bottom - top );
		}

		return default;
	}
}
