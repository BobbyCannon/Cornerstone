#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reactive.Linq;
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
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class MaskedTextBoxTests : ScopedTestBase
{
	#region Properties

	private static TestServices FocusServices =>
		TestServices.MockThreadingInterface.With(
			keyboardDevice: () => new KeyboardDevice(),
			keyboardNavigation: () => new KeyboardNavigationHandler(),
			inputManager: new InputManager(),
			renderInterface: new HeadlessPlatformRenderInterface(),
			fontManagerImpl: new HeadlessFontManagerStub(),
			textShaperImpl: new HarfBuzzTextShaper(),
			standardCursorFactory: new StubCursorFactory());

	private static TestServices Services =>
		TestServices.MockThreadingInterface.With(
			renderInterface: new HeadlessPlatformRenderInterface(),
			standardCursorFactory: new StubCursorFactory(),
			textShaperImpl: new HarfBuzzTextShaper(),
			fontManagerImpl: new HeadlessFontManagerStub());

	#endregion

	#region Methods

	[PresentationTestMethod]
	[DataRow("00/00/0000", "12102000", "12/10/2000")]
	[DataRow("LLLL", "дбs", "____")]
	[DataRow("AA", "Ü1", "__")]
	public void AsciiOnlyShouldNotAcceptNonAscii(string mask, string textEventArg, string expected)
	{
		using (Start())
		{
			var target = new MaskedTextBox
			{
				Template = CreateTemplate(),
				Mask = mask,
				AsciiOnly = true
			};

			RaiseTextEvent(target, textEventArg);

			CornerstoneTest.AreEqual(expected, target.Text);
		}
	}

	[PresentationTestMethod]
	public void CaretIndexCanMovedToPositionAfterTheEndOfTextWithArrowKey()
	{
		using (Start())
		{
			var target = new MaskedTextBox
			{
				Template = CreateTemplate(),
				Text = "1234"
			};

			target.ApplyTemplate();
			target.CaretIndex = 3;
			target.Measure(Size.Infinity);

			RaiseKeyEvent(target, Key.Right, 0);

			CornerstoneTest.AreEqual(4, target.CaretIndex);
		}
	}

	[PresentationTestMethod]
	public void ClearAndSelectedTextReplacementRemainUndoable()
	{
		using (Start(FocusServices))
		{
			var target = new MaskedTextBox
			{
				Template = CreateTemplate(),
				Mask = "000",
				Text = "123"
			};

			target.ApplyTemplate();

			var root = new TestRoot { Child = target };

			target.Focus();

			target.Clear();
			CornerstoneTest.IsTrue(target.CanUndo);

			target.Undo();
			target.SelectionStart = 0;
			target.SelectionEnd = 1;
			target.SelectedText = "9";

			CornerstoneTest.IsTrue(target.CanUndo);
		}
	}

	[PresentationTestMethod]
	public void CoerceCaretIndexDoesntCauseExceptionwithmalformedlineending()
	{
		using (Start())
		{
			var target = new MaskedTextBox
			{
				Template = CreateTemplate(),
				Text = "0123456789\r"
			};
			target.CaretIndex = 11;

			CornerstoneTest.IsTrue(true);
		}
	}

	[PresentationTestMethod]
	public void ControlBackspaceShouldRemoveTheWordBeforeTheCaretIfThereIsNoSelection()
	{
		using (Start())
		{
			var textBox = new MaskedTextBox
			{
				Template = CreateTemplate(),
				Text = "First Second Third Fourth",
				CaretIndex = 5
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
	public void ControlDeleteShouldRemoveTheWordAfterTheCaretIfThereIsNoSelection()
	{
		using (Start())
		{
			var textBox = new MaskedTextBox
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
	public void DefaultBindingModeShouldBeTwoWay()
	{
		CornerstoneTest.AreEqual(BindingMode.TwoWay, TextBox.TextProperty.GetMetadata(typeof(MaskedTextBox)).DefaultBindingMode);
	}

	[PresentationTestMethod]
	public void ExternalReplacementAfterMaskedEditClearsUndoHistory()
	{
		using (Start(FocusServices))
		{
			var target = new MaskedTextBox
			{
				Template = CreateTemplate(),
				Mask = "000",
				Text = "123"
			};

			target.ApplyTemplate();

			var root = new TestRoot { Child = target };

			target.Focus();
			target.CaretIndex = 3;

			RaiseKeyEvent(target, Key.Back, KeyModifiers.None);

			CornerstoneTest.IsTrue(target.CanUndo);

			target.Text = "456";

			CornerstoneTest.IsFalse(target.CanUndo);
			CornerstoneTest.IsFalse(target.CanRedo);
		}
	}

	[PresentationTestMethod]
	public void FocusingAndUnfocusingDoesNotCreateUndoOperation()
	{
		using (Start(FocusServices))
		{
			var target = new MaskedTextBox
			{
				Template = CreateTemplate(),
				Mask = "000",
				HidePromptOnLeave = true,
				Text = "123"
			};

			var other = new MaskedTextBox { Template = CreateTemplate() };

			var sp = new StackPanel();
			sp.Children.Add(target);
			sp.Children.Add(other);

			target.ApplyTemplate();
			other.ApplyTemplate();

			var root = new TestRoot { Child = sp };

			target.Focus();
			other.Focus();

			CornerstoneTest.IsFalse(target.CanUndo);
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
		using (Start())
		{
			var target = new MaskedTextBox
			{
				AcceptsReturn = acceptsReturn,
				TextWrapping = wrapping
			};

			CornerstoneTest.AreEqual(expected, ScrollViewer.GetHorizontalScrollBarVisibility(target));
		}
	}

	[PresentationTestMethod]
	public void InvalidProgrammaticallySetTextShouldBeRejected()
	{
		using (Start())
		{
			var target = new MaskedTextBox
			{
				Template = CreateTemplate(),
				Mask = "00:00:00.000",
				Text = "12:34:560000"
			};

			CornerstoneTest.AreEqual("__:__:__.___", target.Text);
		}
	}

	[PresentationTestMethod]
	public void InvalidTextIsCoercedWithoutRaisingIntermediateChange()
	{
		using (Start())
		{
			var target = new MaskedTextBox
			{
				Template = CreateTemplate()
			};

			var impl = CreateMockTopLevelImpl();
			var topLevel = new TestTopLevel(impl)
			{
				Template = CreateTopLevelTemplate(),
				Content = target
			};
			topLevel.ApplyTemplate();
			topLevel.LayoutManager.ExecuteInitialLayoutPass();

			var texts = new List<string>();

			target.PropertyChanged += (_, e) =>
			{
				if (e.Property == TextBox.TextProperty)
				{
					texts.Add(e.GetNewValue<string>());
				}
			};

			target.Mask = "000";

			target.Text = "123";
			target.Text = "abc";

			CornerstoneTest.AreEqual(["___", "123"], texts);
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
		using (Start())
		{
			var target = new MaskedTextBox
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

			target.SelectionStart = 1;
			target.SelectionEnd = 3;

			RaiseKeyEvent(target, key, modifiers);
			RaiseKeyEvent(target, Key.Z, KeyModifiers.Control); // undo
			CornerstoneTest.AreEqual("0123", target.Text);
		}
	}

	[PresentationTestMethod]
	[DataRow("00/00/0000", "12102000", "12/10/2000")]
	[DataRow("LLLL", "дбs", "дбs_")]
	[DataRow("AA#00", "S2 33", "S2_33")]
	public void MaskShouldWorkCorrectly(string mask, string textEventArg, string expected)
	{
		using (Start())
		{
			var target = new MaskedTextBox
			{
				Template = CreateTemplate(),
				Mask = mask
			};

			RaiseTextEvent(target, textEventArg);

			CornerstoneTest.AreEqual(expected, target.Text);
		}
	}

	[PresentationTestMethod]
	public void MaskedEditRemainsUndoable()
	{
		using (Start(FocusServices))
		{
			var target = new MaskedTextBox
			{
				Template = CreateTemplate(),
				Mask = "000",
				Text = "123"
			};

			target.ApplyTemplate();

			var root = new TestRoot { Child = target };

			target.Focus();
			target.CaretIndex = 3;

			RaiseKeyEvent(target, Key.Back, KeyModifiers.None);

			CornerstoneTest.AreEqual("12_", target.Text);
			CornerstoneTest.IsTrue(target.CanUndo);

			target.Undo();

			CornerstoneTest.AreEqual("123", target.Text);
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
		using (Start())
		{
			var target = new MaskedTextBox
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

			target.ApplyTemplate();

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
	public void OpeningContextFlyoutDoesnotLoseSelection()
	{
		using (Start(FocusServices))
		{
			var target1 = new MaskedTextBox
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
		using (Start(FocusServices))
		{
			var target1 = new MaskedTextBox
			{
				Template = CreateTemplate(),
				Text = "1234",
				ContextMenu = new TestContextMenu()
			};

			var target2 = new MaskedTextBox
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
	[DataRow("00/00/0000", "12102000", "**/**/****")]
	[DataRow("LLLL", "дбs", "***_")]
	[DataRow("AA#00", "S2 33", "**_**")]
	public void PasswordCharShouldHideUserInput(string mask, string textEventArg, string expected)
	{
		using (Start())
		{
			var target = new MaskedTextBox
			{
				Template = CreateTemplate(),
				Mask = mask,
				PasswordChar = '*'
			};

			RaiseTextEvent(target, textEventArg);

			CornerstoneTest.AreEqual(expected, target.Text);
		}
	}

	[PresentationTestMethod]
	public void PressCtrlASelectAllNullText()
	{
		using (Start())
		{
			var target = new MaskedTextBox
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
		using (Start())
		{
			var target = new MaskedTextBox
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
		using (Start())
		{
			var target = new MaskedTextBox
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
		using (Start())
		{
			var target = new MaskedTextBox
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
		using (Start())
		{
			var target = new MaskedTextBox
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
		using (Start())
		{
			var target = new MaskedTextBox
			{
				Template = CreateTemplate(),
				AcceptsReturn = false,
				Text = "1234"
			};

			RaiseKeyEvent(target, Key.Enter, 0);

			CornerstoneTest.AreEqual("1234", target.Text);
		}
	}

	[PresentationTestMethod]
	public void ProgrammaticallySetTextShouldNotBeRemovedOnKeyPress()
	{
		using (Start())
		{
			var target = new MaskedTextBox
			{
				Template = CreateTemplate(),
				Mask = "00:00:00.000",
				Text = "12:34:56.000"
			};

			target.CaretIndex = target.Text.Length;
			RaiseKeyEvent(target, Key.Back, 0);

			CornerstoneTest.AreEqual("12:34:56.00_", target.Text);
		}
	}

	[PresentationTestMethod]
	public void SelectedTextCanClearText()
	{
		using (Start())
		{
			var target = new MaskedTextBox
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
		using (Start())
		{
			var target = new MaskedTextBox
			{
				Template = CreateTemplate(),
				Text = "0123456789"
			};

			CornerstoneTest.IsTrue(target.SelectedText == "");

			target.SelectionStart = 2;
			target.SelectionEnd = 4;

			CornerstoneTest.IsTrue(target.SelectedText == "23");
		}
	}

	[PresentationTestMethod]
	public void SelectedTextEditsText()
	{
		using (Start())
		{
			var target = new MaskedTextBox
			{
				Template = CreateTemplate(),
				Text = "0123"
			};

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
		using (Start())
		{
			var target = new MaskedTextBox
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
		using (Start())
		{
			var target = new MaskedTextBox
			{
				Template = CreateTemplate(),
				Text = "0123456789"
			};

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
		using (Start())
		{
			var target = new MaskedTextBox
			{
				Template = CreateTemplate(),
				Text = "0123456789"
			};

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
		using (Start())
		{
			var target = new MaskedTextBox
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
		using (Start())
		{
			var source = new Class1 { Bar = "bar" };
			var target = new MaskedTextBox { DataContext = source };

			target.Bind(TextBox.TextProperty, new Binding("Bar"));

			CornerstoneTest.AreEqual("bar", target.Text);
			source.Bar = null;
			CornerstoneTest.IsNull(target.Text);
		}
	}

	[PresentationTestMethod]
	public void SettingSelectionStartToSelectionEndSetsCaretPositionToSelectionStart()
	{
		using (Start())
		{
			var textBox = new MaskedTextBox
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
		using (Start())
		{
			var target = new MaskedTextBox
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
	public void TextBoxCaretIndexPersistsWhenFocusLost()
	{
		using (Start(FocusServices))
		{
			var target1 = new MaskedTextBox
			{
				Template = CreateTemplate(),
				Text = "1234"
			};
			var target2 = new MaskedTextBox
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
		using (Start(FocusServices))
		{
			var target1 = new MaskedTextBox
			{
				Template = CreateTemplate(),
				Text = "1234"
			};
			var target2 = new MaskedTextBox
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
	public void TextBoxRevealPasswordResetWhenLostFocus()
	{
		using (Start(FocusServices))
		{
			var target1 = new MaskedTextBox
			{
				Template = CreateTemplate(),
				Text = "1234",
				PasswordChar = '*'
			};
			var target2 = new MaskedTextBox
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
	[DataRow(Key.Up)]
	[DataRow(Key.Down)]
	[DataRow(Key.Home)]
	[DataRow(Key.End)]
	public void TextboxdoesntcrashwhenReceivesinputandtemplatenotapplied(Key key)
	{
		using (Start(FocusServices))
		{
			var target1 = new MaskedTextBox
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

	private static StubWindowImpl CreateMockTopLevelImpl()
	{
		var clipboard = new StubWindowImpl();
		clipboard.SetFeature(typeof(IClipboard), new Clipboard(new HeadlessClipboardImplStub()));
		return clipboard;
	}

	private static IControlTemplate CreateTemplate()
	{
		return new FuncControlTemplate<MaskedTextBox>((control, scope) =>
			new TextPresenter
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
			}.RegisterInNameScope(scope));
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

	private static void RaiseKeyEvent(MaskedTextBox textBox, Key key, KeyModifiers inputModifiers)
	{
		textBox.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			KeyModifiers = inputModifiers,
			Key = key
		});
	}

	private void RaiseTextEvent(MaskedTextBox textBox, string text)
	{
		textBox.RaiseEvent(new TextInputEventArgs
		{
			RoutedEvent = InputElement.TextInputEvent,
			Text = text
		});
	}

	private static IDisposable Start(TestServices services = null)
	{
		CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
		return UnitTestApplication.Start((services ?? Services).With(new StandardAssetLoader()));
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