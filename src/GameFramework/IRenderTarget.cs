using System.Drawing;
using System.Numerics;

namespace GameFramework;

public interface IRenderTarget : IDisposable {

	Matrix4x4 Projection { get; }

	Dimension Size { get; }

	void Clear(
		Color colour
	);

	void Clear(
		uint colour
	);

	void SetClip(
		Rectangle clip
	);

	void SetClip(
		Coordinate position,
		Dimension size
	);

	void ClearClip();

	Rectangle? Clip { get; }

	void Bind();

}
