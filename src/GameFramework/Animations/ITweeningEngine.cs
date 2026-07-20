namespace GameFramework.Animations;

public interface ITweeningEngine {

	// Starts a tween and returns a generational handle that stays valid only for
	// the lifetime of this tween; once it completes the slot may be recycled.
	// The tween interpolates from start to target over the wall-clock duration,
	// e.g. TimeSpan.FromMilliseconds( 75 ) to run the animation for 75ms.
	TweenHandle StartTween(
		float start,
		float target,
		TimeSpan duration,
		Easing ease
	);

	// Advances every active tween. deltaTime is the elapsed time since the last
	// frame expressed as a fraction of a second (seconds).
	void Update(
		double deltaTime
	);

	// Non-throwing variant of GetCurrentValue: returns true and the current value
	// for a live handle, or false (with value defaulted) for a stale/invalid handle.
	bool TryGetCurrentValue(
		TweenHandle handle,
		out float value
	);

	// Cancels a running tween, handing back the value it held at the moment of
	// cancellation and freeing its slot. Returns true and that value when a live,
	// still-running tween was cancelled; returns false (with value defaulted) when
	// there was nothing to cancel (stale/invalid handle or already complete).
	// The handle is invalidated by a successful cancel, so later reads through it
	// report stale/complete rather than observing the recycled slot.
	bool TryCancel(
		TweenHandle handle,
		out float value
	);

	// True when the tween the handle refers to has finished (or its slot has
	// already been recycled by a newer tween); false while it is still running.
	bool IsComplete(
		TweenHandle handle
	);

	// Zero-copy view of every slot's current value, indexed by handle.
	ReadOnlySpan<float> CurrentValues { get; }
}
