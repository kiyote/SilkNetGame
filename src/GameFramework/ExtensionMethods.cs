using System.Drawing;
using Microsoft.Extensions.DependencyInjection;

namespace GameFramework;

public static class ExtensionMethods {

	public static IServiceCollection AddGame<T>(
		this IServiceCollection services
	) where T : GameBase {
		return services.AddSingleton<GameBase, T>();
	}

}

#pragma warning disable CA1034 // CA1034 doesn't understand the C#14 extension syntax
public static class ColorExtensions {
	private static readonly Color _transparentBlack = Color.FromArgb( 0, 0, 0, 0 );


	extension( Color ) {
		public static Color TransparentBlack => _transparentBlack;
	}
}

public static class DeviceExtensions {

	extension( IDevice ) {
		public static IDevice Create( string title, int width, int height, bool vsync, WindowMode windowMode ) {
			return new Device( title, width, height, vsync, windowMode );
		}
	}
}
#pragma warning restore CA1034
