#region References

using Avalonia;
using Cornerstone.Avalonia.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Text;

[TestClass]
public class TextEditorViewModelTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CaretMoveLeftOverNewline()
	{
		var model = new TextEditorViewModel();

		//          012345 6 789012 3
		model.Load("Hello\r\nWorld\r\n");

		// "\r\n|" < move left should move two characters
		model.Caret.Move(7);
		AreEqual(7, model.Caret.Offset);
		model.Caret.MoveLeft();
		AreEqual(5, model.Caret.Offset);

		// "\r|\n" < move left should move one character
		model.Caret.Move(6);
		AreEqual(6, model.Caret.Offset);
		model.Caret.MoveLeft();
		AreEqual(5, model.Caret.Offset);
	}
	
	[TestMethod]
	public void DeleteBackwards()
	{
		var model = new TextEditorViewModel();
		// Delete should move 2 character
		model.Load("Hello\r\nWorld");
		model.Delete(7, false);
		AreEqual(5, model.Caret.Offset);
		// Delete should move 1 character
		model.Load("Hello\nWorld");
		model.Delete(6, false);
		AreEqual(5, model.Caret.Offset);
	}
	
	[TestMethod]
	public void DeleteForwards()
	{
		var model = new TextEditorViewModel();
		// Delete should move 2 character
		model.Load("Hello\r\nWorld");
		model.Delete(5, true);
		AreEqual(5, model.Caret.Offset);
		AreEqual("HelloWorld", model.Buffer.ToString());
		// Delete should remove 1 character
		model.Load("Hello\nWorld");
		model.Delete(5, true);
		AreEqual(5, model.Caret.Offset);
		AreEqual("HelloWorld", model.Buffer.ToString());
	}

	[TestMethod]
	public void CaretMoveRightOverNewline()
	{
		var model = new TextEditorViewModel();

		//                   012345 6 789012 3
		model.Load("Hello\r\nWorld\r\n");

		// "|\r\n" < move right should move two characters
		model.Caret.Move(5);
		AreEqual(5, model.Caret.Offset);
		model.Caret.MoveRight();
		AreEqual(7, model.Caret.Offset);

		// "\r|\n" < move right should move one character
		model.Caret.Move(6);
		AreEqual(6, model.Caret.Offset);
		model.Caret.MoveRight();
		AreEqual(7, model.Caret.Offset);
	}

	[TestMethod]
	public void DocumentLineLayouts()
	{
		var model = new TextEditorViewModel { WordWrap = true };
		model.ViewMetrics.CharacterHeight = 16;
		model.ViewMetrics.CharacterWidth = 12;

		//                          120       240       360
		//                           10        20        30        40        50     
		//                   01234567890123456789012345678901234567890123456789012 3
		//                   The quick brown fox jumped over the lazy dog's back.\r\n
		//                   The quick brown fox -19
		//                   jumped over the -35
		//                   lazy dog's back.\r\n -53
		model.Load("The quick brown fox jumped over the lazy dog's back.\r\n");
		var actual = model.Lines.Measure(new Size(240, 600), model.WordWrap);

		AreEqual(240, actual.Width);
		AreEqual(64, actual.Height);
		//AreEqual(2, model.Lines[0].VisualLineBreaks.Length);
		//AreEqual(19, model.Lines[0].VisualLineBreaks[0]);
		//AreEqual(35, model.Lines[0].VisualLineBreaks[1]);
	}

	[TestMethod]
	public void DocumentLineLayoutsForEmojis()
	{
		var model = new TextEditorViewModel();
		model.ViewMetrics.CharacterHeight = 16;
		model.ViewMetrics.CharacterWidth = 12;
		model.Load("😁💕😘👌😊😂🙌👍😒😍❤️🤣😎😉🎶💖😜");
		AreEqual(34, model.DocumentLength);

		var actual = model.Lines.Measure(new Size(500, 600), false);

		AreEqual(408, actual.Width);
		AreEqual(16, actual.Height);
	}

	[TestMethod]
	public void EmptyViewModel()
	{
		var model = new TextEditorViewModel();
		AreEqual(0, model.Caret.Offset);
		AreEqual(false, model.Caret.IsVisible);
		AreEqual(false, model.Caret.OverstrikeMode);
		AreEqual(0, model.DocumentLength);
		AreEqual(1, model.Lines.Count);
	}

	#endregion
}