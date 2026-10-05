#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Documents;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Harfbuzz;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Platform;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.TextFormatting;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class SelectableTextBlockTests : ScopedTestBase
{
	#region Properties

	private static TestServices ClipboardServices =>
		TestServices.MockThreadingInterface.With(
			new StandardAssetLoader(),
			renderInterface: new HeadlessPlatformRenderInterface(),
			textShaperImpl: new HarfBuzzTextShaper(),
			fontManagerImpl: new TestFontManager());

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void CanCopyTracksSelectionOverInlines()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new SelectableTextBlock();

			target.Inlines!.Add(new Run("foo"));
			target.Inlines!.Add(new Run("bar"));

			target.Measure(Size.Infinity);

			CornerstoneTest.IsFalse(target.CanCopy);

			target.SelectionStart = 2;
			target.SelectionEnd = 5;

			CornerstoneTest.IsTrue(target.CanCopy);

			target.ClearSelection();

			CornerstoneTest.IsFalse(target.CanCopy);
		}
	}

	[PresentationTestMethod]
	[DataRow(2, 2, false)]
	[DataRow(1, 3, true)]
	[DataRow(3, 1, true)]
	[DataRow(0, 4, true)]
	public void CanCopyTracksWhetherSelectionCoversAnyCharacter(int start, int end, bool expected)
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new SelectableTextBlock { Text = "abcd" };

			target.Measure(Size.Infinity);

			target.SelectionStart = start;
			target.SelectionEnd = end;

			CornerstoneTest.AreEqual(expected, target.CanCopy);
		}
	}

	[PresentationTestMethod]
	public void CoerceCaretIndexOnTextChanged()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new SelectableTextBlock
			{
				Text = "foo",
				SelectionStart = 3,
				SelectionEnd = 3
			};

			target.Text = "a";

			CornerstoneTest.AreEqual(1, target.SelectionStart);
			CornerstoneTest.AreEqual(1, target.SelectionEnd);
		}
	}

	[PresentationTestMethod]
	public void CopyDoesNotSwallowUnexpectedExceptions()
	{
		using var app = UnitTestApplication.Start(ClipboardServices);

		var clipboardImpl = new ThrowingClipboardImplStub(typeof(InvalidOperationException));
		var target = CreateSelectableTextBlockInTopLevel(clipboardImpl);

		using var syncContext = UnitTestSynchronizationContext.Begin();

		target.Copy();

		CornerstoneTest.IsType<InvalidOperationException>(Record.Exception(syncContext.ExecutePostedCallbacks));
	}

	[PresentationTestMethod]
	[DataRow(typeof(TimeoutException))]
	[DataRow(typeof(OperationCanceledException))]
	[DataRow(typeof(UnauthorizedAccessException))]
	[DataRow(typeof(COMException))]
	public void CopyDoesNotThrowWhenClipboardFails(Type exceptionType)
	{
		using var app = UnitTestApplication.Start(ClipboardServices);

		var clipboardImpl = new ThrowingClipboardImplStub(exceptionType);
		var target = CreateSelectableTextBlockInTopLevel(clipboardImpl);
		var messages = new List<string>();

		using (TestLogSink.Start((_, _, _, message, _) => messages.Add(message)))
		{
			using var syncContext = UnitTestSynchronizationContext.Begin();

			target.Copy();

			CornerstoneTest.IsNull(Record.Exception(syncContext.ExecutePostedCallbacks));
		}

		CornerstoneTest.AreEqual(1, clipboardImpl.SetDataCount);
		CornerstoneTest.AreEqual(["Failed to write text to clipboard: {Error}"], messages);
	}

	[PresentationTestMethod]
	[DataRow(TextAlignment.Center)]
	[DataRow(TextAlignment.Right)]
	public void DraggingSelectionShouldReachEndOfTextWhenTextIsAligned(TextAlignment textAlignment)
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new SelectableTextBlock
			{
				Width = 200,
				Text = "Aligned text",
				TextAlignment = textAlignment
			};

			var root = new TestRoot(target)
			{
				ClientSize = new Size(300, 100)
			};

			root.Measure(root.ClientSize);
			root.Arrange(new Rect(root.ClientSize));
			root.ExecuteInitialLayoutPass();

			var firstCharacterBounds = target.TextLayout.HitTestTextPosition(0);
			var lastCharacterBounds = target.TextLayout.HitTestTextPosition(target.Text!.Length - 1);
			var mouse = new MouseTestHelper();
			var startPoint = new Point(
				firstCharacterBounds.X + (firstCharacterBounds.Width / 2),
				firstCharacterBounds.Y + (firstCharacterBounds.Height / 2));
			var endPoint = new Point(
				Math.Min(target.Bounds.Width - 1, lastCharacterBounds.Right + 10),
				lastCharacterBounds.Y + (lastCharacterBounds.Height / 2));

			mouse.Down(target, position: target.TranslatePoint(startPoint, root));
			mouse.Move(target, target.TranslatePoint(endPoint, root).GetValueOrDefault());

			CornerstoneTest.AreEqual(target.Text!.Length, Math.Max(target.SelectionStart, target.SelectionEnd));
		}
	}

	[PresentationTestMethod]
	public async Task Pointer_Selection_Is_Published_To_Primary_Selection()
	{
		using (UnitTestApplication.Start(TextBoxTests.CreatePrimarySelectionServices()))
		{
			var target = new SelectableTextBlock { Text = "0123" };
			var window = new Window { Content = target };
			window.Show();

			var mouse = new MouseTestHelper();
			mouse.Down(target, MouseButton.Left, new Point(1, 300));
			mouse.Move(target, new Point(700, 300));
			mouse.Up(target, MouseButton.Left, new Point(700, 300));

			CornerstoneTest.AreEqual("0123", target.SelectedText);
			CornerstoneTest.AreEqual("0123", await window.TryGetClipboard(ClipboardType.PrimarySelection).TryGetTextAsync());
		}
	}

	[PresentationTestMethod]
	public void Right_Click_Below_Text_Should_Keep_Selection()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new SelectableTextBlock
			{
				Width = 200,
				Text = "first line\nsecond line\n"
			};

			var root = new TestRoot(target)
			{
				ClientSize = new Size(300, 200)
			};

			root.Measure(root.ClientSize);
			root.Arrange(new Rect(root.ClientSize));
			root.ExecuteInitialLayoutPass();

			var mouse = new MouseTestHelper();
			var firstCharacterBounds = target.TextLayout.HitTestTextPosition(0);
			var start = target.TranslatePoint(new Point(
				firstCharacterBounds.X + 1,
				firstCharacterBounds.Y + firstCharacterBounds.Height / 2), root);
			var belowText = target.TranslatePoint(new Point(
				target.TextLayout.Width / 2,
				target.TextLayout.Height + 10), root);

			mouse.Down(target, MouseButton.Left, start);
			mouse.Move(target, belowText.GetValueOrDefault());
			mouse.Up(target, MouseButton.Left, belowText);

			mouse.Down(target, MouseButton.Right, belowText);
			mouse.Up(target, MouseButton.Right, belowText);

			CornerstoneTest.AreEqual(0, target.SelectionStart);
			CornerstoneTest.AreEqual(target.Text.Length, target.SelectionEnd);
			CornerstoneTest.IsTrue(target.CanCopy);
		}
	}

	[PresentationTestMethod]
	public void Right_Click_On_Unselected_Text_Should_Move_Selection()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new SelectableTextBlock
			{
				Width = 200,
				Text = "first line\nsecond line"
			};

			var root = new TestRoot(target)
			{
				ClientSize = new Size(300, 200)
			};

			root.Measure(root.ClientSize);
			root.Arrange(new Rect(root.ClientSize));
			root.ExecuteInitialLayoutPass();

			target.SelectionStart = 0;
			target.SelectionEnd = 5;

			var characterBounds = target.TextLayout.HitTestTextPosition(14);
			var onUnselectedText = target.TranslatePoint(characterBounds.Center, root);
			var mouse = new MouseTestHelper();

			mouse.Down(target, MouseButton.Right, onUnselectedText);
			mouse.Up(target, MouseButton.Right, onUnselectedText);

			CornerstoneTest.AreEqual(target.SelectionStart, target.SelectionEnd);
			CornerstoneTest.IsFalse(target.CanCopy);
		}
	}

	[PresentationTestMethod]
	public void InlinesChangesShouldUpdateSelection()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new SelectableTextBlock();
			target.Inlines!.Add(new Run("foo"));
			target.SelectionEnd = 3;

			var selectedTextChanged = false;
			target.PropertyChanged += (_, e) =>
			{
				if (e.Property == SelectableTextBlock.SelectedTextProperty)
				{
					selectedTextChanged = true;
				}
			};

			target.Inlines.Add(new Run("bar"));

			CornerstoneTest.IsTrue(selectedTextChanged);

			target.SelectionStart = 6;
			target.SelectionEnd = 0;
			target.Inlines.RemoveAt(1);

			CornerstoneTest.AreEqual(3, target.SelectionStart);
			CornerstoneTest.AreEqual(0, target.SelectionEnd);

			target.SelectionStart = 0;
			target.SelectionEnd = 3;

			target.Inlines[0] = new Run("a");

			CornerstoneTest.AreEqual(0, target.SelectionStart);
			CornerstoneTest.AreEqual(1, target.SelectionEnd);
			CornerstoneTest.AreEqual("a", target.SelectedText);
		}
	}

	[PresentationTestMethod]
	public void SelectionForegroundShouldNotResetRunTypefaceAndStyle()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new SelectableTextBlock
			{
				SelectionForegroundBrush = Brushes.Red
			};

			var run = new Run("Hello")
			{
				FontWeight = FontWeight.Bold,
				FontStyle = FontStyle.Italic,
				FontSize = 20
			};

			target.Inlines!.Add(run);

			target.Measure(Size.Infinity);

			target.SelectionStart = 0;
			target.SelectionEnd = run.Text!.Length;

			target.Measure(Size.Infinity);

			var textLayout = target.TextLayout;
			CornerstoneTest.IsNotNull(textLayout);

			var textRuns = textLayout.TextLines
				.SelectMany(l => l.TextRuns)
				.OfType<ShapedTextRun>()
				.ToList();

			CornerstoneTest.NotEmpty(textRuns);

			var selectedRun = textRuns[0];
			var props = selectedRun.Properties;

			CornerstoneTest.AreEqual(FontWeight.Bold, props.Typeface.Weight);
			CornerstoneTest.AreEqual(FontStyle.Italic, props.Typeface.Style);

			CornerstoneTest.Same(target.SelectionForegroundBrush, props.ForegroundBrush);
		}
	}

	// Content: Run("foo") + InlineUIContainer + Run("bar")
	// Inlines.Text after fix: "foo\uFFFCbar" (indices 0-6)
	//   0='f', 1='o', 2='o', 3='\uFFFC' (embedded control), 4='b', 5='a', 6='r'
	[PresentationTestMethod]

	// Entirely before InlineUIContainer
	[DataRow(0, 3, "foo")]

	// Exactly the InlineUIContainer character
	[DataRow(3, 4, "\uFFFC")]

	// Up to and including InlineUIContainer (fencepost: last char before "bar")
	[DataRow(0, 4, "foo\uFFFC")]

	// Starting exactly after InlineUIContainer (fencepost: first char of "bar")
	[DataRow(4, 7, "bar")]

	// InlineUIContainer through end
	[DataRow(3, 7, "\uFFFCbar")]

	// Spanning InlineUIContainer (one char either side)
	[DataRow(2, 5, "o\uFFFCb")]

	// Entire content
	[DataRow(0, 7, "foo\uFFFCbar")]
	public void SelectionWithInlineUIContainerReturnsCorrectText(int start, int end, string expected)
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new SelectableTextBlock();

			target.Inlines!.Add(new Run("foo"));
			target.Inlines!.Add(new InlineUIContainer(new Border()));
			target.Inlines!.Add(new Run("bar"));

			target.Measure(Size.Infinity);

			// SelectionStart/End values correspond to TextLayout character positions.
			// EmbeddedControlRun occupies 1 position (TextRun.DefaultTextSourceLength),
			// and Inlines.Text now has a matching U+FFFC placeholder, so they stay in sync.
			target.SelectionStart = start;
			target.SelectionEnd = end;

			CornerstoneTest.AreEqual(expected, target.SelectedText);
		}
	}

	[PresentationTestMethod]
	public void TextChangesShouldUpdateSelection()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var target = new SelectableTextBlock
			{
				Text = "foo",
				SelectionEnd = 3
			};

			var selectedTextChanged = false;
			target.PropertyChanged += (_, e) =>
			{
				if (e.Property == SelectableTextBlock.SelectedTextProperty)
				{
					selectedTextChanged = true;
				}
			};

			target.Text = "a";

			CornerstoneTest.AreEqual(0, target.SelectionStart);
			CornerstoneTest.AreEqual(1, target.SelectionEnd);
			CornerstoneTest.IsTrue(selectedTextChanged);
		}
	}

	private static SelectableTextBlock CreateSelectableTextBlockInTopLevel(IClipboardImpl clipboardImpl)
	{
		var target = new SelectableTextBlock
		{
			Text = "abcd",
			SelectionStart = 1,
			SelectionEnd = 3
		};

		var impl = new StubWindowImpl();
		impl.SetFeature(typeof(IClipboard), new Clipboard(clipboardImpl));

		var topLevel = new TestTopLevel(impl)
		{
			Template = new FuncControlTemplate<TestTopLevel>((x, scope) =>
				new ContentPresenter
				{
					Name = "PART_ContentPresenter",
					[!ContentPresenter.ContentProperty] = x[!ContentControl.ContentProperty]
				}.RegisterInNameScope(scope)),
			Content = target
		};

		topLevel.ApplyTemplate();
		topLevel.LayoutManager.ExecuteInitialLayoutPass();

		CornerstoneTest.IsTrue(target.CanCopy);

		return target;
	}

	#endregion

	#region Classes

	private class TestTopLevel(ITopLevelImpl impl) : TopLevel(impl);

	#endregion
}