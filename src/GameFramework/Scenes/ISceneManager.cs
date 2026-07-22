using GameFramework.Sprites;

namespace GameFramework.Scenes;

public interface ISceneManager {

	SceneNode Root { get; }

	void Reparent(
		SceneNode node,
		SceneNode newParent
	);

	void Reorder(
		SceneNode node,
		int index
	);

	void ReorderBefore(
		SceneNode node,
		SceneNode before
	);

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

	void Render(
		ISpriteBatch spriteBatch
	);

	void Update(
		double deltaTime
	);
}
