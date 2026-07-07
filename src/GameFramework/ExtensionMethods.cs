using System.Drawing;
using Microsoft.Extensions.DependencyInjection;
using Silk.NET.Core.Contexts;
using Silk.NET.GLFW;
using Silk.NET.Windowing;

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

public static class GlfwExtensionMethods {

	public unsafe static float GetDisplayDPI(
		this Glfw glfw,
		IWindow window
	) {
		INativeWindow? nativeWindow = window.Native ?? throw new InvalidOperationException( "Unable to retrieve window handle." );
		nint? glfwWindow = nativeWindow.Glfw ?? throw new InvalidOperationException( "Unable to retrieve window pointer." );
		void* rawVoidPtr = (void*)glfwWindow;
		WindowHandle* rawWindowHandle = (WindowHandle*)rawVoidPtr;

		Silk.NET.GLFW.Monitor* primaryMonitor = GetCurrentMonitor( glfw, rawWindowHandle );
		glfw.GetMonitorPhysicalSize( primaryMonitor, out int widthMm, out int heightMm );
		Silk.NET.GLFW.VideoMode* videoMode = glfw.GetVideoMode( primaryMonitor );
		int widthPx = videoMode->Width;
		int heightPx = videoMode->Height;

		// Constant factor to convert millimeters to inches
		const double MmToInches = 25.4d;

		// 6. Calculate Horizontal and Vertical DPI
		double dpiX = widthPx / ( widthMm / MmToInches );
		//double dpiY = heightPx / ( heightMm / MmToInches );

		return (float)dpiX;
	}

	private unsafe static Silk.NET.GLFW.Monitor* GetCurrentMonitor(
		Glfw glfw,
		WindowHandle* window
	) {
		// 1. Get window boundaries
		glfw.GetWindowPos( window, out int wx, out int wy );
		glfw.GetWindowSize( window, out int ww, out int wh );

		// 2. Fetch all available monitors
		Silk.NET.GLFW.Monitor** monitors = glfw.GetMonitors( out int monitorCount );

		Silk.NET.GLFW.Monitor* bestMonitor = null;
		int maxOverlapArea = 0;

		for( int i = 0; i < monitorCount; i++ ) {
			Silk.NET.GLFW.Monitor* monitor = monitors[i];

			// 3. Get monitor screen positioning boundaries
			glfw.GetMonitorPos( monitor, out int mx, out int my );
			Silk.NET.GLFW.VideoMode* mode = glfw.GetVideoMode( monitor );
			if( mode == null ) {
				continue;
			}

			int mw = mode->Width;
			int mh = mode->Height;

			// 4. Calculate overlapping intersection rectangle bounds
			int overlapX = Math.Max( 0, Math.Min( wx + ww, mx + mw ) - Math.Max( wx, mx ) );
			int overlapY = Math.Max( 0, Math.Min( wy + wh, my + mh ) - Math.Max( wy, my ) );
			int overlapArea = overlapX * overlapY;

			// 5. Pick the monitor with the most screen presence
			if( overlapArea > maxOverlapArea ) {
				maxOverlapArea = overlapArea;
				bestMonitor = monitor;
			}
		}

		// Fallback to primary if the window is completely off-screen
		return bestMonitor != null ? bestMonitor : glfw.GetPrimaryMonitor();
	}


}
