using GameFramework.Textures;

namespace GameFramework.Fonts; 
public interface IFont : IDisposable {
	void DrawOutlinedText(
		ITexture framebuffer,
		ReadOnlySpan<byte> text,
		int x,
		int y,
		uint textColour = 0xFFFFFFFF,
		uint outlineColour = 0x000000FF,
		int outlineThickness = 1
	);

	void DrawText(
		ITexture framebuffer,
		ReadOnlySpan<byte> text,
		int x,
		int y,
		uint colour = 0xFFFFFFFF
	);

	void MeasureText(
		ReadOnlySpan<byte> text,
		int outlineWidth,
		out int width,
		out int height
	);

	void DrawOutlinedText(
		ITexture framebuffer,
		string text,
		int x,
		int y,
		uint textColour = 0xFFFFFFFF,
		uint outlineColour = 0x000000FF,
		int outlineThickness = 1
	);

	void DrawText(
		ITexture framebuffer,
		string text,
		int x,
		int y,
		uint colour = 0xFFFFFFFF
	);

	void MeasureText(
		string text,
		int outlineWidth,
		out int width,
		out int height
	);
}
