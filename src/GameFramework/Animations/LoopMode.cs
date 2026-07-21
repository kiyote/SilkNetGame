namespace GameFramework.Animations;

public enum LoopMode: int
{
	// Run once from start to target, then the slot completes and is freed. This is
	// the default and preserves the engine's original one-shot behavior.
	OneShot = 0,

	// Repeat indefinitely: on reaching target the tween restarts at start and plays
	// start->target again, forever, until cancelled. The per-cycle easing is applied
	// to the wrapped 0->1 progress.
	Loop = 1,

	// Ping-pong indefinitely: play start->target, then target->start, then again,
	// forever, until cancelled. The per-cycle easing is applied to the triangle
	// (0->1->0) position so both directions are eased.
	Bounce = 2
}
