using StbRectPackSharp;

namespace Game.Framework;

/// <summary>
/// Holds metadata for a texture allocated inside the framebuffer atlas.
/// </summary>
public class CachedTexture : IComparable<CachedTexture> {
	public string AssetId { get; set; }

	// Final pixel coordinates on the framebuffer for rendering
	public int X { get; set; }
	public int Y { get; set; }

	// Original requested dimensions (used for UV mapping calculations)
	public int Width { get; set; }
	public int Height { get; set; }

	// Padded grid dimensions reserved in the packer to prevent fragmentation
	public int AllocatedWidth { get; set; }
	public int AllocatedHeight { get; set; }

	// LFU tracking metrics
	public double UseCount { get; set; }
	public long InsertionId { get; set; }

	public int CompareTo( CachedTexture? other ) {
		if( other is null ) {
			return 1;
		}

		// Primary sort: Lowest frequency first (Min-Heap behavior in SortedSet)
		int compare = UseCount.CompareTo( other.UseCount );
		if( compare != 0 ) {
			return compare;
		}

		// Secondary sort: FIFO (oldest first) as a tie-breaker for equal frequencies
		return InsertionId.CompareTo( other.InsertionId );
	}
}

/// <summary>
/// Manages framebuffer space using StbRectPackSharp's Packer, LFU eviction, and size quantization.
/// </summary>
public class LfuTextureCache {
	private Packer _packer;
	private readonly int _atlasWidth;
	private readonly int _atlasHeight;
	private readonly int _minBlockSize;

	private readonly Dictionary<string, CachedTexture> _registry;
	private readonly SortedSet<CachedTexture> _lfuHeap;

	private long _globalInsertionCounter = 0;
	private long _accessCounter = 0;

	// Frequency decay settings to ensure old active items don't hog space forever
	private const long DecayInterval = 10000; // Decay every 10,000 accesses
	private const double DecayFactor = 0.5;   // Halve the weights during decay

	/// <summary>
	/// Initializes a new instance of the LfuTextureCache.
	/// </summary>
	/// <param name="width">Width of the framebuffer container.</param>
	/// <param name="height">Height of the framebuffer container.</param>
	/// <param name="minBlockSize">Minimum pixel granularity for allocations (e.g., 32, 64).</param>
	public LfuTextureCache( int width, int height, int minBlockSize = 64 ) {
		_atlasWidth = width;
		_atlasHeight = height;
		_minBlockSize = minBlockSize;

		// Instantiating the official StbRectPackSharp main utility class
		_packer = new Packer( width, height );
		_registry = [];
		_lfuHeap = [];
	}

	/// <summary>
	/// Attempts to get the coordinates of an existing texture and bumps its frequency.
	/// </summary>
	public bool TryGetTextureCoordinates( string assetId, out int x, out int y ) {
		if( _registry.TryGetValue( assetId, out CachedTexture? tex ) ) {
			// Maintain SortedSet tracking invariants by removing and re-adding modified item
			_lfuHeap.Remove( tex );
			tex.UseCount += 1.0;
			_lfuHeap.Add( tex );

			x = tex.X;
			y = tex.Y;

			// Handle generational frequency decay if threshold is crossed
			_accessCounter++;
			if( _accessCounter >= DecayInterval ) {
				ApplyFrequencyDecay();
			}

			return true;
		}

		x = y = 0;
		return false;
	}

	/// <summary>
	/// Reserves space inside the framebuffer, evicting LFU entries incrementally if full.
	/// </summary>
	public CachedTexture AllocateSpace( string assetId, int rawWidth, int rawHeight ) {
		if( _registry.ContainsKey( assetId ) ) {
			throw new ArgumentException( $"Asset '{assetId}' is already cached.", nameof( assetId ) );
		}

		// Quantize requested size up to the nearest block alignment boundary
		int packedWidth = ( rawWidth + _minBlockSize - 1 ) / _minBlockSize * _minBlockSize;
		int packedHeight = ( rawHeight + _minBlockSize - 1 ) / _minBlockSize * _minBlockSize;

		packedWidth = Math.Max( packedWidth, _minBlockSize );
		packedHeight = Math.Max( packedHeight, _minBlockSize );

		if( packedWidth > _atlasWidth || packedHeight > _atlasHeight ) {
			throw new InvalidOperationException( "Requested texture is too large for the entire framebuffer container." );
		}

		// 1. First Pass: Attempt clean packing into currently available spaces.
		// Packer.PackRect returns a PackerRectangle if successful, or null if it cannot fit.
		PackerRectangle packedResult = _packer.PackRect( packedWidth, packedHeight, assetId );

		if( packedResult != null ) {
			return CommitToCache( assetId, packedResult, rawWidth, rawHeight, packedWidth, packedHeight );
		}

		// 2. Fallback Loop: Evict least-frequently-used items one by one and defragment.
		while( _lfuHeap.Count > 0 ) {
			// Evict the absolute least-frequently-used node
			CachedTexture? lowest = _lfuHeap.Min;
			if( lowest is not null ) {
				_lfuHeap.Remove( lowest );
				_registry.Remove( lowest.AssetId );
			}

			// Rebuild the packing context entirely to combine scattered gaps into large contiguous zones
			RebuildAndConsolidateContext();

			// Re-attempt packing the new layout configuration
			packedResult = _packer.PackRect( packedWidth, packedHeight, assetId );
			if( packedResult != null ) {
				// Note: Surrounding systems must listen to this layout change 
				// and re-render surviving textures to their newly assigned shifted coordinates.
				return CommitToCache( assetId, packedResult, rawWidth, rawHeight, packedWidth, packedHeight );
			}
		}

		throw new InvalidOperationException( "Failed to allocate space even after evicting all cached elements." );
	}

	/// <summary>
	/// Registers a successfully packed allocation into tracking collections.
	/// </summary>
	private CachedTexture CommitToCache( string assetId, PackerRectangle rect, int rw, int rh, int pw, int ph ) {
		var newTex = new CachedTexture {
			AssetId = assetId,
			X = rect.X, // Using StbRectPackSharp's uppercase coordinate properties
			Y = rect.Y,
			Width = rw,
			Height = rh,
			AllocatedWidth = pw,
			AllocatedHeight = ph,
			UseCount = 1.0, // Baseline entry frequency weight
			InsertionId = _globalInsertionCounter++
		};

		_registry[assetId] = newTex;
		_lfuHeap.Add( newTex );
		return newTex;
	}

	/// <summary>
	/// Clears the packer grid completely and repacks surviving elements tightly.
	/// </summary>
	private void RebuildAndConsolidateContext() {
		// Re-instantiate the Packer to drop previous structural node configurations completely
		_packer = new Packer( _atlasWidth, _atlasHeight );

		// Re-pack remaining textures sequentially into the fresh canvas matrix
		foreach( CachedTexture tex in _lfuHeap ) {
			PackerRectangle pr = _packer.PackRect( tex.AllocatedWidth, tex.AllocatedHeight, tex.AssetId )
				?? throw new InvalidOperationException( "Fatal error: Existing items failed to repack during consolidation." );

			// Re-bind math coordinates to match new structural layouts
			tex.X = pr.X;
			tex.Y = pr.Y;
		}
	}

	/// <summary>
	/// Shrinks all use metrics down to prevent stale high-frequency spikes from blocking evictions.
	/// </summary>
	private void ApplyFrequencyDecay() {
		_accessCounter = 0;

		// Temporary storage needed to re-index tree structure safely without breaking Sort invariants
		List<CachedTexture> tempElements = [.. _lfuHeap];
		_lfuHeap.Clear();

		foreach( CachedTexture tex in tempElements ) {
			tex.UseCount *= DecayFactor;
			_lfuHeap.Add( tex );
		}
	}
}
