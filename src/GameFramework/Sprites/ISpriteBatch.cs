using GameFramework.Textures;

namespace GameFramework.Sprites;

public interface ISpriteBatch : IDisposable {
	// Begins a batch that renders to the given target. The batch starts unclipped
	// and with no texture bound; the texture and blend mode are chosen per-draw (see
	// the Draw overloads) and the clip is changed via ReplaceClip/RestoreClip.
	void Start( IRenderTarget renderTarget );

	// Ends the batch, flushing any pending sprites. Returns the monotonic count of
	// GPU flushes (draw calls) performed over this batch's lifetime -- incremented
	// whenever pending sprites are flushed on a texture change, a clip change, a
	// capacity overflow, or by this Finish() call itself.
	long Finish();

	// Pushes a clip onto the clip stack and applies it as the scissor for
	// subsequent draws, flushing any pending sprites first if the scissor actually
	// changes. May be called before Start to establish an initial clip that the
	// batch will adopt when it begins. The matching RestoreClip pops it back off.
	void ReplaceClip( Bounds clip );

	// Pops the most recently pushed clip off the stack and restores the scissor to
	// the previous clip (or clears it if the stack becomes empty), flushing any
	// pending sprites first if the scissor actually changes. While a batch is
	// running this throws if there is no clip pushed after Start to restore (i.e. it
	// would pop below the depth recorded at Start). Outside a batch, restoring an
	// empty stack is a silent no-op.
	void RestoreClip();

	// Methods that perform actual drawing
	// -----------------------------------
	// The textureId selects the source texture (its ITexture.Id). Drawing with an id
	// different from the currently bound one implicitly flushes and rebinds. Likewise,
	// drawing with a blendMode different from the one currently in effect implicitly
	// flushes and reprograms the blend state.
	void Draw( uint textureId, float x, float y, float width, float height, float u1, float v1, float u2, float v2, uint colour = 0xFFFFFFFF, BlendMode blendMode = BlendMode.Premultiplied );
	void Draw( uint textureId, float x, float y, float width, float height, float u1, float v1, float u2, float v2, float rotation, float originX, float originY, uint colour = 0xFFFFFFFF, BlendMode blendMode = BlendMode.Premultiplied );

	// Helper methods for drawing
	// --------------------------
	void Draw( ISubTexture subTexture, float x, float y, float width, float height, uint colour = 0xFFFFFFFF, BlendMode blendMode = BlendMode.Premultiplied ) => Draw( subTexture.Texture.Id, x, y, width, height, subTexture.U1, subTexture.V1, subTexture.U2, subTexture.V2, colour, blendMode );
	void Draw( ISubTexture subTexture, float x, float y, uint colour = 0xFFFFFFFF, BlendMode blendMode = BlendMode.Premultiplied ) => Draw( subTexture.Texture.Id, x, y, subTexture.Size.Width, subTexture.Size.Height, subTexture.U1, subTexture.V1, subTexture.U2, subTexture.V2, colour, blendMode );
	void Draw( ISubTexture subTexture, float x, float y, float width, float height, float rotation, uint colour = 0xFFFFFFFF, BlendMode blendMode = BlendMode.Premultiplied ) => Draw( subTexture.Texture.Id, x, y, width, height, subTexture.U1, subTexture.V1, subTexture.U2, subTexture.V2, rotation, width * 0.5f, height * 0.5f, colour, blendMode );
	void Draw( ISubTexture subTexture, float x, float y, float rotation, uint colour = 0xFFFFFFFF, BlendMode blendMode = BlendMode.Premultiplied ) => Draw( subTexture.Texture.Id, x, y, subTexture.Size.Width, subTexture.Size.Height, subTexture.U1, subTexture.V1, subTexture.U2, subTexture.V2, rotation, subTexture.Size.Width * 0.5f, subTexture.Size.Height * 0.5f, colour, blendMode );
	void Draw( ISubTexture subTexture, float x, float y, float width, float height, float rotation, float originX, float originY, uint colour = 0xFFFFFFFF, BlendMode blendMode = BlendMode.Premultiplied ) => Draw( subTexture.Texture.Id, x, y, width, height, subTexture.U1, subTexture.V1, subTexture.U2, subTexture.V2, rotation, originX, originY, colour, blendMode );

	// Coordinate-based overloads
	// --------------------------
	// Convenience overloads that take the position as a Coordinate rather than
	// explicit x/y floats.
	void Draw( ISubTexture subTexture, Coordinate position, Dimension size, uint colour = 0xFFFFFFFF, BlendMode blendMode = BlendMode.Premultiplied ) => Draw( subTexture, position.X, position.Y, size.Width, size.Height, colour, blendMode );
	void Draw( ISubTexture subTexture, Coordinate position, uint colour = 0xFFFFFFFF, BlendMode blendMode = BlendMode.Premultiplied ) => Draw( subTexture, position.X, position.Y, colour, blendMode );
	void Draw( ISubTexture subTexture, Coordinate position, Dimension size, float rotation, uint colour = 0xFFFFFFFF, BlendMode blendMode = BlendMode.Premultiplied ) => Draw( subTexture, position.X, position.Y, size.Width, size.Height, rotation, colour, blendMode );
	void Draw( ISubTexture subTexture, Coordinate position, float rotation, uint colour = 0xFFFFFFFF, BlendMode blendMode = BlendMode.Premultiplied ) => Draw( subTexture, position.X, position.Y, rotation, colour, blendMode );
	void Draw( ISubTexture subTexture, Coordinate position, Dimension size, float rotation, float originX, float originY, uint colour = 0xFFFFFFFF, BlendMode blendMode = BlendMode.Premultiplied ) => Draw( subTexture, position.X, position.Y, size.Width, size.Height, rotation, originX, originY, colour, blendMode );


	// Methods to draw clipped sprites
	// (Geometry-space per-sprite clipping. This is distinct from SetClip's scissor and
	// avoids flushing the batch when only a single sprite needs trimming.)
	// -------------------------------
	void Draw( ISubTexture subTexture, float x, float y, float width, float height, Bounds clip, uint colour = 0xFFFFFFFF, BlendMode blendMode = BlendMode.Premultiplied ) {
		float left = MathF.Max( x, clip.Left );
		float top = MathF.Max( y, clip.Top );
		float right = MathF.Min( x + width, clip.Right );
		float bottom = MathF.Min( y + height, clip.Bottom );

		// Nothing of the sprite falls inside the clip rectangle.
		if( right <= left || bottom <= top ) {
			return;
		}

		// Convert the visible bounds into the sprite's UV space.
		float uPerPixel = ( subTexture.U2 - subTexture.U1 ) / width;
		float vPerPixel = ( subTexture.V2 - subTexture.V1 ) / height;

		float u1 = subTexture.U1 + ( ( left - x ) * uPerPixel );
		float v1 = subTexture.V1 + ( ( top - y ) * vPerPixel );
		float u2 = subTexture.U1 + ( ( right - x ) * uPerPixel );
		float v2 = subTexture.V1 + ( ( bottom - y ) * vPerPixel );

		Draw( subTexture.Texture.Id, left, top, right - left, bottom - top, u1, v1, u2, v2, colour, blendMode );
	}

	void Draw( ISubTexture subTexture, float x, float y, Bounds clip, uint colour = 0xFFFFFFFF, BlendMode blendMode = BlendMode.Premultiplied ) => Draw( subTexture, x, y, subTexture.Size.Width, subTexture.Size.Height, clip, colour, blendMode );

	void Draw( ISubTexture subTexture, Coordinate position, float width, float height, Bounds clip, uint colour = 0xFFFFFFFF, BlendMode blendMode = BlendMode.Premultiplied ) => Draw( subTexture, position.X, position.Y, width, height, clip, colour, blendMode );
	void Draw( ISubTexture subTexture, Coordinate position, Bounds clip, uint colour = 0xFFFFFFFF, BlendMode blendMode = BlendMode.Premultiplied ) => Draw( subTexture, position.X, position.Y, clip, colour, blendMode );

}
