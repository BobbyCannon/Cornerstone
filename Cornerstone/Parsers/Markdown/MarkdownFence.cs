#region References

using System;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Parsers.Markdown;

/// <summary>
/// CommonMark-style fenced code block scanner for Markdown.
/// Supports incomplete (still-open) fences for streaming: if no closer is found by EOF,
/// the match still succeeds and spans to the end of the buffer.
/// </summary>
public static class MarkdownFence
{
	#region Methods

	/// <summary>
	/// Tries to read a fenced code block starting at <paramref name="position" />.
	/// Position must be on the first fence character (<c>`</c> or <c>~</c>).
	/// </summary>
	/// <returns>
	/// True when an opening fence of length ≥ 3 is present. Always true for a valid open fence,
	/// even when the closing fence has not arrived yet (streaming).
	/// </returns>
	public static bool TryRead(IStringBuffer buffer, int position, out MarkdownFenceMatch match)
	{
		match = default;

		if ((buffer is null) || (position < 0) || (position >= buffer.Count))
		{
			return false;
		}

		var fenceChar = buffer[position];
		if ((fenceChar != '`') && (fenceChar != '~'))
		{
			return false;
		}

		var count = buffer.Count;
		var fenceLength = 0;
		var i = position;
		while ((i < count) && (buffer[i] == fenceChar))
		{
			fenceLength++;
			i++;
		}

		if (fenceLength < 3)
		{
			return false;
		}

		// Info string runs to end of opening line. For backtick fences, info may not contain backticks (CM).
		var contentRegionStart = i;
		while (i < count)
		{
			var c = buffer[i];
			if ((c == '\r') || (c == '\n'))
			{
				break;
			}

			if ((fenceChar == '`') && (c == '`'))
			{
				// Invalid opening fence info — treat as not a fence.
				return false;
			}

			i++;
		}

		// Scan subsequent lines for a closing fence (same char, length >= open, only optional spaces after).
		var scan = i;
		while (scan < count)
		{
			// Move to start of next line
			if ((buffer[scan] == '\r') || (buffer[scan] == '\n'))
			{
				if ((buffer[scan] == '\r') && ((scan + 1) < count) && (buffer[scan + 1] == '\n'))
				{
					scan += 2;
				}
				else
				{
					scan++;
				}
			}

			if (scan >= count)
			{
				break;
			}

			var lineStart = scan;

			// Optional indentation (spaces/tabs) before closing fence
			while ((scan < count) && ((buffer[scan] == ' ') || (buffer[scan] == '\t')))
			{
				scan++;
			}

			if ((scan < count) && (buffer[scan] == fenceChar))
			{
				var closeLength = 0;
				while ((scan < count) && (buffer[scan] == fenceChar))
				{
					closeLength++;
					scan++;
				}

				if (closeLength >= fenceLength)
				{
					// Remainder of line must be only spaces/tabs (or empty)
					var rest = scan;
					var validClose = true;
					while (rest < count)
					{
						var c = buffer[rest];
						if ((c == '\r') || (c == '\n'))
						{
							break;
						}

						if ((c != ' ') && (c != '\t'))
						{
							validClose = false;
							break;
						}

						rest++;
					}

					if (validClose)
					{
						// Content ends at start of closing fence markers (after indent)
						var contentRegionEnd = scan - closeLength;
						match = new MarkdownFenceMatch(
							startOffset: position,
							endOffset: scan,
							contentRegionStart: contentRegionStart,
							contentRegionEnd: contentRegionEnd,
							isComplete: true,
							fenceChar: fenceChar,
							fenceLength: fenceLength
						);
						return true;
					}
				}
			}

			// Not a closing line — skip to end of this line and continue
			scan = lineStart;
			while ((scan < count) && (buffer[scan] != '\r') && (buffer[scan] != '\n'))
			{
				scan++;
			}
		}

		// Incomplete: open fence through EOF (streaming)
		match = new MarkdownFenceMatch(
			startOffset: position,
			endOffset: count,
			contentRegionStart: contentRegionStart,
			contentRegionEnd: count,
			isComplete: false,
			fenceChar: fenceChar,
			fenceLength: fenceLength
		);
		return true;
	}

	#endregion
}

/// <summary>
/// Result of scanning a fenced code block.
/// Offsets[0]/contentRegionStart, Offsets[1]/contentRegionEnd for <see cref="MarkdownRenderer.ExtractCodeBlockInfo" />.
/// </summary>
public readonly struct MarkdownFenceMatch(
	int startOffset,
	int endOffset,
	int contentRegionStart,
	int contentRegionEnd,
	bool isComplete,
	char fenceChar,
	int fenceLength)
{
	#region Properties

	public int ContentRegionEnd { get; } = contentRegionEnd;

	public int ContentRegionStart { get; } = contentRegionStart;

	public int EndOffset { get; } = endOffset;

	public char FenceChar { get; } = fenceChar;

	public int FenceLength { get; } = fenceLength;

	public bool IsComplete { get; } = isComplete;

	public int StartOffset { get; } = startOffset;

	#endregion
}
