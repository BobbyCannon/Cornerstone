#region References

using System;
using System.Collections.Generic;
using Cornerstone.Collections;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Controls.Text;
using Cornerstone.Presentation.Controls.Text.Completion;
using Cornerstone.Presentation.Controls.Text.Input;
using Cornerstone.Presentation.Controls.Text.Rendering;
using Cornerstone.Text.Formatting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation.Text;

[TestClass]
public class TextEditorViewModelTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AppendReadOnlySpanCoalescesIntoDocument()
	{
		var model = new TextEditorViewModel();
		model.Append("Hello");
		model.Append(" ".AsSpan());
		model.Append("World".AsSpan());

		AreEqual("Hello World", model.Buffer.ToString());
		AreEqual(11, model.DocumentLength);
	}

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
	public void ClampOffsetLimitsToExtent()
	{
		var metrics = new ViewMetrics { Offset = new Vector(50, 500) };
		var clamped = metrics.ClampOffset(new Size(100, 200), new Size(80, 50));

		AreEqual(new Vector(20, 150), clamped);
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
	public void DocumentChangedListenerFaultDoesNotDropEdit()
	{
		var model = new TextEditorViewModel();
		model.DocumentChanged += (_, _) => throw new InvalidOperationException();
		model.Insert(0, "Hi");
		AreEqual("Hi", model.Buffer.ToString());
		AreEqual(2, model.DocumentLength);
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
	public void DuplicateDoesNothingWhenReadOnly()
	{
		var model = new TextEditorViewModel();
		model.Load("Hello");
		model.IsReadOnly = true;
		model.Caret.Move(2);

		model.Duplicate();

		AreEqual("Hello", model.Buffer.ToString());
		AreEqual(2, model.Caret.Offset);
	}

	[TestMethod]
	public void DuplicateLastLineWithoutNewlineCreatesNewLine()
	{
		var model = new TextEditorViewModel();
		model.Load("Hello");
		model.Caret.Move(2);

		model.Duplicate();

		AreEqual("Hello\r\nHello", model.Buffer.ToString());
		AreEqual(9, model.Caret.Offset);
		AreEqual(0, model.Caret.Selection.Length);
	}

	[TestMethod]
	public void DuplicateLineInsertsLineBelowAndMovesCaret()
	{
		var model = new TextEditorViewModel();
		model.Load("Hello\r\nWorld\r\n");
		model.Caret.Move(2);

		model.Duplicate();

		AreEqual("Hello\r\nHello\r\nWorld\r\n", model.Buffer.ToString());
		AreEqual(9, model.Caret.Offset);
		AreEqual(0, model.Caret.Selection.Length);
	}

	[TestMethod]
	public void DuplicateSelectionInsertsCopyAfterSelection()
	{
		var model = new TextEditorViewModel();
		model.Load("Hello World");
		model.Caret.Move(5);
		model.Caret.Selection.Update(0, 5);

		model.Duplicate();

		AreEqual("HelloHello World", model.Buffer.ToString());
		AreEqual(5, model.Caret.Selection.StartOffset);
		AreEqual(10, model.Caret.Selection.EndOffset);
		AreEqual(10, model.Caret.Offset);
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

	[TestMethod]
	public void FormatDocumentDoesNotUseLoad()
	{
		var model = new TextEditorViewModel();
		model.ConfigureForFileType("json");
		model.Load("x");
		model.Insert(1, "y");
		IsTrue(model.UndoManager.CanUndo());
		model.Load("{\"a\":1}");
		IsFalse(model.UndoManager.CanUndo());
		IsTrue(model.FormatDocument(new JsonFormatOptions { NewLine = "\n" }));
		IsTrue(model.UndoManager.CanUndo());
	}

	[TestMethod]
	public void FormatDocumentNoopsWhenAlreadyFormatted()
	{
		var model = new TextEditorViewModel();
		model.ConfigureForFileType("json");
		model.Load("{\"a\":1}");
		var options = new JsonFormatOptions { NewLine = "\n" };

		IsTrue(model.FormatDocument(options));
		IsFalse(model.FormatDocument(options));
		model.UndoManager.Undo();
		AreEqual("{\"a\":1}", model.Buffer.ToString());
	}

	[TestMethod]
	public void FormatDocumentPrettyPrintsJsonAsOneUndo()
	{
		var model = new TextEditorViewModel();
		model.ConfigureForFileType("json");
		model.Load("{\"a\":1}");
		var options = new JsonFormatOptions { NewLine = "\n" };

		IsTrue(model.FormatDocument(options));
		AreEqual("{\n\t\"a\": 1\n}", model.Buffer.ToString());
		IsTrue(model.UndoManager.CanUndo());

		model.UndoManager.Undo();
		AreEqual("{\"a\":1}", model.Buffer.ToString());
	}

	[TestMethod]
	public void FormatDocumentRestoresWhenInsertCannotApply()
	{
		var model = new TextEditorViewModel();
		model.ConfigureForFileType("json");
		model.Load("{\"a\":1}");
		model.ReadOnlySectionProvider = new RejectAllEditsProvider();
		IsFalse(model.FormatDocument(new JsonFormatOptions { NewLine = "\n" }));
		AreEqual("{\"a\":1}", model.Buffer.ToString());
	}

	[TestMethod]
	public void IndentOnEmptyDocumentDoesNotThrow()
	{
		var model = new TextEditorViewModel();
		model.Indent();
		model.Unindent();
	}

	[TestMethod]
	public void InsertAndRemoveAtIgnoreOutOfRangeOffsets()
	{
		var model = new TextEditorViewModel();
		model.Load("Hello");

		model.Insert(-1, "x");
		model.Insert(6, "x");
		model.Insert(2, null);
		model.Insert(2, string.Empty);
		model.RemoveAt(-1, 1);
		model.RemoveAt(5, 1);
		model.RemoveAt(0, 0);
		model.RemoveAt(4, 50);

		AreEqual("Hell", model.Buffer.ToString());
		AreEqual(4, model.DocumentLength);
	}

	[TestMethod]
	public void InsertUnrestrictedIgnoresOutOfRangeOffsets()
	{
		var model = new TextEditorViewModel();
		model.Load("Hi");
		model.InsertUnrestricted(-1, "x", false);
		model.InsertUnrestricted(3, "x", false);
		model.InsertUnrestricted(2, "!", false);
		AreEqual("Hi!", model.Buffer.ToString());
	}

	[TestMethod]
	public void LoadDoesNotClearViewMetricsOffset()
	{
		var model = new TextEditorViewModel();
		model.ViewMetrics.Offset = new Vector(8, 40);

		model.Load("line one\r\nline two\r\nline three\r\n");

		AreEqual(new Vector(8, 40), model.ViewMetrics.Offset);
	}

	[TestMethod]
	public void LoadNullIsEmptyDocument()
	{
		var model = new TextEditorViewModel();
		model.Load("Hi");
		model.Load(null);
		AreEqual(string.Empty, model.Buffer.ToString());
		AreEqual(0, model.DocumentLength);
	}

	[TestMethod]
	public void RequestFocusRaisesFocusRequested()
	{
		var model = new TextEditorViewModel();
		var raised = false;
		model.FocusRequested += (_, _) => raised = true;
		model.RequestFocus();
		IsTrue(raised);
	}

	[TestMethod]
	public void SelectVisualLineRangePlacesCaretAtFirstLineStartWhenDraggingUp()
	{
		var model = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };
		model.Load("abc\r\ndef\r\nghi");
		model.Lines.Measure(new Size(500, 200), false);

		var first = model.Lines[0];
		var last = model.Lines[2];
		IsTrue(model.SelectVisualLineRange(last.StartOffset, last.EndOffset, first.VisualLayout.Y + 1));
		AreEqual(first.StartOffset, model.Caret.Selection.StartOffset);
		AreEqual(last.EndOffset, model.Caret.Selection.EndOffset);
		AreEqual(first.StartOffset, model.Caret.Offset);
	}

	[TestMethod]
	public void SelectVisualLineRangePlacesCaretAtLastLineEndWhenDraggingDown()
	{
		var model = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };
		model.Load("abc\r\ndef\r\nghi");
		model.Lines.Measure(new Size(500, 200), false);

		var first = model.Lines[0];
		var last = model.Lines[2];
		IsTrue(model.SelectVisualLineRange(first.StartOffset, first.EndOffset, last.VisualLayout.Y + 1));
		AreEqual(first.StartOffset, model.Caret.Selection.StartOffset);
		AreEqual(last.EndOffset, model.Caret.Selection.EndOffset);
		AreEqual(last.EndOffset, model.Caret.Offset);
	}

	[TestMethod]
	public void SelectVisualLineSelectsEmptyLineWithNewline()
	{
		var model = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };
		model.Load("a\r\n\r\nb");
		model.Lines.Measure(new Size(500, 200), false);

		AreEqual(3, model.Lines.Count);
		var empty = model.Lines[1];
		IsTrue(empty.EndOffset > empty.StartOffset);

		IsTrue(model.SelectVisualLine(empty.VisualLayout.Y + 1));
		AreEqual(empty.StartOffset, model.Caret.Selection.StartOffset);
		AreEqual(empty.EndOffset, model.Caret.Selection.EndOffset);
		AreEqual(empty.EndOffset, model.Caret.Offset);
		IsTrue(model.Caret.Selection.Length > 0);
	}

	[TestMethod]
	public void SelectVisualLineSelectsUnwrappedLine()
	{
		var model = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };
		model.Load("abc\r\ndef");
		model.Lines.Measure(new Size(500, 200), false);

		IsTrue(model.SelectVisualLine(0));
		AreEqual(0, model.Caret.Selection.StartOffset);
		AreEqual(model.Lines[0].EndOffset, model.Caret.Selection.EndOffset);
		AreEqual(model.Lines[0].EndOffset, model.Caret.Offset);
		IsFalse(model.Caret.Selection.IsSelectingUsingMouse);

		IsTrue(model.SelectVisualLine(0));
		AreEqual(0, model.Caret.Selection.StartOffset);
		AreEqual(model.Lines[0].EndOffset, model.Caret.Selection.EndOffset);
	}

	[TestMethod]
	public void SelectVisualLineSelectsWrappedSubline()
	{
		var model = new TextEditorViewModel { ViewMetrics = { CharacterHeight = 20, CharacterWidth = 10 } };
		model.Load("Hello World");
		model.Lines.Measure(new Size(50, 200), true);

		IsTrue(model.SelectVisualLine(0));
		AreEqual(0, model.Caret.Selection.StartOffset);
		AreEqual(5, model.Caret.Selection.EndOffset);
		AreEqual(5, model.Caret.Offset);

		IsTrue(model.SelectVisualLine(20));
		AreEqual(5, model.Caret.Selection.StartOffset);
		AreEqual(10, model.Caret.Selection.EndOffset);
		AreEqual(10, model.Caret.Offset);

		IsTrue(model.SelectVisualLine(20));
		AreEqual(5, model.Caret.Selection.StartOffset);
		AreEqual(10, model.Caret.Selection.EndOffset);
	}

	[TestMethod]
	public void UndoCompletionApplyLeavesCaretInDocument()
	{
		var model = new TextEditorViewModel();
		model.Load("Get-");
		model.Caret.Move(4);
		model.CompletionManager.Source = new PrefixReplaceCompletionSource();
		model.CompletionManager.RequestCompletions();
		IsTrue(model.CompletionManager.IsOpen);

		model.CompletionManager.ApplySelected();
		AreEqual("Get-Process", model.ToString());
		AreEqual(11, model.Caret.Offset);

		model.UndoManager.Undo();
		IsTrue(model.Caret.Offset >= 0);
		IsTrue(model.Caret.Offset <= model.DocumentLength);
		model.Delete(model.Caret.Offset, false);
		IsTrue(model.Caret.Offset >= 0);
		IsTrue(model.Caret.Offset <= model.DocumentLength);
	}

	[TestMethod]
	public void UndoInsertClampsCaretThenBackspaceIsSafe()
	{
		var model = new TextEditorViewModel();
		model.Load("Get-");
		model.Caret.Move(4);
		model.Insert(4, "Item");
		model.Caret.Move(8);
		AreEqual("Get-Item", model.ToString());

		model.UndoManager.Undo();
		AreEqual("Get-", model.ToString());
		IsTrue(model.Caret.Offset >= 0);
		IsTrue(model.Caret.Offset <= model.DocumentLength);
		AreEqual(4, model.Caret.Offset);

		model.Delete(model.Caret.Offset, false);
		AreEqual("Get", model.ToString());
		AreEqual(3, model.Caret.Offset);
	}

	#endregion

	#region Classes

	private sealed class PrefixReplaceCompletionSource : ICompletionSource
	{
		#region Properties

		public bool QueryOnBackgroundThread => false;

		#endregion

		#region Methods

		public bool ShouldTrigger(Key key, KeyModifiers modifiers, out bool silent)
		{
			silent = false;
			return false;
		}

		public bool TryGetCompletions(
			CompletionQueryContext context,
			out IReadOnlyList<CompletionItem> items,
			out int replaceStart,
			out int replaceLength)
		{
			items =
			[
				new CompletionItem("Get-Process", "Get-Process"),
				new CompletionItem("Get-Item", "Get-Item")
			];
			replaceStart = 0;
			replaceLength = context.CaretOffset;
			return true;
		}

		#endregion
	}

	private sealed class RejectAllEditsProvider : IReadOnlySectionProvider
	{
		#region Methods

		public bool CanModify(int offset)
		{
			return false;
		}

		public IEnumerable<IRange> GetDeletableSegments(IRange range)
		{
			return [];
		}

		#endregion
	}

	#endregion
}