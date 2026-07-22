using GameFramework.Animations;
using GameFramework.Scenes;
using GameFramework.Sprites;
using GameFramework.Textures;

namespace SilkNetGame;

internal sealed class MapNodeRenderer : ISceneRenderHandler {

	private readonly ITextureAtlas _terrainAtlas;
	private readonly ITweeningEngine _tween;
	private readonly TweenHandle _handle;
	private readonly TweenHandle _alpha;


	public MapNodeRenderer(
		ITextureAtlas terrainAtlas,
		ITweeningEngine tween
	) {
		_terrainAtlas = terrainAtlas;
		_tween = tween;

		_alpha = _tween.StartTween( 0, 1, TimeSpan.FromMilliseconds( 300 ), Easing.Linear, LoopMode.Bounce );
		_handle = _tween.StartTween( 0, MathF.Tau, TimeSpan.FromSeconds( 1 ), Easing.Linear, LoopMode.Loop );
	}

	public void OnRender(
		SceneNode node,
		ISpriteBatch spriteBatch
	) {
		_tween.TryGetCurrentValue( _handle, out float interval );
		_tween.TryGetCurrentValue( _alpha, out float alpha );
		int offset = (int)(( MathF.Cos( interval ) + 1.0f ) * 0.5f * 100.0f);
		spriteBatch.Draw( node.Clip.Position.Add( offset, offset ), node.Clip.Size, _terrainAtlas["tall_grass"], Lerp.Colour( 0xFFFFFFFFu, 0xFFFFFF00u, alpha ) );
	}

	public void OnUpdate(
		SceneNode node,
		double detlaTime
	) {
	}
}
