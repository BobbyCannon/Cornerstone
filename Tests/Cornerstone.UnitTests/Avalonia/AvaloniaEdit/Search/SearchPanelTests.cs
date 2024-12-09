#region References

using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Cornerstone.Avalonia.AvaloniaEdit;
using NUnit.Framework;
using NUnit.Framework.Legacy;

#endregion

namespace Cornerstone.UnitTests.Avalonia.AvaloniaEdit.Search;

[TestFixture]
public class SearchPanelTests : BaseTextEditorTests
{
	#region Methods

	[AvaloniaTest]
	public void ClearSearchPatternShouldCleanSelection()
	{
		UnitTestApplication.InitializeStyles();

		var textEditor = CreateEditor();
		textEditor.Text = "hello world";
		textEditor.Select(0, 5);
		ClassicAssert.AreEqual(5, textEditor.SelectionLength);

		textEditor.SearchPanel.SearchPattern = "world";
		textEditor.SearchPanel.Open();
		textEditor.SearchPanel.SearchPattern = "";

		ClassicAssert.AreEqual(0, textEditor.SelectionLength);
	}

	[AvaloniaTest]
	public void FindNextAfterFindNextShouldFindTheOccurence()
	{
		UnitTestApplication.InitializeStyles();

		var textEditor = CreateEditor();
		textEditor.Text = "hello world world";

		textEditor.SearchPanel.SearchPattern = "world";
		textEditor.SearchPanel.Open();
		textEditor.SearchPanel.FindNext();
		textEditor.SearchPanel.FindNext();

		ClassicAssert.AreEqual(12, textEditor.SelectionStart);
		ClassicAssert.AreEqual(5, textEditor.SelectionLength);
	}

	[AvaloniaTest]
	public void FindNextShouldFindNextOccurence()
	{
		UnitTestApplication.InitializeStyles();

		var textEditor = CreateEditor();
		textEditor.Text = "hello world";

		textEditor.SearchPanel.SearchPattern = "world";
		textEditor.SearchPanel.Open();
		textEditor.SearchPanel.FindNext();

		ClassicAssert.AreEqual(6, textEditor.SelectionStart);
		ClassicAssert.AreEqual(5, textEditor.SelectionLength);
	}

	[AvaloniaTest]
	public void FindPreviousAfterFindPreviousShouldFindOccurence()
	{
		UnitTestApplication.InitializeStyles();

		var textEditor = CreateEditor();
		textEditor.Text = "hello world world";
		textEditor.TextArea.Caret.Offset = 17;

		textEditor.SearchPanel.SearchPattern = "world";
		textEditor.SearchPanel.Open();
		textEditor.SearchPanel.FindPrevious();
		textEditor.SearchPanel.FindPrevious();

		ClassicAssert.AreEqual(6, textEditor.SelectionStart);
		ClassicAssert.AreEqual(5, textEditor.SelectionLength);
	}

	[AvaloniaTest]
	public void FindPreviousShouldFindPreviousOccurence()
	{
		UnitTestApplication.InitializeStyles();

		var textEditor = CreateEditor();
		textEditor.Text = "hello world";
		textEditor.TextArea.Caret.Offset = 6;

		textEditor.SearchPanel.SearchPattern = "hello";
		textEditor.SearchPanel.Open();
		textEditor.SearchPanel.FindPrevious();

		ClassicAssert.AreEqual(0, textEditor.SelectionStart);
		ClassicAssert.AreEqual(5, textEditor.SelectionLength);
	}

	[AvaloniaTest]
	public void FindShouldSelectFirstResultWhenThereAreNoResultsAfterCaret()
	{
		UnitTestApplication.InitializeStyles();

		var textEditor = CreateEditor();
		textEditor.Text = "hello world lovely";
		textEditor.TextArea.Caret.Offset = 12;

		textEditor.SearchPanel.SearchPattern = "world";
		textEditor.SearchPanel.Open();
		textEditor.SearchPanel.FindNext();

		ClassicAssert.AreEqual(6, textEditor.SelectionStart);
		ClassicAssert.AreEqual(5, textEditor.SelectionLength);
	}

	[AvaloniaTest]
	public void FindShouldSelectTheFirstResultAfterCaret()
	{
		UnitTestApplication.InitializeStyles();

		var textEditor = CreateEditor();
		textEditor.Text = "hello world world";

		textEditor.TextArea.Caret.Offset = 6;

		textEditor.SearchPanel.SearchPattern = "world";
		textEditor.SearchPanel.Open();
		textEditor.SearchPanel.FindNext();

		ClassicAssert.AreEqual(6, textEditor.SelectionStart);
		ClassicAssert.AreEqual(5, textEditor.SelectionLength);
	}

	[AvaloniaTest]
	public void ReplaceAllShouldReplaceAllOccurrences()
	{
		UnitTestApplication.InitializeStyles();

		var textEditor = CreateEditor();
		textEditor.Text = "hello world hello world";

		textEditor.SearchPanel.SearchPattern = "hello";
		textEditor.SearchPanel.ReplacePattern = "bye";
		textEditor.SearchPanel.Open();
		textEditor.SearchPanel.IsReplaceMode = true;
		textEditor.SearchPanel.ReplaceAll();

		ClassicAssert.AreEqual("bye world bye world", textEditor.Text);
	}

	[AvaloniaTest]
	public void ReplaceNextAfterReplaceNextShouldReplaceOccurence()
	{
		UnitTestApplication.InitializeStyles();

		var textEditor = CreateEditor();
		textEditor.Text = "hello world world world";
		textEditor.TextArea.Caret.Offset = 6;

		textEditor.SearchPanel.SearchPattern = "world";
		textEditor.SearchPanel.ReplacePattern = "universe";
		textEditor.SearchPanel.Open();
		textEditor.SearchPanel.IsReplaceMode = true;
		textEditor.SearchPanel.ReplaceNext();
		textEditor.SearchPanel.ReplaceNext();

		ClassicAssert.AreEqual("hello universe universe world", textEditor.Text);
	}

	[AvaloniaTest]
	public void ReplaceNextShouldReplaceOccurence()
	{
		UnitTestApplication.InitializeStyles();

		var textEditor = CreateEditor();
		textEditor.Text = "hello world";

		textEditor.TextArea.Caret.Offset = 6;

		textEditor.SearchPanel.SearchPattern = "world";
		textEditor.SearchPanel.ReplacePattern = "universe";
		textEditor.SearchPanel.Open();
		textEditor.SearchPanel.IsReplaceMode = true;
		textEditor.SearchPanel.ReplaceNext();

		ClassicAssert.AreEqual("hello universe", textEditor.Text);
	}

	[AvaloniaTest]
	public void ReplaceNextShouldReplaceOccurenceAtCaretIndex()
	{
		UnitTestApplication.InitializeStyles();

		var textEditor = CreateEditor();
		textEditor.Text = "hello world world";
		textEditor.TextArea.Caret.Offset = 6;

		textEditor.SearchPanel.SearchPattern = "world";
		textEditor.SearchPanel.ReplacePattern = "universe";
		textEditor.SearchPanel.Open();
		textEditor.SearchPanel.IsReplaceMode = true;
		textEditor.SearchPanel.ReplaceNext();

		ClassicAssert.AreEqual("hello universe world", textEditor.Text);
	}

	#endregion
}

public abstract class BaseTextEditorTests : CornerstoneUnitTest
{
	#region Methods

	protected static TextEditorControl CreateEditor()
	{
		var textEditor = new TextEditorControl();
		var window = new Window { Content = textEditor };
		window.Show();
		textEditor.ApplyTemplate();
		return textEditor;
	}

	#endregion
}