using System.Runtime.InteropServices;

namespace Game.Framework;

[StructLayout( LayoutKind.Sequential, Pack = 4 )]
internal struct SpriteVertex {
	public float X;
	public float Y;
	public float U;
	public float V;
	public uint Color;

	public SpriteVertex(
		float x,
		float y,
		float u,
		float v,
		uint color
	) {
		X = x;
		Y = y;
		U = u;
		V = v;
		Color = color;
	}

	public static uint PackRGBA(
		float r,
		float g,
		float b,
		float a
	) {
		return ( (uint)( a * 255.0f ) << 24 )
			   | ( (uint)( b * 255.0f ) << 16 )
			   | ( (uint)( g * 255.0f ) << 8 )
			   | ( (uint)( r * 255.0f ) );
	}
}
