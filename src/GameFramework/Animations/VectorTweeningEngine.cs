using System.Numerics;
using System.Runtime.InteropServices;

namespace GameFramework.Animations;

public class VectorTweeningEngine : ITweeningEngine {
	private readonly int _capacity;
	private readonly int _vectorSize;
	private readonly int _alignedCapacity; // _capacity rounded up to a whole number of Vector<float>.Count
	private readonly Vector<float> _oneVec = new Vector<float>( 1.0f );
	private readonly Vector<float> _twoVec = new Vector<float>( 2.0f );
	private readonly Vector<float> _halfVec = new Vector<float>( 0.5f );
	private readonly Vector<int> _quadInCode = new Vector<int>( (int)Easing.QuadIn );
	private readonly Vector<int> _quadOutCode = new Vector<int>( (int)Easing.QuadOut );
	private readonly Vector<int> _loopCode = new Vector<int>( (int)LoopMode.Loop );
	private readonly Vector<int> _bounceCode = new Vector<int>( (int)LoopMode.Bounce );

	// SoA Data Arrays
	private readonly float[] _start;
	private readonly float[] _target;
	private readonly float[] _current;
	private readonly float[] _progress;
	private readonly float[] _speed;
	private readonly Easing[] _easeTypes;
	private readonly LoopMode[] _modes;
	private readonly int[] _generation;

	public VectorTweeningEngine(
		int capacity = 100
	) {
		_capacity = capacity;

		// Pad the SoA arrays up to a whole number of SIMD vectors so Update's
		// vector-width strides never read or write past the end of the arrays.
		_vectorSize = Vector<float>.Count;
		_alignedCapacity = ( capacity + _vectorSize - 1 ) / _vectorSize * _vectorSize;

		_start = new float[_alignedCapacity];
		_target = new float[_alignedCapacity];
		_current = new float[_alignedCapacity];
		_progress = new float[_alignedCapacity];
		_speed = new float[_alignedCapacity];
		_easeTypes = new Easing[_alignedCapacity];
		_modes = new LoopMode[_alignedCapacity]; // defaults to OneShot (0)
		_generation = new int[_alignedCapacity];

		// A slot is "free" once it is a completed OneShot (progress >= 1.0). Seed every
		// slot at 1.0 so the whole pool reads as free/complete until StartTween claims
		// a slot (modes default to OneShot).
		Array.Fill( _progress, 1.0f );
	}

	// A slot is available for reuse only when it is a completed OneShot. Loop and
	// Bounce slots stay occupied even though their wrapped progress may reach or
	// exceed 1.0, so they are never reclaimed until TryCancel resets them back to
	// OneShot.
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
		// the SIMD Update loop can keep advancing progress by (rate * deltaTime)
		// where deltaTime is measured in seconds.
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
		Vector<float> dtVec = new Vector<float>( dt );

		ReadOnlySpan<int> easeCodes = MemoryMarshal.Cast<Easing, int>( _easeTypes.AsSpan() );
		ReadOnlySpan<int> modeCodes = MemoryMarshal.Cast<LoopMode, int>( _modes.AsSpan() );

		for( int i = 0; i < _alignedCapacity; i += _vectorSize ) {
			// 1. Load data into SIMD registers
			Vector<float> p = new Vector<float>( _progress, i );
			Vector<float> s = new Vector<float>( _speed, i );

			// 2. Advance progress for every lane. There is no need to mask inactive
			// lanes: completed OneShot slots are pinned at 1.0 by the clamp below, and
			// generational handles keep consumers from ever reading a dead slot.
			p = Vector.Add( p, Vector.Multiply( s, dtVec ) );

			// 3. Wrap/clamp progress per mode with branchless masks. OneShot clamps to
			// 1.0 (also marking the slot free); Loop wraps into [0,1); Bounce wraps
			// into [0,2). The stored progress is the wrapped/clamped value so it never
			// grows without bound over a long-running loop.
			Vector<int> mode = new Vector<int>( modeCodes[i..] );
			Vector<float> loopMask = Vector.AsVectorSingle( Vector.Equals( mode, _loopCode ) );
			Vector<float> bounceMask = Vector.AsVectorSingle( Vector.Equals( mode, _bounceCode ) );

			Vector<float> oneShotP = Vector.Min( p, _oneVec );
			Vector<float> loopP = Vector.Subtract( p, Vector.Floor( p ) );
			Vector<float> bounceP = Vector.Subtract( p, Vector.Multiply( _twoVec, Vector.Floor( Vector.Multiply( p, _halfVec ) ) ) );

			Vector<float> storedP = oneShotP;
			storedP = Vector.ConditionalSelect( loopMask, loopP, storedP );
			storedP = Vector.ConditionalSelect( bounceMask, bounceP, storedP );
			storedP.CopyTo( _progress, i );

			// 4. Curve position t. It equals the stored progress for OneShot and Loop;
			// for Bounce it is the triangle 1 - |bounceP - 1| (0->1->0) so the value
			// eases in both directions.
			Vector<float> bounceT = Vector.Subtract( _oneVec, Vector.Abs( Vector.Subtract( bounceP, _oneVec ) ) );
			Vector<float> tProg = Vector.ConditionalSelect( bounceMask, bounceT, storedP );

			// 5. Easing Calculations (Branchless processing for different types)
			Vector<float> start = new Vector<float>( _start, i );
			Vector<float> target = new Vector<float>( _target, i );

			// We calculate the potential curves for ALL lanes uniformly
			Vector<float> tLinear = tProg;
			Vector<float> tQuadIn = Vector.Multiply( tProg, tProg );
			Vector<float> tQuadOut = Vector.Multiply( tProg, Vector.Subtract( _twoVec, tProg ) );

			// Select the correct curve per lane with branchless masks: compare the
			// ease codes to each type and blend the precomputed curves. Lanes default
			// to the linear curve and are overwritten where a Quad ease matches.
			Vector<int> ease = new Vector<int>( easeCodes[i..] );
			Vector<float> quadInMask = Vector.AsVectorSingle( Vector.Equals( ease, _quadInCode ) );
			Vector<float> quadOutMask = Vector.AsVectorSingle( Vector.Equals( ease, _quadOutCode ) );

			Vector<float> t = tLinear;
			t = Vector.ConditionalSelect( quadInMask, tQuadIn, t );
			t = Vector.ConditionalSelect( quadOutMask, tQuadOut, t );

			// current = start + (target - start) * t, across the whole vector width
			Vector<float> current = Vector.Add( start, Vector.Multiply( Vector.Subtract( target, start ), t ) );
			current.CopyTo( _current, i );
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

	public ReadOnlySpan<float> CurrentValues => _current.AsSpan( 0, _capacity );
}
