
namespace GameFramework.Textures;

internal sealed class SubTexture : ISubTexture {

	private Coordinate _position;
	private Dimension _size;

	private float _u1;
	private float _v1;
	private float _u2;
	private float _v2;

	private readonly Coordinate _storedPosition;
	private readonly Dimension _storedSize;

	public SubTexture(
		string name,
		ITexture texture,
		Coordinate position,
		Dimension size,
		Coordinate storedPosition,
		Dimension storedSize
	) {
		Name = name;
		Texture = texture;
		_storedPosition = storedPosition;
		_storedSize = storedSize;
		Update( position, size );
	}

	public void Update(
		Coordinate position,
		Dimension size
	) {
		_position = position;
		_size = size;
		_u1 = position.X / (float)Texture.TextureSize.Width;
		_v1 = position.Y / (float)Texture.TextureSize.Height;
		_u2 = ( position.X + size.Width ) / (float)Texture.TextureSize.Width;
		_v2 = ( position.Y + size.Height ) / (float)Texture.TextureSize.Height;
	}

	public uint Id => Texture.Id;

	public Coordinate StoredPosition => _storedPosition;

	public Dimension StoredSize => _storedSize;

	public string Name { get; }

	public ITexture Texture { get; }

	public Coordinate Position => _position;

	public Dimension Size => _size;

	public float U1 => _u1;

	public float V1 => _v1;

	public float U2 => _u2;

	public float V2 => _v2;

	public bool Equals(
		ISubTexture? other
	) {
		if( other is null ) {
			return false;
		}
		return other.U1 == _u1 && other.V1 == _v1 && other.U2 == _u2 && other.V2 == _v2;
	}

	public override bool Equals( object? obj ) {
		return Equals( obj as ISubTexture );
	}

	public override int GetHashCode() {
		return HashCode.Combine( _u1, _v1, _u2, _v2 );
	}
}
