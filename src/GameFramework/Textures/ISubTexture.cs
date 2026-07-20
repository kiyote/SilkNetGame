
namespace GameFramework.Textures;

public interface ISubTexture : IEquatable<ISubTexture> {

	string Name { get; }

	Coordinate Position { get; }

	Dimension Size { get; }

	Coordinate StoredPosition { get; }

	Dimension StoredSize { get; }

	ITexture Texture { get; }

	float U1 { get; }

	float V1 { get; }

	float U2 { get; }

	float V2 { get; }

	void Update(
		Coordinate position,
		Dimension size
	);
}
