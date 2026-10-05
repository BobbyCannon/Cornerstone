#region References

using System;

#endregion

namespace Cornerstone.VisualStudio.Core;

/// <summary>
/// One new guid written several ways. Index 0 is the value Tab inserts.
/// </summary>
public struct NewGuidFormat
{
	#region Constructors

	public NewGuidFormat(string text, string description)
	{
		Text = text;
		Description = description;
	}

	#endregion

	#region Properties

	public string Description { get; }

	public string Text { get; }

	#endregion
}

/// <summary>
/// Formats produced for the "nguid" token, and the caret check that recognizes that token.
/// </summary>
public static class NewGuidFormats
{
	#region Constants

	public const string Token = "nguid";

	#endregion

	#region Methods

	public static NewGuidFormat[] Create(Guid guid)
	{
		var dashed = guid.ToString("D");
		var digits = guid.ToString("N");
		var braces = guid.ToString("B");
		var parentheses = guid.ToString("P");
		return new[]
		{
			new NewGuidFormat(dashed.ToUpperInvariant(), "Dashes"),
			new NewGuidFormat(dashed.ToLowerInvariant(), "Dashes, lowercase"),
			new NewGuidFormat(digits.ToUpperInvariant(), "Digits"),
			new NewGuidFormat(digits.ToLowerInvariant(), "Digits, lowercase"),
			new NewGuidFormat(braces.ToUpperInvariant(), "Braces"),
			new NewGuidFormat(braces.ToLowerInvariant(), "Braces, lowercase"),
			new NewGuidFormat(parentheses.ToUpperInvariant(), "Parentheses"),
			new NewGuidFormat(parentheses.ToLowerInvariant(), "Parentheses, lowercase"),
			new NewGuidFormat(guid.ToString("X"), "Hexadecimal")
		};
	}

	/// <summary>
	/// The token is the letters immediately before <paramref name="caret" />.
	/// A letter, digit, or underscore on either side means this is part of a longer word.
	/// </summary>
	public static bool TryGetTokenSpan(string text, int caret, out int start, out int length)
	{
		start = 0;
		length = 0;
		if ((text == null) || (caret < Token.Length) || (caret > text.Length))
		{
			return false;
		}

		var tokenStart = caret;
		while (tokenStart > 0)
		{
			var ch = text[tokenStart - 1];
			if (!char.IsLetterOrDigit(ch) && (ch != '_'))
			{
				break;
			}

			tokenStart--;
			if ((caret - tokenStart) > Token.Length)
			{
				return false;
			}
		}

		if ((caret - tokenStart) != Token.Length)
		{
			return false;
		}

		if ((caret < text.Length) && (char.IsLetterOrDigit(text[caret]) || (text[caret] == '_')))
		{
			return false;
		}

		if (!string.Equals(text.Substring(tokenStart, Token.Length), Token, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		start = tokenStart;
		length = Token.Length;
		return true;
	}

	#endregion
}
