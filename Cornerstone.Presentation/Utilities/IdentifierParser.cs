#region References

using System;
using System.Globalization;

#endregion

namespace Cornerstone.Presentation.Utilities;

internal static class IdentifierParser
{
	#region Methods

	public static ReadOnlySpan<char> ParseIdentifier(this
		#if !BUILDTASK
		scoped
		#endif
		ref CharacterReader r)
	{
		if (IsValidIdentifierStart(r.Peek))
		{
			return r.TakeWhile(c => IsValidIdentifierChar(c));
		}
		return ReadOnlySpan<char>.Empty;
	}

	/// <summary>
	/// Parses an identifier that may name a nested type, e.g. "Outer+Inner".
	/// </summary>
	public static ReadOnlySpan<char> ParseTypeIdentifier(this
		#if !BUILDTASK
		scoped
		#endif
		ref CharacterReader r)
	{
		if (IsValidIdentifierStart(r.Peek))
		{
			return r.TakeWhile(c => IsValidIdentifierChar(c) || (c == '+'));
		}
		return ReadOnlySpan<char>.Empty;
	}

	internal static ReadOnlySpan<char> ParseNumber(this ref CharacterReader r)
	{
		return r.TakeWhile(c => IsValidNumberChar(c));
	}

	private static bool IsValidIdentifierChar(char c)
	{
		if (IsValidIdentifierStart(c))
		{
			return true;
		}
		var cat = CharUnicodeInfo.GetUnicodeCategory(c);
		return (cat == UnicodeCategory.NonSpacingMark) ||
			(cat == UnicodeCategory.SpacingCombiningMark) ||
			(cat == UnicodeCategory.ConnectorPunctuation) ||
			(cat == UnicodeCategory.Format) ||
			(cat == UnicodeCategory.DecimalDigitNumber);
	}

	private static bool IsValidIdentifierStart(char c)
	{
		return char.IsLetter(c) || (c == '_');
	}

	private static bool IsValidNumberChar(char c)
	{
		var cat = CharUnicodeInfo.GetUnicodeCategory(c);
		return cat == UnicodeCategory.DecimalDigitNumber;
	}

	#endregion
}