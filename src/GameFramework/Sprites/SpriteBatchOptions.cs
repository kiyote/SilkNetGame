namespace GameFramework.Sprites;

/// <summary>
/// Tunables for <see cref="SpriteBatchPMO"/>, supplied at DI registration time.
///
/// <para><see cref="MaxSpritesPerFrame"/> is the per-segment capacity: it sizes the
/// persistent VBO/index buffer and sets the mid-batch overflow-flush threshold.</para>
///
/// <para><see cref="NumSegments"/> is the fence-ring depth: how many in-flight
/// FlushBatch draws can exist before a write must stall on the oldest segment's fence.
/// Workloads with many small Start/Finish pairs rotate segments rapidly, so a deeper
/// ring paired with a smaller per-segment capacity can reduce CPU stalls.</para>
/// </summary>
public sealed class SpriteBatchOptions {

	public const int DefaultMaxSpritesPerFrame = 10_000;
	public const int DefaultNumSegments = 3; // Triple Buffering

	// A ushort index buffer can only address vertices 0..65535, and each sprite uses
	// 4 vertices, so a single segment can hold at most 16383 sprites.
	public const int MaxAddressableSprites = ushort.MaxValue / 4;

	private int _maxSpritesPerFrame = DefaultMaxSpritesPerFrame;
	private int _numSegments = DefaultNumSegments;

	public int MaxSpritesPerFrame {
		get => _maxSpritesPerFrame;
		set {
			ArgumentOutOfRangeException.ThrowIfLessThan( value, 1, nameof( value ) );
			ArgumentOutOfRangeException.ThrowIfGreaterThan( value, MaxAddressableSprites, nameof( value ) );
			_maxSpritesPerFrame = value;
		}
	}

	public int NumSegments {
		get => _numSegments;
		set {
			// A ring needs at least two segments to overlap CPU writes with GPU reads.
			ArgumentOutOfRangeException.ThrowIfLessThan( value, 2, nameof( value ) );
			_numSegments = value;
		}
	}
}
