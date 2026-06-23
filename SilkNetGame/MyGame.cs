using Game.Framework;
using Game.Framework.Fonts;
using Game.Framework.Sprites;
using Game.Framework.Textures;
using Silk.NET.Input;
using System.Drawing;

namespace SilkNetGame;

public sealed class MyGame : GameBase, IKeyHandler {

	private readonly IDevice _device;
	private readonly IDisplay _display;
	private readonly ISpriteBatch _spriteBatch;
	private readonly Keyboard _keyboard;
	private readonly IFont _font;
	private readonly ISpriteAtlas _atlas;

	public MyGame(
		IDevice device,
		IDisplay display,
		ISpriteBatch spriteBatch,
		Keyboard keyboard
	) {
		_device = device;
		_display = display;
		_spriteBatch = spriteBatch;
		_keyboard = keyboard;

		_keyboard.AddHandler( this );
		ITexture terrain = _device.LoadTexture( "terrain.png" );

		_font = device.LoadTtfFont( "Roboto-Regular.ttf", 24 );

		_atlas = device.CreateSpriteAtlas(
			device.CreateFramebuffer( 1024, 1024 ),
			spriteBatch
		);

		_atlas.Add( "tall_grass", terrain, 384, 256, 96, 96 );
		_atlas.Add( "hello", _font, "Hello world.", 0xFFFFFFFF, 0x000000FF, 5 );

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
			_device.Exit();
			return true;
		}
		return false;
	}

	public override void Render(
		double deltaTime
	) {
		_display.Clear( Color.CornflowerBlue );

		_atlas.Begin( _display );
		_atlas.Draw( "tall_grass", 100, 100 );
		_atlas.Draw( "tall_grass", 200, 100, 96 * 2, 96 * 2 );
		_atlas.Draw( "tall_grass", 400, 100, 96 * 3, 96 * 3 );
		_atlas.Draw( "tall_grass", 700, 100, 96 * 4, 96 * 4 );
		_atlas.Draw( "hello", 10, 10 );
		_atlas.End();
	}

	public override void Update(
		double deltaTime
	) {
	}

	public override void Dispose() {
		_atlas.Dispose();
		_font.Dispose();
	}
}
