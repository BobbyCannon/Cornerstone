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

	/// <summary>
	/// True when the caret is inside a free-text attribute value (Text, Title, and the same kind of field).
	/// Those values have no completion list. A value that starts with { is a markup extension, not free text.
	/// </summary>
	public static bool IsFreeTextAttributeValue(string textBeforeCaret)
	{
		if (string.IsNullOrEmpty(textBeforeCaret))
		{
			return false;
		}

		return IsFreeTextAttributeValue(XmlParser.Parse(textBeforeCaret));
	}

	/// <summary>
	/// True when the parser is between the quotes of a free-text attribute, or on the = before those quotes.
	/// </summary>
	public static bool IsFreeTextAttributeValue(XmlParser parser)
	{
		if (parser == null)
		{
			return false;
		}

		if ((parser.State != XmlParser.ParserState.AttributeValue) &&
			(parser.State != XmlParser.ParserState.BeforeAttributeValue))
		{
			return false;
		}

		if ((parser.AttributeValue != null) && parser.AttributeValue.StartsWith("{", StringComparison.Ordinal))
		{
			return false;
		}

		return IsFreeTextAttributeName(parser.AttributeName);
	}

	/// <summary>
	/// True for attribute names whose value is prose. The local name is the part after : or .
	/// </summary>
	public static bool IsFreeTextAttributeName(string attributeName)
	{
		if (string.IsNullOrEmpty(attributeName))
		{
			return false;
		}

		var local = attributeName;
		var colon = attributeName.LastIndexOf(':');
		var dot = attributeName.LastIndexOf('.');
		var separator = colon > dot ? colon : dot;
		if ((separator >= 0) && (separator < attributeName.Length - 1))
		{
			local = attributeName.Substring(separator + 1);
		}

		return local.Equals("Text", StringComparison.Ordinal)
			|| local.Equals("Watermark", StringComparison.Ordinal)
			|| local.Equals("PasswordChar", StringComparison.Ordinal)
			|| local.Equals("PlaceholderText", StringComparison.Ordinal)
			|| local.Equals("Title", StringComparison.Ordinal)
			|| local.Equals("Caption", StringComparison.Ordinal)
			|| local.Equals("Content", StringComparison.Ordinal)
			|| local.Equals("Header", StringComparison.Ordinal)
			|| local.Equals("ToolTip", StringComparison.Ordinal)
			|| local.Equals("Name", StringComparison.Ordinal);
	}

	/// <summary>
	/// True when text before the caret is still inside a tag (unclosed start tag).
	/// Used to skip tag manipulators and completion while typing element content.
	/// </summary>
	public static bool IsInsideOpenTag(string textBeforeCaret)
	{
		if (string.IsNullOrEmpty(textBeforeCaret))
		{
			return false;
		}

		var lastLt = textBeforeCaret.LastIndexOf('<');
		if (lastLt < 0)
		{
			return false;
		}

		return textBeforeCaret.LastIndexOf('>') < lastLt;
	}

	/// <summary>
	/// True when an insert or delete itself contains markup characters that can
	/// start or close a tag, even if the caret is no longer inside a tag afterwards.
	/// </summary>
	public static bool ChangeLooksLikeMarkup(string oldText, string newText)
	{
		return ContainsMarkupChar(oldText) || ContainsMarkupChar(newText);
	}

	private static bool ContainsMarkupChar(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}

		foreach (var c in text)
		{
			if ((c == '<') || (c == '>') || (c == '/'))
			{
				return true;
			}
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
