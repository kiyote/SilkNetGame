using Silk.NET.OpenGL;

namespace GameFramework.Textures;

public enum TextureFilter : int {
	Unknown = 0,
	Nearest = TextureMinFilter.Nearest,
	Linear = TextureMinFilter.Linear
}
