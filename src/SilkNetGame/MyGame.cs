using System.Globalization;
using GameFramework;
using GameFramework.Fonts;
using GameFramework.Sprites;
using GameFramework.Textures;
using Silk.NET.Input;

namespace SilkNetGame;

internal sealed class MyGame : GameBase, IKeyHandler {

	private const float ScaleFactor = 2.0f;
	private readonly IDevice _device;
	private readonly IDisplay _display;
	private readonly Keyboard _keyboard;
	private readonly IFont _font;
	private readonly TextureDebug _textureDebug;
	private readonly ISpriteBatch _spriteBatch;
	private readonly ITexture _terrain;

	private readonly ITextureAtlas _source;
	private readonly ISubTexture _terrainPiece;
	private readonly ISubTexture _rotationLabel;

	private readonly IRenderTexture _surface;
	private readonly ITextureAtlas _textSource;
	private readonly float _scaledWidth;
	private readonly float _scaledHeight;

	public MyGame(
		IDevice device,
		IDisplay display,
		ISpriteBatch spriteBatch,
		Keyboard keyboard,
		TextureDebug textureDebug
	) {
		_device = device;
		_display = display;
		_spriteBatch = spriteBatch;
		_keyboard = keyboard;
		_textureDebug = textureDebug;

		_keyboard.AddHandler( this );

		//_font = device.LoadTtfFont( "Roboto_Condensed-Medium.ttf", 16 );
		_font = device.LoadTtfFont( "m5x7.ttf", 16 );

		int scaledHeight = (int)Math.Ceiling( display.Size.Height / ScaleFactor );
		int scaledWidth = (int)Math.Ceiling( display.Size.Width / ScaleFactor );
		_scaledWidth = scaledWidth * ScaleFactor;
		_scaledHeight = scaledHeight * ScaleFactor;
		_surface = device.CreateRenderTexture( new Dimension( scaledWidth, scaledHeight ), TextureFilter.Nearest );

		_terrain = _device.LoadTexture( "terrain.png", true, TextureFilter.Nearest );

		_source = _device.CreateTextureAtlas( new Dimension( 1024, 1024 ), TextureFilter.Nearest );
		_terrainPiece = _source.Create( "tall_grass", _terrain, new Coordinate( 384, 256 ), new Dimension( 96, 96 ) );

		_lastRotation = "0.00";
		_textSource = _device.CreateTextureAtlas( new Dimension( 1024, 1024 ), TextureFilter.Linear );
		_rotationLabel = _textSource.Create( "rotation", _font, "789.0123"u8, 0xFFFFFFFF, 0x000000FF, 1 );

	}

	public bool KeyDown(
		Key key
	) {
		return false;
	}

	public bool KeyUp(
		Key key
	) {
		if( key == Key.Escape ) {
			_textureDebug.Write( _source.Texture, "surface.png" );
			_device.Terminate();
			return true;
		}
		return false;
	}

	public override void Render(
		double deltaTime
	) {

		_surface.Clear( 0x000000FF );
		_spriteBatch.Start( _surface, _source.Texture );
		_spriteBatch.Draw( 32, 100, 96, 96, _terrainPiece, 0xFFFFFFFF );
		_spriteBatch.Draw( 200, 100, _terrainPiece.Size.Width * 2, _terrainPiece.Size.Height * 2, _terrainPiece, 0xFFFFFFFF );
		_spriteBatch.Finish();

		_spriteBatch.Start( _surface, _textSource.Texture );
		_spriteBatch.Draw( 300, 250, _rotationLabel, _rotation, 0xFFFFFFFF );
		_spriteBatch.Finish();

		// Copy the final surface to the display, scaling it up by the ScaleFactor
		_display.Clear( 0x6495EDFF );
		_spriteBatch.Start( _display, _surface );
		_spriteBatch.Draw( 0.0f, 0.0f, _scaledWidth, _scaledHeight, 0.0f, 0.0f, 1.0f, 1.0f, 0xFFFFFFFF );
		_spriteBatch.Finish();
	}

	private float _rotation;
	private string _lastRotation;

	public override void Update(
		double deltaTime
	) {
		UpdateRotation( deltaTime );

		string rotationText = _rotation.ToString( "000.000", CultureInfo.InvariantCulture );
		if (rotationText != _lastRotation) {
			_lastRotation = rotationText;
			_textSource.Update( "rotation", _font, rotationText, 0xFFFFFFFF, 0x000000FF, 1 );
		}
	}

	private void UpdateRotation(
		double deltaTime
	) {
		const float RadiansPerSecond = MathF.PI / 4.0f;
		_rotation = ( _rotation + ( RadiansPerSecond * (float)deltaTime ) ) % MathF.Tau;
	}

	public override void Dispose() {
		_source.Dispose();
		_textSource.Dispose();
		_surface.Dispose();
		_terrain.Dispose();
		_font.Dispose();
	}
}
