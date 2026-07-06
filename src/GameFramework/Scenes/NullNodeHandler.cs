namespace GameFramework.Scenes;

internal sealed class NullNodeHandler: ISceneNodeHandler {

	public static readonly NullNodeHandler Instance = new NullNodeHandler();

	private NullNodeHandler() { }
}
