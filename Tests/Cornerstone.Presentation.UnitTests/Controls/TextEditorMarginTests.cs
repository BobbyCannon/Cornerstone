#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Controls.Text;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class TextEditorMarginTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ShowMarginsFalseHidesFoldingMargin()
	{
		var editor = CreateEditor();
		editor.ShowLineNumbers = true;
		editor.ShowMargins = false;

		Apply(editor);

		CornerstoneTest.IsFalse(editor.ShowMargins);
		AssertMarginsHidden(editor);

		editor.ShowLineNumbers = false;
		editor.ShowLineNumbers = true;

		CornerstoneTest.IsFalse(editor.ShowMargins);
		AssertMarginsHidden(editor);
	}

	[PresentationTestMethod]
	public void ShowLineNumbersFalseHidesFoldingMargin()
	{
		var editor = CreateEditor();
		editor.ShowLineNumbers = false;

		Apply(editor);

		CornerstoneTest.IsFalse(editor.ShowMargins);
		AssertMarginsHidden(editor);

		editor.ShowLineNumbers = true;

		CornerstoneTest.IsTrue(editor.ShowMargins);
		CornerstoneTest.IsTrue(FoldingMarginOf(editor).IsVisible);
	}

	[PresentationTestMethod]
	public void ShowMarginsTrueKeepsFoldingMarginWithoutLineNumbers()
	{
		var editor = CreateEditor();
		editor.ShowLineNumbers = false;
		editor.ShowMargins = false;
		editor.ShowMargins = true;

		Apply(editor);

		CornerstoneTest.IsTrue(editor.ShowMargins);
		CornerstoneTest.IsFalse(LineNumberMarginOf(editor).IsVisible);
		CornerstoneTest.IsTrue(FoldingMarginOf(editor).IsVisible);
	}

	private static TextEditor CreateEditor()
	{
		return new TextEditor
		{
			Template = new FuncControlTemplate<TextEditor>((_, _) => new Border())
		};
	}

	private static void Apply(TextEditor editor)
	{
		editor.ApplyTemplate();
		Dispatcher.UIThread.RunJobs();
	}

	private static void AssertMarginsHidden(TextEditor editor)
	{
		foreach (var margin in editor.LeftMargins)
		{
			CornerstoneTest.IsFalse(margin.IsVisible);
		}
	}

	private static FoldingMargin<TextEditorViewModel> FoldingMarginOf(TextEditor editor)
	{
		foreach (var margin in editor.LeftMargins)
		{
			if (margin is FoldingMargin<TextEditorViewModel> folding)
			{
				return folding;
			}
		}

		CornerstoneTest.Fail("Folding margin was not created.");
		return null;
	}

	private static LineNumberMargin<TextEditorViewModel> LineNumberMarginOf(TextEditor editor)
	{
		foreach (var margin in editor.LeftMargins)
		{
			if (margin is LineNumberMargin<TextEditorViewModel> lineNumbers)
			{
				return lineNumbers;
			}
		}

		CornerstoneTest.Fail("Line number margin was not created.");
		return null;
	}

	#endregion
}
