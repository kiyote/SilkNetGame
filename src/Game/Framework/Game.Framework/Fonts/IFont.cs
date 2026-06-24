using Game.Framework.Textures;

namespace Game.Framework.Fonts; 
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
}
