namespace GameFramework.Textures.Tests;

[TestFixture]
internal sealed class SubTextureTests {

	// Minimal ITexture whose only meaningful member is TextureSize, which is all
	// SubTexture consults to compute UV coordinates.
	private sealed class StubTexture : ITexture {
		public StubTexture( Dimension size ) {
			TextureSize = size;
		}

		public Dimension TextureSize { get; }
		public uint Id => 0;
		public float HalfX => 0;
		public float HalfY => 0;
		public TextureFilter Filter => default;
		public string Name => nameof( StubTexture );
		public Coordinate Position => default;
		public Dimension Size => TextureSize;
		public Coordinate StoredPosition => default;
		public Dimension StoredSize => TextureSize;
		public ITexture Texture => this;
		public float U1 => 0;
		public float V1 => 0;
		public float U2 => 1;
		public float V2 => 1;

		public void Clear( Coordinate position, Dimension size, uint colour = 0 ) => throw new NotSupportedException();
		public void Copy( Coordinate position, ITexture source, Coordinate sourcePosition, Dimension sourceSize ) => throw new NotSupportedException();
		public ITextureAtlas CreateAtlas() => throw new NotSupportedException();
		public ISubTexture CreateSubTexture( string name, Coordinate position, Dimension size ) => throw new NotSupportedException();
		public INinePatch CreateNinePatch( string name, Coordinate position, Dimension size, int leftBorder, int rightBorder, int topBorder, int bottomBorder ) => throw new NotSupportedException();
		public void Bind( int textureUnit = 0 ) => throw new NotSupportedException();
		public void Update( Coordinate position, Dimension size ) => throw new NotSupportedException();
		public bool Equals( ISubTexture? other ) => ReferenceEquals( this, other );
		public void Dispose() { }
	}

	private readonly List<StubTexture> _textures = [];

	[TearDown]
	public void TearDown() {
		foreach( StubTexture texture in _textures ) {
			texture.Dispose();
		}
		_textures.Clear();
	}

	private SubTexture CreateSubTexture(
		Dimension textureSize,
		Coordinate position,
		Dimension size
	) {
		StubTexture texture = new StubTexture( textureSize );
		_textures.Add( texture );
		return new SubTexture( "sub", texture, position, size, position, size );
	}

	[Test]
	public void Ctor_ComputesUvFromPositionAndSizeOverTextureSize() {
		SubTexture sub = CreateSubTexture(
			new Dimension( 200, 100 ),
			new Coordinate( 50, 25 ),
			new Dimension( 100, 50 )
		);

		using( Assert.EnterMultipleScope() ) {
			Assert.That( sub.U1, Is.EqualTo( 0.25f ).Within( 0.0001f ) ); // 50 / 200
			Assert.That( sub.V1, Is.EqualTo( 0.25f ).Within( 0.0001f ) ); // 25 / 100
			Assert.That( sub.U2, Is.EqualTo( 0.75f ).Within( 0.0001f ) ); // (50+100) / 200
			Assert.That( sub.V2, Is.EqualTo( 0.75f ).Within( 0.0001f ) ); // (25+50) / 100
		}
	}

	[Test]
	public void Ctor_ExposesPositionSizeAndStoredMetadata() {
		SubTexture sub = CreateSubTexture(
			new Dimension( 200, 100 ),
			new Coordinate( 10, 20 ),
			new Dimension( 30, 40 )
		);

		using( Assert.EnterMultipleScope() ) {
			Assert.That( sub.Position, Is.EqualTo( new Coordinate( 10, 20 ) ) );
			Assert.That( sub.Size, Is.EqualTo( new Dimension( 30, 40 ) ) );
			Assert.That( sub.StoredPosition, Is.EqualTo( new Coordinate( 10, 20 ) ) );
			Assert.That( sub.StoredSize, Is.EqualTo( new Dimension( 30, 40 ) ) );
			Assert.That( sub.Name, Is.EqualTo( "sub" ) );
		}
	}

	[Test]
	public void Update_RecomputesUvForNewRegion() {
		SubTexture sub = CreateSubTexture(
			new Dimension( 200, 100 ),
			new Coordinate( 0, 0 ),
			new Dimension( 10, 10 )
		);

		sub.Update( new Coordinate( 100, 50 ), new Dimension( 100, 50 ) );

		using( Assert.EnterMultipleScope() ) {
			Assert.That( sub.U1, Is.EqualTo( 0.5f ).Within( 0.0001f ) );
			Assert.That( sub.V1, Is.EqualTo( 0.5f ).Within( 0.0001f ) );
			Assert.That( sub.U2, Is.EqualTo( 1.0f ).Within( 0.0001f ) );
			Assert.That( sub.V2, Is.EqualTo( 1.0f ).Within( 0.0001f ) );
		}
	}

	[Test]
	public void Equals_MatchingUvs_AreEqualAndShareHashCode() {
		SubTexture a = CreateSubTexture( new Dimension( 200, 100 ), new Coordinate( 50, 25 ), new Dimension( 100, 50 ) );
		SubTexture b = CreateSubTexture( new Dimension( 200, 100 ), new Coordinate( 50, 25 ), new Dimension( 100, 50 ) );

		using( Assert.EnterMultipleScope() ) {
			Assert.That( a.Equals( (ISubTexture)b ), Is.True );
			Assert.That( a.GetHashCode(), Is.EqualTo( b.GetHashCode() ) );
		}
	}

	[Test]
	public void Equals_DifferentUvs_AreNotEqual() {
		SubTexture a = CreateSubTexture( new Dimension( 200, 100 ), new Coordinate( 50, 25 ), new Dimension( 100, 50 ) );
		SubTexture b = CreateSubTexture( new Dimension( 200, 100 ), new Coordinate( 0, 0 ), new Dimension( 100, 50 ) );

		using( Assert.EnterMultipleScope() ) {
			Assert.That( a.Equals( (ISubTexture)b ), Is.False );
			Assert.That( a.Equals( (ISubTexture?)null ), Is.False );
		}
	}
}
