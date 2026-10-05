#region References

using System.Globalization;
using Cornerstone.Presentation.Media.Fonts;
using Cornerstone.Presentation.Media.TextFormatting.Unicode;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media.Fonts;

[TestClass]
public class FontFallbackScriptHintsTests
{
	#region Methods

	[PresentationTestMethod]
	public void IsLocaleSensitiveFalseForLatin()
	{
		CornerstoneTest.IsFalse(FontFallbackScriptHints.IsLocaleSensitive(Script.Latin));
	}

	[PresentationTestMethod]
	public void IsLocaleSensitiveTrueForHan()
	{
		CornerstoneTest.IsTrue(FontFallbackScriptHints.IsLocaleSensitive(Script.Han));
	}

	[PresentationTestMethod]
	public void RefineWithCultureCommonCodepointPassesThrough()
	{
		// U+30FC has primary Script=Common in Cornerstone's data (its Hrkt membership lives
		// in Script_Extensions and is consulted directly through Codepoint.HasScriptExtension).
		// RefineWithCulture only refines codepoints whose primary script is ambiguous.
		var cp = new Codepoint(0x30FC);

		var refined = FontFallbackScriptHints.RefineWithCulture(cp, null);

		CornerstoneTest.AreEqual(cp.Script, refined);
	}

	[PresentationTestMethod]
	public void RefineWithCultureHanWithChineseCultureStaysHan()
	{
		var cp = new Codepoint(0x4E2D);

		var refinedSimplified = FontFallbackScriptHints.RefineWithCulture(cp, CultureInfo.GetCultureInfo("zh-CN"));
		var refinedTraditional = FontFallbackScriptHints.RefineWithCulture(cp, CultureInfo.GetCultureInfo("zh-TW"));

		CornerstoneTest.AreEqual(Script.Han, refinedSimplified);
		CornerstoneTest.AreEqual(Script.Han, refinedTraditional);
	}

	[PresentationTestMethod]
	public void RefineWithCultureHanWithJapaneseCultureMapsToHiragana()
	{
		var cp = new Codepoint(0x4E2D);

		var refined = FontFallbackScriptHints.RefineWithCulture(cp, CultureInfo.GetCultureInfo("ja-JP"));

		CornerstoneTest.AreEqual(Script.Hiragana, refined);
	}

	[PresentationTestMethod]
	public void RefineWithCultureHanWithKoreanCultureMapsToHangul()
	{
		var cp = new Codepoint(0x4E2D);

		var refined = FontFallbackScriptHints.RefineWithCulture(cp, CultureInfo.GetCultureInfo("ko-KR"));

		CornerstoneTest.AreEqual(Script.Hangul, refined);
	}

	[PresentationTestMethod]
	public void RefineWithCultureLatinCodepointReturnsLatinUnchanged()
	{
		var cp = new Codepoint('A');

		var refined = FontFallbackScriptHints.RefineWithCulture(cp, CultureInfo.GetCultureInfo("en-US"));

		CornerstoneTest.AreEqual(Script.Latin, refined);
	}

	[PresentationTestMethod]
	public void RefineWithCultureReturnsHanForHanWithoutCulture()
	{
		// U+4E2D 中 — Han script.
		var cp = new Codepoint(0x4E2D);

		var refined = FontFallbackScriptHints.RefineWithCulture(cp, null);

		CornerstoneTest.AreEqual(Script.Han, refined);
	}

	[PresentationTestMethod]
	public void TryGetOS2BitLatinReturnsFalse()
	{
		CornerstoneTest.IsFalse(FontFallbackScriptHints.TryGetOS2Bit(Script.Latin, out var bit));
		CornerstoneTest.AreEqual(-1, bit);
	}

	[PresentationTestMethod]
	[DataRow(Script.Hiragana, 49)]
	[DataRow(Script.Katakana, 50)]
	[DataRow(Script.Hangul, 56)]
	[DataRow(Script.Han, 59)]
	[DataRow(Script.Cyrillic, 9)]
	[DataRow(Script.Arabic, 13)]
	public void TryGetOS2BitReturnsSpecBit(Script script, int expected)
	{
		CornerstoneTest.IsTrue(FontFallbackScriptHints.TryGetOS2Bit(script, out var bit));
		CornerstoneTest.AreEqual(expected, bit);
	}

	#endregion
}