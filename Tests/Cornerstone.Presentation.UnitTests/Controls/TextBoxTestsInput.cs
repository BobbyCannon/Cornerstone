#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Harfbuzz;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Platform;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class TextBoxTestsInput : ScopedTestBase
{
	#region Properties

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
	public void TouchDoubleTapSelectsWord()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = "12 12345678"
			};

			var root = new TestRoot
			{
				Child = target
			};

			target.ApplyTemplate();

			root.LayoutManager.ExecuteInitialLayoutPass();

			var touch = new TouchTestHelper();

			CornerstoneTest.AreEqual(target.CaretIndex, 0);

			// Move to index 8
			touch.Down(target, new Point(50, 0));
			touch.Up(target, new Point(50, 0));

			// Double tap
			touch.Down(target, new Point(50, 0));
			touch.Up(target, new Point(50, 0));

			CornerstoneTest.AreEqual(target.SelectionStart, 3);
			CornerstoneTest.AreEqual(target.SelectionEnd, 11);
			CornerstoneTest.AreEqual(target.CaretIndex, 8);
		}
	}

	[PresentationTestMethod]
	public void TouchHoldOnSelectionRequestsContext()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = "12 12345678"
			};

			var root = new TestRoot
			{
				Child = target
			};

			target.ApplyTemplate();
			var requested = false;

			target.SelectionStart = 3;
			target.SelectionEnd = 11;

			target.ContextRequested += TargetContextRequested;

			root.LayoutManager.ExecuteInitialLayoutPass();

			var touch = new TouchTestHelper();

			CornerstoneTest.AreEqual(target.CaretIndex, 0);

			// Move to index 8
			touch.Down(target, new Point(50, 0));

			var timer = CornerstoneTest.Single(Dispatcher.SnapshotTimersForUnitTests());
			timer.ForceFire();
			touch.Up(target, new Point(50, 0));

			CornerstoneTest.IsTrue(requested);

			void TargetContextRequested(object sender, ContextRequestedEventArgs e)
			{
				requested = true;
			}
		}
	}

	[PresentationTestMethod]
	public void TouchHoldSelectsWord()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = "12 12345678"
			};

			var root = new TestRoot
			{
				Child = target
			};

			target.ApplyTemplate();

			root.LayoutManager.ExecuteInitialLayoutPass();

			var touch = new TouchTestHelper();

			CornerstoneTest.AreEqual(target.CaretIndex, 0);

			// Move to index 8
			touch.Down(target, new Point(50, 0));

			var timer = CornerstoneTest.Single(Dispatcher.SnapshotTimersForUnitTests());
			timer.ForceFire();
			touch.Up(target, new Point(50, 0));

			CornerstoneTest.AreEqual(target.SelectionStart, 3);
			CornerstoneTest.AreEqual(target.SelectionEnd, 11);
			CornerstoneTest.AreEqual(target.CaretIndex, 8);
		}
	}

	[PresentationTestMethod]
	public void TouchTapMovesCaret()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				Template = CreateTemplate(),
				Text = "12 12345678"
			};

			var root = new TestRoot
			{
				Child = target
			};

			target.ApplyTemplate();

			root.LayoutManager.ExecuteInitialLayoutPass();

			var touch = new TouchTestHelper();

			CornerstoneTest.AreEqual(target.CaretIndex, 0);

			// Move to index 8
			touch.Down(target, new Point(50, 0));
			touch.Up(target, new Point(50, 0));

			CornerstoneTest.AreEqual(target.CaretIndex, 8);
		}
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

	private static StubWindowImpl CreateMockTopLevelImpl()
	{
		var clipboard = new StubWindowImpl();
		clipboard.SetFeature(typeof(IClipboard), new Clipboard(new HeadlessClipboardImplStub()));
		return clipboard;
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

	private static void RaiseKeyEvent(TextBox textBox, Key key, KeyModifiers inputModifiers)
	{
		textBox.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			KeyModifiers = inputModifiers,
			Key = key
		});
	}

	private static void RaiseTextEvent(TextBox textBox, string text)
	{
		textBox.RaiseEvent(new TextInputEventArgs
		{
			RoutedEvent = InputElement.TextInputEvent,
			Text = text
		});
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