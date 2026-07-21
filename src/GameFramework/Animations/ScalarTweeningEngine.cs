namespace GameFramework.Animations;

public class ScalarTweeningEngine : ITweeningEngine {
	private readonly int _capacity;

	// SoA Data Arrays
	private readonly float[] _start;
	private readonly float[] _target;
	private readonly float[] _current;
	private readonly float[] _progress;
	private readonly float[] _speed;
	private readonly Easing[] _easeTypes;
	private readonly LoopMode[] _modes; // OneShot slots free themselves at progress 1.0; Loop/Bounce run until cancelled
	private readonly int[] _generation; // Bumped when a slot is reused so stale handles are detectable

	public ScalarTweeningEngine(
		int capacity
	) {
		_capacity = capacity;
		_start = new float[capacity];
		_target = new float[capacity];
		_current = new float[capacity];
		_progress = new float[capacity];
		_speed = new float[capacity];
		_easeTypes = new Easing[capacity];
		_modes = new LoopMode[capacity]; // defaults to OneShot (0)
		_generation = new int[capacity];

		// A slot is "free" once it is a completed OneShot (progress >= 1.0). Seed every
		// slot at 1.0 so the whole pool reads as free/complete until StartTween claims
		// a slot (modes default to OneShot).
		Array.Fill( _progress, 1.0f );
	}

	// A slot is available for reuse only when it is a completed OneShot. Loop and
	// Bounce slots stay occupied even though their progress may reach or exceed 1.0,
	// so they are never reclaimed until TryCancel resets them back to OneShot.
	private bool IsSlotFree(
		int index
	) {
		return _modes[index] == LoopMode.OneShot && _progress[index] >= 1.0f;
	}

	// Allocation: O(N) worst-case scan for a free slot
	public TweenHandle StartTween(
		float start,
		float target,
		TimeSpan duration,
		Easing ease,
		LoopMode loopMode = LoopMode.OneShot
	) {
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual( duration, TimeSpan.Zero, nameof( duration ) );

		// Convert the wall-clock interval into a per-second progress rate once, so
		// Update can keep advancing progress by (rate * deltaTime) where deltaTime
		// is measured in seconds.
		float speed = 1.0f / (float)duration.TotalSeconds;

		for( int i = 0; i < _capacity; i++ ) {
			if( IsSlotFree( i ) ) {
				_start[i] = start;
				_target[i] = target;
				_current[i] = start;
				_progress[i] = 0.0f; // Initialize the 0->1 progress
				_speed[i] = speed;
				_easeTypes[i] = ease;
				_modes[i] = loopMode;
				_generation[i] = unchecked(_generation[i] + 1);
				return new TweenHandle( i, _generation[i] );
			}
		}
		throw new InvalidOperationException( "Tween engine pool exhausted! No free slots available." );
	}

	public void Update(
		double deltaTime
	) {
		float dt = (float)deltaTime;
		for( int i = 0; i < _capacity; i++ ) {
			// A completed OneShot is pinned at 1.0 and needs no further work. Loop and
			// Bounce never pin, so they keep advancing here until cancelled.
			if( IsSlotFree( i ) ) {
				continue;
			}

			float p = _progress[i] + ( _speed[i] * dt );

			// Advance and wrap progress per mode, then derive the 0->1 curve position t.
			// The stored progress is the wrapped/clamped value so it never grows without
			// bound over a long-running loop.
			float t;
			switch( _modes[i] ) {
				case LoopMode.Loop:
					p -= MathF.Floor( p ); // wrap into [0,1)
					t = p;
					break;
				case LoopMode.Bounce:
					p -= 2.0f * MathF.Floor( p * 0.5f ); // wrap into [0,2)
					t = 1.0f - MathF.Abs( p - 1.0f ); // triangle 0->1->0
					break;
				case LoopMode.OneShot:
				default:
					p = MathF.Min( 1.0f, p ); // clamp; also marks the slot free
					t = p;
					break;
			}

			_progress[i] = p;

			float easedT = _easeTypes[i] switch {
				Easing.QuadIn => t * t,
				Easing.QuadOut => t * ( 2.0f - t ),
				_ => t
			};

			_current[i] = _start[i] + ( ( _target[i] - _start[i] ) * easedT );
		}
	}

	public bool TryGetCurrentValue(
		TweenHandle handle,
		out float value
	) {
		if( handle.Index < 0
			|| handle.Index >= _capacity
			|| _generation[handle.Index] != handle.Generation
		) {
			value = 0.0f;
			return false;
		}
		value = _current[handle.Index];
		return true;
	}

	public bool TryCancel(
		TweenHandle handle,
		out float value
	) {
		if( handle.Index < 0
			|| handle.Index >= _capacity
			|| _generation[handle.Index] != handle.Generation
			|| IsSlotFree( handle.Index )
		) {
			value = 0.0f;
			return false;
		}

		value = _current[handle.Index];

		// Retire the slot: resetting the mode to OneShot and pinning progress at 1.0
		// frees it (this is what ends an otherwise-indefinite Loop/Bounce), and bumping
		// the generation invalidates this handle so a later Update recomputing the slot
		// can never surface the post-cancel value through it.
		_progress[handle.Index] = 1.0f;
		_modes[handle.Index] = LoopMode.OneShot;
		_generation[handle.Index] = unchecked(_generation[handle.Index] + 1);
		return true;
	}

	public bool IsComplete(
		TweenHandle handle
	) {
		ArgumentOutOfRangeException.ThrowIfNegative( handle.Index );
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual( handle.Index, _capacity );

		// Loop/Bounce tweens never complete on their own; only generation reuse (via a
		// successful cancel or a later StartTween) makes their handle report complete.
		return _generation[handle.Index] != handle.Generation
			|| IsSlotFree( handle.Index );
	}

	public ReadOnlySpan<float> CurrentValues => _current;
}
