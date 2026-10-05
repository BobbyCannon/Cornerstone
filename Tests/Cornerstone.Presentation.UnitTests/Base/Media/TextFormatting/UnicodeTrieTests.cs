#region References

using Cornerstone.Presentation.Media.TextFormatting.Unicode;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media.TextFormatting;

/// <summary>
/// Direct coverage for <see cref="UnicodeTrie" /> and <see cref="UnicodeTrieBuilder" />.
/// The production tries (UnicodeData, BiDi, GraphemeBreak, EastAsianWidth) are
/// used to exercise the four branches of <see cref="UnicodeTrie.Get" />; a small
/// synthetic trie covers the error-value path (which is otherwise unreachable
/// because the committed tries are generated with <c> errorValue == 0 </c>) and the
/// builder round-trip.
/// </summary>
[TestClass]
public class UnicodeTrieTests
{
	#region Methods

	[PresentationTestMethod]
	public void BuilderRoundTripsSetValues()
	{
		var builder = new UnicodeTrieBuilder(7u);
		builder.Set(0x0061, 0xAA);
		builder.Set(0x4E2D, 0xBB);
		builder.Set(0x1F600, 0xCC);

		var trie = builder.Freeze();

		CornerstoneTest.AreEqual(0xAAu, trie.Get(0x0061));
		CornerstoneTest.AreEqual(0xBBu, trie.Get(0x4E2D));
		CornerstoneTest.AreEqual(0xCCu, trie.Get(0x1F600));
	}

	[PresentationTestMethod]
	public void BuilderSetRangeAppliesValueToEveryCodepointInRange()
	{
		var builder = new UnicodeTrieBuilder();
		builder.SetRange(0x2000, 0x2010, 0x42);

		var trie = builder.Freeze();

		for (uint cp = 0x2000; cp <= 0x2010; cp++)
		{
			CornerstoneTest.AreEqual(0x42u, trie.Get(cp));
		}

		// Just outside the range stays at the initial value (0 by default).
		CornerstoneTest.AreEqual(0u, trie.Get(0x1FFF));
		CornerstoneTest.AreEqual(0u, trie.Get(0x2011));
	}

	[PresentationTestMethod]
	public void BuilderUnassignedCodepointsGetInitialValue()
	{
		var builder = new UnicodeTrieBuilder(0xDEAD);
		builder.Set(0x0061, 0xBEEF);

		var trie = builder.Freeze();

		CornerstoneTest.AreEqual(0xBEEFu, trie.Get(0x0061));
		CornerstoneTest.AreEqual(0xDEADu, trie.Get(0x0062));
		CornerstoneTest.AreEqual(0xDEADu, trie.Get(0x4E2D));
		CornerstoneTest.AreEqual(0xDEADu, trie.Get(0x1F600));
	}

	[PresentationTestMethod]
	public void GetAtAndAboveHighStartAllCodepointsShareFallback()
	{
		// Every codepoint >= HighStart short-circuits to the trie's last data
		// block. The committed tries set HighStart at 0x100000, so all of Plane
		// 16 collapses to one fallback value — this is a compression artifact
		// of the trie format. The test verifies the SHAPE of that contract (one
		// value for the whole high range) rather than asserting any specific
		// per-codepoint property: callers querying Plane 16 should not rely on
		// PUA / Unassigned distinctions surviving the trie.
		var v100000 = UnicodeDataTrie.Trie.Get(0x100000u);
		CornerstoneTest.AreEqual(v100000, UnicodeDataTrie.Trie.Get(0x100001u));
		CornerstoneTest.AreEqual(v100000, UnicodeDataTrie.Trie.Get(0x10FFFDu));
		CornerstoneTest.AreEqual(v100000, UnicodeDataTrie.Trie.Get(0x10FFFFu));

		var bidi100000 = BiDiTrie.Trie.Get(0x100000u);
		CornerstoneTest.AreEqual(bidi100000, BiDiTrie.Trie.Get(0x10FFFFu));
	}

	[PresentationTestMethod]
	public void GetAtAndAboveHighStartOnSyntheticTrieUsesHighFallback()
	{
		// SetRange across a huge supplementary span forces the builder to allocate
		// a high block. Codepoints at and above the resulting HighStart should
		// return the value that covers the high range.
		var builder = new UnicodeTrieBuilder(0u);
		builder.SetRange(0x80000, 0x10FFFF, 0x55);

		var trie = builder.Freeze();

		CornerstoneTest.AreEqual(0x55u, trie.Get(0x100000u));
		CornerstoneTest.AreEqual(0x55u, trie.Get(0x10FFFFu));

		// Below the high range — still the initial value.
		CornerstoneTest.AreEqual(0u, trie.Get(0x1000u));
	}

	[PresentationTestMethod]
	public void GetBeyondMaxCodepointIsHandledGracefullyByProductionTries()
	{
		// The committed tries are generated with errorValue == 0, so codepoints
		// past 0x10FFFF return 0 (Other category, LeftToRight bidi, etc.). This
		// documents that contract; the synthetic-trie test below covers the
		// case where errorValue is non-zero.
		const uint beyondRange = 0x110000u;

		CornerstoneTest.AreEqual(0u, UnicodeDataTrie.Trie.Get(beyondRange));
		CornerstoneTest.AreEqual(0u, BiDiTrie.Trie.Get(beyondRange));
		CornerstoneTest.AreEqual(0u, SegmentationTrie.Trie.Get(beyondRange));
		CornerstoneTest.AreEqual(0u, EastAsianWidthTrie.Trie.Get(beyondRange));
	}

	[PresentationTestMethod]
	[DataRow(0x0020u)] // ASCII space
	[DataRow(0x0061u)] // 'a'
	[DataRow(0x4E2Du)] // '中' (BMP CJK)
	[DataRow(0xFFFFu)] // last BMP non-surrogate
	public void GetBmpNonSurrogateReturnsValueMatchingUnicodeDataWrapper(uint codepoint)
	{
		// Walking the trie directly and re-applying the published shift/mask must
		// produce the same answer as the public UnicodeData wrapper. This catches
		// packing-layout drift between generator (writes packed bits) and
		// UnicodeData.Get* (reads packed bits).
		var packed = UnicodeDataTrie.Trie.Get(codepoint);

		var categoryFromTrie = (GeneralCategory) (packed & UnicodeData.CATEGORY_MASK);
		var scriptFromTrie = (Script) ((packed >> UnicodeData.SCRIPT_SHIFT) & UnicodeData.SCRIPT_MASK);

		CornerstoneTest.AreEqual(UnicodeData.GetGeneralCategory(codepoint), categoryFromTrie);
		CornerstoneTest.AreEqual(UnicodeData.GetScript(codepoint), scriptFromTrie);
	}

	[PresentationTestMethod]
	public void GetOutOfRangeReturnsConfiguredErrorValue()
	{
		// Build with a non-zero errorValue so the "> 0x10FFFF" branch produces a
		// distinguishable result. This is the only feasible test of that branch —
		// the committed tries all use errorValue == 0 which collides with the
		// happy-path zero value.
		var builder = new UnicodeTrieBuilder(0u, 0xFFFFu);
		builder.Set(0x0061, 0x11);

		var trie = builder.Freeze();

		CornerstoneTest.AreEqual(0xFFFFu, trie.Get(0x110000u));
		CornerstoneTest.AreEqual(0xFFFFu, trie.Get(0xFFFFFFFFu));
	}

	[PresentationTestMethod]
	[DataRow(0x10000u)] // first supplementary
	[DataRow(0x1F600u)] // 😀
	[DataRow(0x2F800u)] // CJK compatibility supplement
	public void GetSupplementaryBelowHighStartResolvesViaTwoLevelLookup(uint codepoint)
	{
		// Just walking the trie and reapplying the published mask must match the
		// wrapper — same guarantee as the BMP test, but exercises the two-level
		// supplementary lookup branch.
		var packed = BiDiTrie.Trie.Get(codepoint);
		var bidiFromTrie = (BidiClass) ((packed >> UnicodeData.BIDICLASS_SHIFT) & UnicodeData.BIDICLASS_MASK);

		CornerstoneTest.AreEqual(UnicodeData.GetBiDiClass(codepoint), bidiFromTrie);
	}

	[PresentationTestMethod]
	[DataRow(0xD800u)] // first high surrogate
	[DataRow(0xDB00u)] // mid high surrogate
	[DataRow(0xDBFFu)] // last high surrogate
	[DataRow(0xDC00u)] // first low surrogate
	[DataRow(0xDFFFu)] // last low surrogate
	public void GetSurrogateRangeResolvesViaLscpIndex(uint codepoint)
	{
		// Surrogates have a dedicated index region (LSCP_INDEX_2_OFFSET) in the
		// trie. The general category for every codepoint in this range is
		// Surrogate; this asserts the LSCP branch returns the correct row.
		CornerstoneTest.AreEqual(GeneralCategory.Surrogate, UnicodeData.GetGeneralCategory(codepoint));
	}

	#endregion
}