using GameFramework.Fonts;
using GameFramework.Textures;
using Silk.NET.OpenGL;

namespace GameFramework.Sprites;

internal sealed class SpriteAtlas : ISpriteAtlas {

	private readonly GL _gl;
	private readonly SpriteCache _cache;
	private readonly float[] _transparentBlack;
	private readonly ITexture _texture;
	private readonly ISpriteBatch _spriteBatch;

	public SpriteAtlas(
		GL gl,
		ITexture texture,
		ISpriteBatch spriteBatch
	) {
		_gl = gl;
		_texture = texture;
		_spriteBatch = spriteBatch;
		_cache = new SpriteCache( (int)texture.TextureWidth, (int)texture.TextureHeight );
		_transparentBlack = [0.0f, 0.0f, 0.0f, 0.0f];
	}

	void ISpriteAtlas.Add(
		string id,
		IFont font,
		ReadOnlySpan<byte> text,
		uint colour
	) {
		DoAdd( id, font, text, colour );
	}

	private void DoAdd(
		string id,
		IFont font,
		ReadOnlySpan<byte> text,
		uint colour
	) {
		font.MeasureText( text, 0, out int width, out int height );
		Sprite sprite = _cache.Insert( id, _texture, width, height );
		font.DrawText( _texture, text, sprite.X, sprite.Y, colour );
	}

	void ISpriteAtlas.Add(
		string id,
		IFont font,
		ReadOnlySpan<byte> text,
		uint textColour,
		uint outlineColour,
		int outlineWidth
	) {
		DoAdd( id, font, text, textColour, outlineColour, outlineWidth );
	}

	private void DoAdd(
		string id,
		IFont font,
		ReadOnlySpan<byte> text,
		uint textColour,
		uint outlineColour,
		int outlineWidth
	) {
		font.MeasureText( text, outlineWidth, out int width, out int height );
		Sprite sprite = _cache.Insert( id, _texture, width, height );
		font.DrawOutlinedText( _texture, text, sprite.X, sprite.Y, textColour, outlineColour, outlineWidth );
	}

	void ISpriteAtlas.Update(
        string id,
        IFont font,
        ReadOnlySpan<byte> text,
        uint colour
    ) {
		DoUpdate( id, font, text, colour );
    }

	private void DoUpdate(
		string id,
		IFont font,
		ReadOnlySpan<byte> text,
		uint colour
	) {
		font.MeasureText( text, 0, out int width, out int height );
		CachedSprite sprite = _cache[id];
		if( width > sprite.AllocatedWidth || height > sprite.AllocatedHeight ) {
			throw new InvalidOperationException( "Updated text dimensions exceed the allocated sprite dimensions." );
		}
		sprite.Sprite.Update( _texture, sprite.Sprite.X, sprite.Sprite.Y, width, height );
		font.DrawText( _texture, text, sprite.Sprite.X, sprite.Sprite.Y, colour );
	}

	void ISpriteAtlas.Update(
        string id,
        IFont font,
        ReadOnlySpan<byte> text,
        uint textColour,
        uint outlineColour,
        int outlineWidth
    ) {
		DoUpdate( id, font, text, textColour, outlineColour, outlineWidth );
		
    }

	private void DoUpdate(
		string id,
		IFont font,
		ReadOnlySpan<byte> text,
		uint textColour,
		uint outlineColour,
		int outlineWidth
	) {
		font.MeasureText( text, outlineWidth, out int width, out int height );
		CachedSprite sprite = _cache[id];
		if( width > sprite.AllocatedWidth || height > sprite.AllocatedHeight ) {
			throw new InvalidOperationException( "Updated text dimensions exceed the allocated sprite dimensions." );
		}
		sprite.Sprite.Update( _texture, sprite.Sprite.X, sprite.Sprite.Y, width, height );
		font.DrawOutlinedText( _texture, text, sprite.Sprite.X, sprite.Sprite.Y, textColour, outlineColour, outlineWidth );
	}

	void ISpriteAtlas.Add(
		string id,
		ITexture source,
		int x,
		int y,
		int w,
		int h
	) {
		Sprite sprite = _cache.Insert( id, _texture, w, h );
		_gl.CopyImageSubData(
			srcName: source.Id,          // ID of the source texture
			srcTarget: (GLEnum)TextureTarget.Texture2D, // Source target type
			srcLevel: 0,                       // Source mipmap level
			srcX: x,                          // Starting X coordinate in source
			srcY: y,                          // Starting Y coordinate in source
			srcZ: 0,                           // Starting Z coordinate (0 for 2D)

			dstName: _texture.Id,            // ID of the destination texture
			dstTarget: (GLEnum)TextureTarget.Texture2D, // Destination target type
			dstLevel: 0,                       // Destination mipmap level
			dstX: sprite.X,                         // Insertion X coordinate in destination
			dstY: sprite.Y,                         // Insertion Y coordinate in destination
			dstZ: 0,                           // Insertion Z coordinate (0 for 2D)

			srcWidth: (uint)w,                     // Width of the portion to copy
			srcHeight: (uint)h,                    // Height of the portion to copy
			srcDepth: 1                        // Depth of the portion to copy (1 for 2D)
		);
	}

	void ISpriteAtlas.Update(
		string id,
		ITexture source,
		int x,
		int y,
		int w,
		int h
	) {
		CachedSprite sprite = _cache[id];
		if( w > sprite.AllocatedWidth || h > sprite.AllocatedHeight ) {
			throw new InvalidOperationException( "Updated texture dimensions exceed the allocated sprite dimensions." );
		}
		_gl.CopyImageSubData(
			srcName: source.Id,          // ID of the source texture
			srcTarget: (GLEnum)TextureTarget.Texture2D, // Source target type
			srcLevel: 0,                       // Source mipmap level
			srcX: x,                          // Starting X coordinate in source
			srcY: y,                          // Starting Y coordinate in source
			srcZ: 0,                           // Starting Z coordinate (0 for 2D)

			dstName: _texture.Id,            // ID of the destination texture
			dstTarget: (GLEnum)TextureTarget.Texture2D, // Destination target type
			dstLevel: 0,                       // Destination mipmap level
			dstX: sprite.Sprite.X,                         // Insertion X coordinate in destination
			dstY: sprite.Sprite.Y,                         // Insertion Y coordinate in destination
			dstZ: 0,                           // Insertion Z coordinate (0 for 2D)

			srcWidth: (uint)w,                     // Width of the portion to copy
			srcHeight: (uint)h,                    // Height of the portion to copy
			srcDepth: 1                        // Depth of the portion to copy (1 for 2D)
		);
	}

	void ISpriteAtlas.Add(
		string id,
		IFont font,
		string text,
		uint colour
	) {
		font.MeasureText( text, 0, out int width, out int height );
		Sprite sprite = _cache.Insert( id, _texture, width, height );
		font.DrawText( _texture, text, sprite.X, sprite.Y, colour );
	}

	void ISpriteAtlas.Add(
		string id,
		IFont font,
		string text,
		uint textColour,
		uint outlineColour,
		int outlineWidth
	) {
		font.MeasureText( text, outlineWidth, out int width, out int height );
		Sprite sprite = _cache.Insert( id, _texture, width, height );
		font.DrawOutlinedText( _texture, text, sprite.X, sprite.Y, textColour, outlineColour, outlineWidth );
	}

	void ISpriteAtlas.Update(
		string id,
		IFont font,
		string text,
		uint colour
	) {
		font.MeasureText( text, 0, out int width, out int height );
		CachedSprite sprite = _cache[id];
		if( width > sprite.AllocatedWidth || height > sprite.AllocatedHeight ) {
			throw new InvalidOperationException( "Updated text dimensions exceed the allocated sprite dimensions." );
		}
		sprite.Sprite.Update( _texture, sprite.Sprite.X, sprite.Sprite.Y, width, height );
		font.DrawText( _texture, text, sprite.Sprite.X, sprite.Sprite.Y, colour );
	}

	void ISpriteAtlas.Update(
		string id,
		IFont font,
		string text,
		uint textColour,
		uint outlineColour,
		int outlineWidth
	) {
		font.MeasureText( text, outlineWidth, out int width, out int height );
		CachedSprite sprite = _cache[id];
		if( width > sprite.AllocatedWidth || height > sprite.AllocatedHeight ) {
			throw new InvalidOperationException( "Updated text dimensions exceed the allocated sprite dimensions." );
		}
		sprite.Sprite.Update( _texture, sprite.Sprite.X, sprite.Sprite.Y, width, height );
		font.DrawOutlinedText( _texture, text, sprite.Sprite.X, sprite.Sprite.Y, textColour, outlineColour, outlineWidth );
	}

	void ISpriteAtlas.Start(
		IRenderTarget renderTarget
	) {
		_spriteBatch.Start( renderTarget, _texture );
	}

	void ISpriteAtlas.Draw(
		string id,
		int x,
		int y,
		uint colour
	) {
		CachedSprite sprite = _cache[id];
		_spriteBatch.Draw( x, y, sprite.Sprite, colour );
	}

	void ISpriteAtlas.Draw(
		string id,
		int x,
		int y,
		int w,
		int h,
		uint colour
	) {
		CachedSprite sprite = _cache[id];
		_spriteBatch.Draw( x, y, w, h, sprite.Sprite, colour );
	}

	void ISpriteAtlas.Draw(
		string id,
		int x,
		int y,
		float rotation,
		uint colour
	) {
		CachedSprite sprite = _cache[id];
		_spriteBatch.Draw( x, y, sprite.Sprite, rotation, colour );
	}

	void ISpriteAtlas.Draw(
		string id,
		int x,
		int y,
		int w,
		int h,
		float rotation,
		uint colour
	) {
		CachedSprite sprite = _cache[id];
		_spriteBatch.Draw( x, y, w, h, sprite.Sprite, rotation, colour );
	}

	void ISpriteAtlas.Draw(
		string id,
		int x,
		int y,
		float rotation,
		float originX,
		float originY,
		uint colour
	) {
		CachedSprite sprite = _cache[id];
		_spriteBatch.Draw( x, y, sprite.Sprite, rotation, originX, originY, colour );
	}

	void ISpriteAtlas.Draw(
		string id,
		int x,
		int y,
		int w,
		int h,
		float rotation,
		float originX,
		float originY,
		uint colour
	) {
		CachedSprite sprite = _cache[id];
		_spriteBatch.Draw( x, y, w, h, sprite.Sprite, rotation, originX, originY, colour );
	}

	void ISpriteAtlas.Finish() {
		_spriteBatch.Finish();
	}

	void ISpriteAtlas.Remove(
		string id
	) {
		CachedSprite sprite = _cache[id];

		unsafe {
			fixed( float* colorPtr = _transparentBlack ) {
				_gl.ClearTexSubImage(
					texture: _texture.Id,             // The texture ID to modify
					level: 0,                       // Target mipmap level
					xoffset: sprite.Sprite.X,                    // Starting X coordinate of the fill box
					yoffset: sprite.Sprite.Y,                    // Starting Y coordinate of the fill box
					zoffset: 0,                     // Starting Z coordinate (0 for 2D)
					width: (uint)sprite.Sprite.Width,                     // Width of the area to fill
					height: (uint)sprite.Sprite.Height,                    // Height of the area to fill
					depth: 1,                       // Depth of the area to fill (1 for 2D)
					format: PixelFormat.Rgba,       // Format of the source color array
					type: PixelType.Float,          // Data type of the source color array
					data: colorPtr                  // Pointer to your color values
				);
			}
		}
	}

	void IDisposable.Dispose() {
		_texture.Dispose();
		_cache.Dispose();
	}

}
