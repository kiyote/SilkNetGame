using System.Drawing;
using GameFramework;
using GameFramework.Fonts;
using GameFramework.Sprites;
using GameFramework.Textures;
using Silk.NET.Input;

namespace SilkNetGame;

internal sealed class MyGame : GameBase, IKeyHandler {

	private readonly IDevice _device;
	private readonly IDisplay _display;
	private readonly Keyboard _keyboard;
	private readonly IFont _font;
	private readonly ISpriteAtlas _atlas;
	private readonly Rectangle _clip;

	public MyGame(
		IDevice device,
		IDisplay display,
		ISpriteBatch spriteBatch,
		Keyboard keyboard
	) {
		_device = device;
		_display = display;
		_keyboard = keyboard;

		_keyboard.AddHandler( this );
		ITexture terrain = _device.LoadTexture( "terrain.png" );

		_font = device.LoadTtfFont( "Roboto_Condensed-Medium.ttf", 24 );

		_atlas = device.CreateSpriteAtlas(
			device.CreateFramebuffer( 1024, 1024 ),
			spriteBatch
		);

		_atlas.Add( "tall_grass", terrain, 384, 256, 96, 96 );
		_atlas.Add( "hello", _font, "0123456789.0123456789"u8, 0xFFFFFFFF, 0x000000FF, 1 );

		_clip = new Rectangle( 75, 75, 300, 300 );

		terrain.Dispose();
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
			_device.Terminate();
			return true;
		}
		return false;
	}

	private float _oldRotation = -1.0f;
	private float _rotation;

	public override void Render(
		double deltaTime
	) {
		_display.Clear( Color.CornflowerBlue );

		if (_oldRotation != _rotation) {
			_oldRotation = _rotation;
			string formattedRotation = _rotation.ToString( "F5", System.Globalization.CultureInfo.InvariantCulture );
			_atlas.Update( "hello", _font, formattedRotation, 0xFFFFFFFF, 0x000000FF, 1 );
		}

		//_display.SetClip( _clip );
		_atlas.Start( _display );
		_atlas.Draw( "tall_grass", 100, 100 );
		_atlas.Draw( "tall_grass", 200, 100, 96 * 2, 96 * 2 );
		_atlas.Draw( "tall_grass", 400, 100, 96 * 3, 96 * 3 );
		_atlas.Draw( "tall_grass", 700, 100, 96 * 4, 96 * 4 );
		_atlas.Draw( "hello", 75, 75, _rotation );
		_atlas.Finish();
		//_display.ClearClip();
	}

	public override void Update(
		double deltaTime
	) {
		_rotation += (float)(180.0 * deltaTime) * (MathF.PI / 180f);
		_rotation %= 360f;
		_rotation = MathF.Round( _rotation, 5 );
	}

	public override void Dispose() {
		_atlas.Dispose();
		_font.Dispose();
	}
}
