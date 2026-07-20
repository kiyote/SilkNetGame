using GameFramework.Textures;

namespace GameFramework.Fonts; 
public interface IFont : IDisposable {
	void DrawOutlinedText(
		ITexture framebuffer,
		ReadOnlySpan<byte> text,
		Coordinate position,
		uint textColour = 0xFFFFFFFF,
		uint outlineColour = 0x000000FF,
		int outlineThickness = 1
	);

	void DrawText(
		ITexture framebuffer,
		ReadOnlySpan<byte> text,
		Coordinate position,
		uint colour = 0xFFFFFFFF
	);

	Dimension MeasureText(
		ReadOnlySpan<byte> text,
		int outlineWidth
	);

	void DrawOutlinedText(
		ITexture framebuffer,
		string text,
		Coordinate position,
		uint textColour = 0xFFFFFFFF,
		uint outlineColour = 0x000000FF,
		int outlineThickness = 1
	);

	void DrawText(
		ITexture framebuffer,
		string text,
		Coordinate position,
		uint colour = 0xFFFFFFFF
	);

	Dimension MeasureText(
		string text,
		int outlineWidth
	);
}
