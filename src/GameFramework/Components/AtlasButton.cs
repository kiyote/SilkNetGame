using GameFramework.Scenes;

namespace GameFramework.Components;

public sealed class AtlasButton {

	public AtlasButton(
		SceneNode parent,
		int left,
		int top,
		int width,
		int height
	) {
		parent.AddChild( left, top, width, height, new ButtonBehaviour() );
	}
}
