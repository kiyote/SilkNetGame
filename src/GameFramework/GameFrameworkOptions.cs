using GameFramework.Sprites;

namespace GameFramework;

/// <summary>
/// Configures optional behaviour of the game framework at registration time.
/// Supplied through the <c>AddGame</c> overload that accepts a configuration
/// delegate.
/// </summary>
public sealed class GameFrameworkOptions {

	public GameFrameworkOptions() {
		SpriteBatch = new SpriteBatchOptions();
	}

	/// <summary>
	/// Tunables for the sprite batch (per-segment capacity and fence-ring depth).
	/// The configured instance is registered so it flows into the sprite batch on
	/// construction.
	/// </summary>
	public SpriteBatchOptions SpriteBatch { get; } 
}
