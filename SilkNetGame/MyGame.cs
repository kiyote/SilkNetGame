using Game.Framework;
using Silk.NET.Input;
using System.Drawing;

namespace SilkNetGame;

public sealed class MyGame : GameBase, IKeyHandler {

	private readonly Device _device;
	private readonly Display _display;
	private readonly SpriteBatch _spriteBatch;
	private readonly Keyboard _keyboard;
	private readonly Texture _terrain;
	private readonly Sprite _sprite;
	private readonly TtfFont _font;
	private readonly Framebuffer _framebuffer;

	public MyGame(
		Device device,
		Display display,
		SpriteBatch spriteBatch,
		Keyboard keyboard
	) {
		_device = device;
		_display = display;
		_spriteBatch = spriteBatch;
		_keyboard = keyboard;

		_keyboard.AddHandler( this );
		_terrain = _device.LoadTexture( "terrain.png" );

		_sprite = new Sprite( _terrain, 384, 256, 96, 96 );

		_framebuffer = device.CreateRenderTarget( 512, 128 );
		_framebuffer.Clear( Color.TransparentBlack );
		_font = device.LoadFont( "Roboto-Regular.ttf", 24 );
		_font.OutlineRenderTo( _framebuffer, "Hello world.", 10, 10 );
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
		if( _display.IsResizing ) {
			return;
		}

		_display.Clear( Color.CornflowerBlue );
		_spriteBatch.Begin( _display, _terrain );
		_spriteBatch.Sprite( 100, 100, _sprite, 0xFFFFFFFF );
		_spriteBatch.Sprite( 200, 100, 96 * 2, 96 * 2, _sprite, 0xFFFFFFFF );
		_spriteBatch.Sprite( 400, 100, 96 * 3, 96 * 3, _sprite, 0xFFFFFFFF );
		_spriteBatch.Sprite( 700, 100, 96 * 4, 96 * 4, _sprite, 0xFFFFFFFF );
		_spriteBatch.End();

		_spriteBatch.Begin( _display, _framebuffer.Texture );
		_spriteBatch.Sprite( 0, 0, 512, 128, 0, 0, 1, 1, 0xFFFFFFFF );
		_spriteBatch.End();
	}

	public override void Update(
		double deltaTime
	) {
	}

	public override void Dispose() {
		_terrain.Dispose();
		_framebuffer.Dispose();
		_font.Dispose();
	}
}
