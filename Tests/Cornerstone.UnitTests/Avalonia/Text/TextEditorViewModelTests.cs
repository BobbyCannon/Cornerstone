#region References

using Avalonia;
using Cornerstone.Avalonia.Text;
using Cornerstone.Avalonia.Text.Rendering;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Text;

public class TextEditorViewModelTests : CornerstoneUnitTest
{
	#region Methods

	[Test]
	public void CaretMoveLeftOverNewline()
	{
		var model = new TextEditorViewModel();

		//                   012345 6 789012 3
		model.Document.Load("Hello\r\nWorld\r\n");

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

	[Test]
	public void CaretMoveRightOverNewline()
	{
		var model = new TextEditorViewModel();

		//                   012345 6 789012 3
		model.Document.Load("Hello\r\nWorld\r\n");

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

	[Test]
	public void DocumentLineLayouts()
	{
		var model = new TextEditorViewModel { WordWrap = true };

		//                          120       240       360
		//                           10        20        30        40        50     
		//                   01234567890123456789012345678901234567890123456789012 3
		//                   The quick brown fox jumped over the lazy dog's back.\r\n
		//                   The quick brown fox -19
		//                   jumped over the -35
		//                   lazy dog's back.\r\n -53
		model.Document.Load("The quick brown fox jumped over the lazy dog's back.\r\n");
		var actual = model.Document.Lines.Measure(new Size(240, 600),
			model.WordWrap, new TextMetrics { CharacterHeight = 16, CharacterWidth = 12 }
		);

		AreEqual(240, actual.Width);
		AreEqual(64, actual.Height);
		//AreEqual(2, model.Document.Lines[0].VisualLineBreaks.Length);
		//AreEqual(19, model.Document.Lines[0].VisualLineBreaks[0]);
		//AreEqual(35, model.Document.Lines[0].VisualLineBreaks[1]);
	}

	[Test]
	public void DocumentLineLayoutsForEmojis()
	{
		var model = new TextEditorViewModel();
		model.Document.Load("😁💕😘👌😊😂🙌👍😒😍❤️🤣😎😉🎶💖😜");
		AreEqual(34, model.Document.Length);

		var actual = model.Document.Lines.Measure(new Size(500, 600), false,
			new TextMetrics { CharacterHeight = 16, CharacterWidth = 12 });

		AreEqual(408, actual.Width);
		AreEqual(16, actual.Height);
	}

	[Test]
	public void EmptyViewModel()
	{
		var model = new TextEditorViewModel();
		AreEqual(0, model.Caret.Offset);
		AreEqual(false, model.Caret.IsVisible);
		AreEqual(false, model.Caret.OverstrikeMode);
		AreEqual(0, model.Document.Length);
		AreEqual(1, model.Document.Lines.Count);
	}

	#endregion
}