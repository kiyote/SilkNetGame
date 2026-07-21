namespace GameFramework.Textures;

internal sealed class TextureManager : ITextureManager {

	private readonly IDevice _device;
	private readonly Dictionary<string, ITexture> _textures = [];
	private bool _isDisposed;

	public TextureManager(
		IDevice device
	) {
		_device = device;
	}
	public ITexture this[string id] => _textures[id];

	public ITexture Load(
		string id,
		string textureFile,
		bool premultiplyAlpha = true,
		TextureFilter filter = TextureFilter.Linear
	) {
		if (_textures.TryGetValue( id, out ITexture? texture )) {
			return texture;
		}
		texture = _device.LoadTexture( textureFile, premultiplyAlpha, filter );
		_textures[id] = texture;
		return texture;
	}

	public ITexture Get(
		string id
	) {
		return _textures[id];
	}

	void IDisposable.Dispose() {
		if( !_isDisposed ) {
			foreach( ITexture texture in _textures.Values ) {
				texture.Dispose();
			}
			_isDisposed = true;
		}
		GC.SuppressFinalize( this );
	}

	public void Clear() {
		foreach( ITexture texture in _textures.Values ) {
			texture.Dispose();
		}
		_textures.Clear();
	}

	~TextureManager() {
		if( !_isDisposed ) {
			System.Diagnostics.Debug.Fail( $"Warning: Texture was not disposed properly!" );

		}
	}
}
