using GameFramework;
using GameFramework.Fonts;
using GameFramework.Sprites;
using GameFramework.Textures;
using Silk.NET.Input;

namespace SilkNetGame;

internal sealed class MyGame : GameBase, IKeyHandler {

	//private const float ScaleFactor = 2.0f;
	private readonly IDevice _device;
	private readonly IDisplay _display;
	private readonly Keyboard _keyboard;
	private readonly IFont _font;
	//private readonly ISpriteAtlas _atlas;
	//private readonly Rectangle _clip;
	//private readonly TextureDebug _textureDebug;
	private readonly ISpriteBatch _spriteBatch;
	private readonly ITexture _terrain;
	private readonly ISubTexture _terrainPiece;
	//private readonly ITextureAtlas _textBuffer;

	//private readonly IFramebuffer _surface;
	//private readonly float _scaledWidth;
	//private readonly float _scaledHeight;

	public MyGame(
		IDevice device,
		IDisplay display,
		ISpriteBatch spriteBatch,
		Keyboard keyboard
		//TextureDebug textureDebug
	) {
		_device = device;
		_display = display;
		_spriteBatch = spriteBatch;
		_keyboard = keyboard;
		//_textureDebug = textureDebug;

		_keyboard.AddHandler( this );

		//_font = device.LoadTtfFont( "Roboto_Condensed-Medium.ttf", 16 );
		_font = device.LoadTtfFont( "m5x7.ttf", 16 );

		//int scaledHeight = (int)Math.Ceiling( display.Height / ScaleFactor );
		//int scaledWidth = (int)Math.Ceiling( display.Width / ScaleFactor );
		//_scaledWidth = scaledWidth * ScaleFactor;
		//_scaledHeight = scaledHeight * ScaleFactor;
		/*
		_surface = device.CreateFramebuffer( scaledWidth, scaledHeight, TextureFilter.Nearest );

		_textBuffer = device.CreateSpriteAtlas(
			device.CreateFramebuffer( 1024, 1024, TextureFilter.Linear ),
			spriteBatch
		);


		_textBuffer.Add( "hello", _font, "Scaled Text!"u8, 0xFFFFFFFF, 0x707070FF, 1 );
		*/

		_terrain = _device.LoadTexture( "terrain.png", true, TextureFilter.Linear );
		_terrainPiece = _terrain.CreateSubTexture( "terrain1", 384, 256, 96, 96 );
		/*
		_atlas = device.CreateSpriteAtlas(
			device.CreateFramebuffer( 1024, 1024 ),
			spriteBatch
		);

		_atlas.Add( "tall_grass", terrain, 384, 256, 96, 96 );
		_atlas.Add( "hello", _font, "0123456789.0123456789"u8, 0xFFFFFFFF, 0x000000FF, 1 );

		_clip = new Rectangle( 75, 75, 300, 300 );

		terrain.Dispose();
		*/
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
			//_textureDebug.Write( _surface, "surface.png" );
			_device.Terminate();
			return true;
		}
		return false;
	}

	public override void Render(
		double deltaTime
	) {

		_display.Clear( 0x6495EDFF);

		_spriteBatch.Start( _display, _terrain );
		_spriteBatch.Draw( 32, 100, 96, 96, _terrainPiece, 0xFFFFFFFF );
		_spriteBatch.Draw( 200, 100, 96 * 2, 96 * 2, _terrainPiece, 0xFFFFFFFF );
		_spriteBatch.Draw( 500, 100, 96 * 2, 96 * 2, _terrainPiece, 45 * MathF.PI / 180f, 0xFFFFFFFF );
		_spriteBatch.Finish();

		//_surface.Clear( 0x000000FF );

/*
		_textBuffer.Measure( "hello", out int textWidth, out int textHeight );
		_textBuffer.Start( _surface );
		_textBuffer.Draw( "hello", ( _surface.Width / 2 ) - (textWidth / 2), ( _surface.Height / 2 ) - (textHeight / 2) );
		_textBuffer.Finish();

		// Copy the final surface to the display, scaling it up by the ScaleFactor
		_display.Clear( 0x6495EDFF );
		_spriteBatch.Start( _display, _surface );
		_spriteBatch.Draw( 0.0f, 0.0f, _scaledWidth, _scaledHeight, 0.0f, 0.0f, 1.0f, 1.0f, 0xFFFFFFFF );
		_spriteBatch.Finish();
		*/
	}

	public override void Update(
		double deltaTime
	) {
	}

	public override void Dispose() {
		//_surface.Dispose();
		//_textBuffer.Dispose();
		_terrain.Dispose();
		_font.Dispose();
	}
}
