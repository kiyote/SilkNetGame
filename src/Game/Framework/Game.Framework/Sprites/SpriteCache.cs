using Game.Framework.Textures;
using StbRectPackSharp;

namespace Game.Framework.Sprites;

internal sealed class CachedSprite {
	public required string AssetId { get; init; }

	public required Sprite Sprite { get; init; }

	public required int AllocatedWidth { get; init; }
	public required int AllocatedHeight { get; init; }
}

/// <summary>
/// Manages framebuffer space using StbRectPackSharp's Packer, LIFO batch eviction, and position change notifications.
/// </summary>
internal sealed class SpriteCache : IDisposable {
	private readonly Packer _packer;
	private readonly int _atlasWidth;
	private readonly int _atlasHeight;
	private readonly int _minBlockSize;

	private readonly Dictionary<string, CachedSprite> _registry;
	//private readonly List<CachedSprite> _insertionOrder;

	public SpriteCache(
		int atlasWidth,
		int atlasHeight,
		int minBlockSize = 48
	) {
		_atlasWidth = atlasWidth;
		_atlasHeight = atlasHeight;
		_minBlockSize = minBlockSize;

		_packer = new Packer( _atlasWidth, _atlasHeight );
		_registry = [];
		//_insertionOrder = [];
	}

	public Sprite this[string id] {
		get {
			return _registry[id].Sprite;
		}
	}

	public Sprite Insert(
		string assetId,
		ITexture texture,
		int width,
		int height
	) {
		if( _registry.TryGetValue( assetId, out CachedSprite? cached ) ) {
			return cached.Sprite;
		}

		// Quantize size to reduce fragmentation
		int allocatedWidth =  ( width + _minBlockSize - 1 ) / _minBlockSize  * _minBlockSize;
		int allocatedHeight =  ( height + _minBlockSize - 1 ) / _minBlockSize  * _minBlockSize;

		PackerRectangle result = AllocateSpace( allocatedWidth, allocatedHeight );

		Sprite sprite = new Sprite(
			texture,
			result.X,
			result.Y,
			width,
			height
		);
		CachedSprite cachedSprite = new CachedSprite {
			AssetId = assetId,
			Sprite = sprite,
			AllocatedWidth = allocatedWidth,
			AllocatedHeight = allocatedHeight
		};

		_registry.Add( assetId, cachedSprite );
		//_insertionOrder.Add( cachedSprite );

		return sprite;
	}

	public bool Remove(
		string assetId
	) {
		/*
		if( !_registry.TryGetValue( assetId, out CachedSprite? cachedSprite ) ) {
			return false;
		}
		*/

		return _registry.Remove( assetId );
		//_insertionOrder.Remove( cachedSprite );

		//return true;
	}

	public void Dispose() {
		_packer.Dispose();
	}

	/// <summary>
	/// Finds space for the new texture, evicting the newest items in batches if full.
	/// </summary>
	private PackerRectangle AllocateSpace(
		int width,
		int height
	) {
		PackerRectangle result = _packer.PackRect( width, height, null );
		if( result != null ) {
			return result;
		}

		/*
		// Batch eviction loop
		while( _insertionOrder.Count > 0 ) {
			int batchSize = Math.Max( 1, _insertionOrder.Count / 10 );

			for( int i = 0; i < batchSize && _insertionOrder.Count > 0; i++ ) {
				int lastIndex = _insertionOrder.Count - 1;
				CachedSprite spriteToEvict = _insertionOrder[lastIndex];

				_insertionOrder.RemoveAt( lastIndex );
				_registry.Remove( spriteToEvict.AssetId );
			}

			// Rebuild packer once per batch instead of per item
			RebuildPacker();

			result = _packer.PackRect( width, height, null );
			if( result != null ) {
				return result;
			}
		}
		*/

		throw new InvalidOperationException( "Texture is too large to fit in remaining atlas space." );
	}

	/*
	private void RebuildPacker() {
		_packer = new Packer( _atlasWidth, _atlasHeight );

		foreach( CachedSprite sprite in _insertionOrder ) {
			PackerRectangle result = _packer.PackRect( texture.AllocatedWidth, texture.AllocatedHeight, null ) ?? throw new InvalidOperationException( "Failed to repack existing textures during eviction." );

			// Check if the coordinates actually shifted
			if( texture.X != result.X
				|| texture.Y != result.Y
			) {
				int oldX = texture.X;
				int oldY = texture.Y;

				texture.X = result.X;
				texture.Y = result.Y;

				// Notify listeners to update external GPU regions or UV maps
				TextureMoved?.Invoke( this, new TextureMovedEventArgs {
					AssetId = texture.AssetId,
					OldX = oldX,
					OldY = oldY,
					NewX = texture.X,
					NewY = texture.Y
				} );
			} else {
				texture.X = result.X;
				texture.Y = result.Y;
			}
		}
	}
	*/
}
