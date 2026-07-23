using GameFramework;
using GameFramework.Fonts;
using GameFramework.Scenes;
using GameFramework.Scenes.Components;
using GameFramework.Sprites;
using GameFramework.Textures;
using Silk.NET.Input;

namespace SilkNetGame;

internal sealed class MyGame : GameBase, IKeyHandler, IMouseHandler {

	private const float ScaleFactor = 2.0f;
	private readonly IDevice _device;
	private readonly IDisplay _display;
	private readonly IFont _font;
	private readonly TextureDebug _textureDebug;
	private readonly ISpriteBatch _spriteBatch;
	private readonly ITexture _terrain;

	private readonly ITextureAtlas _source;

	private readonly IRenderTexture _surface;
	private readonly float _scaledWidth;
	private readonly float _scaledHeight;

	private readonly ITextureManager _textureManager;
	private readonly ISceneManager _sceneManager;

	private readonly ITexture _ui;
	private readonly ITextureAtlas _uiAtlas;
	private readonly INinePatch _panel;
	private readonly INinePatch _buttonUp;
	private readonly INinePatch _buttonDown;

	private readonly ITextureAtlas _textAtlas;

	private Coordinate _mousePosition;

	public MyGame(
		IDevice device,
		IDisplay display,
		ISpriteBatch spriteBatch,
		Keyboard keyboard,
		Mouse mouse,
		ITextureManager textureManager,
		TextureDebug textureDebug
	) {
		_device = device;
		_display = display;
		_spriteBatch = spriteBatch;
		_textureDebug = textureDebug;
		_sceneManager = new SceneManager( _display.Size );

		keyboard.AddHandler( this );
		mouse.AddHandler( this );

		//_font = device.LoadTtfFont( "Roboto_Condensed-Medium.ttf", 16 );
		_font = device.LoadTtfFont( "m5x7.ttf", 16 );

		int scaledHeight = (int)Math.Ceiling( display.Size.Height / ScaleFactor );
		int scaledWidth = (int)Math.Ceiling( display.Size.Width / ScaleFactor );
		_scaledWidth = scaledWidth * ScaleFactor;
		_scaledHeight = scaledHeight * ScaleFactor;
		_surface = device.CreateRenderTexture( new Dimension( scaledWidth, scaledHeight ), TextureFilter.Nearest );

		_textAtlas = device.CreateTextureAtlas( new Dimension( 256, 256 ), TextureFilter.Linear );
		_textAtlas.Create( "mouse_position", _font, "0000,0000"u8, 0xFFFFFFFFu, 0x00000000u, 1);

		_textureManager = textureManager;
		_terrain = _textureManager.Load( "terrain", "terrain.png", true, TextureFilter.Nearest );

		_ui = _textureManager.Load( "ui", "ui.png", true, TextureFilter.Nearest );
		_uiAtlas = _device.CreateTextureAtlas( new Dimension( 1024, 1024 ), TextureFilter.Nearest );
		_panel = _uiAtlas.Create( "panel", _ui, new Coordinate( 190, 0 ), new Dimension( 100, 100 ), 8, 8, 8, 8 );
		_buttonDown = _uiAtlas.Create( "button_down", _ui, new Coordinate( 0, 143 ), new Dimension( 190, 45 ), 8, 8, 8, 8 );
		_buttonUp = _uiAtlas.Create( "button_up", _ui, new Coordinate( 0, 188 ), new Dimension( 190, 49 ), 8, 8, 8, 11 );

		_source = _device.CreateTextureAtlas( new Dimension( 1024, 1024 ), TextureFilter.Nearest );
		_source.Create( "tall_grass", _terrain, new Coordinate( 384, 256 ), new Dimension( 96, 96 ) );

		SceneNode panelNode = _sceneManager.Root.AddPanel( new Coordinate( 128, 128 ), new Dimension( 96 * 2, 96 * 2 ), _panel );
		panelNode.AddButton( new Coordinate( 10, 10 ), new Dimension( 86 * 2, 48 ), _buttonUp, _buttonDown, 3, 0xFFFFFFFFu, 0x00FF00FFu );
	}

	public bool KeyDown(
		Key key
	) {
		return true;
	}

	public bool KeyUp(
		Key key
	) {
		if( key == Key.Escape ) {
			_textureDebug.Write( _source.Texture, "surface.png" );
			_device.Terminate();
			return true;
		}
		return true;
	}

	public bool MouseDown(
		Coordinate position,
		int button
	) {
		Coordinate scaled = new Coordinate( (int)( position.X / ScaleFactor ), (int)( position.Y / ScaleFactor ) );
		_sceneManager.MouseDown( scaled, button );
		return true;
	}

	public bool MouseUp(
		Coordinate position,
		int button
	) {
		Coordinate scaled = new Coordinate( (int)( position.X / ScaleFactor ), (int)( position.Y / ScaleFactor ) );
		_sceneManager.MouseUp( scaled, button );
		return true;
	}

	public bool MouseMove(
		Coordinate position
	) {
		Coordinate scaled = new Coordinate( (int)( position.X / ScaleFactor ), (int)( position.Y / ScaleFactor ) );
		if( scaled != _mousePosition) {
			_mousePosition = scaled;
			string text = $"{scaled.X},{scaled.Y}";
			_textAtlas.Update( "mouse_position", _font, text, 0xFFFFFFFFu, 0x00000000u, 1 );
		}
		_sceneManager.MouseMove( scaled );
		return true;
	}

	public override void Render(
		double deltaTime
	) {
		_surface.Clear( 0x6495EDFF );

		_spriteBatch.Start( _surface );
		_sceneManager.Render( _spriteBatch );
		_spriteBatch.Draw( _textAtlas.SubTexture( "mouse_position" ), 0, 0 );
		_spriteBatch.Finish();

		// Copy the final surface to the display, scaling it up by the ScaleFactor
		_display.Clear( 0x000000FF );
		_spriteBatch.Start( _display );
		_spriteBatch.Draw( _surface.Texture.Id, 0.0f, 0.0f, _scaledWidth, _scaledHeight, 0.0f, 0.0f, 1.0f, 1.0f );

		_spriteBatch.Finish();
	}

	public override void Update(
		double deltaTime
	) {
		_sceneManager.Update( deltaTime );
	}

	public override void Dispose() {
		_textAtlas.Dispose();
		_source.Dispose();
		_uiAtlas.Dispose();
		_surface.Dispose();
		_terrain.Dispose();
		_ui.Dispose();
		_font.Dispose();
	}
}
