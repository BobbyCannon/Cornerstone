#region References

using System.Text;
using Avalonia.Headless.NUnit;
using Cornerstone.Avalonia.TextEditor.Editing;
using Cornerstone.Testing;
using Cornerstone.Text.Document;
using Cornerstone.UnitTests;
using NUnit.Framework;
using NUnit.Framework.Legacy;

#endregion

namespace Cornerstone.IntegrationTests.Avalonia.TextEditor.Editing;

[TestFixture]
public class ChangeDocumentTests : CornerstoneUnitTest
{
	#region Methods

	[AvaloniaTest]
	public void CheckEventOrderOnDocumentChange()
	{
		var textArea = new TextArea();
		var newDocument = new TextEditorDocument();
		var b = new StringBuilder();
		textArea.TextView.DocumentChanged += delegate
		{
			b.Append("TextView.DocumentChanged;");
			ClassicAssert.AreSame(newDocument, textArea.TextView.Document);
			ClassicAssert.AreSame(newDocument, textArea.Document);
		};
		textArea.DocumentChanged += delegate
		{
			b.Append("TextArea.DocumentChanged;");
			ClassicAssert.AreSame(newDocument, textArea.TextView.Document);
			ClassicAssert.AreSame(newDocument, textArea.Document);
		};
		textArea.Document = newDocument;
		b.ToString().Dump();
		AreEqual("TextView.DocumentChanged;TextArea.DocumentChanged;", b.ToString());
	}

	[AvaloniaTest]
	public void ClearCaretAndSelectionOnDocumentChange()
	{
		var textArea = new TextArea();
		textArea.Document = new TextEditorDocument("1\n2\n3\n4th line");
		textArea.Caret.Offset = 6;
		textArea.Selection = Selection.Create(textArea, 3, 6);
		textArea.Document = new TextEditorDocument("1\n2nd");
		AreEqual(0, textArea.Caret.Offset);
		AreEqual(new TextLocation(1, 1), textArea.Caret.Location);
		IsTrue(textArea.Selection.IsEmpty);
	}

	[AvaloniaTest]
	public void SetDocumentToNull()
	{
		var textArea = new TextArea();
		textArea.Document = new TextEditorDocument("1\n2\n3\n4th line");
		textArea.Caret.Offset = 6;
		textArea.Selection = Selection.Create(textArea, 3, 6);
		textArea.Document = null;
		AreEqual(0, textArea.Caret.Offset);
		AreEqual(new TextLocation(1, 1), textArea.Caret.Location);
		IsTrue(textArea.Selection.IsEmpty);
	}

	#endregion
}