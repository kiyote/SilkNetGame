using Game.Framework.Fonts;
using Game.Framework.Textures;

namespace Game.Framework.Sprites;

public interface ISpriteAtlas: IDisposable {

	void Start(
		IRenderTarget renderTarget
	);

	void Finish();

	void Add(
		string id,
		IFont font,
		ReadOnlySpan<byte> text,
		uint colour = 0xFFFFFFFF
	);

	void Add(
		string id,
		IFont font,
		ReadOnlySpan<byte> text,
		uint textColour = 0xFFFFFFFF,
		uint outlineColour = 0x000000FF,
		int outlineWidth = 1
	);

	void Add(
		string id,
		ITexture source,
		int sourceX,
		int sourceY,
		int sourceW,
		int sourceH
	);

	void Remove(
		string id
	);

	void Draw(
		string id,
		int x,
		int y,
		uint colour = 0xFFFFFFFF
	);

	void Draw(
		string id,
		int x,
		int y,
		int w,
		int h,
		uint colour = 0xFFFFFFFF
	);

	// TODO: Update
}
