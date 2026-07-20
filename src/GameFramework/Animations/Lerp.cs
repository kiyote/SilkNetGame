namespace GameFramework.Animations;

public static class Lerp {

	public static uint Colour(
		uint start,
		uint target,
		float interval
	) {
		if( !float.IsFinite( interval ) || interval < 0.0f || interval > 1.0f ) {
			throw new ArgumentOutOfRangeException( nameof( interval ), interval, "The interval must be between 0 and 1." );
		}
		if( interval == 0.0f ) {
			return start;
		}
		if( interval == 1.0f ) {
			return target;
		}

		(float startL, float startA, float startB) = ToOklab( start );
		(float targetL, float targetA, float targetB) = ToOklab( target );

		float l = Interpolate( startL, targetL, interval );
		float a = Interpolate( startA, targetA, interval );
		float b = Interpolate( startB, targetB, interval );
		byte alpha = ToByte( Interpolate( GetAlpha( start ), GetAlpha( target ), interval ) );

		return FromOklab( l, a, b, alpha );
	}

	private static (float L, float A, float B) ToOklab(
		uint colour
	) {
		float red = SrgbToLinear( ( ( colour >> 24 ) & 0xFF ) / 255.0f );
		float green = SrgbToLinear( ( ( colour >> 16 ) & 0xFF ) / 255.0f );
		float blue = SrgbToLinear( ( ( colour >> 8 ) & 0xFF ) / 255.0f );

		float l = ( 0.4122214708f * red ) + ( 0.5363325363f * green ) + ( 0.0514459929f * blue );
		float m = ( 0.2119034982f * red ) + ( 0.6806995451f * green ) + ( 0.1073969566f * blue );
		float s = ( 0.0883024619f * red ) + ( 0.2817188376f * green ) + ( 0.6299787005f * blue );

		float lRoot = MathF.Cbrt( l );
		float mRoot = MathF.Cbrt( m );
		float sRoot = MathF.Cbrt( s );

		return (
			( 0.2104542553f * lRoot ) + ( 0.7936177850f * mRoot ) - ( 0.0040720468f * sRoot ),
			( 1.9779984951f * lRoot ) - ( 2.4285922050f * mRoot ) + ( 0.4505937099f * sRoot ),
			( 0.0259040371f * lRoot ) + ( 0.7827717662f * mRoot ) - ( 0.8086757660f * sRoot )
		);
	}

	private static uint FromOklab(
		float lightness,
		float a,
		float b,
		byte alpha
	) {
		float lRoot = lightness + ( 0.3963377774f * a ) + ( 0.2158037573f * b );
		float mRoot = lightness - ( 0.1055613458f * a ) - ( 0.0638541728f * b );
		float sRoot = lightness - ( 0.0894841775f * a ) - ( 1.2914855480f * b );

		float l = lRoot * lRoot * lRoot;
		float m = mRoot * mRoot * mRoot;
		float s = sRoot * sRoot * sRoot;

		byte red = ToByte( LinearToSrgb( ( 4.0767416621f * l ) - ( 3.3077115913f * m ) + ( 0.2309699292f * s ) ) );
		byte green = ToByte( LinearToSrgb( ( -1.2684380046f * l ) + ( 2.6097574011f * m ) - ( 0.3413193965f * s ) ) );
		byte blue = ToByte( LinearToSrgb( ( -0.0041960863f * l ) - ( 0.7034186147f * m ) + ( 1.7076147010f * s ) ) );

		return ( (uint)red << 24 ) | ( (uint)green << 16 ) | ( (uint)blue << 8 ) | alpha;
	}

	private static float SrgbToLinear(
		float channel
	) {
		return channel <= 0.04045f
			? channel / 12.92f
			: MathF.Pow( ( channel + 0.055f ) / 1.055f, 2.4f );
	}

	private static float LinearToSrgb(
		float channel
	) {
		return channel <= 0.0031308f
			? 12.92f * channel
			: ( 1.055f * MathF.Pow( channel, 1.0f / 2.4f ) ) - 0.055f;
	}

	private static float Interpolate(
		float start,
		float target,
		float interval
	) {
		return start + ( ( target - start ) * interval );
	}

	private static float GetAlpha(
		uint colour
	) {
		return ( colour & 0xFF ) / 255.0f;
	}

	private static byte ToByte(
		float channel
	) {
		return (byte)MathF.Round( Math.Clamp( channel, 0.0f, 1.0f ) * 255.0f );
	}
}
