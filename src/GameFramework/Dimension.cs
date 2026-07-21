namespace GameFramework;

public record struct Dimension( int Width, int Height ) {

	public readonly Dimension Subtract( in Dimension other ) {
		return new Dimension( Width - other.Width, Height - other.Height );
	}
}
