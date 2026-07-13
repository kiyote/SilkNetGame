
namespace GameFramework.Textures;

internal sealed class SubTexture : ISubTexture {

	private int _left;
	private int _top;
	private int _width;
	private int _height;

	private float _u1;
	private float _v1;
	private float _u2;
	private float _v2;

	private readonly int _allocatedWidth;
	private readonly int _allocatedHeight;

	public SubTexture(
		string name,
		ITexture texture,
		int left,
		int top,
		int width,
		int height,
		int allocatedWidth,
		int allocatedHeight
	) {
		Name = name;
		Texture = texture;
		_allocatedWidth = allocatedWidth;
		_allocatedHeight = allocatedHeight;
		Update( left, top, width, height );
	}

	public void Update(
		int left,
		int top,
		int width,
		int height
	) {
		_left = left;
		_top = top;
		_width = width;
		_height = height;
		_u1 = left / (float)Texture.TextureWidth;
		_v1 = top / (float)Texture.TextureHeight;
		_u2 = ( left + width ) / (float)Texture.TextureWidth;
		_v2 = ( top + height ) / (float)Texture.TextureHeight;
	}

	public uint Id => Texture.Id;

	public int TextureWidth => Texture.TextureWidth;

	public int TextureHeight => Texture.TextureHeight;

	public int AllocatedWidth => _allocatedWidth;

	public int AllocatedHeight => _allocatedHeight;

	public string Name { get; }

	public ITexture Texture { get; }

	public int Left => _left;

	public int Top => _top;

	public int Width => _width;

	public int Height => _height;

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
