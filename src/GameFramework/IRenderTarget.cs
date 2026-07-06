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

	// Restricts subsequent drawing to the supplied rectangle, expressed in this
	// render target's own coordinate space. The clip is applied as a scissor
	// rectangle every time Bind (or Clear) is called.
	void SetClip(
		Rectangle clip
	);

	void SetClip(
		int x,
		int y,
		int w,
		int h
	);

	// Removes any previously set clip so subsequent drawing covers the whole
	// render target again. Takes effect on the next Bind.
	void ClearClip();

	void Bind();

}
