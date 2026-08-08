#region References

using System;

#endregion

namespace Cornerstone.Parsers.Markdown;

public class MarkdownRenderer : Renderer
{
	#region Methods

	/// <summary>
	/// Extracts language/info string and the actual content start offset from a fenced/delimited block.
	/// Works for ``` fenced code blocks, ~~~, and similar delimited raw blocks (HTML, XAML, etc.).
	///
	/// The returned content has **trailing newlines removed** (but keeps internal ones).
	/// Safe when block offsets are stale or past the buffer (streaming): returns empty body instead of throwing.
	/// </summary>
	public static (string language, int contentStartOffset, int contentLength) ExtractCodeBlockInfo(ReadOnlySpan<char> buffer, Block block)
	{
		if ((block?.Offsets == null) || (block.Offsets.Length < 2))
		{
			return (string.Empty, block?.Offsets is { Length: > 0 } ? Math.Clamp(block.Offsets[0], 0, buffer.Length) : 0, 0);
		}

		// Streaming can leave Offsets past the current buffer (block not yet reparsed, or partial slice).
		if (!TryClampRegion(buffer.Length, block.Offsets[0], block.Offsets[1], out var blockStart, out var blockEnd))
		{
			return (string.Empty, 0, 0);
		}

		var fullSpan = buffer.Slice(blockStart, blockEnd - blockStart);
		if (fullSpan.IsEmpty)
		{
			return (string.Empty, blockStart, 0);
		}

		// Find end of the opening fence line
		var firstEolRelative = fullSpan.IndexOfAny('\r', '\n');
		if (firstEolRelative == -1)
		{
			firstEolRelative = fullSpan.Length;
		}

		var openingLineSpan = fullSpan[..firstEolRelative];

		// Skip the opening fence itself (``` or ~~~)
		var fenceEnd = 0;
		if (openingLineSpan.Length > 0)
		{
			var firstChar = openingLineSpan[0];
			if (firstChar is '`' or '~')
			{
				while ((fenceEnd < openingLineSpan.Length) && (openingLineSpan[fenceEnd] == firstChar))
				{
					fenceEnd++;
				}
			}
		}

		// Extract info string (language + optional metadata)
		var infoSpan = openingLineSpan[fenceEnd..].TrimStart();
		var langEnd = infoSpan.IndexOfAny(' ', '\t');
		if (langEnd == -1)
		{
			langEnd = infoSpan.Length;
		}

		var language = infoSpan[..langEnd].ToString().Trim();

		// Offsets from MarkdownFence point at contentRegionStart (after the ```/~~~ markers).
		// For an untagged fence that is often the newline of the opening line (firstEolRelative == 0).
		// That is a valid body start — not an empty block.
		if (firstEolRelative >= fullSpan.Length)
		{
			// No newline in the content region: info-only / empty body
			return (language, blockStart + fullSpan.Length, 0);
		}

		var contentStartAbsolute = blockStart + firstEolRelative;
		if (contentStartAbsolute > blockEnd)
		{
			return (language, blockEnd, 0);
		}

		// Skip the opening fence line's trailing newline(s) and any blank lines before the first body line.
		// (Preserves spaces on the first non-empty body line for indentation.)
		var remainingAfterOpening = buffer.Slice(contentStartAbsolute, blockEnd - contentStartAbsolute);
		var skip = 0;
		while (skip < remainingAfterOpening.Length)
		{
			var c = remainingAfterOpening[skip];
			if (c == '\r')
			{
				skip++;
				if ((skip < remainingAfterOpening.Length) && (remainingAfterOpening[skip] == '\n'))
				{
					skip++;
				}

				// blank line continues
				continue;
			}

			if (c == '\n')
			{
				skip++;
				continue;
			}

			// First non-EOL character (may be space — keep as code indent)
			break;
		}

		// Only skip pure blank lines after the opening fence; if the body is all whitespace, treat as empty.
		if (skip >= remainingAfterOpening.Length)
		{
			return (language, contentStartAbsolute, 0);
		}

		// After the first break above we're on first body char.
		contentStartAbsolute += skip;

		// Calculate raw content length (up to blockEnd)
		var rawContentLength = blockEnd - contentStartAbsolute;
		if (rawContentLength <= 0)
		{
			return (language, contentStartAbsolute, 0);
		}

		// Guard against any clamp/skip drift past the span.
		if ((contentStartAbsolute < 0)
			|| (rawContentLength < 0)
			|| ((contentStartAbsolute + rawContentLength) > buffer.Length))
		{
			return (language, Math.Clamp(contentStartAbsolute, 0, buffer.Length), 0);
		}

		var contentSpan = buffer.Slice(contentStartAbsolute, rawContentLength);
		var trimEnd = contentSpan.Length;

		// Trim trailing newlines/carriage returns from the end of the code block
		while (trimEnd > 0)
		{
			var lastChar = contentSpan[trimEnd - 1];
			if (lastChar is '\r' or '\n')
			{
				trimEnd--;
			}
			else
			{
				break;
			}
		}

		return (language, contentStartAbsolute, trimEnd);
	}

	/// <summary>
	/// Clamp [start, end) into [0, bufferLength]. False when offsets are unusable (e.g. entirely past EOF).
	/// </summary>
	private static bool TryClampRegion(int bufferLength, int start, int end, out int clampedStart, out int clampedEnd)
	{
		clampedStart = 0;
		clampedEnd = 0;

		if (bufferLength < 0)
		{
			return false;
		}

		// Stale stream offsets past the current document — nothing to extract.
		if (start > bufferLength)
		{
			clampedStart = bufferLength;
			clampedEnd = bufferLength;
			return false;
		}

		clampedStart = Math.Clamp(start, 0, bufferLength);
		clampedEnd = Math.Clamp(end, clampedStart, bufferLength);
		return true;
	}

	/// <summary>
	/// Extracts header information
	/// </summary>
	public static (int size, int contentStartOffset, int contentLength) ExtractHeaderInfo(ReadOnlySpan<char> buffer, Block block)
	{
		return (
			block.Offsets[0] - block.StartOffset,
			block.Offsets[1],
			block.EndOffset - block.Offsets[1]
		);
	}

	#endregion
}