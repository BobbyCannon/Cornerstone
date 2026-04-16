#region References

using System;
using Cornerstone.Text;

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
	/// </summary>
	public static (string language, int contentStartOffset, int contentLength) ExtractCodeBlockInfo(ReadOnlySpan<char> buffer, Block block)
	{
		if ((block?.Offsets == null) || (block.Offsets.Length < 2))
		{
			return (string.Empty, block?.Offsets?[0] ?? 0, 0);
		}

		var blockStart = block.Offsets[0];
		var blockEnd = block.Offsets[1];
		var fullSpan = buffer.Slice(blockStart, blockEnd - blockStart);

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
		var langEnd = infoSpan.IndexOfAny(TextService.Whitespace);
		if (langEnd == -1)
		{
			langEnd = infoSpan.Length;
		}

		// Start of content: after opening fence line + any whitespace/newlines
		var language = infoSpan[..langEnd].ToString().Trim();
		var contentStartAbsolute = blockStart + firstEolRelative;

		var remainingAfterOpening = buffer.Slice(contentStartAbsolute, blockEnd - contentStartAbsolute);
		var skipWhitespace = remainingAfterOpening.IndexOfAnyExcept(TextService.Whitespace);
		if (skipWhitespace == -1)
		{
			skipWhitespace = remainingAfterOpening.Length;
		}
		contentStartAbsolute += skipWhitespace;

		// Calculate raw content length (up to blockEnd)
		var rawContentLength = blockEnd - contentStartAbsolute;
		if (rawContentLength <= 0)
		{
			return (language, contentStartAbsolute, 0);
		}

		var contentSpan = buffer.Slice(contentStartAbsolute, rawContentLength);
		var trimEnd = contentSpan.Length;

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

		var finalContentLength = trimEnd;

		return (language, contentStartAbsolute, finalContentLength);
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