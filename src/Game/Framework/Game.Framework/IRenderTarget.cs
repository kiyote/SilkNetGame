using System.Drawing;
using System.Numerics;

namespace Game.Framework;

public interface IRenderTarget : IDisposable {

	Matrix4x4 Projection { get; }

	uint Width { get; }

	uint Height { get; }

	void Clear(
		Color colour
	);

	void Bind();

}
