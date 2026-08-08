#region References

using System;
using System.Linq;
using Cornerstone.VisualStudio.Core.Completion;
using Xunit;

#endregion

namespace Cornerstone.VisualStudio.Tests;

/// <summary>
/// Element-name completion: replace span, self-closing vs paired tags, caret placement, indent.
/// </summary>
public class ElementCompletionTests : XamlCompletionTestBase
{
	#region Methods

	[Fact]
	public void PartialTagStartPositionCoversTypedFilterNotAngleBracket()
	{
		var set = GetCompletionsFor("<TextB");
		Assert.NotNull(set);

		var (start, length) = CompletionEngine.GetApplicableSpan(set.StartPosition, "<TextB".Length);
		Assert.Equal(1, start); // after '<'
		Assert.Equal(5, length); // "TextB"
		Assert.Equal("TextB", ("<" + "TextB").Substring(start, length));
	}

	[Fact]
	public void PartialTagTextBOffersTextBlockSelfClosing()
	{
		var set = GetCompletionsFor("<TextB");
		Assert.NotNull(set);

		var textBlock = set.Completions.Single(c => c.DisplayText == "TextBlock");
		Assert.Equal("TextBlock />", textBlock.InsertText);
		Assert.Equal(9, textBlock.RecommendedCursorOffset); // after "TextBlock", before " />"
	}

	[Fact]
	public void PartialTagStackOffersStackPanelPairedTags()
	{
		var set = GetCompletionsFor("<Stack");
		Assert.NotNull(set);

		var stackPanel = set.Completions.Single(c => c.DisplayText == "StackPanel");
		Assert.Equal("StackPanel></StackPanel>", stackPanel.InsertText);
		// Caret after opening '>' — between tags.
		Assert.Equal("StackPanel>".Length, stackPanel.RecommendedCursorOffset);
	}

	[Fact]
	public void CommitStackPanelPlacesCursorBetweenTags()
	{
		const string before = "\t\t<TextBlock /><Stack";
		const string insert = "StackPanel></StackPanel>";
		var caret = before.Length;
		var start = before.LastIndexOf('<') + 1;

		var after = CompletionEngine.ApplyCompletionReplace(before, start, caret, insert);

		Assert.Equal("\t\t<TextBlock /><StackPanel></StackPanel>", after);

		var cursorFromEnd = CompletionEngine.GetCursorOffsetFromEnd(
			insert, recommendedCursorOffset: "StackPanel>".Length);
		var caretAfter = after.Length - cursorFromEnd;
		Assert.Equal("\t\t<TextBlock /><StackPanel>", after.Substring(0, caretAfter));
		Assert.Equal("</StackPanel>", after.Substring(caretAfter));
	}

	[Fact]
	public void CommitReplacesFilterWithSelfClosingTag()
	{
		const string before = "<StackPanel>\n  <TextB";
		const string insert = "TextBlock />";
		var caret = before.Length;
		var start = before.LastIndexOf('<') + 1;

		var after = CompletionEngine.ApplyCompletionReplace(before, start, caret, insert);

		Assert.Equal("<StackPanel>\n  <TextBlock />", after);

		var cursorFromEnd = CompletionEngine.GetCursorOffsetFromEnd(insert, recommendedCursorOffset: "TextBlock".Length);
		var caretAfterCommit = after.Length - cursorFromEnd;
		Assert.Equal("<StackPanel>\n  <TextBlock", after.Substring(0, caretAfterCommit));
		Assert.Equal(" />", after.Substring(caretAfterCommit));
	}

	[Fact]
	public void PreserveLineLeadingWhitespaceStripsExtraTabFromSmartIndent()
	{
		// Editor grew indent from 1 tab to 2 when session started on '<' — restore 1.
		var original = "\t<Grid></Grid><StackPanel></StackPanel><TextB";
		var afterSmartIndent = "\t\t<Grid></Grid><StackPanel></StackPanel><TextBlock />";
		var fixedLine = CompletionEngine.PreserveLineLeadingWhitespace(original, afterSmartIndent);
		Assert.Equal("\t<Grid></Grid><StackPanel></StackPanel><TextBlock />", fixedLine);
	}

	[Fact]
	public void PreserveLineLeadingWhitespacePinsExactOriginalIndentEvenIfStyleChanges()
	{
		// Tab → spaces conversion / different indent style: still pin original leading.
		var original = "\t<Stack";
		var after = "    <StackPanel></StackPanel>";
		var fixedLine = CompletionEngine.PreserveLineLeadingWhitespace(original, after);
		Assert.Equal("\t<StackPanel></StackPanel>", fixedLine);
	}

	[Fact]
	public void PreserveLineLeadingWhitespaceKeepsSameIndent()
	{
		var line = "\t\t<StackPanel></StackPanel>";
		Assert.Equal(line, CompletionEngine.PreserveLineLeadingWhitespace(line, line));
	}

	[Fact]
	public void PreferSelfClosingTextBlockTrueStackPanelFalse()
	{
		Assert.True(CompletionEngine.PreferSelfClosingElement("TextBlock"));
		Assert.False(CompletionEngine.PreferSelfClosingElement("StackPanel"));
		Assert.False(CompletionEngine.PreferSelfClosingElement("Grid"));
		Assert.False(CompletionEngine.PreferSelfClosingElement("Button"));
		Assert.True(CompletionEngine.PreferSelfClosingElement("Image"));
	}

	[Fact]
	public void ApplicableSpanClampsInvalidEngineStart()
	{
		var (start, length) = CompletionEngine.GetApplicableSpan(engineStartPosition: 50, caretPosition: 10);
		Assert.Equal(10, start);
		Assert.Equal(0, length);

		(start, length) = CompletionEngine.GetApplicableSpan(engineStartPosition: -3, caretPosition: 4);
		Assert.Equal(0, start);
		Assert.Equal(4, length);
	}

	[Fact]
	public void BestMatchTextBIsTextBlockNotFirstClosingTag()
	{
		var set = GetCompletionsFor("<UserControl><TextB");
		Assert.NotNull(set);

		var filter = "TextB";
		var best = set.Completions.FirstOrDefault(c =>
			c.DisplayText.StartsWith(filter, System.StringComparison.OrdinalIgnoreCase));
		Assert.NotNull(best);
		Assert.Equal("TextBlock", best.DisplayText);
		Assert.Equal("TextBlock />", best.InsertText);
	}

	[Fact]
	public void GenericElementStillUsesTypeArgumentsNotSelfClose()
	{
		var set = GetCompletionsFor("<FuncDataTemplate");
		Assert.NotNull(set);

		var generic = set.Completions.First(c => c.DisplayText.Contains("<"));
		Assert.Contains("x:TypeArguments", generic.InsertText);
		Assert.DoesNotContain("/>", generic.InsertText);
	}

	/// <summary>
	/// Regression: MainView-style document with ProgressBar multi-line, then incomplete &lt;TextB
	/// must still complete to TextBlock /&gt; without eating prior siblings or indent.
	/// </summary>
	[Fact]
	public void MainViewIncompleteTextBAfterProgressBarCompletesCleanly()
	{
		// Mirrors the real MainView.axaml failure case (body only; prologue supplies root).
		var before = """
			  <Design.DataContext>
			    <local:MyButton />
			  </Design.DataContext>

			    <StackPanel>
			        <TextBlock Text="{Binding Greeting}" HorizontalAlignment="Left" VerticalAlignment="Bottom"/>
			        <ProgressBar IsIndeterminate="true"
			                     Height="100">
			        </ProgressBar>
			        <TextB
			""";

		// Caret at end of "<TextB" (no content after — same as completing before typing next sibling).
		var set = GetCompletionsFor(before.TrimEnd());
		Assert.NotNull(set);

		var textBlock = set.Completions.FirstOrDefault(c => c.DisplayText == "TextBlock");
		Assert.NotNull(textBlock);
		Assert.Equal("TextBlock />", textBlock.InsertText);

		// Engine start must only cover "TextB", not the whole StackPanel body.
		var body = before.TrimEnd();
		var (start, length) = CompletionEngine.GetApplicableSpan(set.StartPosition, body.Length);
		Assert.Equal("TextB", body.Substring(start, length));
		Assert.True(start > 0 && body[start - 1] == '<');

		var after = CompletionEngine.ApplyCompletionReplace(body, start, body.Length, textBlock.InsertText);

		// Prior siblings preserved; incomplete TextB fully replaced; no double-prefix.
		Assert.Contains("<ProgressBar IsIndeterminate=\"true\"", after);
		Assert.Contains("<TextBlock />", after);
		Assert.DoesNotContain("TextBTextBlock", after);
		// Incomplete tag gone (do not use Contains("<TextB") — that matches inside "TextBlock").
		Assert.DoesNotContain("<TextB\n", after);
		Assert.DoesNotContain("<TextB\r", after);
		Assert.False(after.TrimEnd().EndsWith("<TextB"), "left incomplete <TextB at end");

		var textBlockLine = after.Split('\n').Last(l => l.Contains("<TextBlock />"));
		// Same indent as the incomplete line had (8 spaces in this fixture).
		Assert.StartsWith("        <TextBlock />", textBlockLine.TrimEnd('\r'));

		var cursorFromEnd = CompletionEngine.GetCursorOffsetFromEnd(
			textBlock.InsertText, textBlock.RecommendedCursorOffset);
		var caret = after.Length - cursorFromEnd;
		Assert.Equal(" />", after.Substring(caret, 3));
	}

	/// <summary>
	/// Repro: incomplete &lt;Grid before &lt;/UserControl&gt; must become paired Grid tags
	/// without corrupting the parent closing tag (was producing &lt;/U&gt;serControl&gt;).
	/// </summary>
	[Fact]
	public void GridBeforeUserControlCloseDoesNotCorruptParent()
	{
		const string before = """
			<UserControl xmlns="https://github.com/avaloniaui"
			             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			             x:Class="AvaloniaApplication.test">
			  <Grid
			</UserControl>
			""";

		// Caret after "Grid" on the incomplete open tag line.
		var caret = before.IndexOf("Grid", StringComparison.Ordinal) + "Grid".Length;
		var start = before.IndexOf("Grid", StringComparison.Ordinal);
		Assert.True(start > 0 && before[start - 1] == '<');

		var insert = "Grid></Grid>";
		var after = CompletionEngine.ApplyCompletionReplace(before, start, caret, insert);

		Assert.Contains("<Grid></Grid>", after);
		Assert.Contains("</UserControl>", after);
		// Corruption pattern was "</U>serControl>" ( '>' inserted after first letter of closing name ).
		Assert.DoesNotContain("</U>", after);

		// Caret between Grid tags.
		var cursorFromEnd = CompletionEngine.GetCursorOffsetFromEnd(insert, "Grid>".Length);
		var caretAfter = start + insert.Length - cursorFromEnd;
		Assert.Equal("<Grid>", after.Substring(start - 1, 6)); // includes '<'
		Assert.Equal("</Grid>", after.Substring(caretAfter, 7));
	}

	/// <summary>
	/// Caret math: insertStart + (len - cursorOffset), NOT insertStartAfterPositive + len - offset.
	/// Positive tracking after replace sits *after* the insert; adding len again walks into the next tag.
	/// </summary>
	[Theory]
	[InlineData("Grid></Grid>", 5)] // after "Grid>"
	[InlineData("TextBlock />", 9)] // after "TextBlock"
	public void CaretIndexWithinInsertDoesNotDoubleCountLength(string insert, int recommendedCursorOffset)
	{
		const int replaceStart = 10; // arbitrary
		var cursorOffsetFromEnd = CompletionEngine.GetCursorOffsetFromEnd(insert, recommendedCursorOffset);

		// Correct (Negative tracking stays at start of insert):
		var correctCaret = replaceStart + (insert.Length - cursorOffsetFromEnd);
		Assert.Equal(replaceStart + recommendedCursorOffset, correctCaret);

		// Bug pattern (Positive tracking at end of insert, then add length again):
		var wrongMappedStart = replaceStart + insert.Length;
		var wrongCaret = wrongMappedStart + insert.Length - cursorOffsetFromEnd;
		Assert.True(wrongCaret > correctCaret + insert.Length / 2,
			"documents the old double-count landing deep past the insert");
	}

	[Fact]
	public void MainViewTextBWithFollowingSiblingOnlyReplacesFilter()
	{
		// Cursor between TextB and the next sibling (user continues editing around incomplete tag).
		var prefix = """
			    <StackPanel>
			        <ProgressBar Height="100"></ProgressBar>
			        <TextB
			""";
		var suffix = """

			        <TextBox Text="{StaticResource ErrorBrush}"></TextBox>
			    </StackPanel>
			""";

		var set = GetCompletionsFor(prefix.TrimEnd(), suffix);
		Assert.NotNull(set);
		Assert.Equal("TextB", prefix.TrimEnd().Substring(set.StartPosition));

		var insert = "TextBlock />";
		var full = prefix.TrimEnd() + suffix;
		var caret = prefix.TrimEnd().Length;
		var start = set.StartPosition;
		var after = CompletionEngine.ApplyCompletionReplace(full, start, caret, insert);

		Assert.Contains("<TextBlock />", after);
		Assert.Contains("<TextBox Text=\"{StaticResource ErrorBrush}\">", after);
		Assert.DoesNotContain("TextBTextBlock", after);
		Assert.DoesNotContain("<TextB\n", after);
	}

	#endregion
}
