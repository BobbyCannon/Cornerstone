#region References

using System;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Parsers.Markdown;

/// <summary>
/// Parses inline markdown links of the form <c>[text](destination)</c>.
/// Offsets: text start/end, destination start/end (destination excludes surrounding parentheses).
/// </summary>
public static class MarkdownLink
{
	#region Methods

	/// <summary>
	/// Attempts to read a complete inline link starting at <paramref name="position" />.
	/// Does not advance any external cursor; caller must set position to <paramref name="endOffset" /> on success.
	/// </summary>
	public static bool TryRead(
		IStringBuffer buffer,
		int position,
		out int startOffset,
		out int endOffset,
		out int textStart,
		out int textEnd,
		out int destinationStart,
		out int destinationEnd)
	{
		startOffset = position;
		endOffset = position;
		textStart = position;
		textEnd = position;
		destinationStart = position;
		destinationEnd = position;

		if ((position >= buffer.Count) || (buffer[position] != '['))
		{
			return false;
		}

		// [text]
		textStart = position + 1;
		var i = textStart;
		while (i < buffer.Count)
		{
			var c = buffer[i];
			if (c is '\r' or '\n')
			{
				return false;
			}

			if (c == ']')
			{
				break;
			}

			// Unescaped '[' inside label is rare; reject nested for v1 simplicity.
			if (c == '[')
			{
				return false;
			}

			i++;
		}

		if ((i >= buffer.Count) || (buffer[i] != ']'))
		{
			return false;
		}

		textEnd = i;
		i++; // past ]

		if ((i >= buffer.Count) || (buffer[i] != '('))
		{
			return false;
		}

		i++; // past (
		destinationStart = i;

		// destination until matching ')' — no newlines for v1
		var depth = 1;
		while (i < buffer.Count)
		{
			var c = buffer[i];
			if (c is '\r' or '\n')
			{
				return false;
			}

			if (c == '(')
			{
				depth++;
			}
			else if (c == ')')
			{
				depth--;
				if (depth == 0)
				{
					destinationEnd = i;
					endOffset = i + 1;
					return true;
				}
			}

			i++;
		}

		return false;
	}

	/// <summary>
	/// Builds a GitHub-flavored-markdown-style heading id from header text
	/// (lowercase, spaces to '-', strip most punctuation).
	/// </summary>
	public static string ToHeadingId(ReadOnlySpan<char> headerText)
	{
		if (headerText.IsEmpty)
		{
			return string.Empty;
		}

		var start = 0;
		var end = headerText.Length;
		while ((start < end) && char.IsWhiteSpace(headerText[start]))
		{
			start++;
		}
		while ((end > start) && char.IsWhiteSpace(headerText[end - 1]))
		{
			end--;
		}

		if (start >= end)
		{
			return string.Empty;
		}

		Span<char> buffer = stackalloc char[end - start];
		var length = 0;
		var previousWasDash = false;

		for (var i = start; i < end; i++)
		{
			var c = headerText[i];
			if (char.IsWhiteSpace(c) || (c == '_'))
			{
				if (!previousWasDash && (length > 0))
				{
					buffer[length++] = '-';
					previousWasDash = true;
				}
				continue;
			}

			if (char.IsLetterOrDigit(c) || (c == '-'))
			{
				buffer[length++] = char.ToLowerInvariant(c);
				previousWasDash = c == '-';
			}
		}

		while ((length > 0) && (buffer[length - 1] == '-'))
		{
			length--;
		}

		return length == 0 ? string.Empty : new string(buffer[..length]);
	}

	#endregion
}
