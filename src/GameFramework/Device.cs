using GameFramework.Fonts;
using GameFramework.Sprites;
using GameFramework.Textures;
using Microsoft.Extensions.DependencyInjection;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

using Monitor = Silk.NET.Windowing.Monitor;

namespace GameFramework;

internal sealed class Device : IDevice {

	private readonly IWindow _window;
	private readonly WindowMode _windowMode;

	private IInputContext _input = default!;
	private GL _gl = default!;
	private GameBase _game = default!;
	private IServiceProvider _services = default!;

	// We make the action nullable so it's suitable for
	// collection at some point in the future and releasing
	// whatever it may have captured.
	private Action<IServiceCollection>? _configureServices;

	public Device(
		string title,
		int width,
		int height,
		bool vsync,
		WindowMode windowMode
	) {
		Silk.NET.Windowing.Glfw.GlfwWindowing.RegisterPlatform();
		Silk.NET.Input.Glfw.GlfwInput.RegisterPlatform();

		IMonitor mainMonitor = Monitor.GetMainMonitor( null );
		Vector2D<int> screenResolution = mainMonitor.Bounds.Size;

		WindowOptions options = WindowOptions.Default;
		options.API = new GraphicsAPI( ContextAPI.OpenGL, new APIVersion( 4, 4 ) );
		options.Title = title;
		options.VSync = vsync;

		if( windowMode == WindowMode.Windowed ) {
			options.Size = new Vector2D<int>( width, height );
			options.WindowState = WindowState.Normal;
		} else if( windowMode == WindowMode.Fullscreen ) {
			options.Size = new Vector2D<int>( screenResolution.X, screenResolution.Y );
			options.WindowState = WindowState.Fullscreen;
			options.WindowBorder = WindowBorder.Hidden;
		} else {
			options.Size = new Vector2D<int>( screenResolution.X, screenResolution.Y );
			options.WindowState = WindowState.Normal;
			options.WindowBorder = WindowBorder.Hidden;
			options.Position = mainMonitor.Bounds.Origin;
		}
		_windowMode = windowMode;

		_window = Window.Create( options );

		_window.Load += Load;
		_window.Closing += Unload;
	}

	IDevice IDevice.ConfigureServices(
		Action<IServiceCollection> configure
	) {
		_configureServices = configure;

		return this;
	}

	void IDevice.Run() {
		_window.Run();
		_input.Dispose();
		_window.Dispose();
		_gl.Dispose();
	}

	ITexture IDevice.LoadTexture(
		string textureFile,
		bool premultiplyAlpha
	) {
		return new Textures.Texture(
			_gl,
			textureFile,
			premultiplyAlpha
		);
	}

	IFramebuffer IDevice.CreateFramebuffer(
		uint width,
		uint height
	) {
		return new Framebuffer(
			_gl,
			width,
			height
		);
	}

	ISpriteAtlas IDevice.CreateSpriteAtlas(
		ITexture texture,
		ISpriteBatch spriteBatch
	) {
		return new SpriteAtlas(
			_gl,
			texture,
			spriteBatch
		);
	}

	IFont IDevice.LoadTtfFont(
		string fontFile,
		float fontSize
	) {
		return new TtfFont(
			_gl,
			fontFile,
			fontSize
		);
	}

	void IDevice.Terminate() {
		_window.Close();
	}

	private void Load() {
		if( _windowMode != WindowMode.Windowed ) {
			_window.WindowBorder = WindowBorder.Hidden;
			_window.TopMost = true;
		}

		int width = _window.Size.X;
		int height = _window.Size.Y;

#pragma warning disable CA1508 // CreateOpnGL/CreateInput are not nullable-aware
		_gl = _window.CreateOpenGL() ?? throw new InvalidOperationException( "Unable to obtain GL context." );
		_input = _window.CreateInput() ?? throw new InvalidOperationException( "Unable to obtain input context." );
#pragma warning restore CA1508

#pragma warning disable CA2000 // Display is put int to DI container and disposed later
		Display display = new Display( _window, _gl, width, height );
#pragma warning restore CA2000

		IServiceCollection serviceCollection = new ServiceCollection();
		serviceCollection.AddSingleton( _gl );
		serviceCollection.AddSingleton( _input );
		serviceCollection.AddSingleton( _window );
		serviceCollection.AddSingleton<IDisplay>( display );
		serviceCollection.AddSingleton<IDevice>( this );
		serviceCollection.AddSingleton<ISpriteBatch, SpriteBatchPMO>();
		serviceCollection.AddSingleton<Keyboard>();
		_configureServices?.Invoke( serviceCollection );
		_services = serviceCollection.BuildServiceProvider();
		_configureServices = null;
		GC.Collect(); // Force a cleanup after prepping, but before the game runs

		_game = _services.GetRequiredService<GameBase>();

		_window.Update += _game.Update;
		_window.Render += _game.Render;
	}

	private void Unload() {
		_game.Dispose();
		_services?.GetRequiredService<ISpriteBatch>().Dispose();
		_services?.GetRequiredService<Keyboard>().Dispose();
	}
}
