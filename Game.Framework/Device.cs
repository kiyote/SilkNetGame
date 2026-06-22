using Microsoft.Extensions.DependencyInjection;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

using Monitor = Silk.NET.Windowing.Monitor;

namespace Game.Framework;

public sealed class Device {

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

	public Device ConfigureServices(
		Action<IServiceCollection> configure
	) {
		_configureServices = configure;

		return this;
	}

	public void Run() {
		_window.Run();
		_input.Dispose();
		_window.Dispose();
		_gl.Dispose();
	}

	public Texture LoadTexture(
		string textureFile,
		bool premultiplyAlpha = true
	) {
		return new Texture(
			_gl,
			textureFile,
			premultiplyAlpha
		);
	}

	public Framebuffer CreateRenderTarget(
		uint width,
		uint height
	) {
		return new Framebuffer(
			_gl,
			width,
			height
		);
	}

	public TtfFont LoadFont(
		string fontFile,
		float fontSize
	) {
		return new TtfFont(
			_gl,
			fontFile,
			fontSize
		);
	}

	public void Exit() {
		_window.Close();
	}

	private void Load() {
		if( _windowMode != WindowMode.Windowed ) {
			_window.WindowBorder = WindowBorder.Hidden;
			_window.TopMost = true;
		}

		int width = _window.Size.X;
		int height = _window.Size.Y;

		_gl = _window.CreateOpenGL() ?? throw new InvalidOperationException( "Unable to obtain GL context." );
		_input = _window.CreateInput() ?? throw new InvalidOperationException( "Unable to obtain input context." );

		Display display = new Display( _window, _gl, width, height );

		IServiceCollection serviceCollection = new ServiceCollection();
		serviceCollection.AddSingleton( _gl );
		serviceCollection.AddSingleton( _input );
		serviceCollection.AddSingleton( display );
		serviceCollection.AddSingleton( this );
		serviceCollection.AddSingleton<SpriteBatch, SpriteBatchPMO>();
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
		_services?.GetRequiredService<SpriteBatch>().Dispose();
		_services?.GetRequiredService<Keyboard>().Dispose();
	}
}
