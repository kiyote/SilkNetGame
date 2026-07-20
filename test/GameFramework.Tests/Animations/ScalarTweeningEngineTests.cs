namespace GameFramework.Animations.Tests;

[TestFixture]
internal sealed class ScalarTweeningEngineTests {

	[Test]
	public void Ctor_ValidCapacity_EngineCreated() {
		ITweeningEngine engine = new ScalarTweeningEngine( 10 );
		Assert.That( engine, Is.Not.Null );
	}

	[Test]
	public void StartTween_NoAvailableSlots_ThrowsInvalidOperationException() {
		ITweeningEngine engine = new ScalarTweeningEngine( 1 );
		engine.StartTween( 0.0f, 1.0f, TimeSpan.FromSeconds( 1.0 ), Easing.Linear );
		Assert.Throws<InvalidOperationException>( () => engine.StartTween( 0.0f, 1.0f, TimeSpan.FromSeconds( 1.0 ), Easing.Linear ) );
	}

	[Test]
	public void StartTween_NonPositiveDuration_ThrowsArgumentOutOfRangeException() {
		ITweeningEngine engine = new ScalarTweeningEngine( 1 );
		Assert.Throws<ArgumentOutOfRangeException>(
			() => engine.StartTween( 0.0f, 1.0f, TimeSpan.Zero, Easing.Linear )
		);
	}

	[Test]
	public void StartTween_FillToCapacity_AllocatesEverySlotThenThrows() {
		const int capacity = 5;
		ITweeningEngine engine = new ScalarTweeningEngine( capacity );

		for( int i = 0; i < capacity; i++ ) {
			TweenHandle handle = engine.StartTween( 0.0f, 1.0f, TimeSpan.FromSeconds( 1.0 ), Easing.Linear );
			Assert.That( handle.Index, Is.EqualTo( i ) );
		}

		Assert.Throws<InvalidOperationException>( () => engine.StartTween( 0.0f, 1.0f, TimeSpan.FromSeconds( 1.0 ), Easing.Linear ) );
	}

	[Test]
	public void Update_LinearEasingHalfwayDone_ValueIsHalfway() {
		ITweeningEngine engine = new ScalarTweeningEngine( 1 );
		TweenHandle handle = engine.StartTween( 0.0f, 1.0f, TimeSpan.FromSeconds( 2.0 ), Easing.Linear );
		engine.Update( 1.0f ); // Halfway through the tween
		using( Assert.EnterMultipleScope() ) {
			Assert.That( engine.TryGetCurrentValue( handle, out float currentValue ), Is.True );
			Assert.That( currentValue, Is.EqualTo( 0.5f ).Within( 0.01f ) );
		}
	}

	[Test]
	public void Update_EachEasingCurve_ProducesExpectedMidpoint() {
		// At t == 0.5: Linear -> 0.5, QuadIn( t*t ) -> 0.25, QuadOut( t*(2-t) ) -> 0.75.
		ITweeningEngine engine = new ScalarTweeningEngine( 3 );
		TweenHandle linear = engine.StartTween( 0.0f, 100.0f, TimeSpan.FromSeconds( 2.0 ), Easing.Linear );
		TweenHandle quadIn = engine.StartTween( 0.0f, 100.0f, TimeSpan.FromSeconds( 2.0 ), Easing.QuadIn );
		TweenHandle quadOut = engine.StartTween( 0.0f, 100.0f, TimeSpan.FromSeconds( 2.0 ), Easing.QuadOut );

		engine.Update( 1.0f ); // Halfway through every tween

		using( Assert.EnterMultipleScope() ) {
			Assert.That( engine.TryGetCurrentValue( linear, out float linearValue ), Is.True );
			Assert.That( linearValue, Is.EqualTo( 50.0f ).Within( 0.01f ) );
			Assert.That( engine.TryGetCurrentValue( quadIn, out float quadInValue ), Is.True );
			Assert.That( quadInValue, Is.EqualTo( 25.0f ).Within( 0.01f ) );
			Assert.That( engine.TryGetCurrentValue( quadOut, out float quadOutValue ), Is.True );
			Assert.That( quadOutValue, Is.EqualTo( 75.0f ).Within( 0.01f ) );
		}
	}

	[Test]
	public void Update_DistinctTargetsPerSlot_AllSlotsTweenedCorrectly() {
		const int capacity = 7;
		ITweeningEngine engine = new ScalarTweeningEngine( capacity );

		TweenHandle[] handles = new TweenHandle[capacity];
		for( int i = 0; i < capacity; i++ ) {
			// Distinct target per slot so a mis-indexed slot would be caught.
			handles[i] = engine.StartTween( 0.0f, i + 1, TimeSpan.FromSeconds( 2.0 ), Easing.Linear );
		}

		engine.Update( 1.0f ); // Halfway through every tween

		for( int i = 0; i < capacity; i++ ) {
			float expected = ( i + 1 ) * 0.5f;
			using( Assert.EnterMultipleScope() ) {
				Assert.That( engine.TryGetCurrentValue( handles[i], out float currentValue ), Is.True );
				Assert.That( currentValue, Is.EqualTo( expected ).Within( 0.01f ) );
			}
		}
	}

	[Test]
	public void Update_MillisecondDurationDrivenBySecondDeltas_ReachesTargetAtInterval() {
		// Tween 10 -> 100 over 75ms, driven by per-frame deltaTime values expressed
		// as fractions of a second.
		ITweeningEngine engine = new ScalarTweeningEngine( 1 );
		TweenHandle handle = engine.StartTween( 10.0f, 100.0f, TimeSpan.FromMilliseconds( 75 ), Easing.Linear );

		// A 25ms frame (0.025s) advances one third of the 75ms interval.
		engine.Update( 0.025f );
		using( Assert.EnterMultipleScope() ) {
			Assert.That( engine.TryGetCurrentValue( handle, out float currentValue ), Is.True );
			Assert.That( currentValue, Is.EqualTo( 40.0f ).Within( 0.01f ) );
			Assert.That( engine.IsComplete( handle ), Is.False );
		}

		// Two more 25ms frames complete the interval and pin the value to target.
		engine.Update( 0.025f );
		engine.Update( 0.025f );
		using( Assert.EnterMultipleScope() ) {
			Assert.That( engine.TryGetCurrentValue( handle, out float currentValue ), Is.True );
			Assert.That( currentValue, Is.EqualTo( 100.0f ).Within( 0.01f ) );
			Assert.That( engine.IsComplete( handle ), Is.True );
		}
	}

	[Test]
	public void Update_DeltaTimeFarExceedsDuration_ClampsToTargetForEveryEase() {
		// A single frame many times longer than the tween duration must pin each
		// tween to its target rather than overshoot. QuadOut( t ) = t * ( 2 - t )
		// would regress toward the start once t > 1, so it only lands on target
		// because progress is clamped to 1.0 before the easing curve is applied.
		ITweeningEngine engine = new ScalarTweeningEngine( 3 );
		TweenHandle linear = engine.StartTween( 0.0f, 10.0f, TimeSpan.FromSeconds( 2.0 ), Easing.Linear );
		TweenHandle quadIn = engine.StartTween( 0.0f, 20.0f, TimeSpan.FromSeconds( 2.0 ), Easing.QuadIn );
		TweenHandle quadOut = engine.StartTween( 0.0f, 30.0f, TimeSpan.FromSeconds( 2.0 ), Easing.QuadOut );

		// 60s is 30x the 2s duration, driving progress far past 1.0 in one frame.
		engine.Update( 60.0f );

		using( Assert.EnterMultipleScope() ) {
			Assert.That( engine.TryGetCurrentValue( linear, out float linearValue ), Is.True );
			Assert.That( linearValue, Is.EqualTo( 10.0f ).Within( 0.01f ) );
			Assert.That( engine.TryGetCurrentValue( quadIn, out float quadInValue ), Is.True );
			Assert.That( quadInValue, Is.EqualTo( 20.0f ).Within( 0.01f ) );
			Assert.That( engine.TryGetCurrentValue( quadOut, out float quadOutValue ), Is.True );
			Assert.That( quadOutValue, Is.EqualTo( 30.0f ).Within( 0.01f ) );

			Assert.That( engine.IsComplete( linear ), Is.True );
			Assert.That( engine.IsComplete( quadIn ), Is.True );
			Assert.That( engine.IsComplete( quadOut ), Is.True );
		}
	}

	[Test]
	public void StartTween_SlotRecycledAfterCompletion_StaleHandleRejectedAndCompletionReported() {
		// Capacity of 1 forces the single slot to be recycled on the second tween.
		ITweeningEngine engine = new ScalarTweeningEngine( 1 );

		TweenHandle first = engine.StartTween( 0.0f, 10.0f, TimeSpan.FromSeconds( 1.0 ), Easing.Linear );

		// duration == deltaTime, so the tween reaches progress 1.0 and frees its slot.
		engine.Update( 1.0f );
		Assert.That( engine.IsComplete( first ), Is.True );

		// Reusing the freed slot yields the same index but a newer generation.
		TweenHandle second = engine.StartTween( 0.0f, 20.0f, TimeSpan.FromSeconds( 1.0 ), Easing.Linear );
		using( Assert.EnterMultipleScope() ) {
			Assert.That( second.Index, Is.EqualTo( first.Index ) );
			Assert.That( second.Generation, Is.Not.EqualTo( first.Generation ) );

			// The stale handle must not silently read the recycled slot's value.
			Assert.That( engine.TryGetCurrentValue( first, out _ ), Is.False );

			// The stale handle still reports complete; the live one does not.
			Assert.That( engine.IsComplete( first ), Is.True );
			Assert.That( engine.IsComplete( second ), Is.False );

			// And the fresh handle reads correctly (still at its start value).
			Assert.That( engine.TryGetCurrentValue( second, out float currentValue ), Is.True );
			Assert.That( currentValue, Is.Zero.Within( 0.01f ) );
		}
	}

	[Test]
	public void TryGetCurrentValue_OutOfRangeHandle_ReturnsFalseWithoutThrowing() {
		ITweeningEngine engine = new ScalarTweeningEngine( 1 );
		using( Assert.EnterMultipleScope() ) {
			Assert.That( engine.TryGetCurrentValue( new TweenHandle( -1, 0 ), out float low ), Is.False );
			Assert.That( low, Is.Zero );
			Assert.That( engine.TryGetCurrentValue( new TweenHandle( 999, 1 ), out float high ), Is.False );
			Assert.That( high, Is.Zero );
		}
	}

	[Test]
	public void TryCancel_RunningTween_ReturnsFrozenValueAndFreesSlot() {
		// Capacity of 1 so we can prove the slot is genuinely freed by reusing it.
		ITweeningEngine engine = new ScalarTweeningEngine( 1 );
		TweenHandle handle = engine.StartTween( 0.0f, 100.0f, TimeSpan.FromSeconds( 4.0 ), Easing.Linear );

		engine.Update( 1.0f ); // Quarter of the way: value is 25.

		using( Assert.EnterMultipleScope() ) {
			// Cancelling a running tween hands back the value frozen at this instant.
			Assert.That( engine.TryCancel( handle, out float cancelled ), Is.True );
			Assert.That( cancelled, Is.EqualTo( 25.0f ).Within( 0.01f ) );

			// The cancel retires the tween: it now reports complete and the handle is
			// invalidated, so later reads never surface a post-cancel value.
			Assert.That( engine.IsComplete( handle ), Is.True );
			Assert.That( engine.TryGetCurrentValue( handle, out _ ), Is.False );
		}

		// A further Update must not resurrect or advance the cancelled slot.
		engine.Update( 1.0f );
		Assert.That( engine.TryGetCurrentValue( handle, out _ ), Is.False );

		// The freed slot is available for a brand-new tween.
		TweenHandle reused = engine.StartTween( 5.0f, 20.0f, TimeSpan.FromSeconds( 2.0 ), Easing.Linear );
		using( Assert.EnterMultipleScope() ) {
			Assert.That( reused.Index, Is.EqualTo( handle.Index ) );
			Assert.That( engine.TryGetCurrentValue( reused, out float currentValue ), Is.True );
			Assert.That( currentValue, Is.EqualTo( 5.0f ).Within( 0.01f ) );
		}
	}

	[Test]
	public void TryCancel_CompletedOrStaleHandle_ReturnsFalse() {
		ITweeningEngine engine = new ScalarTweeningEngine( 1 );
		TweenHandle handle = engine.StartTween( 0.0f, 10.0f, TimeSpan.FromSeconds( 1.0 ), Easing.Linear );

		// Finish the tween normally; there is nothing left to cancel.
		engine.Update( 1.0f );
		using( Assert.EnterMultipleScope() ) {
			Assert.That( engine.IsComplete( handle ), Is.True );
			Assert.That( engine.TryCancel( handle, out float completed ), Is.False );
			Assert.That( completed, Is.Zero );
		}

		// A stale handle (recycled slot) is likewise not cancellable.
		TweenHandle reused = engine.StartTween( 1.0f, 2.0f, TimeSpan.FromSeconds( 1.0 ), Easing.Linear );
		using( Assert.EnterMultipleScope() ) {
			Assert.That( reused.Index, Is.EqualTo( handle.Index ) );
			Assert.That( engine.TryCancel( handle, out _ ), Is.False );

			// An out-of-range handle returns false rather than throwing.
			Assert.That( engine.TryCancel( new TweenHandle( 999, 1 ), out _ ), Is.False );

			// The live handle is untouched by the failed cancels.
			Assert.That( engine.IsComplete( reused ), Is.False );
		}
	}
}
