using Game.Framework;

namespace SilkNetGame;

public class DisplayOptions {

	public const string Section = "Display";

	public int Width { get; set; }
	public int Height { get; set; }
	public bool VSync { get; set; }

	public WindowMode Mode {get; set; }
}
