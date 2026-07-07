using System.Drawing;
using System.Numerics;

namespace GameFramework;

public interface IRenderTarget : IDisposable {

	Matrix4x4 Projection { get; }

	int Width { get; }

	int Height { get; }

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
		int x,
		int y,
		int w,
		int h
	);

	void ClearClip();

	void Bind();

}
