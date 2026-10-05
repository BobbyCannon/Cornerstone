#region References

using System;
using System.Threading;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Raw;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Markup.Xaml.Templates;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public sealed class PresentationSourceTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ChromeHitTestPrefersContentOverUnderlay()
	{
		var underlay = new Border
		{
			Background = Brushes.Red,
			[WindowDecorationProperties.ElementRoleProperty] = WindowDecorationsElementRole.TitleBar
		};

		var content = new Border
		{
			Background = Brushes.Blue
		};

		DoChromeHitTest(
			underlay,
			content,
			null,
			content,
			null);
	}

	[PresentationTestMethod]
	public void ChromeHitTestPrefersOverlayOverContent()
	{
		var overlay = new Border
		{
			Background = Brushes.Red,
			[WindowDecorationProperties.ElementRoleProperty] = WindowDecorationsElementRole.TitleBar
		};

		var content = new Border
		{
			Background = Brushes.Blue
		};

		DoChromeHitTest(
			null,
			content,
			overlay,
			overlay,
			WindowDecorationsElementRole.TitleBar);
	}

	[PresentationTestMethod]
	public void ClosingShouldDetachPlatformInputHandler()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);
		var windowImpl = MockWindowingPlatform.CreateWindowMock();
		var window = new Window(windowImpl);

		CornerstoneTest.IsNotNull(windowImpl.Input);

		windowImpl.Closed!();

		CornerstoneTest.IsNull(windowImpl.Input);
	}

	[PresentationTestMethod]
	public void CursorShouldFollowCapturedElement()
	{
		using var app = UnitTestApplication.Start(
			TestServices.StyledWindow.With(inputManager: new InputManager()));

		var captured = new Border
		{
			Background = Brushes.Red,
			Width = 20,
			Cursor = new Cursor(StandardCursorType.SizeWestEast)
		};

		var other1 = new Border
		{
			Background = Brushes.Blue,
			Width = 100,
			Cursor = new Cursor(StandardCursorType.Ibeam)
		};

		var other2 = new Border
		{
			Background = Brushes.Blue,
			Width = 100,
			Cursor = new Cursor(StandardCursorType.Cross)
		};

		ICursorImpl currentCursor = null;
		var renderTimer = new CompositorTestServices.ManualRenderTimer();
		var compositor = RendererMocks.CreateDummyCompositor(renderTimer);
		var windowImpl = MockWindowingPlatform.CreateWindowMock(200, 100, compositor);
		windowImpl.SetCursorHandler = cursor => currentCursor = cursor;

		var window = new Window(windowImpl)
		{
			Content = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Children = { captured, other1, other2 }
			}
		};

		IPointer pointer = null;
		captured.PointerPressed += (_, e) =>
		{
			e.Pointer.Capture(captured);
			pointer = e.Pointer;
		};

		window.Show();
		Render(renderTimer);

		var mouse = new MouseDevice();
		var root = window.PresentationSource;

		// Press inside the first border: the pointer becomes captured and the cursor is its own.
		windowImpl.Input!(new RawPointerEventArgs(
			mouse, 1, root, RawPointerEventType.LeftButtonDown, new Point(10, 50),
			RawInputModifiers.LeftMouseButton));

		CornerstoneTest.IsNotNull(pointer);
		CornerstoneTest.Same(captured, pointer.Captured);
		CornerstoneTest.Same(captured.Cursor!.PlatformImpl, currentCursor);
		var cursorWhileCaptured = currentCursor;

		// Drag over the other border. With the pointer still captured by the first border,
		// PresentationSource.PointerOverElement changes (it becomes null), but the displayed
		// cursor must keep coming from the captured element rather than following the new
		// PointerOverElement.
		windowImpl.Input!(new RawPointerEventArgs(
			mouse, 2, root, RawPointerEventType.Move, new Point(70, 50),
			RawInputModifiers.LeftMouseButton));

		CornerstoneTest.Same(captured, pointer.Captured);
		CornerstoneTest.Same(cursorWhileCaptured, currentCursor);

		// Changing the captured element's cursor should still work.
		var newCursor = new Cursor(StandardCursorType.Hand);
		captured.Cursor = newCursor;
		CornerstoneTest.Same(newCursor.PlatformImpl, currentCursor);

		// Changing the capture explicitly should update the cursor.
		pointer.Capture(other1);
		CornerstoneTest.Same(other1.Cursor!.PlatformImpl, currentCursor);

		// Move the pointer to an unrelated element and release the capture:
		// it should reset the cursor to match that new element.
		((IInputRoot) root).PointerOverElement = other2;

		windowImpl.Input!(new RawPointerEventArgs(
			mouse, 2, root, RawPointerEventType.Move, new Point(120, 50),
			RawInputModifiers.LeftMouseButton));

		pointer.Capture(null);
		CornerstoneTest.Same(other2.Cursor!.PlatformImpl, currentCursor);
	}

	private static ControlTheme CreateDecorationsTheme(WindowDrawnDecorationsContent content)
	{
		var template = new WindowDrawnDecorationsTemplate
		{
			Content = (IServiceProvider _) => new TemplateResult<WindowDrawnDecorationsContent>(content, new NameScope())
		};

		return new ControlTheme(typeof(WindowDrawnDecorations))
		{
			Setters =
			{
				new Setter(WindowDrawnDecorations.TemplateProperty, template)
			}
		};
	}

	private static void DoChromeHitTest(
		Control underlay,
		Control content,
		Control overlay,
		Visual expectedChromeVisual,
		WindowDecorationsElementRole? expectedRole)
	{
		const double width = 100;
		const double height = 100;

		if (underlay is not null)
		{
			underlay.Width = width;
			underlay.Height = height;
		}

		if (content is not null)
		{
			content.Width = width;
			content.Height = height;
		}

		if (overlay is not null)
		{
			overlay.Width = width;
			overlay.Height = height;
		}

		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var decorations = new WindowDrawnDecorationsContent
		{
			Underlay = underlay,
			Overlay = overlay
		};

		Application.Current!.Resources.Add(typeof(WindowDrawnDecorations), CreateDecorationsTheme(decorations));

		var renderTimer = new CompositorTestServices.ManualRenderTimer();
		var compositor = RendererMocks.CreateDummyCompositor(renderTimer);
		var windowImpl = MockWindowingPlatform.CreateWindowMock(width, height, compositor);
		windowImpl.IsClientAreaExtendedToDecorations = true;
		windowImpl.RequestedDrawnDecorations = PlatformRequestedDrawnDecoration.TitleBar;
		windowImpl.NeedsManagedDecorations = true;

		var window = new Window(windowImpl)
		{
			WindowDecorations = WindowDecorations.Full,
			ExtendClientAreaToDecorationsHint = true,
			Content = content
		};

		window.Show();
		Render(renderTimer);

		var hitTestPoint = new Point(width / 2, height / 2);

		var clientVisual = window.GetVisualAt(hitTestPoint);
		CornerstoneTest.Same(window.Content, clientVisual);

		var chromeVisual = window.PresentationSource.RootVisual.GetVisualAt(hitTestPoint);
		CornerstoneTest.Same(expectedChromeVisual, chromeVisual);

		var chromeRole = ((IInputRoot) window.PresentationSource).HitTestChromeElement(hitTestPoint);
		CornerstoneTest.AreEqual(expectedRole, chromeRole);
	}

	private static void Render(CompositorTestServices.ManualRenderTimer renderTimer)
	{
		Dispatcher.CurrentDispatcher.RunJobs(null, CancellationToken.None);
		renderTimer.TriggerTick();
		Dispatcher.CurrentDispatcher.RunJobs(null, CancellationToken.None);
	}

	#endregion
}