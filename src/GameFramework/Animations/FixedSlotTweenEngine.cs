using System;
using System.Numerics;

namespace GameFramework.Animations;

public class FixedSlotTweenEngine {
	private readonly int _capacity;

	// SoA Data Arrays
	private readonly float[] _start;
	private readonly float[] _target;
	private readonly float[] _current;
	private readonly float[] _progress;
	private readonly float[] _speed;
	private readonly float[] _isActive; // 1.0f = Active, 0.0f = Inactive
	private readonly EaseType[] _easeTypes;

	public FixedSlotTweenEngine(
		int capacity
	) {
		_capacity = capacity;
		_start = new float[capacity];
		_target = new float[capacity];
		_current = new float[capacity];
		_progress = new float[capacity];
		_speed = new float[capacity];
		_isActive = new float[capacity];
		_easeTypes = new EaseType[capacity];
	}

	// Allocation: O(N) worst-case scan for a free slot
	public int StartTween(
		float start,
		float target,
		float duration,
		EaseType ease
	) {
		for( int i = 0; i < _capacity; i++ ) {
			if( _isActive[i] == 0.0f ) // Found an empty slot
			{
				_start[i] = start;
				_target[i] = target;
				_current[i] = start;
				_progress[i] = 0.0f;
				_speed[i] = 1.0f / duration;
				_easeTypes[i] = ease;
				_isActive[i] = 1.0f; // Mark as active
				return i; // This index is their fixed handle
			}
		}
		throw new InvalidOperationException( "Tween engine pool exhausted! No free slots available." );
	}

	public void Update(
		float deltaTime
	) {
		if( !Vector.IsHardwareAccelerated ) {
			UpdateScalar( deltaTime );
			return;
		}

		int vectorSize = Vector<float>.Count;
		Vector<float> dtVec = new Vector<float>( deltaTime );
		Vector<float> oneVec = new Vector<float>( 1.0f );
		Vector<float> zeroVec = new Vector<float>( 0.0f );
		Vector<float> twoVec = new Vector<float>( 2.0f );

		for( int i = 0; i < _capacity; i += vectorSize ) {
			// 1. Load data into SIMD registers
			Vector<float> mask = new Vector<float>( _isActive, i );
			Vector<float> p = new Vector<float>( _progress, i );
			Vector<float> s = new Vector<float>( _speed, i );

			// 2. Advance progress ONLY for active slots (multiply speed by mask)
			Vector<float> activeSpeed = Vector.Multiply( s, mask );
			p = Vector.Add( p, Vector.Multiply( activeSpeed, dtVec ) );
			p = Vector.Min( p, oneVec ); // Clamp to 1.0
			p.CopyTo( _progress, i );

			// 3. Easing Calculations (Branchless processing for different types)
			Vector<float> start = new Vector<float>( _start, i );
			Vector<float> target = new Vector<float>( _target, i );

			// We calculate the potential curves for ALL lanes uniformly
			Vector<float> tLinear = p;
			Vector<float> tQuadIn = Vector.Multiply( p, p );
			Vector<float> tQuadOut = Vector.Multiply( p, Vector.Subtract( twoVec, p ) );

			// Select the correct curve per lane based on scalar metadata fallback
			// Or use simplified mixed vector array logic. For maximum SIMD purity
			// with fixed slots, we evaluate lanes individually for easing assignment:
			Vector<float> chosenT = zeroVec;
			for( int lane = 0; lane < vectorSize; lane++ ) {
				float tVal = _easeTypes[i + lane] switch {
					EaseType.QuadIn => tQuadIn[lane],
					EaseType.QuadOut => tQuadOut[lane],
					_ => tLinear[lane]
				};
				// Reconstruct vectorized progress modifier
				_current[i + lane] = _start[i + lane] + ( ( _target[i + lane] - _start[i + lane] ) * tVal );
			}

			// 4. Auto-deactivate completed animations
			for( int lane = 0; lane < vectorSize; lane++ ) {
				if( _progress[i + lane] >= 1.0f && _isActive[i + lane] == 1.0f ) {
					_isActive[i + lane] = 0.0f; // Free the slot immediately
				}
			}
		}
	}

	private void UpdateScalar(
		float deltaTime
	) {
		for( int i = 0; i < _capacity; i++ ) {
			if( _isActive[i] == 0.0f ) { continue; }

			_progress[i] = Math.Min( 1.0f, _progress[i] + ( _speed[i] * deltaTime ) );

			float t = _progress[i];
			float easedT = _easeTypes[i] switch {
				EaseType.QuadIn => t * t,
				EaseType.QuadOut => t * ( 2.0f - t ),
				_ => t
			};

			_current[i] = _start[i] + ( ( _target[i] - _start[i] ) * easedT );

			if( _progress[i] >= 1.0f ) {
				_isActive[i] = 0.0f;
			}
		}
	}
}
