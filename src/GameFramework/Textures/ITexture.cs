namespace GameFramework.Textures;

public interface ITexture : ISubTexture, IDisposable {
	Dimension TextureSize { get; }
	uint Id { get; }

	// The U offset for one-half pixel
	float HalfX { get; }
	// The V offset for one-half pixel
	float HalfY { get; }

	TextureFilter Filter { get; }

	void Clear(
		Coordinate position,
		Dimension size,
		uint colour = 0
	);

	void Copy(
		Coordinate position,
		ITexture source,
		Coordinate sourcePosition,
		Dimension sourceSize
	);

	ITextureAtlas CreateAtlas();

	ISubTexture CreateSubTexture(
		string name,
		Coordinate position,
		Dimension size
	);

	INinePatch CreateNinePatch(
		string name,
		Coordinate position,
		Dimension size,
		int leftBorder,
		int rightBorder,
		int topBorder,
		int bottomBorder
	) {
		ArgumentNullException.ThrowIfNull( name );
		ArgumentOutOfRangeException.ThrowIfNegative( leftBorder );
		ArgumentOutOfRangeException.ThrowIfNegative( rightBorder );
		ArgumentOutOfRangeException.ThrowIfNegative( topBorder );
		ArgumentOutOfRangeException.ThrowIfNegative( bottomBorder );

		int centerWidth = (int)size.Width - leftBorder - rightBorder;
		int middleHeight = (int)size.Height - topBorder - bottomBorder;
		if( centerWidth < 0 || middleHeight < 0 ) {
			throw new ArgumentException( "The borders exceed the nine-patch bounds." );
		}

		int x0 = (int)position.X;
		int x1 = (int)position.X + leftBorder;
		int x2 = (int)position.X + (int)size.Width - rightBorder;
		int y0 = (int)position.Y;
		int y1 = (int)position.Y + topBorder;
		int y2 = (int)position.Y + (int)size.Height - bottomBorder;

		ISubTexture topLeft = CreateSubTexture( $"{name}_TopLeft", new Coordinate( x0, y0 ), new Dimension( leftBorder, topBorder ) );
		ISubTexture topCenter = CreateSubTexture( $"{name}_TopCenter", new Coordinate( x1, y0 ), new Dimension( centerWidth, topBorder ) );
		ISubTexture topRight = CreateSubTexture( $"{name}_TopRight", new Coordinate( x2, y0 ), new Dimension( rightBorder, topBorder ) );

		ISubTexture middleLeft = CreateSubTexture( $"{name}_MiddleLeft", new Coordinate( x0, y1 ), new Dimension( leftBorder, middleHeight ) );
		ISubTexture middleCenter = CreateSubTexture( $"{name}_MiddleCenter", new Coordinate( x1, y1 ), new Dimension( centerWidth, middleHeight ) );
		ISubTexture middleRight = CreateSubTexture( $"{name}_MiddleRight", new Coordinate( x2, y1 ), new Dimension( rightBorder, middleHeight ) );

		ISubTexture bottomLeft = CreateSubTexture( $"{name}_BottomLeft", new Coordinate( x0, y2 ), new Dimension( leftBorder, bottomBorder ) );
		ISubTexture bottomCenter = CreateSubTexture( $"{name}_BottomCenter", new Coordinate( x1, y2 ), new Dimension( centerWidth, bottomBorder ) );
		ISubTexture bottomRight = CreateSubTexture( $"{name}_BottomRight", new Coordinate( x2, y2 ), new Dimension( rightBorder, bottomBorder ) );

		return new NinePatch(
			name,
			topLeft,
			topCenter,
			topRight,
			middleLeft,
			middleCenter,
			middleRight,
			bottomLeft,
			bottomCenter,
			bottomRight
		);
	}

	void Bind( int textureUnit = 0 );
}
