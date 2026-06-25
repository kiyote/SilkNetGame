using System.Text;
using GameFramework.Fonts;

namespace GameFramework.Text;

public ref struct Utf8LineSplitter {
	private readonly ReadOnlySpan<byte> _text;
	private readonly float _maxWidth;
	private readonly IFont _font;
	private readonly int _outlineWidth;
	private int _currentByteOffset;

	public Utf8LineSplitter(
		IFont font,
		int outlineWidth,
		ReadOnlySpan<byte> text,
		float maxWidth
	) {
		_text = text;
		_maxWidth = maxWidth;
		_font = font;
		_currentByteOffset = 0;
		_outlineWidth = outlineWidth;
	}

	public bool NextLine(
		out ReadOnlySpan<byte> lineSlice
	) {
		lineSlice = default;

		if( _currentByteOffset >= _text.Length ) {
			return false;
		}

		ReadOnlySpan<byte> remainingText = _text[_currentByteOffset..];

		// Safe stack limit for lookahead processing
		int maxLookaheadBytes = Math.Min( remainingText.Length, 1024 );

		// Ensure we don't truncate a multi-byte UTF-8 character mid-sequence
		while( maxLookaheadBytes > 0
			&& maxLookaheadBytes < remainingText.Length
			&& ( remainingText[maxLookaheadBytes] & 0xC0 ) == 0x80
		) {
			maxLookaheadBytes--;
		}

		Span<char> charBuffer = stackalloc char[maxLookaheadBytes];
		int charsWritten = Encoding.UTF8.GetChars( remainingText[..maxLookaheadBytes], charBuffer );
		ReadOnlySpan<char> lookaheadChars = charBuffer.Slice( 0, charsWritten );

		var breaker = new LineBreakEnumerator( lookaheadChars );

		int lastSafeBreakByteIndex = 0;
		bool foundBreakInLine = false;

		while( breaker.MoveNext( out LineBreak lineBreak ) ) {
			// Width is measured to PositionMeasure, which excludes trailing
			// whitespace, so spaces at a break opportunity never count toward _maxWidth.
			int measureByteLength = Encoding.UTF8.GetByteCount( lookaheadChars[..lineBreak.PositionMeasure] );

			// The line is still cut/advanced at PositionWrap, which keeps the
			// trailing whitespace attached to this line.
			int wrapByteLength = Encoding.UTF8.GetByteCount( lookaheadChars[..lineBreak.PositionWrap] );

			ReadOnlySpan<byte> textSegmentToMeasure = remainingText[..measureByteLength];
			_font.MeasureText( textSegmentToMeasure, _outlineWidth, out int segmentWidth, out int _ );

			if( segmentWidth <= _maxWidth ) {
				lastSafeBreakByteIndex = wrapByteLength;
				foundBreakInLine = true;

				if( lineBreak.Required ) {
					break;
				}
			} else {
				if( !foundBreakInLine ) {
					// Single word is wider than maxWidth; force-break at the current opportunity to avoid infinite loop
					lastSafeBreakByteIndex = wrapByteLength == 0 ? remainingText.Length : wrapByteLength;
				}
				break;
			}
		}

		// If the layout hasn't overflowed width bounds but we ran out of lookahead window space,
		// use our last known safe offset instead of jumping straight to the end of the text array.
		if( lastSafeBreakByteIndex == 0 ) {
			lastSafeBreakByteIndex = remainingText.Length;
		}

		lineSlice = remainingText.Slice( 0, lastSafeBreakByteIndex );
		_currentByteOffset += lastSafeBreakByteIndex;
		return true;
	}
}
