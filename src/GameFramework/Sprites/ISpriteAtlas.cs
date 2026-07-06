using GameFramework.Fonts;
using GameFramework.Textures;

namespace GameFramework.Sprites;

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

    void Update(
        string id,
        IFont font,
        ReadOnlySpan<byte> text,
        uint colour = 0xFFFFFFFF
    );

    void Update(
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

	void Update(
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

	void Draw(
		string id,
		int x,
		int y,
		float rotation,
		uint colour = 0xFFFFFFFF
	);

	void Draw(
		string id,
		int x,
		int y,
		int w,
		int h,
		float rotation,
		uint colour = 0xFFFFFFFF
	);

	// rotation is in radians; origin is the pivot in the sprite's local space (0,0 = top-left).
	void Draw(
		string id,
		int x,
		int y,
		float rotation,
		float originX,
		float originY,
		uint colour = 0xFFFFFFFF
	);

	// rotation is in radians; origin is the pivot in the sprite's local space (0,0 = top-left).
	void Draw(
		string id,
		int x,
		int y,
		int w,
		int h,
		float rotation,
		float originX,
		float originY,
		uint colour = 0xFFFFFFFF
	);

	void Add(
		string id,
		IFont font,
		string text,
		uint colour = 0xFFFFFFFF
	);

	void Add(
		string id,
		IFont font,
		string text,
		uint textColour = 0xFFFFFFFF,
		uint outlineColour = 0x000000FF,
		int outlineWidth = 1
	);

	void Update(
		string id,
		IFont font,
		string text,
		uint colour = 0xFFFFFFFF
	);

	void Update(
		string id,
		IFont font,
		string text,
		uint textColour = 0xFFFFFFFF,
		uint outlineColour = 0x000000FF,
		int outlineWidth = 1
	);

	ITexture Texture { get; }
}
