namespace GameFramework.Animations;

// Identifies a single tween slot together with the generation that owned it when
// the handle was issued. The generation lets the engine detect stale handles:
// once a tween completes its slot can be recycled, and the recycled slot is given
// a new generation so an old handle no longer refers to the live tween.
public readonly record struct TweenHandle(
	int Index,
	int Generation
);
