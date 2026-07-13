
namespace GameFramework.Textures;

public interface ISubTexture : IEquatable<ISubTexture> {

	string Name { get; }

	int Left { get; }

	int Top { get; }

	int Width { get; }

	int Height { get; }

	int AllocatedWidth { get; }

	int AllocatedHeight { get; }

	ITexture Texture { get; }

	float U1 { get; }

	float V1 { get; }

	float U2 { get; }

	float V2 { get; }

	void Update(
		int left,
		int top,
		int width,
		int height
	);
}
