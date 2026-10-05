#region References

using Cornerstone.Presentation.Media.TextFormatting.Unicode;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media.TextFormatting;

/// <summary>
/// Spot-checks for <see cref="UnicodeData" />. The trie generator already round-trips
/// every assigned codepoint against its source dictionary, so these tests focus on
/// pinning the public surface against drift: shift/mask layout, default values for
/// unassigned codepoints, and a few well-known codepoints across the four tries.
/// </summary>
[TestClass]
public class UnicodeDataTests
{
	#region Methods

	[PresentationTestMethod]
	[DataRow(0x0061u, BidiClass.LeftToRight)] // 'a'
	[DataRow(0x0041u, BidiClass.LeftToRight)] // 'A'
	[DataRow(0x0039u, BidiClass.EuropeanNumber)] // '9'
	[DataRow(0x0024u, BidiClass.EuropeanTerminator)] // '$'
	[DataRow(0x002Cu, BidiClass.CommonSeparator)] // ','
	[DataRow(0x0020u, BidiClass.WhiteSpace)] // ' '
	[DataRow(0x0009u, BidiClass.SegmentSeparator)] // '\t'
	[DataRow(0x000Au, BidiClass.ParagraphSeparator)] // '\n'
	[DataRow(0x05D0u, BidiClass.RightToLeft)] // 'א'
	[DataRow(0x0627u, BidiClass.ArabicLetter)] // 'ا'
	public void GetBiDiClassKnownCodepoints(uint codepoint, BidiClass expected)
	{
		CornerstoneTest.AreEqual(expected, UnicodeData.GetBiDiClass(codepoint));
	}

	[PresentationTestMethod]
	[DataRow(0x0028u, BidiPairedBracketType.Open, 0x0029u)] // '(' → ')'
	[DataRow(0x0029u, BidiPairedBracketType.Close, 0x0028u)] // ')' → '('
	[DataRow(0x005Bu, BidiPairedBracketType.Open, 0x005Du)] // '[' → ']'
	[DataRow(0x005Du, BidiPairedBracketType.Close, 0x005Bu)] // ']' → '['
	[DataRow(0x007Bu, BidiPairedBracketType.Open, 0x007Du)] // '{' → '}'
	public void GetBiDiPairedBracketRoundTripsKnownPairs(uint codepoint, BidiPairedBracketType expectedType, uint expectedPair)
	{
		CornerstoneTest.AreEqual(expectedType, UnicodeData.GetBiDiPairedBracketType(codepoint));
		CornerstoneTest.AreEqual(expectedPair, UnicodeData.GetBiDiPairedBracket(codepoint).Value);
	}

	[PresentationTestMethod]
	public void GetBiDiPairedBracketTypeNonBracketIsNone()
	{
		CornerstoneTest.AreEqual(BidiPairedBracketType.None, UnicodeData.GetBiDiPairedBracketType(0x0061u));
		CornerstoneTest.AreEqual(BidiPairedBracketType.None, UnicodeData.GetBiDiPairedBracketType(0x0020u));
	}

	[PresentationTestMethod]
	[DataRow(0x0061u, EastAsianWidthClass.Narrow)] // 'a'
	[DataRow(0x0020u, EastAsianWidthClass.Narrow)] // ' '
	[DataRow(0x4E2Du, EastAsianWidthClass.Wide)] // '中'
	[DataRow(0xFF21u, EastAsianWidthClass.Fullwidth)] // 'Ａ' FULLWIDTH LATIN CAPITAL A
	[DataRow(0xFF71u, EastAsianWidthClass.Halfwidth)] // 'ｱ' HALFWIDTH KATAKANA A
	[DataRow(0x03B1u, EastAsianWidthClass.Ambiguous)] // 'α'
	[DataRow(0x200Bu, EastAsianWidthClass.Neutral)] // ZWSP
	public void GetEastAsianWidthClassKnownCodepoints(uint codepoint, EastAsianWidthClass expected)
	{
		CornerstoneTest.AreEqual(expected, UnicodeData.GetEastAsianWidthClass(codepoint));
	}

	[PresentationTestMethod]
	[DataRow(0x0061u, GeneralCategory.LowercaseLetter)] // 'a'
	[DataRow(0x0041u, GeneralCategory.UppercaseLetter)] // 'A'
	[DataRow(0x0030u, GeneralCategory.DecimalNumber)] // '0'
	[DataRow(0x0020u, GeneralCategory.SpaceSeparator)] // ' '
	[DataRow(0x000Au, GeneralCategory.Control)] // '\n'
	[DataRow(0x0009u, GeneralCategory.Control)] // '\t'
	[DataRow(0x002Eu, GeneralCategory.OtherPunctuation)] // '.'
	[DataRow(0x0028u, GeneralCategory.OpenPunctuation)] // '('
	[DataRow(0x0029u, GeneralCategory.ClosePunctuation)] // ')'
	[DataRow(0x0024u, GeneralCategory.CurrencySymbol)] // '$'
	[DataRow(0x002Bu, GeneralCategory.MathSymbol)] // '+'
	[DataRow(0x200Bu, GeneralCategory.Format)] // ZWSP
	[DataRow(0xE000u, GeneralCategory.PrivateUse)] // BMP PUA start
	[DataRow(0xD800u, GeneralCategory.Surrogate)] // high surrogate start (LSCP path)
	[DataRow(0xDFFFu, GeneralCategory.Surrogate)] // low surrogate end (LSCP path)
	// Note: codepoints >= HighStart (currently 0x100000) all collapse to a single
	// fallback value because the trie compresses Plane 16 to save space. The
	// resulting GeneralCategory is not the per-codepoint UCD value. See
	// UnicodeTrieTests.Get_AtAndAboveHighStart_AllCodepointsShareFallback.
	public void GetGeneralCategoryKnownCodepoints(uint codepoint, GeneralCategory expected)
	{
		CornerstoneTest.AreEqual(expected, UnicodeData.GetGeneralCategory(codepoint));
	}

	[PresentationTestMethod]
	[DataRow(0x000Du, GraphemeBreakClass.CR)] // '\r'
	[DataRow(0x000Au, GraphemeBreakClass.LF)] // '\n'
	[DataRow(0x200Du, GraphemeBreakClass.ZWJ)] // ZWJ
	[DataRow(0x1F600u, GraphemeBreakClass.ExtendedPictographic)] // 😀 (overridden by emoji-data.txt)
	[DataRow(0x1100u, GraphemeBreakClass.L)] // HANGUL CHOSEONG KIYEOK
	[DataRow(0x1161u, GraphemeBreakClass.V)] // HANGUL JUNGSEONG A
	[DataRow(0x11A8u, GraphemeBreakClass.T)] // HANGUL JONGSEONG KIYEOK
	[DataRow(0x0061u, GraphemeBreakClass.Other)] // 'a'
	[DataRow(0x0030u, GraphemeBreakClass.Other)] // '0'
	public void GetGraphemeClusterBreakKnownCodepoints(uint codepoint, GraphemeBreakClass expected)
	{
		CornerstoneTest.AreEqual(expected, UnicodeData.GetGraphemeClusterBreak(codepoint));
	}

	[PresentationTestMethod]
	[DataRow(0x0061u, LineBreakClass.Alphabetic)] // 'a'
	[DataRow(0x000Au, LineBreakClass.LineFeed)] // '\n'
	[DataRow(0x000Du, LineBreakClass.CarriageReturn)] // '\r'
	[DataRow(0x0020u, LineBreakClass.Space)] // ' '
	[DataRow(0x0009u, LineBreakClass.BreakAfter)] // '\t'
	[DataRow(0x002Du, LineBreakClass.Hyphen)] // '-'
	[DataRow(0x0028u, LineBreakClass.OpenPunctuation)] // '('
	[DataRow(0x0029u, LineBreakClass.CloseParenthesis)] // ')'
	[DataRow(0x0030u, LineBreakClass.Numeric)] // '0'
	[DataRow(0x4E2Du, LineBreakClass.Ideographic)] // '中'
	[DataRow(0x2028u, LineBreakClass.MandatoryBreak)] // LINE SEPARATOR
	[DataRow(0x2029u, LineBreakClass.MandatoryBreak)] // PARAGRAPH SEPARATOR
	public void GetLineBreakClassKnownCodepoints(uint codepoint, LineBreakClass expected)
	{
		CornerstoneTest.AreEqual(expected, UnicodeData.GetLineBreakClass(codepoint));
	}

	[PresentationTestMethod]
	[DataRow(0x0061u, Script.Latin)] // 'a'
	[DataRow(0x0041u, Script.Latin)] // 'A'
	[DataRow(0x044Fu, Script.Cyrillic)] // 'я'
	[DataRow(0x4E2Du, Script.Han)] // '中'
	[DataRow(0x05D0u, Script.Hebrew)] // 'א'
	[DataRow(0x0627u, Script.Arabic)] // 'ا'
	[DataRow(0x0020u, Script.Common)] // ' '
	[DataRow(0x0030u, Script.Common)] // '0'
	[DataRow(0x1F600u, Script.Common)] // 😀 (supplementary, common)
	[DataRow(0x0300u, Script.Inherited)] // combining grave (inherited)
	public void GetScriptKnownCodepoints(uint codepoint, Script expected)
	{
		CornerstoneTest.AreEqual(expected, UnicodeData.GetScript(codepoint));
	}

	[PresentationTestMethod]
	[DataRow(0x0061u, WordBreakClass.ALetter)] // 'a'
	[DataRow(0x000Du, WordBreakClass.CarriageReturn)] // '\r'
	[DataRow(0x000Au, WordBreakClass.LineFeed)] // '\n'
	[DataRow(0x0020u, WordBreakClass.WSegSpace)] // ' '
	[DataRow(0x0030u, WordBreakClass.Numeric)] // '0'
	[DataRow(0x200Du, WordBreakClass.ZWJ)] // ZWJ
	[DataRow(0x05D0u, WordBreakClass.HebrewLetter)] // 'א'
	[DataRow(0x4E2Du, WordBreakClass.Other)] // '中' (CJK is WB=Other)
	public void GetWordBreakClassKnownCodepoints(uint codepoint, WordBreakClass expected)
	{
		CornerstoneTest.AreEqual(expected, UnicodeData.GetWordBreakClass(codepoint));
	}

	/// <summary>
	/// Regression test for the BiDi / GraphemeBreak / UnicodeData trie builders'
	/// reliance on the seeded default class sitting at int position 0 (caught at
	/// generation time by the ABI validator, but only this asserts the runtime
	/// behavior of an unassigned codepoint).
	/// </summary>
	[PresentationTestMethod]
	public void UnassignedCodepointFallsBackToSeededDefaults()
	{
		// U+0378 is an unassigned BMP code point (and has been for decades — stable choice).
		const uint unassigned = 0x0378u;

		// Default Bidi class for unassigned codepoints is LeftToRight (seeded at position 0).
		CornerstoneTest.AreEqual(BidiClass.LeftToRight, UnicodeData.GetBiDiClass(unassigned));

		// Default grapheme break class is Other (seeded at position 0).
		CornerstoneTest.AreEqual(GraphemeBreakClass.Other, UnicodeData.GetGraphemeClusterBreak(unassigned));

		// Default line break class is Unknown — set explicitly via initialValue in the
		// UnicodeData trie builder, not via seed position 0.
		CornerstoneTest.AreEqual(LineBreakClass.Unknown, UnicodeData.GetLineBreakClass(unassigned));

		// Default word break class is Other — also set explicitly in the generator's
		// post-pass that maps unset WordBreakClass to Other.
		CornerstoneTest.AreEqual(WordBreakClass.Other, UnicodeData.GetWordBreakClass(unassigned));
	}

	#endregion
}