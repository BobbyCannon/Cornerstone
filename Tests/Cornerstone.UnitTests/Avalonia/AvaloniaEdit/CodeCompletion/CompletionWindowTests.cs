#region References

using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Cornerstone.Avalonia.AvaloniaEdit;
using Cornerstone.Avalonia.AvaloniaEdit.CodeCompletion;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.AvaloniaEdit.CodeCompletion;

[TestClass]
public class CompletionWindowTests : CornerstoneUnitTest
{
	#region Methods

	[AvaloniaTest]
	public void Insertion()
	{
		UnitTestApplication.InitializeStyles();

		var editor = CreateEditor();
		editor.Text = "cd \\";
		editor.TextArea.Caret.Offset = editor.Document.TextLength;

		var window = new CompletionWindow(editor.TextArea, "\\", null, [
			new CompletionData { CompletionText = "'C:\\Program Files'", DisplayText = "C:\\Program Files" }
		]);

		var item = window.CompletionList.Suggestions[0];
		window.CompleteRequest(item);

		AreEqual("cd 'C:\\Program Files'", editor.Text);
	}
	
	[AvaloniaTest]
	public void GetSegment()
	{
		UnitTestApplication.InitializeStyles();

		var editor = CreateEditor();
		editor.Text = "cd 'C:\\Program Files\\Win'";
		editor.TextArea.Caret.Offset = editor.Document.TextLength - 1;

		var window = new CompletionWindow(editor.TextArea, "'C:\\Program Files\\Win", null, [
			new CompletionData { CompletionText = "'C:\\\\Program Files\\\\Windows Defender'", DisplayText = "Windows Defender" }
		]);

		var item = window.CompletionList.Suggestions[0];
		var actual = window.GetSegment(item);

		AreEqual(3, actual?.StartIndex);
	}

	private static TextEditorControl CreateEditor()
	{
		var textEditor = new TextEditorControl();
		var window = new Window { Content = textEditor };
		window.Show();
		textEditor.ApplyTemplate();
		return textEditor;
	}

	#endregion
}