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
		_generation = new int[capacity];

		// A slot is "free" once its progress reaches 1.0. Seed every slot at 1.0 so
		// the whole pool reads as free/complete until StartTween claims a slot.
		Array.Fill( _progress, 1.0f );
	}

	// Allocation: O(N) worst-case scan for a free slot
	public TweenHandle StartTween(
		float start,
		float target,
		TimeSpan duration,
		Easing ease
	) {
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual( duration, TimeSpan.Zero, nameof( duration ) );

		// Convert the wall-clock interval into a per-second progress rate once, so
		// Update can keep advancing progress by (rate * deltaTime) where deltaTime
		// is measured in seconds.
		float speed = 1.0f / (float)duration.TotalSeconds;

		for( int i = 0; i < _capacity; i++ ) {
			if( _progress[i] >= 1.0f ) {
				_start[i] = start;
				_target[i] = target;
				_current[i] = start;
				_progress[i] = 0.0f; // Initialize the 0->1 progress
				_speed[i] = speed;
				_easeTypes[i] = ease;
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
			if( _progress[i] >= 1.0f ) {
				continue;
			}

			_progress[i] = Math.Min( 1.0f, _progress[i] + ( _speed[i] * dt ) );

			float t = _progress[i];
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
			|| _progress[handle.Index] >= 1.0f
		) {
			value = 0.0f;
			return false;
		}

		value = _current[handle.Index];

		// Retire the slot: pinning progress at 1.0 frees it and bumping the
		// generation invalidates this handle so a later Update recomputing the
		// slot can never surface the post-cancel value through it.
		_progress[handle.Index] = 1.0f;
		_generation[handle.Index] = unchecked(_generation[handle.Index] + 1);
		return true;
	}

	public bool IsComplete(
		TweenHandle handle
	) {
		ArgumentOutOfRangeException.ThrowIfNegative( handle.Index );
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual( handle.Index, _capacity );

		return _generation[handle.Index] != handle.Generation
			|| _progress[handle.Index] >= 1.0f;
	}

	public ReadOnlySpan<float> CurrentValues => _current;
}
