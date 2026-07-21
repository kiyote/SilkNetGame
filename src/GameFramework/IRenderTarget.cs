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
		Bounds clip
	);

	void SetClip(
		Coordinate position,
		Dimension size
	);

	void ClearClip();

	Bounds? Clip { get; }

	void Bind();

}
