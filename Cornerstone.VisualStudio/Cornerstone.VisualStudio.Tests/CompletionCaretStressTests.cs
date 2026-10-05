#region References

using System;
using System.Linq;
using Cornerstone.VisualStudio.Core.Completion;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests;

/// <summary>
/// Stress tests for element completion caret placement — pure pipeline shared with the IDE.
/// </summary>
[TestClass]
public class CompletionCaretStressTests : XamlCompletionTestBase
{
	#region Methods

	[TestMethod]
	[DataRow("TextBlock", "TextBlock />", 9, 'k', ' ')] // after 'k' of TextBlock, before space of " />"
	[DataRow("Grid", "Grid></Grid>", 5, '>', '<')] // after open '>', before closing '<'
	[DataRow("StackPanel", "StackPanel></StackPanel>", 11, '>', '<')]
	[DataRow("Button", "Button></Button>", 7, '>', '<')]
	[DataRow("Image", "Image />", 5, 'e', ' ')]
	public void BuildElementTagInsertCaretIndexSplitsExpectedChars(
		string name,
		string expectedInsert,
		int expectedCaretIndex,
		char expectBefore,
		char expectAfter)
	{
		var (insert, caretIndex) = CompletionEngine.BuildElementTagInsert(name);
		Assert.AreEqual(expectedInsert, insert);
		Assert.AreEqual(expectedCaretIndex, caretIndex);

		var resolved = CompletionCaretPlacement.ResolveCaretIndexInInsert(insert, caretIndex);
		Assert.AreEqual(expectedCaretIndex, resolved);

		Assert.AreEqual(expectBefore, insert[resolved - 1]);
		if (resolved < insert.Length)
		{
			Assert.AreEqual(expectAfter, insert[resolved]);
		}
	}

	[TestMethod]
	public void SimulateCommitTextBToTextBlockCaretBeforeSpaceSlash()
	{
		const string doc = "\t<Grid></Grid><StackPanel></StackPanel><TextB";
		var filterStart = doc.LastIndexOf("TextB", StringComparison.Ordinal);
		var caretBefore = doc.Length;

		var set = GetCompletionsFor(doc);
		Assert.IsNotNull(set);
		var item = set.Completions.Single(c => c.DisplayText == "TextBlock");
		Assert.AreEqual("TextBlock />", item.InsertText);
		Assert.AreEqual(9, item.RecommendedCursorOffset);

		// Engine start must be filter start (relative to body — already transformed in set).
		Assert.AreEqual(filterStart, set.StartPosition);

		var (after, caret) = CompletionCaretPlacement.SimulateCommit(
			doc, filterStart, caretBefore, item.InsertText, item.RecommendedCursorOffset);

		Assert.AreEqual("\t<Grid></Grid><StackPanel></StackPanel><TextBlock />", after);
		var (before, afterCh) = CompletionCaretPlacement.CharsAroundCaret(after, caret);
		Assert.AreEqual('k', before); // end of TextBlock
		Assert.AreEqual(' ', afterCh); // space of " />"
		Assert.AreEqual("TextBlock", after.Substring(caret - 9, 9));
		Assert.AreEqual(" />", after.Substring(caret, 3));
	}

	[TestMethod]
	public void SimulateCommitGriToGridCaretBetweenTags()
	{
		const string doc = """
			<UserControl>
			  <Gri
			</UserControl>
			""";
		var filterStart = doc.IndexOf("Gri", StringComparison.Ordinal);
		var caretBefore = filterStart + 3;

		var set = GetCompletionsFor("""
			<UserControl>
			  <Gri
			""");
		Assert.IsNotNull(set);
		var item = set.Completions.Single(c => c.DisplayText == "Grid");
		Assert.AreEqual("Grid></Grid>", item.InsertText);
		Assert.AreEqual(5, item.RecommendedCursorOffset);

		// Real case: incomplete open tag then parent close on next line.
		const string realBefore = """
			<UserControl>
			  <Gri
			</UserControl>
			""";
		var start = realBefore.IndexOf("Gri", StringComparison.Ordinal);
		var caret = start + 3; // after Gri, before newline

		var (after, caretPos) = CompletionCaretPlacement.SimulateCommit(
			realBefore, start, caret, item.InsertText, item.RecommendedCursorOffset);

		Assert.Contains("<Grid></Grid>", after);
		Assert.Contains("</UserControl>", after);
		Assert.DoesNotContain("</U>", after);

		var (beforeCh, afterCh) = CompletionCaretPlacement.CharsAroundCaret(after, caretPos);
		Assert.AreEqual('>', beforeCh); // end of <Grid>
		Assert.AreEqual('<', afterCh); // start of </Grid>
		Assert.AreEqual("</Grid>", after.Substring(caretPos, 7));
	}

	[TestMethod]
	public void SimulateCommitWithSmartIndentGrowthStillKeepsCaretInTagGap()
	{
		// Session opened on '<' path: Enter commit grows leading tab, we pin it back.
		const string doc = "\t<StackPanel></StackPanel><TextB";
		var start = doc.LastIndexOf("TextB", StringComparison.Ordinal);
		var caret = doc.Length;

		var (insert, rec) = CompletionEngine.BuildElementTagInsert("TextBlock");
		var (after, caretPos) = CompletionCaretPlacement.SimulateCommit(
			doc, start, caret, insert, rec, smartIndentedLeadingWs: "\t\t");

		// Indent pinned back to single tab.
		StringAssert.StartsWith(after, "\t<");
		Assert.IsFalse(after.StartsWith("\t\t", StringComparison.Ordinal));
		Assert.Contains("<TextBlock />", after);

		var (beforeCh, afterCh) = CompletionCaretPlacement.CharsAroundCaret(after, caretPos);
		Assert.AreEqual('k', beforeCh);
		Assert.AreEqual(' ', afterCh);
	}

	[TestMethod]
	public void SimulateCommitGridWithSmartIndentKeepsCaretBetweenTags()
	{
		const string doc = "\t<Gri";
		var start = doc.IndexOf("Gri", StringComparison.Ordinal);
		var caret = doc.Length;

		var (insert, rec) = CompletionEngine.BuildElementTagInsert("Grid");
		Assert.AreEqual("Grid></Grid>", insert);
		Assert.AreEqual(5, rec);

		var (after, caretPos) = CompletionCaretPlacement.SimulateCommit(
			doc, start, caret, insert, rec, smartIndentedLeadingWs: "\t\t");

		Assert.AreEqual("\t<Grid></Grid>", after);
		var (beforeCh, afterCh) = CompletionCaretPlacement.CharsAroundCaret(after, caretPos);
		Assert.AreEqual('>', beforeCh);
		Assert.AreEqual('<', afterCh);
	}

	[TestMethod]
	[DataRow("TextB", "TextBlock")]
	[DataRow("Gri", "Grid")]
	[DataRow("Stack", "StackPanel")]
	[DataRow("But", "Button")]
	[DataRow("Imag", "Image")]
	public void EngineCompletionRecommendedCursorOffsetMatchesBuildElementTagInsert(
		string typed,
		string displayName)
	{
		var set = GetCompletionsFor("<" + typed);
		Assert.IsNotNull(set);
		var item = set.Completions.Single(c => c.DisplayText == displayName);
		var (insert, rec) = CompletionEngine.BuildElementTagInsert(displayName);
		Assert.AreEqual(insert, item.InsertText);
		Assert.AreEqual(rec, item.RecommendedCursorOffset);

		// Resolve like the IDE does.
		var caretIndex = CompletionCaretPlacement.ResolveCaretIndexInInsert(
			item.InsertText, item.RecommendedCursorOffset);
		Assert.AreEqual(rec, caretIndex);

		// Document after commit must have caret in the documented slot.
		var doc = "    <" + typed;
		var filterStart = doc.Length - typed.Length;
		var (after, caretPos) = CompletionCaretPlacement.SimulateCommit(
			doc, filterStart, doc.Length, item.InsertText, item.RecommendedCursorOffset);

		Assert.AreEqual("    <" + insert, after);
		if (insert.Contains("></"))
		{
			var (b, a) = CompletionCaretPlacement.CharsAroundCaret(after, caretPos);
			Assert.AreEqual('>', b);
			Assert.AreEqual('<', a);
		}
		else
		{
			var (b, a) = CompletionCaretPlacement.CharsAroundCaret(after, caretPos);
			Assert.AreEqual(insert[caretIndex - 1], b);
			Assert.AreEqual(' ', a); // " />"
		}
	}

	[TestMethod]
	public void WrongDoubleCountCaretMathIsRejectedByHelpers()
	{
		// Documents the historical bug: Positive tracking at end + add length again.
		const string insert = "Grid></Grid>";
		const int replaceStart = 4;
		const int rec = 5;

		var correct = CompletionCaretPlacement.GetCaretAfterReplace(replaceStart, insert, rec);
		Assert.AreEqual(replaceStart + rec, correct);

		var wrongPositiveMappedStart = replaceStart + insert.Length;
		var wrong = wrongPositiveMappedStart + insert.Length - (insert.Length - rec);
		Assert.AreNotEqual(correct, wrong);
		Assert.IsTrue(wrong > correct);
	}

	[TestMethod]
	public void CustomCommitPropertyNamePutsCaretBetweenQuotes()
	{
		const string doc = "<CornerstoneApplication RequestedThemeVar";
		var start = doc.LastIndexOf("RequestedThemeVar", StringComparison.Ordinal);
		var (after, caret) = CompletionCaretPlacement.ApplyCustomCommit(
			doc, start, "RequestedThemeVar".Length, "RequestedThemeVariant=\"\"", null);

		Assert.Contains("RequestedThemeVariant=\"\"", after);
		var (before, afterCh) = CompletionCaretPlacement.CharsAroundCaret(after, caret);
		Assert.AreEqual('"', before);
		Assert.AreEqual('"', afterCh);
	}

	[TestMethod]
	public void CustomCommitEnumValuePutsCaretAfterValue()
	{
		const string doc = "RequestedThemeVariant=\"\"";
		var start = doc.IndexOf("=\"\"", StringComparison.Ordinal) + 2;
		var (after, caret) = CompletionCaretPlacement.ApplyCustomCommit(
			doc, start, 0, "Dark", "Dark".Length);

		Assert.AreEqual("RequestedThemeVariant=\"Dark\"", after);
		var (before, afterCh) = CompletionCaretPlacement.CharsAroundCaret(after, caret);
		Assert.AreEqual('k', before);
		Assert.AreEqual('"', afterCh);
	}

	[TestMethod]
	public void CustomCommitLeafElementCaretBeforeSlash()
	{
		const string doc = "<TextB";
		var (after, caret) = CompletionCaretPlacement.ApplyCustomCommit(
			doc, 1, 5, "TextBlock />", 9);

		Assert.AreEqual("<TextBlock />", after);
		var (before, afterCh) = CompletionCaretPlacement.CharsAroundCaret(after, caret);
		Assert.AreEqual('k', before);
		Assert.AreEqual(' ', afterCh);
	}

	[TestMethod]
	public void CustomCommitContainerCaretBetweenTags()
	{
		const string doc = "<Gri";
		var (after, caret) = CompletionCaretPlacement.ApplyCustomCommit(
			doc, 1, 3, "Grid></Grid>", 5);

		Assert.AreEqual("<Grid></Grid>", after);
		var (before, afterCh) = CompletionCaretPlacement.CharsAroundCaret(after, caret);
		Assert.AreEqual('>', before);
		Assert.AreEqual('<', afterCh);
	}

	[TestMethod]
	public void ValueCommitPlacesCaretAfterInsertedValueNotBeforeIt()
	{
		const string doc = "RequestedThemeVariant=\"Dark\"";
		var darkStart = doc.IndexOf("Dark", StringComparison.Ordinal);
		var caret = CompletionCaretPlacement.PlaceCaretOnCommittedLine(
			doc, darkStart, "Dark", "Dark".Length);

		var (before, afterCh) = CompletionCaretPlacement.CharsAroundCaret(doc, caret);
		Assert.AreEqual('k', before);
		Assert.AreEqual('"', afterCh);
	}

	[TestMethod]
	public void SlideDoesNotMoveIntoFilledQuotes()
	{
		const string doc = "RequestedThemeVariant=\"Dark\"";
		var atNameEnd = doc.IndexOf('=');
		var caret = CompletionCaretPlacement.SlideCaretIntoEmptyAttributeQuotes(doc, atNameEnd);
		Assert.AreEqual(atNameEnd, caret);
	}

	[TestMethod]
	public void PlaceCaretOnCommittedLineUsesInsertStartNotLineStart()
	{
		const string doc = "\t\t\t\tRequestedThemeVariant=\"\"";
		const string insert = "RequestedThemeVariant=\"\"";
		var insertStart = doc.IndexOf("RequestedThemeVariant", StringComparison.Ordinal);
		var rec = CompletionCaretPlacement.TryGetCaretIndexBetweenEmptyQuotes(insert);
		var caret = CompletionCaretPlacement.PlaceCaretOnCommittedLine(doc, insertStart, insert, rec);

		var (before, afterCh) = CompletionCaretPlacement.CharsAroundCaret(doc, caret);
		Assert.AreEqual('"', before);
		Assert.AreEqual('"', afterCh);
	}

	[TestMethod]
	public void PlaceCaretOnCommittedLineIgnoresTypedPrefixLengthAndFindsQuotes()
	{
		const string doc = "\t\t\t\tRequestedThemeVariant=\"\"";
		var nameStart = doc.IndexOf("RequestedThemeVariant", StringComparison.Ordinal);
		var wrongIndex = "RequestedThemeVar".Length;
		var caret = CompletionCaretPlacement.PlaceCaretOnCommittedLine(
			doc, nameStart, "RequestedThemeVariant=\"\"", wrongIndex);

		var (before, afterCh) = CompletionCaretPlacement.CharsAroundCaret(doc, caret);
		Assert.AreEqual('"', before);
		Assert.AreEqual('"', afterCh);
	}

	[TestMethod]
	public void PlaceCaretDoesNotUseEarlierEmptyQuotesOnTheSameLine()
	{
		const string doc = "<Button Name=\"\" RequestedThemeVariant=\"\">";
		const string insert = "RequestedThemeVariant=\"\"";
		var insertStart = doc.IndexOf("RequestedThemeVariant", StringComparison.Ordinal);
		var rec = CompletionCaretPlacement.TryGetCaretIndexBetweenEmptyQuotes(insert);
		var caret = CompletionCaretPlacement.PlaceCaretOnCommittedLine(doc, insertStart, insert, rec);

		Assert.AreEqual(insertStart + rec, caret);
		var (before, afterCh) = CompletionCaretPlacement.CharsAroundCaret(doc, caret);
		Assert.AreEqual('"', before);
		Assert.AreEqual('"', afterCh);
		Assert.AreEqual("RequestedThemeVariant=\"\"", doc.Substring(insertStart, insert.Length));
	}

	[TestMethod]
	public void DarkValueCaretIsAfterValueNotBeforeAttributeName()
	{
		const string doc = "RequestedThemeVariant=\"Dark\"";
		var insertStart = doc.IndexOf("Dark", StringComparison.Ordinal);
		var caret = CompletionCaretPlacement.GetCaretAfterReplace(insertStart, "Dark", "Dark".Length);

		Assert.AreNotEqual(doc.IndexOf("RequestedThemeVariant", StringComparison.Ordinal), caret);
		var (before, afterCh) = CompletionCaretPlacement.CharsAroundCaret(doc, caret);
		Assert.AreEqual('k', before);
		Assert.AreEqual('"', afterCh);
	}

	[TestMethod]
	public void SlideCaretFromEndOfNameIntoEmptyQuotes()
	{
		const string doc = "<CornerstoneApplication RequestedThemeVariant=\"\"";
		var atNameEnd = doc.IndexOf('=');
		Assert.AreEqual('=', doc[atNameEnd]);

		var caret = CompletionCaretPlacement.SlideCaretIntoEmptyAttributeQuotes(doc, atNameEnd);
		var (before, afterCh) = CompletionCaretPlacement.CharsAroundCaret(doc, caret);
		Assert.AreEqual('"', before);
		Assert.AreEqual('"', afterCh);
	}

	[TestMethod]
	public void EmptyQuotesInsertAlwaysPlacesCaretBetweenQuotesEvenIfOffsetWrong()
	{
		const string insert = "RequestedThemeVariant=\"\"";
		Assert.AreEqual(insert.IndexOf("=\"\"", StringComparison.Ordinal) + 2,
			CompletionCaretPlacement.TryGetCaretIndexBetweenEmptyQuotes(insert));

		const string doc = "<CornerstoneApplication ";
		var (after, caret) = CompletionCaretPlacement.SimulateCommit(
			doc, doc.Length, doc.Length, insert, recommendedCursorOffset: insert.Length);

		Assert.AreEqual(doc + insert, after);
		var (before, afterCh) = CompletionCaretPlacement.CharsAroundCaret(after, caret);
		Assert.AreEqual('"', before);
		Assert.AreEqual('"', afterCh);
	}

	[TestMethod]
	public void AttributeCommitPlacesCaretBetweenQuotes()
	{
		const string doc = "<CornerstoneApplication xmlns=\"https://github.com/BobbyCannon/Cornerstone\" ";
		const string insert = "RequestedThemeVariant=\"\"";
		var rec = CompletionCaretPlacement.TryGetCaretIndexBetweenEmptyQuotes(insert);

		var (after, caret) = CompletionCaretPlacement.SimulateCommit(
			doc, doc.Length, doc.Length, insert, rec);

		Assert.AreEqual(doc + "RequestedThemeVariant=\"\"", after);
		var (before, afterCh) = CompletionCaretPlacement.CharsAroundCaret(after, caret);
		Assert.AreEqual('"', before);
		Assert.AreEqual('"', afterCh);
	}

	[TestMethod]
	public void LeadingWhitespaceOnlyRestoreKeepsCaretBetweenQuotes()
	{
		const string originalLine = "\t<CornerstoneApplication RequestedThemeVariant=\"\"";
		const string grown = "\t\t<CornerstoneApplication RequestedThemeVariant=\"\"";
		var insertStart = grown.IndexOf("RequestedThemeVariant", StringComparison.Ordinal);
		var caretIndex = "RequestedThemeVariant=".Length + 1;
		var caretOnGrown = insertStart + caretIndex;

		var (restored, shiftedInsert) = CompletionCaretPlacement.RestoreLeadingWhitespaceOnly(
			grown, caretOnGrown, originalLine, insertStart);

		Assert.AreEqual(originalLine, restored);
		var caret = shiftedInsert + caretIndex;
		var (before, afterCh) = CompletionCaretPlacement.CharsAroundCaret(restored, caret);
		Assert.AreEqual('"', before);
		Assert.AreEqual('"', afterCh);
	}

	[TestMethod]
	public void FullLineReplaceWouldMapCaretToLineEnds()
	{
		// Documents why we must not Replace(line.Extent): a tracking point
		// inside that span becomes the start (Negative) or end (Positive) of the line.
		const string line = "\t<Grid></Grid>";
		var caretInGap = line.IndexOf("></", StringComparison.Ordinal) + 1; // between tags
		var lineStart = 0;
		var lineEnd = line.Length;

		var negativeMapsTo = lineStart;
		var positiveMapsTo = lineEnd;
		Assert.AreNotEqual(caretInGap, negativeMapsTo);
		Assert.AreNotEqual(caretInGap, positiveMapsTo);
		Assert.AreEqual('\t', line[negativeMapsTo]);
		Assert.AreEqual('>', line[positiveMapsTo - 1]);
	}

	[TestMethod]
	public void FullMainViewLineTextBCommitCaretAndIndent()
	{
		const string line = "\t\t<Grid></Grid><StackPanel></StackPanel><TextB";
		var start = line.LastIndexOf("TextB", StringComparison.Ordinal);
		var set = GetCompletionsFor(line);
		var item = set.Completions.Single(c => c.DisplayText == "TextBlock");

		var (after, caret) = CompletionCaretPlacement.SimulateCommit(
			line, start, line.Length, item.InsertText, item.RecommendedCursorOffset,
			smartIndentedLeadingWs: "\t\t\t");

		Assert.AreEqual("\t\t<Grid></Grid><StackPanel></StackPanel><TextBlock />", after);
		Assert.AreEqual(" />", after.Substring(caret, 3));
	}

	#endregion
}
