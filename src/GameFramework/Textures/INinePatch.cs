namespace GameFramework.Textures;

public interface INinePatch {
	string Name { get; }
	ISubTexture BottomCenter { get; }
	ISubTexture BottomLeft { get; }
	ISubTexture BottomRight { get; }
	ISubTexture MiddleCenter { get; }
	ISubTexture MiddleLeft { get; }
	ISubTexture MiddleRight { get; }
	ISubTexture TopCenter { get; }
	ISubTexture TopLeft { get; }
	ISubTexture TopRight { get; }
}
