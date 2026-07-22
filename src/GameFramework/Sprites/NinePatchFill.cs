namespace GameFramework.Sprites;

public enum NinePatchFill {
	// The edges and center are scaled to fill the target region.
	Stretch = 0,
	// The edges and center repeat their source tile to fill the target region,
	// trimming the last tile in each row/column to the region bounds.
	Tile
}
