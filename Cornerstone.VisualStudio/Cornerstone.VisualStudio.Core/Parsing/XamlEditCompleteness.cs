#region References

using System;

#endregion

namespace Cornerstone.VisualStudio.Core.Parsing;

/// <summary>
/// Cheap heuristics for "user is mid-edit" XAML so the designer can skip pushing
/// incomplete buffers to the preview host (e.g. a lone <c>&lt;</c>).
/// </summary>
/// <remarks>
/// Intentionally conservative: only skips clearly unfinished tags/comments/attributes.
/// Structural invalidity (unclosed elements after a complete tag) is still sent so the
/// host can report real errors after the user stops typing.
/// </remarks>
public static class XamlEditCompleteness
{
	/// <summary>
	/// Returns true when the buffer looks mid-edit and should not be sent to the previewer yet.
	/// </summary>
	public static bool IsClearlyIncomplete(string xaml)
	{
		if (string.IsNullOrEmpty(xaml))
		{
			return false;
		}

		// Work from the end without allocating when possible.
		var end = xaml.Length;
		while (end > 0 && char.IsWhiteSpace(xaml[end - 1]))
		{
			end--;
		}

		if (end == 0)
		{
			return false;
		}

		// Bare "<" (or trailing "<" after whitespace trim).
		if (xaml[end - 1] == '<')
		{
			return true;
		}

		var lastLt = xaml.LastIndexOf('<', end - 1);
		if (lastLt < 0)
		{
			return false;
		}

		// Incomplete comment or CDATA from the last open marker.
		if (LooksLikeOpenComment(xaml, lastLt, end))
		{
			return true;
		}

		if (LooksLikeOpenCData(xaml, lastLt, end))
		{
			return true;
		}

		// Unclosed start/end tag: no '>' after the last '<'.
		var gt = xaml.IndexOf('>', lastLt, end - lastLt);
		if (gt < 0)
		{
			return true;
		}

		// Inside the last tag (between '<' and '>'), odd quote count ⇒ mid-attribute.
		if (HasUnbalancedQuotes(xaml, lastLt + 1, gt))
		{
			return true;
		}

		return false;
	}

	private static bool LooksLikeOpenComment(string xaml, int lt, int end)
	{
		// "<!--" ... without "-->"
		if ((lt + 4 > end) ||
			(xaml[lt + 1] != '!') ||
			(xaml[lt + 2] != '-') ||
			(xaml[lt + 3] != '-'))
		{
			return false;
		}

		var close = xaml.IndexOf("-->", lt + 4, end - (lt + 4), StringComparison.Ordinal);
		return close < 0;
	}

	private static bool LooksLikeOpenCData(string xaml, int lt, int end)
	{
		// "<![CDATA[" ... without "]]>"
		const string open = "<![CDATA[";
		if ((lt + open.Length > end) ||
			(string.Compare(xaml, lt, open, 0, open.Length, StringComparison.Ordinal) != 0))
		{
			return false;
		}

		var close = xaml.IndexOf("]]>", lt + open.Length, end - (lt + open.Length), StringComparison.Ordinal);
		return close < 0;
	}

	private static bool HasUnbalancedQuotes(string text, int start, int end)
	{
		var inDouble = false;
		var inSingle = false;

		for (var i = start; i < end; i++)
		{
			var c = text[i];
			if (c == '"' && !inSingle)
			{
				inDouble = !inDouble;
			}
			else if (c == '\'' && !inDouble)
			{
				inSingle = !inSingle;
			}
		}

		return inDouble || inSingle;
	}
}
