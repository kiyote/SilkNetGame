namespace GameFramework;

public readonly record struct Dimension( int Width, int Height ) {

	public Dimension Subtract( in Dimension other ) {
		return new Dimension( Width - other.Width, Height - other.Height );
	}
}
