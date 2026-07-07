namespace GameFramework.Scenes;

public interface ISceneManager {

	SceneNode Root { get; }

	bool MouseDown(
		Coordinate coordinate,
		int button
	);

	bool MouseUp(
		Coordinate coordinate,
		int button
	);

	bool MouseMove(
		Coordinate coordinate
	);
}
