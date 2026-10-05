#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Media.TextFormatting.Unicode;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media.TextFormatting;

/// <summary>
/// Direct coverage for <see cref="Codepoint" /> — surrogate-pair decoding via
/// <see cref="Codepoint.ReadAt(ReadOnlySpan{char}, int, out int)" />, the small
/// helper properties / methods, and the bitmask-based <c> IsWhiteSpace </c> path
/// that depends on every used <see cref="GeneralCategory" /> value fitting in 64
/// bits.
/// </summary>
[TestClass]
public class CodepointTests
{
	#region Methods

	[PresentationTestMethod]
	public void CodepointEnumeratorDecodesMixedBmpAndSupplementaryText()
	{
		// 'a' + 😀 (U+1F600) + 'b' + ✓ (U+2713) + 🚀 (U+1F680).
		const string text = "a😀b✓🚀";

		var expected = new uint[] { 'a', 0x1F600, 'b', 0x2713, 0x1F680 };

		var enumerator = new CodepointEnumerator(text.AsSpan());
		var actual = new List<uint>();
		while (enumerator.MoveNext(out var cp))
		{
			actual.Add(cp.Value);
		}

		CornerstoneTest.AreEqual(expected, actual);
	}

	[PresentationTestMethod]
	[DataRow(0x3008u, 0x2329u)] // 〈 → ⟨
	[DataRow(0x3009u, 0x232Au)] // 〉 → ⟩
	[DataRow(0x0061u, 0x0061u)] // 'a' → 'a' (unchanged)
	[DataRow(0x0028u, 0x0028u)] // '(' → '(' (unchanged)
	public void GetCanonicalTypeMapsKnownCodepoints(uint input, uint expected)
	{
		// GetCanonicalType is internal; reachable here via InternalsVisibleTo.
		var actual = Codepoint.GetCanonicalType(new Codepoint(input));

		CornerstoneTest.AreEqual(expected, actual.Value);
	}

	[PresentationTestMethod]
	[DataRow(0x1F600u, true)] // 😀 defaults to emoji presentation
	[DataRow(0x1F4AFu, true)] // 💯 defaults to emoji presentation
	[DataRow(0x231Au, true)] // ⌚ WATCH defaults to emoji presentation
	[DataRow(0x2764u, false)] // ❤ needs U+FE0F to be presented as emoji
	[DataRow(0x0023u, false)] // '#'
	[DataRow(0x0030u, false)] // '0'
	[DataRow(0x0041u, false)] // 'A'
	public void HasEmojiPresentationKnownCodepoints(uint value, bool expected)
	{
		CornerstoneTest.AreEqual(expected, new Codepoint(value).HasEmojiPresentation);
	}

	[PresentationTestMethod]
	public void ImplicitConversionsRoundTripValue()
	{
		var cp = new Codepoint(0x1F600u);

		int asInt = cp;
		uint asUint = cp;

		CornerstoneTest.AreEqual(0x1F600, asInt);
		CornerstoneTest.AreEqual(0x1F600u, asUint);
	}

	[PresentationTestMethod]
	[DataRow(0x000Au, true)] // LF
	[DataRow(0x000Bu, true)] // VT
	[DataRow(0x000Cu, true)] // FF
	[DataRow(0x000Du, true)] // CR
	[DataRow(0x0085u, true)] // NEL
	[DataRow(0x2028u, true)] // LINE SEPARATOR
	[DataRow(0x2029u, true)] // PARAGRAPH SEPARATOR
	[DataRow(0x0020u, false)]
	[DataRow(0x0061u, false)]
	[DataRow(0x0009u, false)] // TAB is not a "break" char in Cornerstone's sense
	public void IsBreakCharKnownCodepoints(uint value, bool expected)
	{
		CornerstoneTest.AreEqual(expected, new Codepoint(value).IsBreakChar);
	}

	[PresentationTestMethod]
	[DataRow(0x00ADu, true)] // SOFT HYPHEN
	[DataRow(0x200Du, true)] // ZERO WIDTH JOINER
	[DataRow(0x034Fu, true)] // COMBINING GRAPHEME JOINER
	[DataRow(0xFE0Fu, true)] // VARIATION SELECTOR-16
	[DataRow(0xFEFFu, true)] // ZERO WIDTH NO-BREAK SPACE
	[DataRow(0xE0100u, true)] // VARIATION SELECTOR-17
	[DataRow(0x0301u, false)] // COMBINING ACUTE ACCENT
	[DataRow(0x20E3u, false)] // COMBINING ENCLOSING KEYCAP
	[DataRow(0x0041u, false)] // 'A'
	[DataRow(0x0020u, false)] // ' '
	public void IsDefaultIgnorableKnownCodepoints(uint value, bool expected)
	{
		CornerstoneTest.AreEqual(expected, new Codepoint(value).IsDefaultIgnorable);
	}

	[PresentationTestMethod]
	[DataRow(0x0061u, false)] // 'a'
	[DataRow(0x4E2Du, true)] // '中' Wide
	[DataRow(0xFF21u, true)] // 'Ａ' Fullwidth
	[DataRow(0xFF71u, true)] // 'ｱ' Halfwidth
	[DataRow(0x03B1u, false)] // 'α' Ambiguous (not east asian per IsEastAsian)
	[DataRow(0x0020u, false)] // ' ' Narrow
	public void IsEastAsianKnownCodepoints(uint value, bool expected)
	{
		CornerstoneTest.AreEqual(expected, new Codepoint(value).IsEastAsian);
	}

	[PresentationTestMethod]
	[DataRow(0x1F600u, true)] // 😀 GRINNING FACE
	[DataRow(0x1F4AFu, true)] // 💯 HUNDRED POINTS SYMBOL
	[DataRow(0x2764u, true)] // ❤ HEAVY BLACK HEART (text presentation by default)
	[DataRow(0x0023u, true)] // '#' (an emoji only as part of a keycap sequence)
	[DataRow(0x0030u, true)] // '0'
	[DataRow(0xFE0Fu, false)] // VARIATION SELECTOR-16 is Emoji_Component, not Emoji
	[DataRow(0x0041u, false)] // 'A'
	public void IsEmojiKnownCodepoints(uint value, bool expected)
	{
		CornerstoneTest.AreEqual(expected, new Codepoint(value).IsEmoji);
	}

	[PresentationTestMethod]
	public void IsInRangeInclusiveBoundsAreInclusive()
	{
		CornerstoneTest.IsTrue(Codepoint.IsInRangeInclusive(new Codepoint(0x10u), 0x10u, 0x20u));
		CornerstoneTest.IsTrue(Codepoint.IsInRangeInclusive(new Codepoint(0x20u), 0x10u, 0x20u));
		CornerstoneTest.IsTrue(Codepoint.IsInRangeInclusive(new Codepoint(0x15u), 0x10u, 0x20u));
		CornerstoneTest.IsFalse(Codepoint.IsInRangeInclusive(new Codepoint(0x0Fu), 0x10u, 0x20u));
		CornerstoneTest.IsFalse(Codepoint.IsInRangeInclusive(new Codepoint(0x21u), 0x10u, 0x20u));
	}

	/// <summary>
	/// <see cref="Codepoint.IsWhiteSpace" /> uses a bitmask trick that assumes every
	/// <see cref="GeneralCategory" /> value used in the mask fits in 64 bits.
	/// If <c> Control </c>, <c> Format </c>, or <c> SpaceSeparator </c> ever moves past
	/// position 63 in the enum, the mask silently produces wrong results. This
	/// guards against that.
	/// </summary>
	[PresentationTestMethod]
	public void IsWhiteSpaceAllMaskedGeneralCategoriesFitInBitmask()
	{
		CornerstoneTest.IsTrue((int) GeneralCategory.Control < 64);
		CornerstoneTest.IsTrue((int) GeneralCategory.Format < 64);
		CornerstoneTest.IsTrue((int) GeneralCategory.SpaceSeparator < 64);
	}

	[PresentationTestMethod]
	[DataRow(0x0020u, true)] // SPACE
	[DataRow(0x0009u, true)] // TAB (Control)
	[DataRow(0x000Au, true)] // LF (Control)
	[DataRow(0x000Du, true)] // CR (Control)
	[DataRow(0x00A0u, true)] // NBSP (SpaceSeparator)
	[DataRow(0x200Bu, true)] // ZWSP (Format)
	[DataRow(0x0061u, false)] // 'a'
	[DataRow(0x0030u, false)] // '0'
	[DataRow(0x002Eu, false)] // '.'
	public void IsWhiteSpaceKnownCodepoints(uint value, bool expected)
	{
		CornerstoneTest.AreEqual(expected, new Codepoint(value).IsWhiteSpace);
	}

	[PresentationTestMethod]
	[DataRow("a", 0, (uint) 'a', 1)]
	[DataRow("abc", 1, (uint) 'b', 1)]
	[DataRow("abc", 2, (uint) 'c', 1)]
	public void ReadAtBmpScalarReturnsCharAndAdvancesByOne(string text, int index, uint expectedValue, int expectedCount)
	{
		var cp = Codepoint.ReadAt(text.AsSpan(), index, out var count);

		CornerstoneTest.AreEqual(expectedValue, cp.Value);
		CornerstoneTest.AreEqual(expectedCount, count);
	}

	[PresentationTestMethod]
	public void ReadAtEmptySpanReturnsReplacement()
	{
		var cp = Codepoint.ReadAt(ReadOnlySpan<char>.Empty, 0, out var count);

		CornerstoneTest.AreEqual(Codepoint.ReplacementCodepoint.Value, cp.Value);
		CornerstoneTest.AreEqual(1, count);
	}

	[PresentationTestMethod]
	public void ReadAtHighSurrogateAtStartDecodesPair()
	{
		// U+1F600 GRINNING FACE — high surrogate at index 0.
		const string text = "😀";

		var cp = Codepoint.ReadAt(text.AsSpan(), 0, out var count);

		CornerstoneTest.AreEqual(0x1F600u, cp.Value);
		CornerstoneTest.AreEqual(2, count);
	}

	[PresentationTestMethod]
	public void ReadAtHighSurrogateFollowedByNonLowReturnsReplacement()
	{
		// High surrogate followed by a regular BMP character (invalid pair).
		const string text = "\uD83Da";

		var cp = Codepoint.ReadAt(text.AsSpan(), 0, out var count);

		CornerstoneTest.AreEqual(Codepoint.ReplacementCodepoint.Value, cp.Value);
		CornerstoneTest.AreEqual(1, count);
	}

	[PresentationTestMethod]
	public void ReadAtHighSurrogateWithoutFollowingLowReturnsReplacement()
	{
		// Lone high surrogate at end of string.
		const string text = "a\uD83D";

		var cp = Codepoint.ReadAt(text.AsSpan(), 1, out var count);

		CornerstoneTest.AreEqual(Codepoint.ReplacementCodepoint.Value, cp.Value);
		CornerstoneTest.AreEqual(1, count);
	}

	[PresentationTestMethod]
	public void ReadAtIndexAtLengthReturnsReplacement()
	{
		const string text = "abc";

		var cp = Codepoint.ReadAt(text.AsSpan(), 3, out var count);

		CornerstoneTest.AreEqual(Codepoint.ReplacementCodepoint.Value, cp.Value);
		CornerstoneTest.AreEqual(1, count);
	}

	[PresentationTestMethod]
	public void ReadAtIndexPastLengthReturnsReplacement()
	{
		const string text = "abc";

		var cp = Codepoint.ReadAt(text.AsSpan(), 5, out var count);

		CornerstoneTest.AreEqual(Codepoint.ReplacementCodepoint.Value, cp.Value);
		CornerstoneTest.AreEqual(1, count);
	}

	[PresentationTestMethod]
	public void ReadAtLoneLowSurrogateAtStartReturnsReplacement()
	{
		// Lone low surrogate with nothing before it.
		const string text = "\uDE00b";

		var cp = Codepoint.ReadAt(text.AsSpan(), 0, out var count);

		CornerstoneTest.AreEqual(Codepoint.ReplacementCodepoint.Value, cp.Value);
		CornerstoneTest.AreEqual(1, count);
	}

	[PresentationTestMethod]
	public void ReadAtLowSurrogateNotPrecededByHighReturnsReplacement()
	{
		// Low surrogate at index 1, but index 0 is a regular char (not a high surrogate).
		const string text = "a\uDE00";

		var cp = Codepoint.ReadAt(text.AsSpan(), 1, out var count);

		CornerstoneTest.AreEqual(Codepoint.ReplacementCodepoint.Value, cp.Value);
		CornerstoneTest.AreEqual(1, count);
	}

	[PresentationTestMethod]
	public void ReadAtLowSurrogateScansBackToHighSurrogate()
	{
		// Reading at the low surrogate position should still return the full
		// supplementary codepoint by looking one index back.
		const string text = "😀";

		var cp = Codepoint.ReadAt(text.AsSpan(), 1, out var count);

		CornerstoneTest.AreEqual(0x1F600u, cp.Value);
		CornerstoneTest.AreEqual(2, count);
	}

	[PresentationTestMethod]
	[DataRow(0x0028u, true, 0x0029u)] // '(' → ')'
	[DataRow(0x0029u, true, 0x0028u)] // ')' → '('
	[DataRow(0x005Bu, true, 0x005Du)] // '[' → ']'
	[DataRow(0x005Du, true, 0x005Bu)] // ']' → '['
	[DataRow(0x0061u, false, 0u)] // 'a' has no pair
	[DataRow(0x0020u, false, 0u)] // ' ' has no pair
	public void TryGetPairedBracketKnownCodepoints(uint codepoint, bool expectedSuccess, uint expectedPair)
	{
		var result = new Codepoint(codepoint).TryGetPairedBracket(out var pair);

		CornerstoneTest.AreEqual(expectedSuccess, result);

		if (expectedSuccess)
		{
			CornerstoneTest.AreEqual(expectedPair, pair.Value);
		}
	}

	#endregion
}