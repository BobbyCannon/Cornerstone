#region References

using System;
using System.Linq;
using Cornerstone.VisualStudio.Core.Completion;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests;

/// <summary>
/// Element-name completion: replace span, self-closing vs paired tags, caret placement, indent.
/// </summary>
[TestClass]
public class ElementCompletionTests : XamlCompletionTestBase
{
	#region Methods

	[TestMethod]
	public void PartialTagStartPositionCoversTypedFilterNotAngleBracket()
	{
		var set = GetCompletionsFor("<TextB");
		Assert.IsNotNull(set);

		var (start, length) = CompletionEngine.GetApplicableSpan(set.StartPosition, "<TextB".Length);
		Assert.AreEqual(1, start); // after '<'
		Assert.AreEqual(5, length); // "TextB"
		Assert.AreEqual("TextB", ("<" + "TextB").Substring(start, length));
	}

	[TestMethod]
	public void PartialTagTextBOffersTextBlockSelfClosing()
	{
		var set = GetCompletionsFor("<TextB");
		Assert.IsNotNull(set);

		var textBlock = set.Completions.Single(c => c.DisplayText == "TextBlock");
		Assert.AreEqual("TextBlock />", textBlock.InsertText);
		Assert.AreEqual(9, textBlock.RecommendedCursorOffset); // after "TextBlock", before " />"
	}

	[TestMethod]
	public void PartialTagStackOffersStackPanelPairedTags()
	{
		var set = GetCompletionsFor("<Stack");
		Assert.IsNotNull(set);

		var stackPanel = set.Completions.Single(c => c.DisplayText == "StackPanel");
		Assert.AreEqual("StackPanel></StackPanel>", stackPanel.InsertText);
		// Caret after opening '>' — between tags.
		Assert.AreEqual("StackPanel>".Length, stackPanel.RecommendedCursorOffset);
	}

	[TestMethod]
	public void CommitStackPanelPlacesCursorBetweenTags()
	{
		const string before = "\t\t<TextBlock /><Stack";
		const string insert = "StackPanel></StackPanel>";
		var caret = before.Length;
		var start = before.LastIndexOf('<') + 1;

		var after = CompletionEngine.ApplyCompletionReplace(before, start, caret, insert);

		Assert.AreEqual("\t\t<TextBlock /><StackPanel></StackPanel>", after);

		var cursorFromEnd = CompletionEngine.GetCursorOffsetFromEnd(
			insert, recommendedCursorOffset: "StackPanel>".Length);
		var caretAfter = after.Length - cursorFromEnd;
		Assert.AreEqual("\t\t<TextBlock /><StackPanel>", after.Substring(0, caretAfter));
		Assert.AreEqual("</StackPanel>", after.Substring(caretAfter));
	}

	[TestMethod]
	public void CommitReplacesFilterWithSelfClosingTag()
	{
		const string before = "<StackPanel>\n  <TextB";
		const string insert = "TextBlock />";
		var caret = before.Length;
		var start = before.LastIndexOf('<') + 1;

		var after = CompletionEngine.ApplyCompletionReplace(before, start, caret, insert);

		Assert.AreEqual("<StackPanel>\n  <TextBlock />", after);

		var cursorFromEnd = CompletionEngine.GetCursorOffsetFromEnd(insert, recommendedCursorOffset: "TextBlock".Length);
		var caretAfterCommit = after.Length - cursorFromEnd;
		Assert.AreEqual("<StackPanel>\n  <TextBlock", after.Substring(0, caretAfterCommit));
		Assert.AreEqual(" />", after.Substring(caretAfterCommit));
	}

	[TestMethod]
	public void PreserveLineLeadingWhitespaceStripsExtraTabFromSmartIndent()
	{
		// Editor grew indent from 1 tab to 2 when session started on '<' — restore 1.
		var original = "\t<Grid></Grid><StackPanel></StackPanel><TextB";
		var afterSmartIndent = "\t\t<Grid></Grid><StackPanel></StackPanel><TextBlock />";
		var fixedLine = CompletionEngine.PreserveLineLeadingWhitespace(original, afterSmartIndent);
		Assert.AreEqual("\t<Grid></Grid><StackPanel></StackPanel><TextBlock />", fixedLine);
	}

	[TestMethod]
	public void PreserveLineLeadingWhitespacePinsExactOriginalIndentEvenIfStyleChanges()
	{
		// Tab → spaces conversion / different indent style: still pin original leading.
		var original = "\t<Stack";
		var after = "    <StackPanel></StackPanel>";
		var fixedLine = CompletionEngine.PreserveLineLeadingWhitespace(original, after);
		Assert.AreEqual("\t<StackPanel></StackPanel>", fixedLine);
	}

	[TestMethod]
	public void PreserveLineLeadingWhitespaceKeepsSameIndent()
	{
		var line = "\t\t<StackPanel></StackPanel>";
		Assert.AreEqual(line, CompletionEngine.PreserveLineLeadingWhitespace(line, line));
	}

	[TestMethod]
	public void PreferSelfClosingTextBlockTrueStackPanelFalse()
	{
		Assert.IsTrue(CompletionEngine.PreferSelfClosingElement("TextBlock"));
		Assert.IsFalse(CompletionEngine.PreferSelfClosingElement("StackPanel"));
		Assert.IsFalse(CompletionEngine.PreferSelfClosingElement("Grid"));
		Assert.IsFalse(CompletionEngine.PreferSelfClosingElement("Button"));
		Assert.IsTrue(CompletionEngine.PreferSelfClosingElement("Image"));
	}

	[TestMethod]
	public void ApplicableSpanClampsInvalidEngineStart()
	{
		var (start, length) = CompletionEngine.GetApplicableSpan(engineStartPosition: 50, caretPosition: 10);
		Assert.AreEqual(10, start);
		Assert.AreEqual(0, length);

		(start, length) = CompletionEngine.GetApplicableSpan(engineStartPosition: -3, caretPosition: 4);
		Assert.AreEqual(0, start);
		Assert.AreEqual(4, length);
	}

	[TestMethod]
	public void BestMatchTextBIsTextBlockNotFirstClosingTag()
	{
		var set = GetCompletionsFor("<UserControl><TextB");
		Assert.IsNotNull(set);

		var filter = "TextB";
		var best = set.Completions.FirstOrDefault(c =>
			c.DisplayText.StartsWith(filter, System.StringComparison.OrdinalIgnoreCase));
		Assert.IsNotNull(best);
		Assert.AreEqual("TextBlock", best.DisplayText);
		Assert.AreEqual("TextBlock />", best.InsertText);
	}

	[TestMethod]
	public void GenericElementStillUsesTypeArgumentsNotSelfClose()
	{
		var set = GetCompletionsFor("<FuncDataTemplate");
		Assert.IsNotNull(set);

		var generic = set.Completions.First(c => c.DisplayText.Contains("<"));
		Assert.Contains("x:TypeArguments", generic.InsertText);
		Assert.DoesNotContain("/>", generic.InsertText);
	}

	/// <summary>
	/// Regression: MainView-style document with ProgressBar multi-line, then incomplete &lt;TextB
	/// must still complete to TextBlock /&gt; without eating prior siblings or indent.
	/// </summary>
	[TestMethod]
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
		Assert.IsNotNull(set);

		var textBlock = set.Completions.FirstOrDefault(c => c.DisplayText == "TextBlock");
		Assert.IsNotNull(textBlock);
		Assert.AreEqual("TextBlock />", textBlock.InsertText);

		// Engine start must only cover "TextB", not the whole StackPanel body.
		var body = before.TrimEnd();
		var (start, length) = CompletionEngine.GetApplicableSpan(set.StartPosition, body.Length);
		Assert.AreEqual("TextB", body.Substring(start, length));
		Assert.IsTrue(start > 0 && body[start - 1] == '<');

		var after = CompletionEngine.ApplyCompletionReplace(body, start, body.Length, textBlock.InsertText);

		// Prior siblings preserved; incomplete TextB fully replaced; no double-prefix.
		Assert.Contains("<ProgressBar IsIndeterminate=\"true\"", after);
		Assert.Contains("<TextBlock />", after);
		Assert.DoesNotContain("TextBTextBlock", after);
		// Incomplete tag gone (do not use Contains("<TextB") — that matches inside "TextBlock").
		Assert.DoesNotContain("<TextB\n", after);
		Assert.DoesNotContain("<TextB\r", after);
		Assert.IsFalse(after.TrimEnd().EndsWith("<TextB"), "left incomplete <TextB at end");

		var textBlockLine = after.Split('\n').Last(l => l.Contains("<TextBlock />"));
		// Same indent as the incomplete line had (8 spaces in this fixture).
		StringAssert.StartsWith(textBlockLine.TrimEnd('\r'), "        <TextBlock />");

		var cursorFromEnd = CompletionEngine.GetCursorOffsetFromEnd(
			textBlock.InsertText, textBlock.RecommendedCursorOffset);
		var caret = after.Length - cursorFromEnd;
		Assert.AreEqual(" />", after.Substring(caret, 3));
	}

	/// <summary>
	/// Repro: incomplete &lt;Grid before &lt;/UserControl&gt; must become paired Grid tags
	/// without corrupting the parent closing tag (was producing &lt;/U&gt;serControl&gt;).
	/// </summary>
	[TestMethod]
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
		Assert.IsTrue(start > 0 && before[start - 1] == '<');

		var insert = "Grid></Grid>";
		var after = CompletionEngine.ApplyCompletionReplace(before, start, caret, insert);

		Assert.Contains("<Grid></Grid>", after);
		Assert.Contains("</UserControl>", after);
		// Corruption pattern was "</U>serControl>" ( '>' inserted after first letter of closing name ).
		Assert.DoesNotContain("</U>", after);

		// Caret between Grid tags.
		var cursorFromEnd = CompletionEngine.GetCursorOffsetFromEnd(insert, "Grid>".Length);
		var caretAfter = start + insert.Length - cursorFromEnd;
		Assert.AreEqual("<Grid>", after.Substring(start - 1, 6)); // includes '<'
		Assert.AreEqual("</Grid>", after.Substring(caretAfter, 7));
	}

	/// <summary>
	/// Caret math: insertStart + (len - cursorOffset), NOT insertStartAfterPositive + len - offset.
	/// Positive tracking after replace sits *after* the insert; adding len again walks into the next tag.
	/// </summary>
	[TestMethod]
	[DataRow("Grid></Grid>", 5)] // after "Grid>"
	[DataRow("TextBlock />", 9)] // after "TextBlock"
	public void CaretIndexWithinInsertDoesNotDoubleCountLength(string insert, int recommendedCursorOffset)
	{
		const int replaceStart = 10; // arbitrary
		var cursorOffsetFromEnd = CompletionEngine.GetCursorOffsetFromEnd(insert, recommendedCursorOffset);

		// Correct (Negative tracking stays at start of insert):
		var correctCaret = replaceStart + (insert.Length - cursorOffsetFromEnd);
		Assert.AreEqual(replaceStart + recommendedCursorOffset, correctCaret);

		// Bug pattern (Positive tracking at end of insert, then add length again):
		var wrongMappedStart = replaceStart + insert.Length;
		var wrongCaret = wrongMappedStart + insert.Length - cursorOffsetFromEnd;
		Assert.IsTrue(wrongCaret > correctCaret + insert.Length / 2,
			"documents the old double-count landing deep past the insert");
	}

	[TestMethod]
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
		Assert.IsNotNull(set);
		Assert.AreEqual("TextB", prefix.TrimEnd().Substring(set.StartPosition));

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
