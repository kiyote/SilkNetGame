using System.Drawing;
using GameFramework.Textures;

namespace GameFramework.Sprites;


public interface ISpriteBatch : IDisposable {
	// The optional clip rectangle is applied to the render target for the entire
	// batch. It is batch-scoped and immutable: to change the clip, Finish() the
	// current batch and Start() a new one. Passing null leaves the batch unclipped
	// (and clears any clip a previous batch may have left on the target).
	void Start( IRenderTarget renderTarget, ITexture texture, BlendMode blendMode = BlendMode.Premultiplied, Rectangle? clip = null );
	void Finish();

	// Ensures the batch is active with exactly the given parameters. If a batch is
	// already running with the same renderTarget, texture (by Id), blendMode and
	// clip, this does nothing and returns false. Otherwise it Finish()es any current
	// batch and Start()s a new one, returning true. Lets consumers avoid hand-rolling
	// the "did anything change?" checks before every Start.
	bool Ensure( IRenderTarget renderTarget, ITexture texture, BlendMode blendMode = BlendMode.Premultiplied, Rectangle? clip = null );

	// Methods that perform actual drawing
	// -----------------------------------
	void Draw( float x, float y, float width, float height, float u1, float v1, float u2, float v2, uint colour );
	void Draw( float x, float y, float width, float height, float u1, float v1, float u2, float v2, float rotation, float originX, float originY, uint colour );

	// Helper methods for drawing
	// --------------------------
	void Draw( float x, float y, float width, float height, ISubTexture subTexture, uint colour ) => Draw( x, y, width, height, subTexture.U1, subTexture.V1, subTexture.U2, subTexture.V2, colour );
	void Draw( float x, float y, ISubTexture subTexture, uint colour ) => Draw( x, y, subTexture.Size.Width, subTexture.Size.Height, subTexture.U1, subTexture.V1, subTexture.U2, subTexture.V2, colour );
	void Draw( float x, float y, float width, float height, ISubTexture subTexture, float rotation, uint colour ) => Draw( x, y, width, height, subTexture.U1, subTexture.V1, subTexture.U2, subTexture.V2, rotation, width * 0.5f, height * 0.5f, colour );
	void Draw( float x, float y, ISubTexture subTexture, float rotation, uint colour ) => Draw( x, y, subTexture.Size.Width, subTexture.Size.Height, subTexture.U1, subTexture.V1, subTexture.U2, subTexture.V2, rotation, subTexture.Size.Width * 0.5f, subTexture.Size.Height * 0.5f, colour );
	void Draw( float x, float y, float width, float height, ISubTexture subTexture, float rotation, float originX, float originY, uint colour ) => Draw( x, y, width, height, subTexture.U1, subTexture.V1, subTexture.U2, subTexture.V2, rotation, originX, originY, colour );

	// Methods to draw clipped sprites
	// (Using these instead of setting the clip on the render target can be more efficient as it avoids flushing the batch if you need to change the clipping rect often.)
	// -------------------------------
	void Draw( float x, float y, float width, float height, ISubTexture subTexture, Rectangle clip, uint colour ) {
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

		Draw( left, top, right - left, bottom - top, u1, v1, u2, v2, colour );
	}

	void Draw( float x, float y, ISubTexture subTexture, Rectangle clip, uint colour ) => Draw( x, y, subTexture.Size.Width, subTexture.Size.Height, subTexture, clip, colour );

}
