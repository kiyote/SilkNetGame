namespace GameFramework.Animations.Tests;

[TestFixture]
internal sealed class LerpTests {

	[TestCase( 0.0f, 0xFF000040u )]
	[TestCase( 1.0f, 0x0000FFC0u )]
	public void Colour_AtEndpoint_ReturnsEndpointExactly(
		float interval,
		uint expected
	) {
		uint result = Lerp.Colour( 0xFF000040u, 0x0000FFC0u, interval );

		Assert.That( result, Is.EqualTo( expected ) );
	}

	[Test]
	public void Colour_HalfwayBetweenRedAndBlue_InterpolatesInOklab() {
		uint result = Lerp.Colour( 0xFF0000FFu, 0x0000FFFFu, 0.5f );

		Assert.That( result, Is.EqualTo( 0x8C53A2FFu ) );
	}

	[Test]
	public void Colour_HalfwayBetweenAlphaValues_InterpolatesAlphaLinearly() {
		uint result = Lerp.Colour( 0x80808000u, 0x808080FFu, 0.5f );

		Assert.That( result, Is.EqualTo( 0x80808080u ) );
	}

	[TestCase( -0.01f )]
	[TestCase( 1.01f )]
	[TestCase( float.NaN )]
	[TestCase( float.PositiveInfinity )]
	public void Colour_InvalidInterval_ThrowsArgumentOutOfRangeException(
		float interval
	) {
		Assert.Throws<ArgumentOutOfRangeException>( () => Lerp.Colour( 0x000000FFu, 0xFFFFFFFFu, interval ) );
	}
}
