#region References

using Cornerstone.Presentation.Media.TextFormatting.Unicode;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media.TextFormatting.Unicode;

[TestClass]
public class CodepointHasScriptExtensionTests
{
	#region Methods

	[PresentationTestMethod]
	public void ArabicTatweelReportsAllListedScripts()
	{
		// U+0640 ARABIC TATWEEL has primary Script=Common with scx covering several
		// Arabic-derived scripts (incl. Arabic, Syriac, Mandaic, ...). Primary Common is
		// *not* part of the extensions set.
		var cp = new Codepoint(0x0640);

		CornerstoneTest.IsTrue(cp.HasScriptExtension(Script.Arabic));
		CornerstoneTest.IsTrue(cp.HasScriptExtension(Script.Syriac));
		CornerstoneTest.IsFalse(cp.HasScriptExtension(Script.Common));
		CornerstoneTest.IsFalse(cp.HasScriptExtension(Script.Latin));
	}

	[PresentationTestMethod]
	public void CodepointWithoutExtensionsFallsBackToPrimaryScript()
	{
		// U+05D0 HEBREW LETTER ALEF has primary Script=Hebrew and no Script_Extensions entry.
		var cp = new Codepoint(0x05D0);

		CornerstoneTest.AreEqual(Script.Hebrew, cp.Script);
		CornerstoneTest.IsTrue(cp.HasScriptExtension(Script.Hebrew));
		CornerstoneTest.IsFalse(cp.HasScriptExtension(Script.Arabic));
	}

	[PresentationTestMethod]
	public void HrktCodepointDoesNotClaimLatinExtension()
	{
		var cp = new Codepoint(0x30FC);

		CornerstoneTest.IsFalse(cp.HasScriptExtension(Script.Latin));
	}

	[PresentationTestMethod]

	// U+30FC Katakana-Hiragana Prolonged Sound Mark: scx={Hira, Kana}.
	[DataRow(0x30FC)]

	// U+3031 Vertical Kana Repeat Mark: scx={Hira, Kana}.
	[DataRow(0x3031)]

	// U+30A0 Katakana-Hiragana Double Hyphen: scx={Hira, Kana}.
	[DataRow(0x30A0)]
	public void HrktSharedCodepointsMatchBothHiraganaAndKatakana(int value)
	{
		var cp = new Codepoint((uint) value);

		CornerstoneTest.IsTrue(cp.HasScriptExtension(Script.Hiragana));
		CornerstoneTest.IsTrue(cp.HasScriptExtension(Script.Katakana));
	}

	[PresentationTestMethod]
	public void ReturnsFalseForUnknownScript()
	{
		var latinA = new Codepoint('A');

		CornerstoneTest.IsFalse(latinA.HasScriptExtension(Script.Unknown));
	}

	[PresentationTestMethod]
	public void ReturnsFalseWhenScriptDoesNotMatch()
	{
		var latinA = new Codepoint('A');

		CornerstoneTest.IsFalse(latinA.HasScriptExtension(Script.Hiragana));
	}

	[PresentationTestMethod]
	public void ReturnsTrueWhenScriptMatchesPrimaryScript()
	{
		var latinA = new Codepoint('A');

		CornerstoneTest.IsTrue(latinA.HasScriptExtension(Script.Latin));
	}

	#endregion
}