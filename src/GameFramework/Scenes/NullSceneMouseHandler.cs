namespace GameFramework.Scenes;

internal sealed class NullSceneMouseHandler: ISceneMouseHandler {

	public static readonly NullSceneMouseHandler Instance = new NullSceneMouseHandler();

	private NullSceneMouseHandler() { }
}
