namespace GameFramework.Tests;

[TestFixture]
internal sealed class CoordinateTests {

	[Test]
	public void Subtract_Coordinate_ComponentwiseDifference() {
		Coordinate result = new Coordinate( 10, 20 ).Subtract( new Coordinate( 3, 4 ) );
		Assert.That( result, Is.EqualTo( new Coordinate( 7, 16 ) ) );
	}

	[Test]
	public void Subtract_IntComponents_ComponentwiseDifference() {
		Coordinate result = new Coordinate( 10, 20 ).Subtract( 3, 4 );
		Assert.That( result, Is.EqualTo( new Coordinate( 7, 16 ) ) );
	}

	[Test]
	public void Subtract_Bounds_SubtractsBoundsOrigin() {
		Coordinate result = new Coordinate( 10, 20 ).Subtract( new Bounds( 3, 4, 100, 200 ) );
		Assert.That( result, Is.EqualTo( new Coordinate( 7, 16 ) ) );
	}

	[Test]
	public void Equality_SameComponents_AreEqual() {
		using( Assert.EnterMultipleScope() ) {
			Assert.That( new Coordinate( 1, 2 ), Is.EqualTo( new Coordinate( 1, 2 ) ) );
			Assert.That( new Coordinate( 1, 2 ), Is.Not.EqualTo( new Coordinate( 2, 1 ) ) );
		}
	}

	[Test]
	public void With_OverridesSingleComponent() {
		Coordinate original = new Coordinate( 1, 2 );
		Coordinate updated = original with { Y = 9 };
		Assert.That( updated, Is.EqualTo( new Coordinate( 1, 9 ) ) );
	}
}
