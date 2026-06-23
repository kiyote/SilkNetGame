using System.Drawing;
using Microsoft.Extensions.DependencyInjection;

namespace Game.Framework;

public static class ExtensionMethods {

	private static readonly Color _transparentBlack = Color.FromArgb( 0, 0, 0, 0 );

	public static IServiceCollection AddGame<T>(
		this IServiceCollection services
	) where T : GameBase {
		return services.AddSingleton<GameBase, T>();
	}

	extension(Color) {
		public static Color TransparentBlack => _transparentBlack;
	}

	extension( IDevice ) {
		public static IDevice Create( string title, int width, int height, bool vsync, WindowMode windowMode ) {
			return new Device( title, width, height, vsync, windowMode );
		}
	}
}
