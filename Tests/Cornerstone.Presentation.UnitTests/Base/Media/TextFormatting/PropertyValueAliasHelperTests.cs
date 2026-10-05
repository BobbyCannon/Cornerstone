#region References

using Cornerstone.Presentation.Media.TextFormatting.Unicode;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media.TextFormatting;

/// <summary>
/// Lightweight round-trip checks for the generated <see cref="PropertyValueAliasHelper" />.
/// Catches regressions in the alias-helper writer (e.g. wrong typeName, missing
/// entries, casing mismatches) without re-asserting the UCD aliases themselves.
/// </summary>
[TestClass]
public class PropertyValueAliasHelperTests
{
	#region Methods

	[PresentationTestMethod]
	[DataRow("L", BidiClass.LeftToRight)]
	[DataRow("R", BidiClass.RightToLeft)]
	[DataRow("AL", BidiClass.ArabicLetter)]
	[DataRow("EN", BidiClass.EuropeanNumber)]
	public void GetBidiClassKnownTags(string tag, BidiClass expected)
	{
		CornerstoneTest.AreEqual(expected, PropertyValueAliasHelper.GetBidiClass(tag));
	}

	[PresentationTestMethod]
	public void GetBidiClassUnknownTagFallsBackToLeftToRight()
	{
		// The generator emits LeftToRight as the fallback for unknown tags.
		CornerstoneTest.AreEqual(BidiClass.LeftToRight, PropertyValueAliasHelper.GetBidiClass("not-a-real-tag"));
	}

	[PresentationTestMethod]
	[DataRow("o", BidiPairedBracketType.Open)]
	[DataRow("c", BidiPairedBracketType.Close)]
	[DataRow("n", BidiPairedBracketType.None)]
	public void GetBidiPairedBracketTypeKnownTags(string tag, BidiPairedBracketType expected)
	{
		CornerstoneTest.AreEqual(expected, PropertyValueAliasHelper.GetBidiPairedBracketType(tag));
	}

	[PresentationTestMethod]
	[DataRow("Lu", GeneralCategory.UppercaseLetter)]
	[DataRow("Ll", GeneralCategory.LowercaseLetter)]
	[DataRow("Nd", GeneralCategory.DecimalNumber)]
	[DataRow("Zs", GeneralCategory.SpaceSeparator)]
	[DataRow("Cc", GeneralCategory.Control)]
	public void GetGeneralCategoryKnownTags(string tag, GeneralCategory expected)
	{
		CornerstoneTest.AreEqual(expected, PropertyValueAliasHelper.GetGeneralCategory(tag));
	}

	[PresentationTestMethod]
	[DataRow("AL", LineBreakClass.Alphabetic)]
	[DataRow("LF", LineBreakClass.LineFeed)]
	[DataRow("CR", LineBreakClass.CarriageReturn)]
	[DataRow("XX", LineBreakClass.Unknown)]
	public void GetLineBreakClassKnownTags(string tag, LineBreakClass expected)
	{
		CornerstoneTest.AreEqual(expected, PropertyValueAliasHelper.GetLineBreakClass(tag));
	}

	[PresentationTestMethod]
	[DataRow("Latn", Script.Latin)]
	[DataRow("Cyrl", Script.Cyrillic)]
	[DataRow("Hani", Script.Han)]
	[DataRow("Hebr", Script.Hebrew)]
	[DataRow("Arab", Script.Arabic)]
	[DataRow("Zyyy", Script.Common)]
	public void GetScriptKnownTags(string tag, Script expected)
	{
		CornerstoneTest.AreEqual(expected, PropertyValueAliasHelper.GetScript(tag));
	}

	[PresentationTestMethod]
	public void GetTagRoundTripsScriptThroughGetScript()
	{
		// Tag -> enum -> tag should round-trip for every script the helper knows.
		// Pick a handful so we catch obvious typoes in the writer without needing
		// a full mirror of UCD here.
		foreach (var script in new[] { Script.Latin, Script.Cyrillic, Script.Han, Script.Hebrew, Script.Arabic })
		{
			var tag = PropertyValueAliasHelper.GetTag(script);
			CornerstoneTest.AreEqual(script, PropertyValueAliasHelper.GetScript(tag));
		}
	}

	[PresentationTestMethod]
	[DataRow("LE", WordBreakClass.ALetter)]
	[DataRow("CR", WordBreakClass.CarriageReturn)]
	[DataRow("LF", WordBreakClass.LineFeed)]
	[DataRow("XX", WordBreakClass.Other)]
	public void GetWordBreakClassKnownTags(string tag, WordBreakClass expected)
	{
		CornerstoneTest.AreEqual(expected, PropertyValueAliasHelper.GetWordBreakClass(tag));
	}

	#endregion
}