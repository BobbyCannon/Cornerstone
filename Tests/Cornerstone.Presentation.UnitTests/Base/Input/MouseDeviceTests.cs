#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Raw;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Input;

[TestClass]
public class MouseDeviceTests : PointerTestsBase
{
	#region Methods

	[PresentationTestMethod]
	public void CaptureIsTransferredToParentWhenControlRemoved()
	{
		using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

		var renderer = new StubHitTester();
		var device = new MouseDevice();
		var impl = CreateTopLevelImplMock();

		Canvas control;
		Panel rootChild;
		var root = CreateInputRoot(impl, rootChild = new Panel
		{
			Children =
			{
				(control = new Canvas())
			}
		}, renderer);

		// Synthesize event to receive a pointer.
		IPointer result = null;
		root.PointerMoved += (_, a) => { result = a.Pointer; };
		SetHit(renderer, control);
		impl.Input!(CreateRawPointerMovedArgs(device, root));

		CornerstoneTest.IsNotNull(result);

		result.Capture(control);
		CornerstoneTest.Same(control, result.Captured);

		rootChild.Children.Clear();

		CornerstoneTest.Same(rootChild, result.Captured);
	}

	[PresentationTestMethod]
	public void ControlShouldNotGainFocusOnMouseRelease()
	{
		using var scope = PresentationLocator.EnterScope();
		var settingsMock = new StubPlatformSettings();

		PresentationLocator.CurrentMutable.BindToSelf(this)
			.Bind<IPlatformSettings>().ToConstant(settingsMock);

		using var app = UnitTestApplication.Start(
			TestServices.RealFocus);

		var renderer = new StubHitTester();
		var impl = CreateTopLevelImplMock();

		var control1 = new Button
		{
			Focusable = true
		};

		var control2 = new Button
		{
			Focusable = true
		};
		var stack = new StackPanel
		{
			Children = { control1, control2 }
		};
		var root = CreateInputRoot(impl, stack, renderer);

		var device = new MouseDevice();

		var down = CreateRawPointerArgs(device, root, RawPointerEventType.LeftButtonDown);
		var up = CreateRawPointerArgs(device, root, RawPointerEventType.LeftButtonUp);

		SetHit(renderer, control1);

		CornerstoneTest.IsFalse(control1.IsFocused);

		impl.Input!(down);

		CornerstoneTest.IsTrue(control1.IsFocused);

		control2.Focus();

		impl.Input!(up);

		CornerstoneTest.IsFalse(control1.IsFocused);
	}

	[PresentationTestMethod]
	public void GetPositionShouldRespectControlRenderTransform()
	{
		using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

		var renderer = new StubHitTester();
		var device = new MouseDevice();
		var impl = CreateTopLevelImplMock();

		Border border;
		var root = CreateInputRoot(impl, new Panel
		{
			Children =
			{
				(border = new Border
				{
					Background = Brushes.Black,
					RenderTransform = new TranslateTransform(10, 0)
				})
			}
		}, renderer);

		Point? result = null;
		root.PointerMoved += (_, a) => { result = a.GetPosition(border); };

		SetHit(renderer, border);
		impl.Input!(CreateRawPointerMovedArgs(device, root, new Point(11, 11)));

		CornerstoneTest.AreEqual(new Point(1, 11), result);
	}

	[PresentationTestMethod]
	public void GetPositionShouldReturnDefaultWhenCrossTreeSourceClosed()
	{
		var topLevelOffset = new PixelPoint(5, 0);
		using (SetupCrossTreePositionRequest(topLevelOffset, out var pointerEvent, out var elementA, out var elementB))
		{
			((PresentationSource) elementA.PresentationSource!).Dispose();

			CornerstoneTest.AreEqual(default, pointerEvent.GetPosition(elementB));
		}
	}

	[PresentationTestMethod]
	public void GetPositionShouldReturnDefaultWhenCrossTreeTargetClosed()
	{
		var topLevelOffset = new PixelPoint(5, 0);
		using (SetupCrossTreePositionRequest(topLevelOffset, out var pointerEvent, out _, out var elementB))
		{
			((PresentationSource) elementB.PresentationSource!).Dispose();

			CornerstoneTest.AreEqual(default, pointerEvent.GetPosition(elementB));
		}
	}

	[PresentationTestMethod]
	public void GetPositionShouldSupportCrossTreeRequests()
	{
		var topLevelOffset = new PixelPoint(5, 0);
		using (SetupCrossTreePositionRequest(topLevelOffset, out var pointerEvent, out _, out var elementB))
		{
			CornerstoneTest.AreEqual(topLevelOffset.ToPoint(1), pointerEvent.GetPosition(elementB));
		}
	}

	[PresentationTestMethod]
	public void InitialButtonsAreNotSetWithoutCorrespondingMouseDown()
	{
		using var scope = PresentationLocator.EnterScope();
		var settingsMock = new StubPlatformSettings();

		PresentationLocator.CurrentMutable.BindToSelf(this)
			.Bind<IPlatformSettings>().ToConstant(settingsMock);

		using var app = UnitTestApplication.Start(
			new TestServices(
				inputManager: new InputManager()));

		var renderer = new StubHitTester();
		var device = new MouseDevice();
		var impl = CreateTopLevelImplMock();

		var control = new Control();
		var root = CreateInputRoot(impl, control, renderer);

		MouseButton button = default;

		root.PointerReleased += (s, e) => button = e.InitialPressMouseButton;

		var down = CreateRawPointerArgs(device, root, RawPointerEventType.LeftButtonDown);
		var up = CreateRawPointerArgs(device, root, RawPointerEventType.LeftButtonUp);

		SetHit(renderer, control);

		impl.Input!(up);

		CornerstoneTest.AreEqual(MouseButton.None, button);

		impl.Input!(down);
		impl.Input!(up);

		CornerstoneTest.AreEqual(MouseButton.Left, button);

		impl.Input!(up);

		CornerstoneTest.AreEqual(MouseButton.None, button);
	}

	[PresentationTestMethod]
	public void MousePointerShouldSetFocusOnPointerPressed()
	{
		using var scope = PresentationLocator.EnterScope();
		var settingsMock = new StubPlatformSettings();

		PresentationLocator.CurrentMutable.BindToSelf(this)
			.Bind<IPlatformSettings>().ToConstant(settingsMock);

		using var app = UnitTestApplication.Start(
			TestServices.RealFocus);

		var renderer = new StubHitTester();
		var impl = CreateTopLevelImplMock();

		var control = new Button
		{
			Focusable = true
		};
		var root = CreateInputRoot(impl, control, renderer);

		var device = new MouseDevice();

		var down = CreateRawPointerArgs(device, root, RawPointerEventType.LeftButtonDown);
		var up = CreateRawPointerArgs(device, root, RawPointerEventType.LeftButtonUp);

		SetHit(renderer, control);

		CornerstoneTest.IsFalse(control.IsFocused);

		impl.Input!(down);

		CornerstoneTest.IsTrue(control.IsFocused);
		impl.Input!(up);

		CornerstoneTest.IsTrue(control.IsFocused);
	}

	private IDisposable SetupCrossTreePositionRequest(PixelPoint topLevelPosition, out PointerEventArgs pointerEvent, out Control elementA, out Control elementB)
	{
		var app = UnitTestApplication.Start(new TestServices(
			inputManager: new InputManager(),
			renderInterface: new HeadlessPlatformRenderInterface()));

		var renderer = new StubHitTester();
		var deviceMock = CreatePointerDeviceMock();
		var impl1 = CreateTopLevelImplMock();

		// Mocked position: topLevelPosition
		impl1.PointToScreenHandler = p => PixelPoint.FromPoint(p, 1) + topLevelPosition;

		elementA = new Border();
		PointerEventArgs moveEventArgs = null;

		elementA.PointerMoved += (s, e) => moveEventArgs = e;
		var root1 = CreateInputRoot(impl1, elementA, renderer);

		SetMove(deviceMock, root1.InputRoot, elementA);
		impl1.Input!(CreateRawPointerMovedArgs(deviceMock, root1));

		CornerstoneTest.IsNotNull(moveEventArgs);
		pointerEvent = moveEventArgs;

		var impl2 = CreateTopLevelImplMock();

		elementB = new Border();
		var root2 = CreateInputRoot(impl2, elementB, renderer);

		return app;
	}

	#endregion
}