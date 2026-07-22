namespace GameFramework.Textures;

internal class NinePatch : INinePatch {

	public NinePatch(
		string name,
		ISubTexture topLeft,
		ISubTexture topCenter,
		ISubTexture topRight,
		ISubTexture middleLeft,
		ISubTexture middleCenter,
		ISubTexture middleRight,
		ISubTexture bottomLeft,
		ISubTexture bottomCenter,
		ISubTexture bottomRight
	) {
		Name = name;
		TopLeft = topLeft;
		TopCenter = topCenter;
		TopRight = topRight;
		MiddleLeft = middleLeft;
		MiddleCenter = middleCenter;
		MiddleRight = middleRight;
		BottomLeft = bottomLeft;
		BottomCenter = bottomCenter;
		BottomRight = bottomRight;
	}

	public string Name { get; }

	public ISubTexture TopLeft { get; }

	public ISubTexture TopCenter { get; }

	public ISubTexture TopRight { get; }

	public ISubTexture MiddleLeft { get; }

	public ISubTexture MiddleCenter { get; }

	public ISubTexture MiddleRight { get; }

	public ISubTexture BottomLeft { get; }

	public ISubTexture BottomCenter { get; }

	public ISubTexture BottomRight { get; }
}
