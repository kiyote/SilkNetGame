namespace GameFramework;

public record struct DimensionF( float Width, float Height ) {

	public readonly DimensionF Subtract( in DimensionF other ) {
		return new DimensionF( Width - other.Width, Height - other.Height );
	}
}
