#region References

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Harfbuzz;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Platform;
using Cornerstone.Presentation.Input.TextInput;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class TextBoxTests : ScopedTestBase
{
	#region Properties

	public static TestRows<Type> ExpectedClipboardExceptions
	{
		get
		{
			var data = new TestRows<Type>();
			data.Add(typeof(TimeoutException));
			data.Add(typeof(OperationCanceledException));
			data.Add(typeof(UnauthorizedAccessException));
			data.Add(typeof(COMException));
			return data;
		}
	}

	private static TestServices FocusServices =>
		TestServices.MockThreadingInterface.With(
			keyboardDevice: () => new KeyboardDevice(),
			keyboardNavigation: () => new KeyboardNavigationHandler(),
			inputManager: new InputManager(),
			standardCursorFactory: new StubCursorFactory(),
			textShaperImpl: new HarfBuzzTextShaper(),
			fontManagerImpl: new TestFontManager());

	private static TestServices Services =>
		TestServices.MockThreadingInterface.With(
			standardCursorFactory: new StubCursorFactory(),
			renderInterface: new HeadlessPlatformRenderInterface(),
			textShaperImpl: new HarfBuzzTextShaper(),
			fontManagerImpl: new TestFontManager(),
			assetLoader: new StandardAssetLoader());

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void BackspaceShouldDeleteCRLFNewlineCharacterAtOnce()
	{
		using var _ = UnitTestApplication.Start(Services);
		var target = new TextBox
		{
			Template = CreateTemplate(),
			Text = "First\r\nSecond",
			CaretIndex = 7
		};
		target.ApplyTemplate();

		// (First\r\nSecond)
		RaiseKeyEvent(target, Key.Back, KeyModifiers.None);

		// (FirstSecond)

		CornerstoneTest.AreEqual("FirstSecond", target.Text);
	}

	[PresentationTestMethod]
	public void BackspaceShouldDeleteLastCharacterInLineAndKeepCaretOnSameLine()
	{
		using var _ = UnitTestApplication.Start(Services);

		var textBox = new TextBox
		{
			Template = CreateTemplate(),
			Text = "a\nb",
			CaretIndex = 3
		};
		textBox.ApplyTemplate();

		var topLevel = new TestTopLevel(CreateMockTopLevelImpl())
		{
			Template = CreateTopLevelTemplate(),
			Content = textBox
		};
		topLevel.ApplyTemplate();
		topLevel.LayoutManager.ExecuteInitialLayoutPass();

		var textPresenter = textBox.FindDescendantOfType<TextPresenter>();
		CornerstoneTest.IsNotNull(textPresenter);

		var oldCaretY = textPresenter.GetCursorRectangle().Top;
		CornerstoneTest.AreNotEqual(0, oldCaretY);

		RaiseKeyEvent(textBox, Key.Back, KeyModifiers.None);

		CornerstoneTest.AreEqual("a\n", textBox.Text);
		CornerstoneTest.AreEqual(2, textBox.CaretIndex);
		CornerstoneTest.AreEqual(2, textPresenter.CaretIndex);

		var caretY = textPresenter.GetCursorRectangle().Top;
		CornerstoneTest.AreEqual(oldCaretY, caretY);
	}

	[PresentationTestMethod]
	public void BindingSourceChangeClearsUndoHistory()
	{
		using (UnitTestApplication.Start(Services))
		{
			var source = new Class1 { Bar = "initial" };
			var textBox = new TextBox
			{
				Template = CreateTemplate(),
				DataContext = source
			};

			textBox.Bind(TextBox.TextProperty, new Binding(nameof(Class1.Bar))
			{
				Mode = BindingMode.TwoWay
			});
			textBox.Measure(Size.Infinity);
			textBox.CaretIndex = textBox.Text!.Length;

			RaiseTextEvent(textBox, " edit");
			RaiseKeyEvent(textBox, Key.Space, KeyModifiers.None);
			RaiseTextEvent(textBox, " more");

			CornerstoneTest.AreEqual("initial edit more", source.Bar);
			CornerstoneTest.IsTrue(textBox.CanUndo);

			source.Bar = "replacement";

			CornerstoneTest.AreEqual("replacement", textBox.Text);
			CornerstoneTest.IsFalse(textBox.CanUndo);
			CornerstoneTest.IsFalse(textBox.CanRedo);
		}
	}

	[PresentationTestMethod]
	public void CanUndoCanRedoIsFalseWhenInitialized()
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox
			{
				Template = CreateTemplate(),
				Text = "New Text"
			};

			tb.Measure(Size.Infinity);

			CornerstoneTest.IsFalse(tb.CanUndo);
			CornerstoneTest.IsFalse(tb.CanRedo);
		}
	}

	[PresentationTestMethod]
	public void CanUndoCanRedoandProgrammaticUndoRedoWorks()
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox
			{
				Template = CreateTemplate()
			};

			tb.Measure(Size.Infinity);

			// See GH #6024 for a bit more insight on when Undo/Redo snapshots are taken:
			// - Every 'Space', but only when space is handled in OnKeyDown - Spaces in TextInput event won't work
			// - Every 7 chars in a long word
			RaiseTextEvent(tb, "ABC");
			RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
			RaiseTextEvent(tb, "DEF");
			RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
			RaiseTextEvent(tb, "123");

			// NOTE: the spaces won't actually add spaces b/c they're sent only as key events and not Text events
			//       so our final text is without spaces
			CornerstoneTest.AreEqual("ABCDEF123", tb.Text);

			CornerstoneTest.IsTrue(tb.CanUndo);

			tb.Undo();

			// Undo will take us back one step
			CornerstoneTest.AreEqual("ABCDEF", tb.Text);

			CornerstoneTest.IsTrue(tb.CanRedo);

			tb.Redo();

			// Redo should restore us
			CornerstoneTest.AreEqual("ABCDEF123", tb.Text);
		}
	}

	[PresentationTestMethod]
	public void CaretIndexCanMovedToPositionAfterTheEndOfTextWithArrowKey()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = "1234"
			};

			target.ApplyTemplate();

			target.Measure(Size.Infinity);

			target.CaretIndex = 3;
			RaiseKeyEvent(target, Key.Right, 0);

			CornerstoneTest.AreEqual(4, target.CaretIndex);
		}
	}

	[PresentationTestMethod]
	public void ClipboardOperationsDoNotSwallowUnexpectedExceptions()
	{
		using var app = UnitTestApplication.Start(Services);

		var target = CreateTextBoxInTopLevel(new ThrowingClipboardImplStub(typeof(InvalidOperationException)));

		CornerstoneTest.IsType<InvalidOperationException>(RunAndCaptureUnhandledException(target.Cut));
		CornerstoneTest.IsType<InvalidOperationException>(RunAndCaptureUnhandledException(target.Copy));
		CornerstoneTest.IsType<InvalidOperationException>(RunAndCaptureUnhandledException(target.Paste));
	}

	[PresentationTestMethod]
	public void CoerceCaretIndexDoesntCauseExceptionwithmalformedlineending()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = "0123456789\r"
			};
			target.CaretIndex = 11;

			CornerstoneTest.IsTrue(true);
		}
	}

	[PresentationTestMethod]
	public void CommandStatesUpdateWhenReadOnlyAndPasswordCharChange()
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox
			{
				Template = CreateTemplate(),
				Text = "1234",
				SelectionStart = 1,
				SelectionEnd = 3
			};

			tb.Measure(Size.Infinity);

			CornerstoneTest.IsTrue(tb.CanCopy);
			CornerstoneTest.IsTrue(tb.CanCut);
			CornerstoneTest.IsTrue(tb.CanPaste);

			tb.IsReadOnly = true;

			CornerstoneTest.IsTrue(tb.CanCopy);
			CornerstoneTest.IsFalse(tb.CanCut);
			CornerstoneTest.IsFalse(tb.CanPaste);

			tb.PasswordChar = '*';

			CornerstoneTest.IsFalse(tb.CanCopy);
			CornerstoneTest.IsFalse(tb.CanCut);
			CornerstoneTest.IsFalse(tb.CanPaste);

			tb.IsReadOnly = false;

			CornerstoneTest.IsFalse(tb.CanCopy);
			CornerstoneTest.IsFalse(tb.CanCut);
			CornerstoneTest.IsTrue(tb.CanPaste);

			tb.PasswordChar = default;

			CornerstoneTest.IsTrue(tb.CanCopy);
			CornerstoneTest.IsTrue(tb.CanCut);
			CornerstoneTest.IsTrue(tb.CanPaste);
		}
	}

	[PresentationTestMethod]
	public void CommandStatesUpdateWhenRevealPasswordChanges()
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox
			{
				Template = CreateTemplate(),
				Text = "1234",
				PasswordChar = '*',
				SelectionStart = 1,
				SelectionEnd = 3
			};

			tb.Measure(Size.Infinity);

			CornerstoneTest.IsFalse(tb.CanCopy);
			CornerstoneTest.IsFalse(tb.CanCut);
			CornerstoneTest.IsTrue(tb.CanPaste);

			tb.RevealPassword = true;

			CornerstoneTest.IsTrue(tb.CanCopy);
			CornerstoneTest.IsTrue(tb.CanCut);
			CornerstoneTest.IsTrue(tb.CanPaste);

			tb.RevealPassword = false;

			CornerstoneTest.IsFalse(tb.CanCopy);
			CornerstoneTest.IsFalse(tb.CanCut);
			CornerstoneTest.IsTrue(tb.CanPaste);
		}
	}

	[PresentationTestMethod]
	public void ControlBackspaceShouldRemoveTheDoubleWhitespaceIfCaretIndexWasAtTheEndOfAWord()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = "First Second Third",
				SelectionStart = 12,
				SelectionEnd = 12
			};

			target.ApplyTemplate();

			// (First Second| Third)
			RaiseKeyEvent(target, Key.Back, KeyModifiers.Control);

			// (First| Third)

			CornerstoneTest.AreEqual("First Third", target.Text);
		}
	}

	[PresentationTestMethod]
	public void ControlBackspaceShouldRemoveTheWordBeforeTheCaretIfThereIsNoSelection()
	{
		using (UnitTestApplication.Start(Services))
		{
			var textBox = new TextBox
			{
				Template = CreateTemplate(),
				Text = "First Second Third Fourth",
				SelectionStart = 5,
				SelectionEnd = 5
			};

			textBox.ApplyTemplate();

			// (First| Second Third Fourth)
			RaiseKeyEvent(textBox, Key.Back, KeyModifiers.Control);
			CornerstoneTest.AreEqual(" Second Third Fourth", textBox.Text);

			// ( Second |Third Fourth)
			textBox.CaretIndex = 8;
			RaiseKeyEvent(textBox, Key.Back, KeyModifiers.Control);
			CornerstoneTest.AreEqual(" Third Fourth", textBox.Text);

			// ( Thi|rd Fourth)
			textBox.CaretIndex = 4;
			RaiseKeyEvent(textBox, Key.Back, KeyModifiers.Control);
			CornerstoneTest.AreEqual(" rd Fourth", textBox.Text);

			// ( rd F[ou]rth)
			textBox.SelectionStart = 5;
			textBox.SelectionEnd = 7;

			RaiseKeyEvent(textBox, Key.Back, KeyModifiers.Control);
			CornerstoneTest.AreEqual(" rd Frth", textBox.Text);

			// ( |rd Frth)
			textBox.CaretIndex = 1;
			RaiseKeyEvent(textBox, Key.Back, KeyModifiers.Control);
			CornerstoneTest.AreEqual("rd Frth", textBox.Text);
		}
	}

	[PresentationTestMethod]
	public void ControlBackspaceShouldSetCaretPositionToTheStartOfTheDeletion()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = "First Second Third",
				SelectionStart = 13,
				SelectionEnd = 13
			};

			target.CaretIndex = 10;
			target.ApplyTemplate();

			// (First Second |Third)
			RaiseKeyEvent(target, Key.Back, KeyModifiers.Control);

			// (First |Third)

			CornerstoneTest.AreEqual(6, target.CaretIndex);
		}
	}

	[PresentationTestMethod]
	public void ControlBackspaceUndoShouldReturnCaretPosition()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = "First Second Third",
				SelectionStart = 9,
				SelectionEnd = 9
			};

			target.ApplyTemplate();

			// (First Second| Third)
			RaiseKeyEvent(target, Key.Back, KeyModifiers.Control);

			// (First| Third)

			target.Undo();

			// (First Second| Third)

			CornerstoneTest.AreEqual(9, target.CaretIndex);
		}
	}

	[PresentationTestMethod]
	public void ControlDeleteShouldRemoveTheWordAfterTheCaretIfThereIsNoSelection()
	{
		using (UnitTestApplication.Start(Services))
		{
			var textBox = new TextBox
			{
				Template = CreateTemplate(),
				Text = "First Second Third Fourth",
				CaretIndex = 19
			};

			textBox.ApplyTemplate();

			// (First Second Third |Fourth)
			RaiseKeyEvent(textBox, Key.Delete, KeyModifiers.Control);
			CornerstoneTest.AreEqual("First Second Third ", textBox.Text);

			// (First Second |Third )
			textBox.CaretIndex = 13;
			RaiseKeyEvent(textBox, Key.Delete, KeyModifiers.Control);
			CornerstoneTest.AreEqual("First Second ", textBox.Text);

			// (First Sec|ond )
			textBox.CaretIndex = 9;
			RaiseKeyEvent(textBox, Key.Delete, KeyModifiers.Control);
			CornerstoneTest.AreEqual("First Sec", textBox.Text);

			// (Fi[rs]t Sec )
			textBox.SelectionStart = 2;
			textBox.SelectionEnd = 4;

			RaiseKeyEvent(textBox, Key.Delete, KeyModifiers.Control);
			CornerstoneTest.AreEqual("Fit Sec", textBox.Text);

			// (Fit Sec| )
			textBox.Text += " ";
			textBox.CaretIndex = 7;
			RaiseKeyEvent(textBox, Key.Delete, KeyModifiers.Control);
			CornerstoneTest.AreEqual("Fit Sec", textBox.Text);
		}
	}

	[PresentationTestMethod]
	[TestData(nameof(ExpectedClipboardExceptions))]
	public void CopyDoesNotThrowWhenClipboardFails(Type exceptionType)
	{
		using var app = UnitTestApplication.Start(Services);

		var clipboardImpl = new ThrowingClipboardImplStub(exceptionType);
		var target = CreateTextBoxInTopLevel(clipboardImpl);
		var messages = new List<string>();

		using (TestLogSink.Start((_, _, _, messageTemplate, _) => messages.Add(messageTemplate)))
		{
			var unhandled = RunAndCaptureUnhandledException(target.Copy);

			CornerstoneTest.IsNull(unhandled);
		}

		CornerstoneTest.AreEqual(1, clipboardImpl.SetDataCount);
		CornerstoneTest.AreEqual("abcd", target.Text);
		CornerstoneTest.AreEqual(["Failed to write text to clipboard: {Error}"], messages);
	}

	[PresentationTestMethod]
	public void CutDeletesSelectionWhenClipboardSucceeds()
	{
		using var app = UnitTestApplication.Start(Services);

		var target = CreateTextBoxInTopLevel(null);

		var unhandled = RunAndCaptureUnhandledException(target.Cut);

		CornerstoneTest.IsNull(unhandled);
		CornerstoneTest.AreEqual("ad", target.Text);
	}

	[PresentationTestMethod]
	[TestData(nameof(ExpectedClipboardExceptions))]
	public void CutDoesNotDeleteSelectionWhenClipboardFails(Type exceptionType)
	{
		using var app = UnitTestApplication.Start(Services);

		var clipboardImpl = new ThrowingClipboardImplStub(exceptionType);
		var target = CreateTextBoxInTopLevel(clipboardImpl);
		var messages = new List<string>();

		using (TestLogSink.Start((_, _, _, messageTemplate, _) => messages.Add(messageTemplate)))
		{
			var unhandled = RunAndCaptureUnhandledException(target.Cut);

			CornerstoneTest.IsNull(unhandled);
		}

		CornerstoneTest.AreEqual(1, clipboardImpl.SetDataCount);
		CornerstoneTest.AreEqual("abcd", target.Text);
		CornerstoneTest.AreEqual(1, target.SelectionStart);
		CornerstoneTest.AreEqual(3, target.SelectionEnd);
		CornerstoneTest.AreEqual(["Failed to write text to clipboard: {Error}"], messages);
	}

	[PresentationTestMethod]
	public void DefaultBindingModeShouldBeTwoWay()
	{
		CornerstoneTest.AreEqual(BindingMode.TwoWay, TextBox.TextProperty.GetMetadata(typeof(TextBox)).DefaultBindingMode);
	}

	[PresentationTestMethod]
	public void EmptyTextBoxInitializesClipboardCommandStates()
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox();

			CornerstoneTest.IsFalse(tb.CanCopy);
			CornerstoneTest.IsFalse(tb.CanCut);
			CornerstoneTest.IsTrue(tb.CanPaste);
		}
	}

	[PresentationTestMethod]
	public void EnteringTextWithSelectedTextShouldFireSingleTextChangedNotification()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = "0123",
				AcceptsReturn = true,
				AcceptsTab = true,
				SelectionStart = 1,
				SelectionEnd = 3
			};

			var values = new List<string>();
			target.GetObservable(TextBox.TextProperty).Subscribe(x => values.Add(x));

			RaiseTextEvent(target, "A");

			CornerstoneTest.AreEqual(new[] { "0123", "0A3" }, values);
		}
	}

	[PresentationTestMethod]
	[DataRow(false, TextWrapping.NoWrap, ScrollBarVisibility.Hidden)]
	[DataRow(false, TextWrapping.Wrap, ScrollBarVisibility.Disabled)]
	[DataRow(true, TextWrapping.NoWrap, ScrollBarVisibility.Auto)]
	[DataRow(true, TextWrapping.Wrap, ScrollBarVisibility.Disabled)]
	public void HasCorrectHorizontalScrollBarVisibility(
		bool acceptsReturn,
		TextWrapping wrapping,
		ScrollBarVisibility expected)
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				AcceptsReturn = acceptsReturn,
				TextWrapping = wrapping
			};

			CornerstoneTest.AreEqual(expected, ScrollViewer.GetHorizontalScrollBarVisibility(target));
		}
	}

	[PresentationTestMethod]
	public void InputMethodClientSelectionSetterUsesDocumentOffsetsForMultilineText()
	{
		using var _ = UnitTestApplication.Start(Services);

		var textBox = new TextBox
		{
			Template = CreateTemplate(),
			Text = "one\ntwo",
			CaretIndex = 5
		};
		textBox.ApplyTemplate();

		var client = GetInputMethodClient(textBox);
		client.Selection = new TextSelection(0, 3);

		CornerstoneTest.AreEqual(0, textBox.SelectionStart);
		CornerstoneTest.AreEqual(3, textBox.SelectionEnd);
		CornerstoneTest.AreEqual("one", textBox.SelectedText);
	}

	[PresentationTestMethod]
	public void InputMethodClientSurroundingTextReturnsEmptyForEmptyLine()
	{
		using var _ = UnitTestApplication.Start(Services);

		var textBox = new TextBox
		{
			Template = CreateTemplate(),
			Text = "",
			CaretIndex = 0
		};
		textBox.ApplyTemplate();

		var eventArgs = new TextInputMethodClientRequestedEventArgs
		{
			RoutedEvent = InputElement.TextInputMethodClientRequestedEvent
		};
		textBox.RaiseEvent(eventArgs);

		var client = eventArgs.Client;
		CornerstoneTest.IsNotNull(client);
		CornerstoneTest.AreEqual(string.Empty, client.SurroundingText);
	}

	[PresentationTestMethod]
	public void InputMethodClientSurroundingTextUsesFullDocumentForMultilineText()
	{
		using var _ = UnitTestApplication.Start(Services);

		var textBox = new TextBox
		{
			Template = CreateTemplate(),
			Text = "one\ntwo",
			CaretIndex = 5
		};
		textBox.ApplyTemplate();

		var client = GetInputMethodClient(textBox);

		CornerstoneTest.AreEqual("one\ntwo", client.SurroundingText);
		CornerstoneTest.AreEqual(new TextSelection(5, 5), client.Selection);
	}

	[PresentationTestMethod]
	public void Preedit_Survives_Reentrant_Layout_Invalidation_From_CaretBoundsChanged()
	{
		using var _ = UnitTestApplication.Start(Services);

		var textBox = new TextBox
		{
			Template = CreateTemplate(),
			Text = "hello",
			CaretIndex = 5
		};

		var impl = CreateMockTopLevelImpl();
		var topLevel = new TestTopLevel(impl)
		{
			Template = CreateTopLevelTemplate(),
			Content = textBox
		};
		topLevel.ApplyTemplate();
		topLevel.LayoutManager.ExecuteInitialLayoutPass();

		var client = GetInputMethodClient(textBox);
		var presenter = textBox.FindDescendantOfType<TextPresenter>();
		CornerstoneTest.IsNotNull(presenter);

		// A CaretBoundsChanged subscriber may synchronously invalidate the text layout
		// (the IME candidate window or a caret-following overlay forcing a layout pass
		// does exactly that). The preedit update must still complete against the layout
		// it computed with instead of dereferencing the reentrantly cleared field.
		presenter.CaretBoundsChanged += (_, _) => presenter.HideCaret();

		client.SetPreeditText("πüïπéô", 2);

		CornerstoneTest.AreEqual("πüïπéô", presenter.PreeditText);
	}

	[PresentationTestMethod]
	public void InsertMultilineTextShouldAcceptExtraLinesWhenAcceptsReturnIsTrue()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				AcceptsReturn = true
			};

			RaiseTextEvent(target, $"123 {Environment.NewLine}456");

			CornerstoneTest.AreEqual($"123 {Environment.NewLine}456", target.Text);
		}
	}

	[PresentationTestMethod]
	public void InsertMultilineTextShouldDiscardExtraLinesWhenAcceptsReturnIsFalse()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				AcceptsReturn = false
			};

			RaiseTextEvent(target, $"123 {"\r"}456");

			CornerstoneTest.AreEqual("123 ", target.Text);

			target.Text = "";

			RaiseTextEvent(target, $"123 {"\r\n"}456");

			CornerstoneTest.AreEqual("123 ", target.Text);
		}
	}

	[PresentationTestMethod]
	[DataRow(Key.X, KeyModifiers.Control)]
	[DataRow(Key.Back, KeyModifiers.None)]
	[DataRow(Key.Delete, KeyModifiers.None)]
	[DataRow(Key.Tab, KeyModifiers.None)]
	[DataRow(Key.Enter, KeyModifiers.None)]
	public void KeysAllowUndo(Key key, KeyModifiers modifiers)
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = "0123",
				AcceptsReturn = true,
				AcceptsTab = true
			};

			var impl = CreateMockTopLevelImpl();
			var topLevel = new TestTopLevel(impl)
			{
				Template = CreateTopLevelTemplate()
			};
			topLevel.Content = target;
			topLevel.ApplyTemplate();
			topLevel.LayoutManager.ExecuteInitialLayoutPass();

			target.ApplyTemplate();
			target.SelectionStart = 1;
			target.SelectionEnd = 3;

			RaiseKeyEvent(target, key, modifiers);
			RaiseKeyEvent(target, Key.Z, KeyModifiers.Control); // undo
			CornerstoneTest.IsTrue(target.Text == "0123");
		}
	}

	[PresentationTestMethod]
	[DataRow(null, 1)]
	[DataRow("", 1)]
	[DataRow("Hello", 1)]
	[DataRow("Hello\r\nWorld", 2)]
	public void LineCountIsCorrect(string text, int lineCount)
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = text,
				AcceptsReturn = true
			};

			var impl = CreateMockTopLevelImpl();
			var topLevel = new TestTopLevel(impl)
			{
				Template = CreateTopLevelTemplate()
			};
			topLevel.Content = target;
			topLevel.ApplyTemplate();
			topLevel.LayoutManager.ExecuteInitialLayoutPass();

			target.ApplyTemplate();
			target.Measure(Size.Infinity);

			CornerstoneTest.AreEqual(lineCount, target.GetLineCount());
		}
	}

	[PresentationTestMethod]
	public void LineCountIsCorrectAfterTextChange()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = "Hello",
				AcceptsReturn = true
			};

			var impl = CreateMockTopLevelImpl();
			var topLevel = new TestTopLevel(impl)
			{
				Template = CreateTopLevelTemplate()
			};
			topLevel.Content = target;
			topLevel.ApplyTemplate();
			topLevel.LayoutManager.ExecuteInitialLayoutPass();

			target.ApplyTemplate();
			target.Measure(Size.Infinity);

			CornerstoneTest.AreEqual(1, target.GetLineCount());

			target.Text = "Hello\r\nWorld";

			CornerstoneTest.AreEqual(2, target.GetLineCount());
		}
	}

	[PresentationTestMethod]
	public void LosingFocusShouldNotResetSelection()
	{
		using (UnitTestApplication.Start(FocusServices))
		{
			var target1 = new TextBox
			{
				Template = CreateTemplate(),
				Text = "1234",
				ClearSelectionOnLostFocus = false
			};

			target1.ApplyTemplate();

			var target2 = new TextBox
			{
				Template = CreateTemplate()
			};

			target2.ApplyTemplate();

			var sp = new StackPanel();
			sp.Children.Add(target1);
			sp.Children.Add(target2);

			var root = new TestRoot { Child = sp };

			target1.SelectionStart = 0;
			target1.SelectionEnd = 4;

			target1.Focus();

			CornerstoneTest.IsTrue(target1.IsFocused);

			CornerstoneTest.AreEqual("1234", target1.SelectedText);

			target2.Focus();

			CornerstoneTest.AreEqual("1234", target1.SelectedText);
		}
	}

	[PresentationTestMethod]
	[DataRow("abc", "d", 3, 0, 0, false, "abc")]
	[DataRow("abc", "dd", 4, 3, 3, false, "abcd")]
	[DataRow("abc", "ddd", 3, 0, 2, true, "ddc")]
	[DataRow("abc", "dddd", 4, 1, 3, true, "addd")]
	[DataRow("abc", "ddddd", 5, 3, 3, true, "abcdd")]
	public async Task MaxLengthWorksProperly(
		string initalText,
		string textInput,
		int maxLength,
		int selectionStart,
		int selectionEnd,
		bool fromClipboard,
		string expected)
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = initalText,
				MaxLength = maxLength,
				SelectionStart = selectionStart,
				SelectionEnd = selectionEnd
			};

			var impl = CreateMockTopLevelImpl();
			var topLevel = new TestTopLevel(impl)
			{
				Template = CreateTopLevelTemplate()
			};
			topLevel.Content = target;
			topLevel.ApplyTemplate();
			topLevel.LayoutManager.ExecuteInitialLayoutPass();

			target.Measure(Size.Infinity);

			if (fromClipboard)
			{
				await topLevel.Clipboard!.SetTextAsync(textInput);

				RaiseKeyEvent(target, Key.V, KeyModifiers.Control);
				await topLevel.Clipboard!.ClearAsync();
			}
			else
			{
				RaiseTextEvent(target, textInput);
			}

			CornerstoneTest.AreEqual(expected, target.Text);
		}
	}

	[PresentationTestMethod]
	[DataRow(1)]
	[DataRow(2)]
	[DataRow(3)]
	public void MaxLinesSetsScrollViewerMaxHeight(int maxLines)
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				MaxLines = maxLines,

				// Define explicit whole number line height for predictable calculations
				LineHeight = 20
			};

			var impl = CreateMockTopLevelImpl();
			var topLevel = new TestTopLevel(impl)
			{
				Template = CreateTopLevelTemplate(),
				Content = target
			};
			topLevel.ApplyTemplate();
			topLevel.LayoutManager.ExecuteInitialLayoutPass();

			var textPresenter = target.FindDescendantOfType<TextPresenter>();
			CornerstoneTest.IsNotNull(textPresenter);
			CornerstoneTest.AreEqual("PART_TextPresenter", textPresenter.Name);
			CornerstoneTest.AreEqual(new Thickness(0), textPresenter.Margin); // Test assumes no margin on TextPresenter

			var scrollViewer = target.FindDescendantOfType<ScrollViewer>();
			CornerstoneTest.IsNotNull(scrollViewer);
			CornerstoneTest.AreEqual("PART_ScrollViewer", scrollViewer.Name);
			CornerstoneTest.AreEqual(maxLines * target.LineHeight, scrollViewer.MaxHeight);
		}
	}

	[PresentationTestMethod]
	[DataRow(1)]
	[DataRow(2)]
	[DataRow(3)]
	public void MaxLinesSetsScrollViewerMaxHeightWithTextPresenterMargin(int maxLines)
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				MaxLines = maxLines,

				// Define explicit whole number line height for predictable calculations
				LineHeight = 20
			};

			var impl = CreateMockTopLevelImpl();
			var topLevel = new TestTopLevel(impl)
			{
				Template = CreateTopLevelTemplate(),
				Content = target
			};
			topLevel.ApplyTemplate();
			topLevel.LayoutManager.ExecuteInitialLayoutPass();

			var textPresenter = target.FindDescendantOfType<TextPresenter>();
			CornerstoneTest.IsNotNull(textPresenter);
			CornerstoneTest.AreEqual("PART_TextPresenter", textPresenter.Name);
			var textPresenterMargin = new Thickness(0, 3);
			textPresenter.Margin = textPresenterMargin;

			target.InvalidateMeasure();
			target.Measure(Size.Infinity);

			var scrollViewer = target.FindDescendantOfType<ScrollViewer>();
			CornerstoneTest.IsNotNull(scrollViewer);
			CornerstoneTest.AreEqual("PART_ScrollViewer", scrollViewer.Name);
			CornerstoneTest.AreEqual((maxLines * target.LineHeight) + textPresenterMargin.Top + textPresenterMargin.Bottom, scrollViewer.MaxHeight);
		}
	}

	[PresentationTestMethod]
	[DataRow(1)]
	[DataRow(2)]
	[DataRow(3)]
	public void MinLinesSetsScrollViewerMinHeight(int minLines)
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				MinLines = minLines,

				// Define explicit whole number line height for predictable calculations
				LineHeight = 20
			};

			var impl = CreateMockTopLevelImpl();
			var topLevel = new TestTopLevel(impl)
			{
				Template = CreateTopLevelTemplate(),
				Content = target
			};
			topLevel.ApplyTemplate();
			topLevel.LayoutManager.ExecuteInitialLayoutPass();

			var textPresenter = target.FindDescendantOfType<TextPresenter>();
			CornerstoneTest.IsNotNull(textPresenter);
			CornerstoneTest.AreEqual("PART_TextPresenter", textPresenter.Name);
			CornerstoneTest.AreEqual(new Thickness(0), textPresenter.Margin); // Test assumes no margin on TextPresenter

			var scrollViewer = target.FindDescendantOfType<ScrollViewer>();
			CornerstoneTest.IsNotNull(scrollViewer);
			CornerstoneTest.AreEqual("PART_ScrollViewer", scrollViewer.Name);
			CornerstoneTest.AreEqual(minLines * target.LineHeight, scrollViewer.MinHeight);
		}
	}

	[PresentationTestMethod]
	[DataRow(1)]
	[DataRow(2)]
	[DataRow(3)]
	public void MinLinesSetsScrollViewerMinHeightWithTextPresenterMargin(int minLines)
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				MinLines = minLines,

				// Define explicit whole number line height for predictable calculations
				LineHeight = 20
			};

			var impl = CreateMockTopLevelImpl();
			var topLevel = new TestTopLevel(impl)
			{
				Template = CreateTopLevelTemplate(),
				Content = target
			};
			topLevel.ApplyTemplate();
			topLevel.LayoutManager.ExecuteInitialLayoutPass();

			var textPresenter = target.FindDescendantOfType<TextPresenter>();
			CornerstoneTest.IsNotNull(textPresenter);
			CornerstoneTest.AreEqual("PART_TextPresenter", textPresenter.Name);
			var textPresenterMargin = new Thickness(0, 3);
			textPresenter.Margin = textPresenterMargin;

			target.InvalidateMeasure();
			target.Measure(Size.Infinity);

			var scrollViewer = target.FindDescendantOfType<ScrollViewer>();
			CornerstoneTest.IsNotNull(scrollViewer);
			CornerstoneTest.AreEqual("PART_ScrollViewer", scrollViewer.Name);
			CornerstoneTest.AreEqual((minLines * target.LineHeight) + textPresenterMargin.Top + textPresenterMargin.Bottom, scrollViewer.MinHeight);
		}
	}

	[PresentationTestMethod]
	public void OpeningContextFlyoutDoesnotLoseSelection()
	{
		using (UnitTestApplication.Start(FocusServices))
		{
			var target1 = new TextBox
			{
				Template = CreateTemplate(),
				Text = "1234",
				ContextFlyout = new MenuFlyout
				{
					Items =
					{
						new MenuItem { Header = "Item 1" },
						new MenuItem { Header = "Item 2" },
						new MenuItem { Header = "Item 3" }
					}
				}
			};

			target1.ApplyTemplate();

			var root = new TestRoot { Child = target1 };

			target1.SelectionStart = 0;
			target1.SelectionEnd = 3;

			target1.Focus();
			CornerstoneTest.IsTrue(target1.IsFocused);

			target1.ContextFlyout.ShowAt(target1);

			CornerstoneTest.AreEqual("123", target1.SelectedText);
		}
	}

	[PresentationTestMethod]
	public void OpeningContextMenuDoesnotLoseSelection()
	{
		using (UnitTestApplication.Start(FocusServices))
		{
			var target1 = new TextBox
			{
				Template = CreateTemplate(),
				Text = "1234",
				ContextMenu = new TestContextMenu()
			};

			var target2 = new TextBox
			{
				Template = CreateTemplate(),
				Text = "5678"
			};

			var sp = new StackPanel();
			sp.Children.Add(target1);
			sp.Children.Add(target2);

			target1.ApplyTemplate();
			target2.ApplyTemplate();

			var root = new TestRoot { Child = sp };

			target1.SelectionStart = 0;
			target1.SelectionEnd = 3;

			target1.Focus();
			CornerstoneTest.IsFalse(target2.IsFocused);
			CornerstoneTest.IsTrue(target1.IsFocused);

			target2.Focus();

			CornerstoneTest.AreEqual("123", target1.SelectedText);
		}
	}

	[PresentationTestMethod]
	[TestData(nameof(ExpectedClipboardExceptions))]
	public void PasteDoesNotChangeTextWhenClipboardFails(Type exceptionType)
	{
		using var app = UnitTestApplication.Start(Services);

		var clipboardImpl = new ThrowingClipboardImplStub(exceptionType);
		var target = CreateTextBoxInTopLevel(clipboardImpl);
		var messages = new List<string>();

		using (TestLogSink.Start((_, _, _, messageTemplate, _) => messages.Add(messageTemplate)))
		{
			var unhandled = RunAndCaptureUnhandledException(target.Paste);

			CornerstoneTest.IsNull(unhandled);
		}

		CornerstoneTest.AreEqual(1, clipboardImpl.TryGetDataCount);
		CornerstoneTest.AreEqual("abcd", target.Text);
		CornerstoneTest.IsFalse(target.CanUndo);
		CornerstoneTest.AreEqual(["Failed to read text from clipboard: {Error}"], messages);
	}

	[PresentationTestMethod]
	public void PlaceholderForegroundCanBeSet()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				PlaceholderText = "Enter text",
				PlaceholderForeground = Brushes.Red
			};

			target.ApplyTemplate();

			CornerstoneTest.AreEqual(Brushes.Red, target.PlaceholderForeground);
		}
	}

	[PresentationTestMethod]
	public void PlaceholderForegroundCanBeSetToNull()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				PlaceholderText = "Enter text",
				PlaceholderForeground = Brushes.Blue
			};

			target.ApplyTemplate();

			target.PlaceholderForeground = null;

			CornerstoneTest.IsNull(target.PlaceholderForeground);
		}
	}

	[PresentationTestMethod]
	public void PlaceholderForegroundDefaultsToNull()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				PlaceholderText = "Enter text"
			};

			target.ApplyTemplate();

			CornerstoneTest.IsNull(target.PlaceholderForeground);
		}
	}

	[PresentationTestMethod]
	public void PressCtrlASelectAllNullText()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate()
			};

			RaiseKeyEvent(target, Key.A, KeyModifiers.Control);

			CornerstoneTest.AreEqual(0, target.SelectionStart);
			CornerstoneTest.AreEqual(0, target.SelectionEnd);
		}
	}

	[PresentationTestMethod]
	public void PressCtrlASelectAllText()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = "1234"
			};

			target.ApplyTemplate();

			RaiseKeyEvent(target, Key.A, KeyModifiers.Control);

			CornerstoneTest.AreEqual(0, target.SelectionStart);
			CornerstoneTest.AreEqual(4, target.SelectionEnd);
		}
	}

	[PresentationTestMethod]
	public void PressCtrlZWillNotModifyText()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = "1234"
			};

			RaiseKeyEvent(target, Key.Z, KeyModifiers.Control);

			CornerstoneTest.AreEqual("1234", target.Text);
		}
	}

	[PresentationTestMethod]
	public void PressEnterAddCustomNewline()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				AcceptsReturn = true,
				NewLine = "Test"
			};

			target.ApplyTemplate();

			RaiseKeyEvent(target, Key.Enter, 0);

			CornerstoneTest.AreEqual("Test", target.Text);
		}
	}

	[PresentationTestMethod]
	public void PressEnterAddDefaultNewline()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				AcceptsReturn = true
			};

			target.ApplyTemplate();

			RaiseKeyEvent(target, Key.Enter, 0);

			CornerstoneTest.AreEqual(Environment.NewLine, target.Text);
		}
	}

	[PresentationTestMethod]
	public void PressEnterDoesNotAcceptReturn()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				AcceptsReturn = false,
				Text = "1234"
			};

			target.ApplyTemplate();

			RaiseKeyEvent(target, Key.Enter, 0);

			CornerstoneTest.AreEqual("1234", target.Text);
		}
	}

	[PresentationTestMethod]
	public void ReadOnlyEditingHotkeysDoNotModifyTextOrUndoState()
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox
			{
				Template = CreateTemplate(),
				AcceptsReturn = true,
				AcceptsTab = true
			};

			tb.Measure(Size.Infinity);

			RaiseTextEvent(tb, "ABC");
			RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
			RaiseTextEvent(tb, "DEF");

			CornerstoneTest.AreEqual("ABCDEF", tb.Text);

			tb.Undo();

			CornerstoneTest.AreEqual("ABC", tb.Text);
			CornerstoneTest.IsTrue(tb.CanUndo);
			CornerstoneTest.IsTrue(tb.CanRedo);

			tb.IsReadOnly = true;
			tb.CaretIndex = tb.Text!.Length;
			tb.SelectionStart = 0;
			tb.SelectionEnd = tb.Text.Length;

			var originalText = tb.Text;
			var originalCaretIndex = tb.CaretIndex;
			var originalSelectionStart = tb.SelectionStart;
			var originalSelectionEnd = tb.SelectionEnd;
			var originalCanUndo = tb.CanUndo;
			var originalCanRedo = tb.CanRedo;

			var cutRaised = 0;
			var pasteRaised = 0;
			tb.CuttingToClipboard += (_, _) => cutRaised++;
			tb.PastingFromClipboard += (_, _) => pasteRaised++;

			AssertReadOnlyHotkeyLeavesStateUntouched(tb, Application.Current!.PlatformSettings!.HotkeyConfiguration.Cut, true);
			AssertReadOnlyHotkeyLeavesStateUntouched(tb, Application.Current.PlatformSettings.HotkeyConfiguration.Paste, true);
			AssertReadOnlyHotkeyLeavesStateUntouched(tb, Application.Current.PlatformSettings.HotkeyConfiguration.Undo, true);
			AssertReadOnlyHotkeyLeavesStateUntouched(tb, Application.Current.PlatformSettings.HotkeyConfiguration.Redo, true);
			AssertReadOnlyHotkeyLeavesStateUntouched(tb, Key.Back, KeyModifiers.None, true);
			AssertReadOnlyHotkeyLeavesStateUntouched(tb, Key.Back, KeyModifiers.Control, true);
			AssertReadOnlyHotkeyLeavesStateUntouched(tb, Key.Delete, KeyModifiers.None, true);
			AssertReadOnlyHotkeyLeavesStateUntouched(tb, Key.Delete, KeyModifiers.Control, true);
			AssertReadOnlyHotkeyLeavesStateUntouched(tb, Key.Enter, KeyModifiers.None, true);
			AssertReadOnlyHotkeyLeavesStateUntouched(tb, Key.Tab, KeyModifiers.None, true);
			AssertReadOnlyHotkeyLeavesStateUntouched(tb, Key.Space, KeyModifiers.None, false);

			CornerstoneTest.AreEqual(originalText, tb.Text);
			CornerstoneTest.AreEqual(originalCaretIndex, tb.CaretIndex);
			CornerstoneTest.AreEqual(originalSelectionStart, tb.SelectionStart);
			CornerstoneTest.AreEqual(originalSelectionEnd, tb.SelectionEnd);
			CornerstoneTest.AreEqual(originalCanUndo, tb.CanUndo);
			CornerstoneTest.AreEqual(originalCanRedo, tb.CanRedo);
			CornerstoneTest.AreEqual(0, cutRaised);
			CornerstoneTest.AreEqual(0, pasteRaised);
		}
	}

	[PresentationTestMethod]
	public void SelectedTextCanClearText()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = "0123"
			};
			target.SelectionStart = 1;
			target.SelectionEnd = 3;
			target.SelectedText = "";

			CornerstoneTest.IsTrue(target.Text == "03");
		}
	}

	[PresentationTestMethod]
	public void SelectedTextChangesOnSelectionChange()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = "0123456789"
			};

			target.ApplyTemplate();

			CornerstoneTest.IsTrue(target.SelectedText == "");

			target.SelectionStart = 2;
			target.SelectionEnd = 4;

			CornerstoneTest.IsTrue(target.SelectedText == "23");
		}
	}

	[PresentationTestMethod]
	public void SelectedTextEditsText()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = "0123"
			};

			target.ApplyTemplate();

			target.SelectedText = "AA";
			CornerstoneTest.IsTrue(target.Text == "AA0123");

			target.SelectionStart = 1;
			target.SelectionEnd = 3;
			target.SelectedText = "BB";

			CornerstoneTest.IsTrue(target.Text == "ABB123");
		}
	}

	[PresentationTestMethod]
	public void SelectedTextNullClearsText()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = "0123"
			};
			target.SelectionStart = 1;
			target.SelectionEnd = 3;
			target.SelectedText = null;

			CornerstoneTest.IsTrue(target.Text == "03");
		}
	}

	[PresentationTestMethod]
	public void SelectionEndDoesntCauseException()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = "0123456789"
			};

			target.ApplyTemplate();

			target.SelectionStart = 0;
			target.SelectionEnd = 9;

			target.Text = "123";

			RaiseTextEvent(target, "456");

			CornerstoneTest.IsTrue(true);
		}
	}

	[PresentationTestMethod]
	public void SelectionStartDoesntCauseException()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = "0123456789"
			};

			target.ApplyTemplate();

			target.SelectionStart = 8;
			target.SelectionEnd = 9;

			target.Text = "123";

			RaiseTextEvent(target, "456");

			CornerstoneTest.IsTrue(true);
		}
	}

	[PresentationTestMethod]
	public void SelectionStartEndAreValidAterTextChange()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = "0123456789"
			};

			target.SelectionStart = 8;
			target.SelectionEnd = 9;

			target.Text = "123";

			CornerstoneTest.IsTrue(target.SelectionStart <= "123".Length);
			CornerstoneTest.IsTrue(target.SelectionEnd <= "123".Length);
		}
	}

	[PresentationTestMethod]
	public void SettingBoundTextToNullWorks()
	{
		using (UnitTestApplication.Start(Services))
		{
			var source = new Class1 { Bar = "bar" };
			var target = new TextBox { Template = CreateTemplate(), DataContext = source };

			target.ApplyTemplate();

			target.Bind(TextBox.TextProperty, new Binding("Bar"));

			CornerstoneTest.AreEqual("bar", target.Text);
			source.Bar = null;
			CornerstoneTest.IsNull(target.Text);
		}
	}

	[PresentationTestMethod]
	public void SettingIsUndoEnabledToFalseClearsUndoRedo()
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox
			{
				Template = CreateTemplate()
			};

			tb.Measure(Size.Infinity);

			// This is all the same as the above test (CanUndo_CanRedo_and_Programmatic_Undo_Redo_Works)
			// We do this to get the undo/redo stacks in a state where both are active
			RaiseTextEvent(tb, "ABC");
			RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
			RaiseTextEvent(tb, "DEF");
			RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
			RaiseTextEvent(tb, "123");

			CornerstoneTest.AreEqual("ABCDEF123", tb.Text);
			CornerstoneTest.IsTrue(tb.CanUndo);
			tb.Undo();

			// Undo will take us back one step
			CornerstoneTest.AreEqual("ABCDEF", tb.Text);
			CornerstoneTest.IsTrue(tb.CanRedo);
			tb.Redo();

			// Redo should restore us
			CornerstoneTest.AreEqual("ABCDEF123", tb.Text);

			// Disable Undo/Redo, this should clear both stacks setting CanUndo and CanRedo to false
			tb.IsUndoEnabled = false;

			CornerstoneTest.IsFalse(tb.CanUndo);
			CornerstoneTest.IsFalse(tb.CanRedo);
		}
	}

	[PresentationTestMethod]
	public void SettingSelectedTextShouldFireSingleTextChangedNotification()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = "0123",
				AcceptsReturn = true,
				AcceptsTab = true,
				SelectionStart = 1,
				SelectionEnd = 3
			};

			var values = new List<string>();
			target.GetObservable(TextBox.TextProperty).Subscribe(x => values.Add(x));

			target.SelectedText = "A";

			CornerstoneTest.AreEqual(new[] { "0123", "0A3" }, values);
		}
	}

	[PresentationTestMethod]
	public void SettingSelectionStartToSelectionEndSetsCaretPositionToSelectionStart()
	{
		using (UnitTestApplication.Start(Services))
		{
			var textBox = new TextBox
			{
				Text = "0123456789"
			};

			textBox.SelectionStart = 2;
			textBox.SelectionEnd = 2;

			CornerstoneTest.AreEqual(2, textBox.CaretIndex);
		}
	}

	[PresentationTestMethod]
	public void SettingTextUpdatesCaretPosition()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Text = "Initial Text",
				CaretIndex = 11
			};

			var invoked = false;

			target.GetObservable(TextBox.TextProperty).Skip(1).Subscribe(_ =>
			{
				// Caret index should be set before Text changed notification, as we don't want
				// to notify with an invalid CaretIndex.
				CornerstoneTest.AreEqual(7, target.CaretIndex);
				invoked = true;
			});

			target.Text = "Changed";

			CornerstoneTest.IsTrue(invoked);
		}
	}

	[PresentationTestMethod]
	public void SettingUndoLimitClearsUndoRedo()
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox
			{
				Template = CreateTemplate()
			};

			tb.Measure(Size.Infinity);

			// This is all the same as the above test (CanUndo_CanRedo_and_Programmatic_Undo_Redo_Works)
			// We do this to get the undo/redo stacks in a state where both are active
			RaiseTextEvent(tb, "ABC");
			RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
			RaiseTextEvent(tb, "DEF");
			RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
			RaiseTextEvent(tb, "123");

			CornerstoneTest.AreEqual("ABCDEF123", tb.Text);
			CornerstoneTest.IsTrue(tb.CanUndo);
			tb.Undo();

			// Undo will take us back one step
			CornerstoneTest.AreEqual("ABCDEF", tb.Text);
			CornerstoneTest.IsTrue(tb.CanRedo);
			tb.Redo();

			// Redo should restore us
			CornerstoneTest.AreEqual("ABCDEF123", tb.Text);

			// Change the undo limit, this should clear both stacks setting CanUndo and CanRedo to false
			tb.UndoLimit = 1;

			CornerstoneTest.IsFalse(tb.CanUndo);
			CornerstoneTest.IsFalse(tb.CanRedo);
		}
	}

	[PresentationTestMethod]
	public async Task ShouldFullfillMaxLinesContraint()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = "ABC",
				MaxLines = 1,
				AcceptsReturn = true
			};

			var impl = CreateMockTopLevelImpl();
			var topLevel = new TestTopLevel(impl)
			{
				Template = CreateTopLevelTemplate()
			};
			topLevel.Content = target;
			topLevel.ApplyTemplate();
			topLevel.LayoutManager.ExecuteInitialLayoutPass();

			target.ApplyTemplate();
			target.Measure(Size.Infinity);

			var initialHeight = target.DesiredSize.Height;

			await topLevel.Clipboard!.SetTextAsync(Environment.NewLine);

			RaiseKeyEvent(target, Key.V, KeyModifiers.Control);
			await topLevel.Clipboard!.ClearAsync();

			RaiseTextEvent(target, Environment.NewLine);

			target.InvalidateMeasure();
			target.Measure(Size.Infinity);

			CornerstoneTest.AreEqual(initialHeight, target.DesiredSize.Height);
		}
	}

	[PresentationTestMethod]
	public void ShouldFullfillMinLinesContraint()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = "ABC \n DEF \n GHI",
				MinLines = 3,
				AcceptsReturn = true
			};

			var impl = CreateMockTopLevelImpl();
			var topLevel = new TestTopLevel(impl)
			{
				Template = CreateTopLevelTemplate()
			};
			topLevel.Content = target;
			topLevel.ApplyTemplate();
			topLevel.LayoutManager.ExecuteInitialLayoutPass();

			target.ApplyTemplate();
			target.Measure(Size.Infinity);

			var initialHeight = target.DesiredSize.Height;

			target.Text = "";

			target.InvalidateMeasure();
			target.Measure(Size.Infinity);

			CornerstoneTest.AreEqual(initialHeight, target.DesiredSize.Height);
		}
	}

	[PresentationTestMethod]
	public void ShouldMoveCaretToEndOfLine()
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox
			{
				Template = CreateTemplate(),
				Text = "AB\nAB"
			};

			tb.Measure(Size.Infinity);

			RaiseKeyEvent(tb, Key.End, KeyModifiers.Shift);

			CornerstoneTest.AreEqual(2, tb.CaretIndex);
		}
	}

	[PresentationTestMethod]
	[DataRow("A\nBB\nCCC\nDDDD", 0, 0)]
	[DataRow("A\nBB\nCCC\nDDDD", 1, 2)]
	[DataRow("A\nBB\nCCC\nDDDD", 2, 5)]
	[DataRow("A\nBB\nCCC\nDDDD", 3, 9)]
	[DataRow("واحد\nاثنين\nثلاثة\nأربعة", 0, 0)]
	[DataRow("واحد\nاثنين\nثلاثة\nأربعة", 1, 5)]
	[DataRow("واحد\nاثنين\nثلاثة\nأربعة", 2, 11)]
	[DataRow("واحد\nاثنين\nثلاثة\nأربعة", 3, 17)]
	public void ShouldScrollCaretToLine(string text, int targetLineIndex, int expectedCaretIndex)
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox
			{
				Template = CreateTemplate(),
				Text = text
			};
			tb.ApplyTemplate();
			tb.ScrollToLine(targetLineIndex);
			CornerstoneTest.AreEqual(expectedCaretIndex, tb.CaretIndex);
		}
	}

	[PresentationTestMethod]
	public void ShouldThrowArgumentOutOfRange()
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox
			{
				Template = CreateTemplate(),
				Text = string.Empty
			};
			tb.ApplyTemplate();

			Assert.Throws<ArgumentOutOfRangeException>(() => tb.ScrollToLine(-1));
			Assert.Throws<ArgumentOutOfRangeException>(() => tb.ScrollToLine(1));
		}
	}

	[PresentationTestMethod]
	public void TextBoxCaretIndexPersistsWhenFocusLost()
	{
		using (UnitTestApplication.Start(FocusServices.With(new StandardAssetLoader())))
		{
			var target1 = new TextBox
			{
				Template = CreateTemplate(),
				Text = "1234"
			};
			var target2 = new TextBox
			{
				Template = CreateTemplate(),
				Text = "5678"
			};
			var sp = new StackPanel();
			sp.Children.Add(target1);
			sp.Children.Add(target2);

			target1.ApplyTemplate();
			target2.ApplyTemplate();

			var root = new TestRoot { Child = sp };

			target2.Focus();
			target2.CaretIndex = 2;
			CornerstoneTest.IsFalse(target1.IsFocused);
			CornerstoneTest.IsTrue(target2.IsFocused);

			target1.Focus();

			CornerstoneTest.AreEqual(2, target2.CaretIndex);
		}
	}

	[PresentationTestMethod]
	public void TextBoxGotFocusAndLostFocusWorkProperly()
	{
		using (UnitTestApplication.Start(FocusServices))
		{
			var target1 = new TextBox
			{
				Template = CreateTemplate(),
				Text = "1234"
			};
			var target2 = new TextBox
			{
				Template = CreateTemplate(),
				Text = "5678"
			};
			var sp = new StackPanel();
			sp.Children.Add(target1);
			sp.Children.Add(target2);

			target1.ApplyTemplate();
			target2.ApplyTemplate();

			var root = new TestRoot { Child = sp };

			var gfcount = 0;
			var lfcount = 0;

			target1.GotFocus += (s, e) => gfcount++;
			target2.LostFocus += (s, e) => lfcount++;

			target2.Focus();
			CornerstoneTest.IsFalse(target1.IsFocused);
			CornerstoneTest.IsTrue(target2.IsFocused);

			target1.Focus();
			CornerstoneTest.IsFalse(target2.IsFocused);
			CornerstoneTest.IsTrue(target1.IsFocused);

			CornerstoneTest.AreEqual(1, gfcount);
			CornerstoneTest.AreEqual(1, lfcount);
		}
	}

	[PresentationTestMethod]
	public void TextBoxIgnoreWordMoveInPasswordField()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				PasswordChar = '*',
				Text = "passw0rd"
			};

			target.ApplyTemplate();
			target.Measure(Size.Infinity);
			target.CaretIndex = 8;
			RaiseKeyEvent(target, Key.Left, KeyModifiers.Control);

			CornerstoneTest.AreEqual(7, target.CaretIndex);
		}
	}

	[PresentationTestMethod]
	public void TextBoxInAdornerLayerWillNotCauseCollectionModifiedInVisualLayerManagerArrange()
	{
		using (UnitTestApplication.Start(Services))
		{
			var button = new Button();
			var visualLayerManager = new VisualLayerManager { Child = button };
			var root = new TestRoot
			{
				Child = visualLayerManager
			};
			var adorner = new TextBox { Template = CreateTemplate(), Text = "a" };
			var adornerLayer = AdornerLayer.GetAdornerLayer(button);
			CornerstoneTest.IsNotNull(adornerLayer);

			root.Measure(new Size(10, 10));

			adornerLayer.Children.Add(adorner);
			AdornerLayer.SetAdornedElement(adorner, button);

			root.Arrange(new Rect(0, 0, 10, 10));
		}
	}

	[PresentationTestMethod]
	public void TextBoxInAdornerLayerWillNotCauseCollectionModifiedInVisualLayerManagerMeasure()
	{
		using (UnitTestApplication.Start(Services))
		{
			var button = new Button();
			var root = new TestRoot
			{
				Child = new VisualLayerManager
				{
					Child = button
				}
			};
			var adorner = new TextBox { Template = CreateTemplate(), Text = "a" };

			var adornerLayer = AdornerLayer.GetAdornerLayer(button);
			CornerstoneTest.IsNotNull(adornerLayer);
			adornerLayer.Children.Add(adorner);
			AdornerLayer.SetAdornedElement(adorner, button);

			root.Measure(Size.Infinity);
		}
	}

	[PresentationTestMethod]
	public void TextBoxRevealPasswordResetWhenLostFocus()
	{
		using (UnitTestApplication.Start(FocusServices))
		{
			var target1 = new TextBox
			{
				Template = CreateTemplate(),
				Text = "1234",
				PasswordChar = '*'
			};
			var target2 = new TextBox
			{
				Template = CreateTemplate(),
				Text = "5678"
			};
			var sp = new StackPanel();
			sp.Children.Add(target1);
			sp.Children.Add(target2);

			target1.ApplyTemplate();
			target2.ApplyTemplate();

			var root = new TestRoot { Child = sp };

			target1.Focus();
			target1.RevealPassword = true;

			target2.Focus();

			CornerstoneTest.IsFalse(target1.RevealPassword);
		}
	}

	[PresentationTestMethod]
	public void TextBoxShouldLoseFocusWhenDisabled()
	{
		using (UnitTestApplication.Start(FocusServices))
		{
			var target = new TextBox
			{
				Template = CreateTemplate()
			};

			target.ApplyTemplate();

			var root = new TestRoot { Child = target };

			target.Focus();
			CornerstoneTest.IsTrue(target.IsFocused);
			target.IsEnabled = false;
			CornerstoneTest.IsFalse(target.IsFocused);
			CornerstoneTest.IsFalse(target.IsEnabled);
		}
	}

	[PresentationTestMethod]
	[DataRow(Key.Up)]
	[DataRow(Key.Down)]
	[DataRow(Key.Home)]
	[DataRow(Key.End)]
	public void TextboxdoesntcrashwhenReceivesinputandtemplatenotapplied(Key key)
	{
		using (UnitTestApplication.Start(FocusServices))
		{
			var target1 = new TextBox
			{
				Template = CreateTemplate(),
				Text = "1234"
			};

			var root = new TestRoot { Child = target1 };

			target1.Focus();
			CornerstoneTest.IsTrue(target1.IsFocused);

			RaiseKeyEvent(target1, key, KeyModifiers.None);
		}
	}

	[PresentationTestMethod]
	public void TwoWayBindingSourceEchoDoesNotClearUndoHistory()
	{
		using (UnitTestApplication.Start(Services))
		{
			var source = new Class1 { Bar = "initial" };
			var textBox = new TextBox
			{
				Template = CreateTemplate(),
				DataContext = source
			};

			textBox.Bind(TextBox.TextProperty, new Binding(nameof(Class1.Bar))
			{
				Mode = BindingMode.TwoWay
			});
			textBox.Measure(Size.Infinity);
			textBox.CaretIndex = textBox.Text!.Length;

			RaiseTextEvent(textBox, " edit");
			RaiseKeyEvent(textBox, Key.Space, KeyModifiers.None);
			RaiseTextEvent(textBox, " more");

			CornerstoneTest.AreEqual("initial edit more", source.Bar);
			CornerstoneTest.IsTrue(textBox.CanUndo);

			textBox.Undo();

			CornerstoneTest.AreEqual("initial edit", textBox.Text);
			CornerstoneTest.AreEqual("initial edit", source.Bar);
		}
	}

	[PresentationTestMethod]
	public void UndoLimitCountIsRespected()
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox
			{
				Template = CreateTemplate(),
				UndoLimit = 3 // Something small for this test
			};

			tb.Measure(Size.Infinity);

			// Push 3 undoable actions, we should only be able to recover 2
			RaiseTextEvent(tb, "ABC");
			RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
			RaiseTextEvent(tb, "DEF");
			RaiseKeyEvent(tb, Key.Space, KeyModifiers.None);
			RaiseTextEvent(tb, "123");

			CornerstoneTest.AreEqual("ABCDEF123", tb.Text);

			// Undo will take us back one step
			tb.Undo();
			CornerstoneTest.AreEqual("ABCDEF", tb.Text);

			// Undo again
			tb.Undo();
			CornerstoneTest.AreEqual("ABC", tb.Text);

			// We now should not be able to undo again
			CornerstoneTest.IsFalse(tb.CanUndo);
		}
	}

	[PresentationTestMethod]
	public void UnmeasuredTextBoxHasNegativeLineCount()
	{
		var b = new TextBox();
		CornerstoneTest.AreEqual(-1, b.GetLineCount());
	}

	[PresentationTestMethod]
	public void VisibleLineCountDoesNotAffectLineCount()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = "Hello\r\nWorld\r\nHello\r\nCornerstone",
				AcceptsReturn = true,
				MaxLines = 2
			};

			var impl = CreateMockTopLevelImpl();
			var topLevel = new TestTopLevel(impl)
			{
				Template = CreateTopLevelTemplate()
			};
			topLevel.Content = target;
			topLevel.ApplyTemplate();
			topLevel.LayoutManager.ExecuteInitialLayoutPass();

			target.ApplyTemplate();
			target.Measure(Size.Infinity);

			CornerstoneTest.AreEqual(4, target.GetLineCount());
		}
	}

	[PresentationTestMethod]
	[DataRow(0)]
	[DataRow(2)]
	[DataRow(4)]
	[DataRow(6)]
	public void WhenSelectAllFromPositionDownShouldRemoveSelectionMovingCaretToEnd(int caretIndex)
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox
			{
				Template = CreateTemplate(),
				Text = "ABCDEF"
			};

			tb.Measure(Size.Infinity);
			tb.CaretIndex = caretIndex;

			RaiseKeyEvent(tb, Key.A, KeyModifiers.Control);
			RaiseKeyEvent(tb, Key.Down, KeyModifiers.None);

			CornerstoneTest.AreEqual(tb.Text.Length, tb.SelectionStart);
			CornerstoneTest.AreEqual(tb.Text.Length, tb.SelectionEnd);
			CornerstoneTest.AreEqual(tb.Text.Length, tb.CaretIndex);
		}
	}

	[PresentationTestMethod]
	[DataRow(0)]
	[DataRow(2)]
	[DataRow(4)]
	[DataRow(6)]
	public void WhenSelectAllFromPositionLeftShouldRemoveSelectionMovingCaretToStart(int caretIndex)
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox
			{
				Template = CreateTemplate(),
				Text = "ABCDEF"
			};

			tb.Measure(Size.Infinity);
			tb.CaretIndex = caretIndex;

			RaiseKeyEvent(tb, Key.A, KeyModifiers.Control);
			RaiseKeyEvent(tb, Key.Left, KeyModifiers.None);

			CornerstoneTest.AreEqual(0, tb.SelectionStart);
			CornerstoneTest.AreEqual(0, tb.SelectionEnd);
			CornerstoneTest.AreEqual(0, tb.CaretIndex);
		}
	}

	[PresentationTestMethod]
	[DataRow(0)]
	[DataRow(2)]
	[DataRow(4)]
	[DataRow(6)]
	public void WhenSelectAllFromPositionRightShouldRemoveSelectionMovingCaretToEnd(int caretIndex)
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox
			{
				Template = CreateTemplate(),
				Text = "ABCDEF"
			};

			tb.Measure(Size.Infinity);
			tb.CaretIndex = caretIndex;

			RaiseKeyEvent(tb, Key.A, KeyModifiers.Control);
			RaiseKeyEvent(tb, Key.Right, KeyModifiers.None);

			CornerstoneTest.AreEqual(tb.Text.Length, tb.SelectionStart);
			CornerstoneTest.AreEqual(tb.Text.Length, tb.SelectionEnd);
			CornerstoneTest.AreEqual(tb.Text.Length, tb.CaretIndex);
		}
	}

	[PresentationTestMethod]
	[DataRow(0)]
	[DataRow(2)]
	[DataRow(4)]
	[DataRow(6)]
	public void WhenSelectAllFromPositionUpShouldRemoveSelectionMovingCaretToStart(int caretIndex)
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox
			{
				Template = CreateTemplate(),
				Text = "ABCDEF"
			};

			tb.Measure(Size.Infinity);
			tb.CaretIndex = caretIndex;

			RaiseKeyEvent(tb, Key.A, KeyModifiers.Control);
			RaiseKeyEvent(tb, Key.Up, KeyModifiers.None);

			CornerstoneTest.AreEqual(0, tb.SelectionStart);
			CornerstoneTest.AreEqual(0, tb.SelectionEnd);
			CornerstoneTest.AreEqual(0, tb.CaretIndex);
		}
	}

	[PresentationTestMethod]
	[DataRow(0)]
	[DataRow(3)]
	[DataRow(6)]
	public void WhenSelectingMultilineSelectionShouldBeExtendedWithDownArrowKeyTillEndOfText(int caretOffsetFromStart)
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox
			{
				Template = CreateTemplate(),
				Text = """
						AAAAAA
						BBBB
						CCCCCCCC
						""",
				AcceptsReturn = true
			};
			tb.ApplyTemplate();
			tb.Measure(Size.Infinity);
			tb.CaretIndex = caretOffsetFromStart;

			RaiseKeyEvent(tb, Key.Down, KeyModifiers.Shift);
			RaiseKeyEvent(tb, Key.Down, KeyModifiers.Shift);
			RaiseKeyEvent(tb, Key.Down, KeyModifiers.Shift);
			RaiseKeyEvent(tb, Key.Down, KeyModifiers.Shift);

			CornerstoneTest.AreEqual(tb.Text.Length, tb.SelectionEnd);
		}
	}

	[PresentationTestMethod]
	[DataRow(0)]
	[DataRow(4)]
	[DataRow(8)]
	public void WhenSelectingMultilineSelectionShouldBeExtendedWithUpArrowKeyTillStartOfText(int caretOffsetFromEnd)
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox
			{
				Template = CreateTemplate(),
				Text = """
						AAAAAA
						BBBB
						CCCCCCCC
						""",
				AcceptsReturn = true
			};
			tb.ApplyTemplate();
			tb.Measure(Size.Infinity);
			tb.CaretIndex = tb.Text.Length - caretOffsetFromEnd;

			RaiseKeyEvent(tb, Key.Up, KeyModifiers.Shift);
			RaiseKeyEvent(tb, Key.Up, KeyModifiers.Shift);
			RaiseKeyEvent(tb, Key.Up, KeyModifiers.Shift);
			RaiseKeyEvent(tb, Key.Up, KeyModifiers.Shift);

			CornerstoneTest.AreEqual(0, tb.SelectionEnd);
		}
	}

	[PresentationTestMethod]
	[DataRow(2, 4)]
	[DataRow(0, 4)]
	[DataRow(2, 6)]
	[DataRow(0, 6)]
	[DataRow(3, 4)]
	public void WhenSelectionFromLeftToRightPressingDownShouldRemoveSelectionMovingCaretToEndOfPreviousSelection(int selectionStart, int selectionEnd)
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox
			{
				Template = CreateTemplate(),
				Text = "ABCDEF"
			};

			tb.Measure(Size.Infinity);
			tb.CaretIndex = selectionStart;
			tb.SelectionStart = selectionStart;
			tb.SelectionEnd = selectionEnd;

			RaiseKeyEvent(tb, Key.Down, KeyModifiers.None);

			CornerstoneTest.AreEqual(selectionEnd, tb.SelectionStart);
			CornerstoneTest.AreEqual(selectionEnd, tb.SelectionEnd);
			CornerstoneTest.AreEqual(selectionEnd, tb.CaretIndex);
		}
	}

	[PresentationTestMethod]
	[DataRow(2, 4)]
	[DataRow(0, 4)]
	[DataRow(2, 6)]
	[DataRow(0, 6)]
	[DataRow(3, 4)]
	public void WhenSelectionFromLeftToRightPressingLeftShouldRemoveSelectionMovingCaretToStartOfPreviousSelection(int selectionStart, int selectionEnd)
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox
			{
				Template = CreateTemplate(),
				Text = "ABCDEF"
			};

			tb.Measure(Size.Infinity);
			tb.CaretIndex = selectionStart;
			tb.SelectionStart = selectionStart;
			tb.SelectionEnd = selectionEnd;

			RaiseKeyEvent(tb, Key.Left, KeyModifiers.None);

			CornerstoneTest.AreEqual(selectionStart, tb.SelectionStart);
			CornerstoneTest.AreEqual(selectionStart, tb.SelectionEnd);
			CornerstoneTest.AreEqual(selectionStart, tb.CaretIndex);
		}
	}

	[PresentationTestMethod]
	[DataRow(2, 4)]
	[DataRow(0, 4)]
	[DataRow(2, 6)]
	[DataRow(0, 6)]
	[DataRow(3, 4)]
	public void WhenSelectionFromLeftToRightPressingRightShouldRemoveSelectionMovingCaretToEndOfPreviousSelection(int selectionStart, int selectionEnd)
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox
			{
				Template = CreateTemplate(),
				Text = "ABCDEF"
			};

			tb.Measure(Size.Infinity);
			tb.CaretIndex = selectionStart;
			tb.SelectionStart = selectionStart;
			tb.SelectionEnd = selectionEnd;

			RaiseKeyEvent(tb, Key.Right, KeyModifiers.None);

			CornerstoneTest.AreEqual(selectionEnd, tb.SelectionStart);
			CornerstoneTest.AreEqual(selectionEnd, tb.SelectionEnd);
			CornerstoneTest.AreEqual(selectionEnd, tb.CaretIndex);
		}
	}

	[PresentationTestMethod]
	[DataRow(2, 4)]
	[DataRow(0, 4)]
	[DataRow(2, 6)]
	[DataRow(0, 6)]
	[DataRow(3, 4)]
	public void WhenSelectionFromLeftToRightPressingUpShouldRemoveSelectionMovingCaretToStartOfPreviousSelection(int selectionStart, int selectionEnd)
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox
			{
				Template = CreateTemplate(),
				Text = "ABCDEF"
			};

			tb.Measure(Size.Infinity);
			tb.CaretIndex = selectionStart;
			tb.SelectionStart = selectionStart;
			tb.SelectionEnd = selectionEnd;

			RaiseKeyEvent(tb, Key.Up, KeyModifiers.None);

			CornerstoneTest.AreEqual(selectionStart, tb.SelectionStart);
			CornerstoneTest.AreEqual(selectionStart, tb.SelectionEnd);
			CornerstoneTest.AreEqual(selectionStart, tb.CaretIndex);
		}
	}

	[PresentationTestMethod]
	[DataRow(4, 2)]
	[DataRow(4, 0)]
	[DataRow(6, 2)]
	[DataRow(6, 0)]
	[DataRow(4, 3)]
	public void WhenSelectionFromRightToLeftPressingDownShouldRemoveSelectionMovingCaretToStartOfPreviousSelection(int selectionStart, int selectionEnd)
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox
			{
				Template = CreateTemplate(),
				Text = "ABCDEF"
			};

			tb.Measure(Size.Infinity);
			tb.CaretIndex = selectionStart;
			tb.SelectionStart = selectionStart;
			tb.SelectionEnd = selectionEnd;

			RaiseKeyEvent(tb, Key.Down, KeyModifiers.None);

			CornerstoneTest.AreEqual(selectionStart, tb.SelectionStart);
			CornerstoneTest.AreEqual(selectionStart, tb.SelectionEnd);
			CornerstoneTest.AreEqual(selectionStart, tb.CaretIndex);
		}
	}

	[PresentationTestMethod]
	[DataRow(4, 2)]
	[DataRow(4, 0)]
	[DataRow(6, 2)]
	[DataRow(6, 0)]
	[DataRow(4, 3)]
	public void WhenSelectionFromRightToLeftPressingLeftShouldRemoveSelectionMovingCaretToEndOfPreviousSelection(int selectionStart, int selectionEnd)
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox
			{
				Template = CreateTemplate(),
				Text = "ABCDEF"
			};

			tb.Measure(Size.Infinity);
			tb.CaretIndex = selectionStart;
			tb.SelectionStart = selectionStart;
			tb.SelectionEnd = selectionEnd;

			RaiseKeyEvent(tb, Key.Left, KeyModifiers.None);

			CornerstoneTest.AreEqual(selectionEnd, tb.SelectionStart);
			CornerstoneTest.AreEqual(selectionEnd, tb.SelectionEnd);
			CornerstoneTest.AreEqual(selectionEnd, tb.CaretIndex);
		}
	}

	[PresentationTestMethod]
	[DataRow(4, 2)]
	[DataRow(4, 0)]
	[DataRow(6, 2)]
	[DataRow(6, 0)]
	[DataRow(4, 3)]
	public void WhenSelectionFromRightToLeftPressingRightShouldRemoveSelectionMovingCaretToStartOfPreviousSelection(int selectionStart, int selectionEnd)
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox
			{
				Template = CreateTemplate(),
				Text = "ABCDEF"
			};

			tb.Measure(Size.Infinity);
			tb.CaretIndex = selectionStart;
			tb.SelectionStart = selectionStart;
			tb.SelectionEnd = selectionEnd;

			RaiseKeyEvent(tb, Key.Right, KeyModifiers.None);

			CornerstoneTest.AreEqual(selectionStart, tb.SelectionStart);
			CornerstoneTest.AreEqual(selectionStart, tb.SelectionEnd);
			CornerstoneTest.AreEqual(selectionStart, tb.CaretIndex);
		}
	}

	[PresentationTestMethod]
	[DataRow(4, 2)]
	[DataRow(4, 0)]
	[DataRow(6, 2)]
	[DataRow(6, 0)]
	[DataRow(4, 3)]
	public void WhenSelectionFromRightToLeftPressingUpShouldRemoveSelectionMovingCaretToEndOfPreviousSelection(int selectionStart, int selectionEnd)
	{
		using (UnitTestApplication.Start(Services))
		{
			var tb = new TextBox
			{
				Template = CreateTemplate(),
				Text = "ABCDEF"
			};

			tb.Measure(Size.Infinity);
			tb.CaretIndex = selectionStart;
			tb.SelectionStart = selectionStart;
			tb.SelectionEnd = selectionEnd;

			RaiseKeyEvent(tb, Key.Up, KeyModifiers.None);

			CornerstoneTest.AreEqual(selectionEnd, tb.SelectionStart);
			CornerstoneTest.AreEqual(selectionEnd, tb.SelectionEnd);
			CornerstoneTest.AreEqual(selectionEnd, tb.CaretIndex);
		}
	}

	[PresentationTestMethod]
	public async Task Pointer_Selection_Is_Published_To_Primary_Selection()
	{
		using (UnitTestApplication.Start(CreatePrimarySelectionServices()))
		{
			var target = new TextBox { Text = "0123" };
			var window = new Window { Content = target };
			window.Show();

			var mouse = new MouseTestHelper();
			mouse.Down(target, MouseButton.Left, new Point(5, 300));
			mouse.Move(target, new Point(700, 300));
			mouse.Up(target, MouseButton.Left, new Point(700, 300));

			CornerstoneTest.AreEqual("0123", target.SelectedText);
			CornerstoneTest.AreEqual("0123", await window.TryGetClipboard(ClipboardType.PrimarySelection).TryGetTextAsync());
		}
	}

	[PresentationTestMethod]
	public async Task Pointer_Selection_Is_Not_Published_To_Primary_Selection_For_Password_Box()
	{
		using (UnitTestApplication.Start(CreatePrimarySelectionServices()))
		{
			var target = new TextBox { Text = "0123", PasswordChar = '*' };
			var window = new Window { Content = target };
			window.Show();

			var mouse = new MouseTestHelper();
			mouse.Down(target, MouseButton.Left, new Point(5, 300));
			mouse.Move(target, new Point(700, 300));
			mouse.Up(target, MouseButton.Left, new Point(700, 300));

			CornerstoneTest.IsNull(await window.TryGetClipboard(ClipboardType.PrimarySelection).TryGetTextAsync());
		}
	}

	[PresentationTestMethod]
	public async Task Middle_Click_Pastes_Primary_Selection_At_Click_Position()
	{
		using (UnitTestApplication.Start(CreatePrimarySelectionServices()))
		{
			var target = new TextBox { Text = "0123" };
			var window = new Window { Content = target };
			window.Show();

			await window.TryGetClipboard(ClipboardType.PrimarySelection).SetTextAsync("abc");

			PastingFromClipboardEventArgs pastingArgs = null;
			target.PastingFromClipboard += (_, e) => pastingArgs = e as PastingFromClipboardEventArgs;

			var mouse = new MouseTestHelper();
			mouse.Down(target, MouseButton.Middle, new Point(700, 300));
			mouse.Up(target, MouseButton.Middle, new Point(700, 300));

			CornerstoneTest.AreEqual("0123abc", target.Text);
			CornerstoneTest.IsNotNull(pastingArgs);
			CornerstoneTest.IsTrue(ReferenceEquals(window.TryGetClipboard(ClipboardType.PrimarySelection), pastingArgs.Clipboard));

			// The pasted-over selection was not changed by the gesture, so it must not be published.
			CornerstoneTest.AreEqual("abc", await window.TryGetClipboard(ClipboardType.PrimarySelection).TryGetTextAsync());
		}
	}

	[PresentationTestMethod]
	public async Task Middle_Click_Does_Not_Paste_When_ReadOnly()
	{
		using (UnitTestApplication.Start(CreatePrimarySelectionServices()))
		{
			var target = new TextBox { Text = "0123", IsReadOnly = true };
			var window = new Window { Content = target };
			window.Show();

			await window.TryGetClipboard(ClipboardType.PrimarySelection).SetTextAsync("abc");

			var mouse = new MouseTestHelper();
			mouse.Down(target, MouseButton.Middle, new Point(700, 300));
			mouse.Up(target, MouseButton.Middle, new Point(700, 300));

			CornerstoneTest.AreEqual("0123", target.Text);
		}
	}

	[PresentationTestMethod]
	public void Middle_Click_Does_Nothing_Without_Primary_Selection()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new TextBox { Text = "0123" };
			var window = new Window { Content = target };
			window.Show();

			CornerstoneTest.IsNull(window.TryGetClipboard(ClipboardType.PrimarySelection));

			var mouse = new MouseTestHelper();
			mouse.Down(target, MouseButton.Middle, new Point(700, 300));
			mouse.Up(target, MouseButton.Middle, new Point(700, 300));

			CornerstoneTest.AreEqual("0123", target.Text);
		}
	}

	[PresentationTestMethod]
	public void Paste_Raises_Event_When_No_Clipboard_Is_Available()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new TextBox { Text = "0123" };
			var window = new Window { Content = target };
			window.Show();

			CornerstoneTest.IsNull(window.Clipboard);

			PastingFromClipboardEventArgs pastingArgs = null;
			target.PastingFromClipboard += (_, e) =>
			{
				pastingArgs = e as PastingFromClipboardEventArgs;
				e.Handled = true;
			};

			target.Paste();

			CornerstoneTest.IsNotNull(pastingArgs);
			CornerstoneTest.IsNull(pastingArgs.Clipboard);
		}
	}

	internal static TestServices CreatePrimarySelectionServices()
	{
		var manager = new PlatformClipboardManager(
			new Clipboard(new HeadlessClipboardImplStub()),
			new Clipboard(new HeadlessClipboardImplStub()));

		return TestServices.StyledWindow.With(
			windowingPlatform: new MockWindowingPlatform(() =>
			{
				var windowImpl = new StubWindowImpl();
				windowImpl.SetFeature(typeof(IPlatformClipboardManagerImpl), manager);
				return windowImpl;
			}));
	}

	internal static IControlTemplate CreateTemplate()
	{
		return new FuncControlTemplate<TextBox>((control, scope) =>
			new ScrollViewer
			{
				Name = "PART_ScrollViewer",
				Template = new FuncControlTemplate<ScrollViewer>(ScrollViewerTests.CreateTemplate),
				Content = new TextPresenter
				{
					Name = "PART_TextPresenter",
					[!!TextPresenter.TextProperty] = new Binding
					{
						Path = nameof(TextPresenter.Text),
						Mode = BindingMode.TwoWay,
						Priority = BindingPriority.Template,
						RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
					},
					[!!TextPresenter.CaretIndexProperty] = new Binding
					{
						Path = nameof(TextPresenter.CaretIndex),
						Mode = BindingMode.TwoWay,
						Priority = BindingPriority.Template,
						RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
					}
				}.RegisterInNameScope(scope)
			}.RegisterInNameScope(scope));
	}

	private static void AssertReadOnlyHotkeyLeavesStateUntouched(
		TextBox textBox,
		IReadOnlyList<KeyGesture> gestures,
		bool handled)
	{
		CornerstoneTest.NotEmpty(gestures);
		var gesture = gestures[0];
		AssertReadOnlyHotkeyLeavesStateUntouched(textBox, gesture.Key, gesture.KeyModifiers, handled);
	}

	private static void AssertReadOnlyHotkeyLeavesStateUntouched(
		TextBox textBox,
		Key key,
		KeyModifiers inputModifiers,
		bool handled)
	{
		var originalText = textBox.Text;
		var originalCaretIndex = textBox.CaretIndex;
		var originalSelectionStart = textBox.SelectionStart;
		var originalSelectionEnd = textBox.SelectionEnd;
		var originalCanUndo = textBox.CanUndo;
		var originalCanRedo = textBox.CanRedo;

		var args = RaiseKeyEvent(textBox, key, inputModifiers);

		CornerstoneTest.AreEqual(handled, args.Handled);
		CornerstoneTest.AreEqual(originalText, textBox.Text);
		CornerstoneTest.AreEqual(originalCaretIndex, textBox.CaretIndex);
		CornerstoneTest.AreEqual(originalSelectionStart, textBox.SelectionStart);
		CornerstoneTest.AreEqual(originalSelectionEnd, textBox.SelectionEnd);
		CornerstoneTest.AreEqual(originalCanUndo, textBox.CanUndo);
		CornerstoneTest.AreEqual(originalCanRedo, textBox.CanRedo);
	}

	private static StubWindowImpl CreateMockTopLevelImpl(IClipboardImpl clipboardImpl = null)
	{
		var clipboard = new StubWindowImpl();
		clipboard.SetFeature(typeof(IClipboard), new Clipboard(clipboardImpl ?? new HeadlessClipboardImplStub()));
		return clipboard;
	}

	private static TextBox CreateTextBoxInTopLevel(IClipboardImpl clipboardImpl)
	{
		var textBox = new TextBox
		{
			Template = CreateTemplate(),
			Text = "abcd",
			SelectionStart = 1,
			SelectionEnd = 3
		};

		var topLevel = new TestTopLevel(CreateMockTopLevelImpl(clipboardImpl))
		{
			Template = CreateTopLevelTemplate(),
			Content = textBox
		};
		topLevel.ApplyTemplate();
		topLevel.LayoutManager.ExecuteInitialLayoutPass();

		textBox.Measure(Size.Infinity);

		return textBox;
	}

	private static FuncControlTemplate<TestTopLevel> CreateTopLevelTemplate()
	{
		return new FuncControlTemplate<TestTopLevel>((x, scope) =>
			new ContentPresenter
			{
				Name = "PART_ContentPresenter",
				[!ContentPresenter.ContentProperty] = x[!ContentControl.ContentProperty]
			}.RegisterInNameScope(scope));
	}

	private static TextInputMethodClient GetInputMethodClient(TextBox textBox)
	{
		var eventArgs = new TextInputMethodClientRequestedEventArgs
		{
			RoutedEvent = InputElement.TextInputMethodClientRequestedEvent
		};
		textBox.RaiseEvent(eventArgs);

		CornerstoneTest.IsNotNull(eventArgs.Client);
		return eventArgs.Client;
	}

	private static KeyEventArgs RaiseKeyEvent(TextBox textBox, Key key, KeyModifiers inputModifiers)
	{
		var args = new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			KeyModifiers = inputModifiers,
			Key = key
		};

		textBox.RaiseEvent(args);

		return args;
	}

	private static void RaiseTextEvent(TextBox textBox, string text)
	{
		textBox.RaiseEvent(new TextInputEventArgs
		{
			RoutedEvent = InputElement.TextInputEvent,
			Text = text
		});
	}

	private static Exception RunAndCaptureUnhandledException(Action action)
	{
		using var syncContext = UnitTestSynchronizationContext.Begin();

		action();

		return Record.Exception(syncContext.ExecutePostedCallbacks);
	}

	#endregion

	#region Classes

	private class Class1 : NotifyingBase
	{
		#region Fields

		private string _bar;
		private int _foo;

		#endregion

		#region Properties

		public string Bar
		{
			get => _bar;
			set
			{
				_bar = value;
				RaisePropertyChanged();
			}
		}

		public int Foo
		{
			get => _foo;
			set
			{
				_foo = value;
				RaisePropertyChanged();
			}
		}

		#endregion
	}

	private class TestContextMenu : ContextMenu
	{
		#region Constructors

		public TestContextMenu()
		{
			IsOpen = true;
		}

		#endregion
	}

	private class TestTopLevel(ITopLevelImpl impl) : TopLevel(impl)
	{
	}

	#endregion
}