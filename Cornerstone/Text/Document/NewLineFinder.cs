namespace Cornerstone.Text.Document;

internal static class NewLineFinder
{
	#region Fields

	internal static readonly string[] NewlineStrings = ["\r\n", "\r", "\n"];
	private static readonly char[] Newline = ['\r', '\n'];

	#endregion

	#region Methods

	/// <summary>
	/// Gets the location of the next new line character, or SegmentExtensions.Invalid
	/// if none is found.
	/// </summary>
	internal static SimpleRange NextNewLine(string text, int offset)
	{
		var pos = text.IndexOfAny(Newline, offset);
		if (pos >= 0)
		{
			if (text[pos] == '\r')
			{
				if (((pos + 1) < text.Length) && (text[pos + 1] == '\n'))
				{
					return new SimpleRange(pos, 2);
				}
			}
			return new SimpleRange(pos, 1);
		}
		return SegmentExtensions.Invalid;
	}

	/// <summary>
	/// Gets the location of the next new line character, or SegmentExtensions.Invalid
	/// if none is found.
	/// </summary>
	internal static SimpleRange NextNewLine(ITextSource text, int offset)
	{
		var textLength = text.TextLength;
		var pos = text.IndexOfAny(Newline, offset, textLength - offset);
		if (pos >= 0)
		{
			if (text.GetCharAt(pos) == '\r')
			{
				if (((pos + 1) < textLength) && (text.GetCharAt(pos + 1) == '\n'))
				{
					return new SimpleRange(pos, 2);
				}
			}
			return new SimpleRange(pos, 1);
		}
		return SegmentExtensions.Invalid;
	}

	#endregion
}