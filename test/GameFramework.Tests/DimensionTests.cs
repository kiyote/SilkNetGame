namespace GameFramework.Tests;

[TestFixture]
internal sealed class DimensionTests {

	[Test]
	public void Subtract_Dimension_ComponentwiseDifference() {
		Dimension result = new Dimension( 100, 200 ).Subtract( new Dimension( 30, 40 ) );
		Assert.That( result, Is.EqualTo( new Dimension( 70, 160 ) ) );
	}

	[Test]
	public void Equality_SameComponents_AreEqual() {
		using( Assert.EnterMultipleScope() ) {
			Assert.That( new Dimension( 3, 4 ), Is.EqualTo( new Dimension( 3, 4 ) ) );
			Assert.That( new Dimension( 3, 4 ), Is.Not.EqualTo( new Dimension( 4, 3 ) ) );
		}
	}

	[Test]
	public void With_OverridesSingleComponent() {
		Dimension original = new Dimension( 3, 4 );
		Dimension updated = original with { Width = 9 };
		Assert.That( updated, Is.EqualTo( new Dimension( 9, 4 ) ) );
	}
}
