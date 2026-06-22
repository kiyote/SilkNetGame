using System.Numerics;

namespace Game.Framework;

public abstract class RenderTarget {

	internal abstract Matrix4x4 Projection { get; }

	internal abstract uint Width { get; }

	internal abstract uint Height { get; }

	internal abstract void Bind();

}
