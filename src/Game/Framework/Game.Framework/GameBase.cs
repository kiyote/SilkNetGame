namespace Game.Framework;

public abstract class GameBase : IDisposable {
	public abstract void Dispose();

	public abstract void Render(
		double deltaTime
	);

	public abstract void Update(
		double deltaTime
	);
}
