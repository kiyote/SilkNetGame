using System.Numerics;
using System.Runtime.InteropServices;

namespace GameFramework.Animations;

public class VectorTweeningEngine : ITweeningEngine {
	private readonly int _capacity;
	private readonly int _vectorSize;
	private readonly int _alignedCapacity; // _capacity rounded up to a whole number of Vector<float>.Count
	private readonly Vector<float> _oneVec = new Vector<float>( 1.0f );
	private readonly Vector<float> _twoVec = new Vector<float>( 2.0f );
	private readonly Vector<int> _quadInCode = new Vector<int>( (int)EaseType.QuadIn );
	private readonly Vector<int> _quadOutCode = new Vector<int>( (int)EaseType.QuadOut );

	// SoA Data Arrays
	private readonly float[] _start;
	private readonly float[] _target;
	private readonly float[] _current;
	private readonly float[] _progress;
	private readonly float[] _speed;
	private readonly EaseType[] _easeTypes;
	private readonly int[] _generation;

	public VectorTweeningEngine(
		int capacity
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
		_easeTypes = new EaseType[_alignedCapacity];
		_generation = new int[_alignedCapacity];

		// A slot is "free" once its progress reaches 1.0. Seed every slot at 1.0 so
		// the whole pool reads as free/complete until StartTween claims a slot.
		Array.Fill( _progress, 1.0f );
	}

	// Allocation: O(N) worst-case scan for a free slot
	public TweenHandle StartTween(
		float start,
		float target,
		TimeSpan duration,
		EaseType ease
	) {
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual( duration, TimeSpan.Zero, nameof( duration ) );

		// Convert the wall-clock interval into a per-second progress rate once, so
		// the SIMD Update loop can keep advancing progress by (rate * deltaTime)
		// where deltaTime is measured in seconds.
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
		Vector<float> dtVec = new Vector<float>( dt );

		ReadOnlySpan<int> easeCodes = MemoryMarshal.Cast<EaseType, int>( _easeTypes.AsSpan() );

		for( int i = 0; i < _alignedCapacity; i += _vectorSize ) {
			// 1. Load data into SIMD registers
			Vector<float> p = new Vector<float>( _progress, i );
			Vector<float> s = new Vector<float>( _speed, i );

			// 2. Advance progress for every lane. Completed slots stay pinned at 1.0
			// by the clamp below, so there is no need to mask inactive lanes:
			// generational handles keep consumers from ever reading a dead slot.
			p = Vector.Add( p, Vector.Multiply( s, dtVec ) );
			p = Vector.Min( p, _oneVec ); // Clamp to 1.0; also marks the slot free
			p.CopyTo( _progress, i );

			// 3. Easing Calculations (Branchless processing for different types)
			Vector<float> start = new Vector<float>( _start, i );
			Vector<float> target = new Vector<float>( _target, i );

			// We calculate the potential curves for ALL lanes uniformly
			Vector<float> tLinear = p;
			Vector<float> tQuadIn = Vector.Multiply( p, p );
			Vector<float> tQuadOut = Vector.Multiply( p, Vector.Subtract( _twoVec, p ) );

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

	public ReadOnlySpan<float> CurrentValues => _current.AsSpan( 0, _capacity );
}
