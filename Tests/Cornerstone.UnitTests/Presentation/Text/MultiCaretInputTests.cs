#region References

using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Controls.Text;
using Cornerstone.Presentation.Controls.Text.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation.Text;

[TestClass]
public class MultiCaretInputTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AltClickAddsCaret()
	{
		var model = Create("abcdefghij");
		model.HandlePointerPressed(4, KeyModifiers.Alt, 1);

		AreEqual(2, model.Carets.Count);
		AreEqual(4, model.Caret.Offset);
		AreEqual(0, model.Carets.All[1].Offset);
		IsTrue(model.Caret.Selection.IsSelectingUsingMouse);
	}

	[TestMethod]
	public void AltClickTogglesExtraCaret()
	{
		var model = Create("abcdefghij");
		model.HandlePointerPressed(4, KeyModifiers.Alt, 1);
		model.HandlePointerReleased();
		AreEqual(2, model.Carets.Count);
		AreEqual(4, model.Caret.Offset);

		model.HandlePointerPressed(4, KeyModifiers.Alt, 1);
		AreEqual(2, model.Carets.Count);
		AreEqual(4, model.Caret.Offset);
	}

	[TestMethod]
	public void CtrlAltClickMakesClickedCaretActive()
	{
		var model = Create("abcdefghij");
		model.HandlePointerPressed(4, KeyModifiers.Control | KeyModifiers.Alt, 1);

		AreEqual(2, model.Carets.Count);
		AreEqual(model.Caret, model.Carets.Primary);
		AreEqual(4, model.Caret.Offset);
		AreEqual(0, model.Carets.All[1].Offset);
		IsTrue(model.Caret.Selection.IsSelectingUsingMouse);

		model.HandlePointerReleased();
		model.HandlePointerPressed(7, KeyModifiers.Control | KeyModifiers.Alt, 1);

		AreEqual(3, model.Carets.Count);
		AreEqual(7, model.Caret.Offset);
		AreEqual(0, model.Carets.All[1].Offset);
		AreEqual(4, model.Carets.All[2].Offset);
	}

	[TestMethod]
	public void CtrlAltClickOnExtraBecomesActive()
	{
		var model = Create("abcdefghij");
		model.Carets.AddAt(4);
		model.HandlePointerPressed(4, KeyModifiers.Control | KeyModifiers.Alt, 1);

		AreEqual(2, model.Carets.Count);
		AreEqual(4, model.Caret.Offset);
		AreEqual(0, model.Carets.All[1].Offset);
	}

	[TestMethod]
	public void AltDragExtendsNewCaretSelection()
	{
		var model = Create("abcdefghij");
		model.HandlePointerPressed(2, KeyModifiers.Alt, 1);
		model.HandlePointerMoved(6);

		AreEqual(2, model.Caret.Selection.StartOffset);
		AreEqual(6, model.Caret.Selection.EndOffset);
		AreEqual(6, model.Caret.Offset);
		AreEqual(0, model.Carets.All[1].Offset);
	}

	[TestMethod]
	public void ArrowMovesEveryCaret()
	{
		var model = Create("abcdefghij");
		model.Caret.Move(1);
		model.Carets.AddAt(5);
		model.MoveAllCarets(CaretMoveDirection.CharRight, false);

		AreEqual(2, model.Caret.Offset);
		AreEqual(6, model.Carets.All[1].Offset);
	}

	[TestMethod]
	public void DoubleClickCollapsesThenSelectsWord()
	{
		var model = Create("hello world");
		model.Carets.AddAt(8);
		model.HandlePointerPressed(1, KeyModifiers.None, 2);

		AreEqual(1, model.Carets.Count);
		AreEqual(0, model.Caret.Selection.StartOffset);
		AreEqual(5, model.Caret.Selection.EndOffset);
	}

	[TestMethod]
	public void EscapeCollapsesCarets()
	{
		var model = Create("abcdefghij");
		model.Carets.AddAt(4);
		var args = new KeyEventArgs { Key = Key.Escape };
		model.ProcessKeyDownEvent(args);

		AreEqual(1, model.Carets.Count);
		IsTrue(args.Handled);
	}

	[TestMethod]
	public void EscapeDoesNotHandleWhenSingleCaret()
	{
		var model = Create("abcdefghij");
		var args = new KeyEventArgs { Key = Key.Escape };
		model.ProcessKeyDownEvent(args);

		AreEqual(1, model.Carets.Count);
		IsFalse(args.Handled);
	}

	[TestMethod]
	public void PlainClickCollapsesToPrimary()
	{
		var model = Create("abcdefghij");
		model.Carets.AddAt(4);
		model.HandlePointerPressed(7, KeyModifiers.None, 1);

		AreEqual(1, model.Carets.Count);
		AreEqual(7, model.Caret.Offset);
	}

	[TestMethod]
	public void ShiftAltDownAddsCaretOnNextLine()
	{
		var model = Create("hello\r\nworld");
		model.Caret.Move(1);
		var args = new KeyEventArgs
		{
			Key = Key.Down,
			KeyModifiers = KeyModifiers.Shift | KeyModifiers.Alt
		};
		model.ProcessKeyDownEvent(args);

		AreEqual(2, model.Carets.Count);
		AreEqual(8, model.Caret.Offset);
		AreEqual(1, model.Carets.All[1].Offset);
	}

	[TestMethod]
	public void ShiftAltDownRepeatedAddsACaretPerLine()
	{
		var model = Create("aa\r\nbb\r\ncc");
		model.Caret.Move(0);
		model.ProcessKeyDownEvent(new KeyEventArgs
		{
			Key = Key.Down,
			KeyModifiers = KeyModifiers.Shift | KeyModifiers.Alt
		});
		model.ProcessKeyDownEvent(new KeyEventArgs
		{
			Key = Key.Down,
			KeyModifiers = KeyModifiers.Shift | KeyModifiers.Alt
		});

		AreEqual(3, model.Carets.Count);
		AreEqual(8, model.Caret.Offset);
		AreEqual(0, model.Carets.All[1].Offset);
		AreEqual(4, model.Carets.All[2].Offset);
	}

	[TestMethod]
	public void ShiftClickDoesNotCollapseExtras()
	{
		var model = Create("abcdefghij");
		model.Carets.AddAt(4);
		model.Caret.Selection.Reset(0);
		model.HandlePointerPressed(3, KeyModifiers.Shift, 1);

		AreEqual(2, model.Carets.Count);
		AreEqual(0, model.Caret.Selection.StartOffset);
		AreEqual(3, model.Caret.Selection.EndOffset);
	}

	private static TextEditorViewModel Create(string text)
	{
		var model = new TextEditorViewModel();
		model.Load(text);
		return model;
	}

	#endregion
}