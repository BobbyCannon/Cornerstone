#region References

using System;
using System.Linq;
using Cornerstone.VisualStudio.Core.Completion;
using Xunit;

#endregion

namespace Cornerstone.VisualStudio.Tests;

/// <summary>
/// Stress tests for element completion caret placement — pure pipeline shared with the IDE.
/// </summary>
public class CompletionCaretStressTests : XamlCompletionTestBase
{
	#region Methods

	[Theory]
	[InlineData("TextBlock", "TextBlock />", 9, 'k', ' ')] // after 'k' of TextBlock, before space of " />"
	[InlineData("Grid", "Grid></Grid>", 5, '>', '<')] // after open '>', before closing '<'
	[InlineData("StackPanel", "StackPanel></StackPanel>", 11, '>', '<')]
	[InlineData("Button", "Button></Button>", 7, '>', '<')]
	[InlineData("Image", "Image />", 5, 'e', ' ')]
	public void BuildElementTagInsertCaretIndexSplitsExpectedChars(
		string name,
		string expectedInsert,
		int expectedCaretIndex,
		char expectBefore,
		char expectAfter)
	{
		var (insert, caretIndex) = CompletionEngine.BuildElementTagInsert(name);
		Assert.Equal(expectedInsert, insert);
		Assert.Equal(expectedCaretIndex, caretIndex);

		var resolved = CompletionCaretPlacement.ResolveCaretIndexInInsert(insert, caretIndex);
		Assert.Equal(expectedCaretIndex, resolved);

		Assert.Equal(expectBefore, insert[resolved - 1]);
		if (resolved < insert.Length)
		{
			Assert.Equal(expectAfter, insert[resolved]);
		}
	}

	[Fact]
	public void SimulateCommitTextBToTextBlockCaretBeforeSpaceSlash()
	{
		const string doc = "\t<Grid></Grid><StackPanel></StackPanel><TextB";
		var filterStart = doc.LastIndexOf("TextB", StringComparison.Ordinal);
		var caretBefore = doc.Length;

		var set = GetCompletionsFor(doc);
		Assert.NotNull(set);
		var item = set.Completions.Single(c => c.DisplayText == "TextBlock");
		Assert.Equal("TextBlock />", item.InsertText);
		Assert.Equal(9, item.RecommendedCursorOffset);

		// Engine start must be filter start (relative to body — already transformed in set).
		Assert.Equal(filterStart, set.StartPosition);

		var (after, caret) = CompletionCaretPlacement.SimulateCommit(
			doc, filterStart, caretBefore, item.InsertText, item.RecommendedCursorOffset);

		Assert.Equal("\t<Grid></Grid><StackPanel></StackPanel><TextBlock />", after);
		var (before, afterCh) = CompletionCaretPlacement.CharsAroundCaret(after, caret);
		Assert.Equal('k', before); // end of TextBlock
		Assert.Equal(' ', afterCh); // space of " />"
		Assert.Equal("TextBlock", after.Substring(caret - 9, 9));
		Assert.Equal(" />", after.Substring(caret, 3));
	}

	[Fact]
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
		Assert.NotNull(set);
		var item = set.Completions.Single(c => c.DisplayText == "Grid");
		Assert.Equal("Grid></Grid>", item.InsertText);
		Assert.Equal(5, item.RecommendedCursorOffset);

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
		Assert.Equal('>', beforeCh); // end of <Grid>
		Assert.Equal('<', afterCh); // start of </Grid>
		Assert.Equal("</Grid>", after.Substring(caretPos, 7));
	}

	[Fact]
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
		Assert.StartsWith("\t<", after);
		Assert.False(after.StartsWith("\t\t", StringComparison.Ordinal));
		Assert.Contains("<TextBlock />", after);

		var (beforeCh, afterCh) = CompletionCaretPlacement.CharsAroundCaret(after, caretPos);
		Assert.Equal('k', beforeCh);
		Assert.Equal(' ', afterCh);
	}

	[Fact]
	public void SimulateCommitGridWithSmartIndentKeepsCaretBetweenTags()
	{
		const string doc = "\t<Gri";
		var start = doc.IndexOf("Gri", StringComparison.Ordinal);
		var caret = doc.Length;

		var (insert, rec) = CompletionEngine.BuildElementTagInsert("Grid");
		Assert.Equal("Grid></Grid>", insert);
		Assert.Equal(5, rec);

		var (after, caretPos) = CompletionCaretPlacement.SimulateCommit(
			doc, start, caret, insert, rec, smartIndentedLeadingWs: "\t\t");

		Assert.Equal("\t<Grid></Grid>", after);
		var (beforeCh, afterCh) = CompletionCaretPlacement.CharsAroundCaret(after, caretPos);
		Assert.Equal('>', beforeCh);
		Assert.Equal('<', afterCh);
	}

	[Theory]
	[InlineData("TextB", "TextBlock")]
	[InlineData("Gri", "Grid")]
	[InlineData("Stack", "StackPanel")]
	[InlineData("But", "Button")]
	[InlineData("Imag", "Image")]
	public void EngineCompletionRecommendedCursorOffsetMatchesBuildElementTagInsert(
		string typed,
		string displayName)
	{
		var set = GetCompletionsFor("<" + typed);
		Assert.NotNull(set);
		var item = set.Completions.Single(c => c.DisplayText == displayName);
		var (insert, rec) = CompletionEngine.BuildElementTagInsert(displayName);
		Assert.Equal(insert, item.InsertText);
		Assert.Equal(rec, item.RecommendedCursorOffset);

		// Resolve like the IDE does.
		var caretIndex = CompletionCaretPlacement.ResolveCaretIndexInInsert(
			item.InsertText, item.RecommendedCursorOffset);
		Assert.Equal(rec, caretIndex);

		// Document after commit must have caret in the documented slot.
		var doc = "    <" + typed;
		var filterStart = doc.Length - typed.Length;
		var (after, caretPos) = CompletionCaretPlacement.SimulateCommit(
			doc, filterStart, doc.Length, item.InsertText, item.RecommendedCursorOffset);

		Assert.Equal("    <" + insert, after);
		if (insert.Contains("></"))
		{
			var (b, a) = CompletionCaretPlacement.CharsAroundCaret(after, caretPos);
			Assert.Equal('>', b);
			Assert.Equal('<', a);
		}
		else
		{
			var (b, a) = CompletionCaretPlacement.CharsAroundCaret(after, caretPos);
			Assert.Equal(insert[caretIndex - 1], b);
			Assert.Equal(' ', a); // " />"
		}
	}

	[Fact]
	public void WrongDoubleCountCaretMathIsRejectedByHelpers()
	{
		// Documents the historical bug: Positive tracking at end + add length again.
		const string insert = "Grid></Grid>";
		const int replaceStart = 4;
		const int rec = 5;

		var correct = CompletionCaretPlacement.GetCaretAfterReplace(replaceStart, insert, rec);
		Assert.Equal(replaceStart + rec, correct);

		var wrongPositiveMappedStart = replaceStart + insert.Length;
		var wrong = wrongPositiveMappedStart + insert.Length - (insert.Length - rec);
		Assert.NotEqual(correct, wrong);
		Assert.True(wrong > correct);
	}

	[Fact]
	public void FullMainViewLineTextBCommitCaretAndIndent()
	{
		const string line = "\t\t<Grid></Grid><StackPanel></StackPanel><TextB";
		var start = line.LastIndexOf("TextB", StringComparison.Ordinal);
		var set = GetCompletionsFor(line);
		var item = set.Completions.Single(c => c.DisplayText == "TextBlock");

		var (after, caret) = CompletionCaretPlacement.SimulateCommit(
			line, start, line.Length, item.InsertText, item.RecommendedCursorOffset,
			smartIndentedLeadingWs: "\t\t\t");

		Assert.Equal("\t\t<Grid></Grid><StackPanel></StackPanel><TextBlock />", after);
		Assert.Equal(" />", after.Substring(caret, 3));
	}

	#endregion
}
