#region References

using Cornerstone.Presentation.Controls.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation.Text;

[TestClass]
public class MultiCaretClipboardTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CanCopyIsTrueWithMultipleCarets()
	{
		var model = Create("aa\r\nbb");
		IsFalse(model.Clipboard.CanCopy());
		model.Carets.AddAt(4);
		IsTrue(model.Clipboard.CanCopy());
	}

	[TestMethod]
	public void CopyJoinsSelectionsInDocumentOrder()
	{
		var model = Create("abcdef");
		model.Caret.Selection.Update(0, 2);
		model.Caret.Move(2);
		var extra = model.Carets.AddAt(4);
		extra.Selection.Update(4, 6);
		extra.Move(6);

		AreEqual("ab\r\nef", model.Clipboard.GetCopyText());
	}

	[TestMethod]
	public void CopyUsesWholeLineWhenCaretHasNoSelection()
	{
		var model = Create("aa\r\nbb");
		model.Caret.Move(0);
		model.Carets.AddAt(4);

		var copy = model.Clipboard.GetCopyText();
		AreEqual("aa\r\n\r\nbb", copy);
	}

	[TestMethod]
	public void CutRemovesEveryPayload()
	{
		var model = Create("abcdef");
		model.Caret.Selection.Update(0, 2);
		model.Caret.Move(2);
		var extra = model.Carets.AddAt(4);
		extra.Selection.Update(4, 6);
		extra.Move(6);

		AreEqual("ab\r\nef", model.Clipboard.GetCopyText());
		model.DeleteCopyPayloads();
		AreEqual("cd", model.ToString());
		AreEqual(1, model.Carets.Count);
	}

	[TestMethod]
	public void PasteRepeatsFullTextWhenLineCountDoesNotMatch()
	{
		var model = Create("aa\r\nbb");
		model.Caret.Move(2);
		model.Carets.AddAt(6);
		model.ProcessPaste("Z");

		AreEqual("aaZ\r\nbbZ", model.ToString());
	}

	[TestMethod]
	public void PasteSplitsWhenLineCountMatchesCaretCount()
	{
		var model = Create("aa\r\nbb");
		model.Caret.Move(2);
		model.Carets.AddAt(6);
		model.ProcessPaste("X\r\nY");

		AreEqual("aaX\r\nbbY", model.ToString());
		AreEqual(3, model.Caret.Offset);
		AreEqual(8, model.Carets.All[1].Offset);
	}

	private static TextEditorViewModel Create(string text)
	{
		var model = new TextEditorViewModel();
		model.Load(text);
		return model;
	}

	#endregion
}