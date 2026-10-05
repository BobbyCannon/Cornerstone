#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Raw;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Input;

[TestClass]
public class PointerOverTests : PointerTestsBase
{
	#region Methods

	// https://github.com/AvaloniaUI/Avalonia/issues/2821
	[PresentationTestMethod]
	public void CloseShouldRemovePointerOver()
	{
		using var app = UnitTestApplication.Start(new TestServices(
			inputManager: new InputManager(),
			renderInterface: new HeadlessPlatformRenderInterface()));

		var renderer = new StubHitTester();
		var device = CreatePointerDeviceMock();
		var impl = CreateTopLevelImplMock();

		Canvas canvas;
		var root = CreateInputRoot(impl, new Panel
		{
			Children =
			{
				(canvas = new Canvas())
			}
		}, renderer);

		SetHit(renderer, canvas);
		impl.Input!(CreateRawPointerMovedArgs(device, root));

		CornerstoneTest.IsTrue(canvas.IsPointerOver);

		impl.Closed!();

		CornerstoneTest.IsFalse(canvas.IsPointerOver);
	}

	[PresentationTestMethod]
	public void DisabledElementShouldSetPointerOverOnVisualParent()
	{
		using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

		var renderer = new StubHitTester();
		var deviceMock = CreatePointerDeviceMock();
		var impl = CreateTopLevelImplMock();

		var disabledChild = new Border
		{
			Background = Brushes.Red,
			Width = 100,
			Height = 100,
			IsEnabled = false
		};

		var visualParent = new Border
		{
			Background = Brushes.Black,
			Width = 100,
			Height = 100,
			Child = disabledChild
		};

		var logicalParent = new Border
		{
			Background = Brushes.Blue,
			Width = 100,
			Height = 100
		};

		// Change the logical parent and check that we're correctly hit testing on the visual tree.
		// This scenario is made up because it's easy to test.
		// In the real world, this happens with nested Popups from MenuItems (but that's very cumbersome to test).
		((ISetLogicalParent) disabledChild).SetParent(null);
		((ISetLogicalParent) disabledChild).SetParent(logicalParent);

		var root = CreateInputRoot(
			impl,
			new Panel
			{
				Children = { visualParent }
			},
			renderer);

		CornerstoneTest.IsFalse(visualParent.IsPointerOver);
		SetHit(renderer, disabledChild);

		impl.Input!(CreateRawPointerMovedArgs(deviceMock, root, new Point(50, 50)));
		CornerstoneTest.IsTrue(visualParent.IsPointerOver);
		CornerstoneTest.IsFalse(logicalParent.IsPointerOver);
	}

	[PresentationTestMethod]
	public void HitTestShouldIgnoreNonCapturedElements()
	{
		using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

		var renderer = new StubHitTester();
		var pointer = new StubPointer();
		var device = CreatePointerDeviceMock(pointer);
		var impl = CreateTopLevelImplMock();

		Canvas canvas;
		Border border;
		Decorator decorator;

		var root = CreateInputRoot(impl, new Panel
		{
			Children =
			{
				(canvas = new Canvas()),
				(border = new Border
				{
					Child = decorator = new Decorator()
				})
			}
		}, renderer);

		pointer.Captured = decorator;

		// Move the pointer over the canvas: the captured decorator should lose the pointer over state.
		SetHit(renderer, canvas);
		impl.Input!(CreateRawPointerMovedArgs(device, root));

		CornerstoneTest.IsFalse(decorator.IsPointerOver);
		CornerstoneTest.IsFalse(border.IsPointerOver);
		CornerstoneTest.IsFalse(canvas.IsPointerOver);
		CornerstoneTest.IsFalse(root.IsPointerOver);

		// Move back the pointer over the decorator: raise events normally for it since it's captured.
		SetHit(renderer, decorator);
		impl.Input!(CreateRawPointerMovedArgs(device, root));

		CornerstoneTest.IsTrue(decorator.IsPointerOver);
		CornerstoneTest.IsTrue(border.IsPointerOver);
		CornerstoneTest.IsFalse(canvas.IsPointerOver);
		CornerstoneTest.IsTrue(root.IsPointerOver);
	}

	[PresentationTestMethod]
	public void IsPointerOverShouldBeUpdatedWhenChildSetsHandledTrue()
	{
		using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

		var renderer = new StubHitTester();
		var device = CreatePointerDeviceMock();
		var impl = CreateTopLevelImplMock();

		Canvas canvas;
		Border border;
		Decorator decorator;

		var root = CreateInputRoot(impl, new Panel
		{
			Children =
			{
				(canvas = new Canvas()),
				(border = new Border
				{
					Child = decorator = new Decorator()
				})
			}
		}, renderer);

		SetHit(renderer, canvas);
		impl.Input!(CreateRawPointerMovedArgs(device, root));

		CornerstoneTest.IsFalse(decorator.IsPointerOver);
		CornerstoneTest.IsFalse(border.IsPointerOver);
		CornerstoneTest.IsTrue(canvas.IsPointerOver);
		CornerstoneTest.IsTrue(root.IsPointerOver);

		// Ensure that e.Handled is reset between controls.
		root.PointerMoved += (s, e) => e.Handled = true;
		decorator.PointerEntered += (s, e) => e.Handled = true;

		SetHit(renderer, decorator);
		impl.Input!(CreateRawPointerMovedArgs(device, root));

		CornerstoneTest.IsTrue(decorator.IsPointerOver);
		CornerstoneTest.IsTrue(border.IsPointerOver);
		CornerstoneTest.IsFalse(canvas.IsPointerOver);
		CornerstoneTest.IsTrue(root.IsPointerOver);
	}

	// https://github.com/AvaloniaUI/Avalonia/issues/7748
	[PresentationTestMethod]
	public void LeaveWindowShouldResetPointerOver()
	{
		using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

		var renderer = new StubHitTester();
		var deviceMock = CreatePointerDeviceMock();
		var impl = CreateTopLevelImplMock();

		var lastClientPosition = new Point(1, 5);
		var invalidateRect = new Rect(0, 0, 15, 15);
		var result = new List<(object, string, Point)>();

		void HandleEvent(object sender, PointerEventArgs e)
		{
			result.Add((sender, e.RoutedEvent!.Name, e.GetPosition(null)));
		}

		Canvas canvas;

		var root = CreateInputRoot(impl, new Panel
		{
			Children =
			{
				(canvas = new Canvas())
			}
		}, renderer);

		AddEnteredExitedHandlers(HandleEvent, root, canvas);

		// Init pointer over.
		SetHit(renderer, canvas);
		impl.Input!(CreateRawPointerMovedArgs(deviceMock, root, lastClientPosition));
		CornerstoneTest.IsTrue(canvas.IsPointerOver);

		// Send LeaveWindow.
		impl.Input!(new RawPointerEventArgs(deviceMock, 0, root.InputRoot, RawPointerEventType.LeaveWindow, new Point(), default));
		CornerstoneTest.IsFalse(canvas.IsPointerOver);

		CornerstoneTest.AreEqual(new[]
		{
			((object) canvas, nameof(InputElement.PointerEntered), lastClientPosition),
			(root, nameof(InputElement.PointerEntered), lastClientPosition),
			(canvas, nameof(InputElement.PointerExited), lastClientPosition),
			(root, nameof(InputElement.PointerExited), lastClientPosition)
		}, result);
	}

	[PresentationTestMethod]
	public void MouseMoveShouldUpdateIsPointerOver()
	{
		using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

		var renderer = new StubHitTester();
		var device = CreatePointerDeviceMock();
		var impl = CreateTopLevelImplMock();

		Canvas canvas;
		Border border;
		Decorator decorator;

		var root = CreateInputRoot(impl, new Panel
		{
			Children =
			{
				(canvas = new Canvas()),
				(border = new Border
				{
					Child = decorator = new Decorator()
				})
			}
		}, renderer);

		SetHit(renderer, decorator);
		impl.Input!(CreateRawPointerMovedArgs(device, root));

		CornerstoneTest.IsTrue(decorator.IsPointerOver);
		CornerstoneTest.IsTrue(border.IsPointerOver);
		CornerstoneTest.IsFalse(canvas.IsPointerOver);
		CornerstoneTest.IsTrue(root.IsPointerOver);

		SetHit(renderer, canvas);
		impl.Input!(CreateRawPointerMovedArgs(device, root));

		CornerstoneTest.IsFalse(decorator.IsPointerOver);
		CornerstoneTest.IsFalse(border.IsPointerOver);
		CornerstoneTest.IsTrue(canvas.IsPointerOver);
		CornerstoneTest.IsTrue(root.IsPointerOver);
	}

	[PresentationTestMethod]
	public void PointerEnterMoveLeaveShouldBeFollowed()
	{
		using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

		var renderer = new StubHitTester();
		var deviceMock = CreatePointerDeviceMock();
		var impl = CreateTopLevelImplMock();
		var result = new List<(object, string)>();

		void HandleEvent(object sender, PointerEventArgs e)
		{
			result.Add((sender, e.RoutedEvent!.Name));
		}

		Canvas canvas;
		Border border;
		Decorator decorator;

		var root = CreateInputRoot(impl, new Panel
		{
			Children =
			{
				(canvas = new Canvas()),
				(border = new Border
				{
					Child = decorator = new Decorator()
				})
			}
		}, renderer);

		AddEnteredExitedHandlers(HandleEvent, canvas, decorator);

		// Enter decorator
		SetHit(renderer, decorator);
		SetMove(deviceMock, root.InputRoot, decorator);
		impl.Input!(CreateRawPointerMovedArgs(deviceMock, root));

		// Leave decorator
		SetHit(renderer, canvas);
		SetMove(deviceMock, root.InputRoot, canvas);
		impl.Input!(CreateRawPointerMovedArgs(deviceMock, root));

		CornerstoneTest.AreEqual(new[]
		{
			((object) decorator, nameof(InputElement.PointerEntered)),
			(decorator, nameof(InputElement.PointerMoved)),
			(decorator, nameof(InputElement.PointerExited)),
			(canvas, nameof(InputElement.PointerEntered)),
			(canvas, nameof(InputElement.PointerMoved))
		}, result);
	}

	[PresentationTestMethod]
	public void PointerEnteredExitedShouldBeRaisedInCorrectOrder()
	{
		using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

		var renderer = new StubHitTester();
		var deviceMock = CreatePointerDeviceMock();
		var impl = CreateTopLevelImplMock();
		var result = new List<(object, string)>();

		void HandleEvent(object sender, PointerEventArgs e)
		{
			result.Add((sender, e.RoutedEvent!.Name));
		}

		Canvas canvas;
		Border border;
		Decorator decorator;

		var root = CreateInputRoot(impl, new Panel
		{
			Children =
			{
				(canvas = new Canvas()),
				(border = new Border
				{
					Child = decorator = new Decorator()
				})
			}
		}, renderer);

		SetHit(renderer, canvas);
		impl.Input!(CreateRawPointerMovedArgs(deviceMock, root));

		AddEnteredExitedHandlers(HandleEvent, root, canvas, border, decorator);

		SetHit(renderer, decorator);
		impl.Input!(CreateRawPointerMovedArgs(deviceMock, root));

		CornerstoneTest.AreEqual(new[]
		{
			((object) canvas, nameof(InputElement.PointerExited)),
			(decorator, nameof(InputElement.PointerEntered)),
			(border, nameof(InputElement.PointerEntered))
		}, result);
	}

	// https://github.com/AvaloniaUI/Avalonia/issues/7896
	[PresentationTestMethod]
	public void PointerEnteredExitedShouldSetCorrectPosition()
	{
		using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

		var expectedPosition = new Point(15, 15);
		var renderer = new StubHitTester();
		var deviceMock = CreatePointerDeviceMock();
		var impl = CreateTopLevelImplMock();
		var result = new List<(object, string, Point)>();

		void HandleEvent(object sender, PointerEventArgs e)
		{
			result.Add((sender, e.RoutedEvent!.Name, e.GetPosition(null)));
		}

		Canvas canvas;

		var root = CreateInputRoot(impl, new Panel
		{
			Children =
			{
				(canvas = new Canvas())
			}
		}, renderer);

		AddEnteredExitedHandlers(HandleEvent, root, canvas);

		SetHit(renderer, canvas);
		impl.Input!(CreateRawPointerMovedArgs(deviceMock, root, expectedPosition));

		SetHit(renderer, null);
		impl.Input!(CreateRawPointerMovedArgs(deviceMock, root, expectedPosition));

		CornerstoneTest.AreEqual(new[]
		{
			((object) canvas, nameof(InputElement.PointerEntered), expectedPosition),
			(root, nameof(InputElement.PointerEntered), expectedPosition),
			(canvas, nameof(InputElement.PointerExited), expectedPosition),
			(root, nameof(InputElement.PointerExited), expectedPosition)
		}, result);
	}

	[PresentationTestMethod]
	public void PointerOverInvalidationShouldUsePreviouslyCapturedElement()
	{
		using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

		var renderer = new StubHitTester();
		var deviceMock = CreatePointerDeviceMock();
		var impl = CreateTopLevelImplMock();

		var invalidateRect = new Rect(0, 0, 15, 15);

		Canvas canvas1, canvas2;

		var root = CreateInputRoot(impl, new Panel
		{
			Children =
			{
				(canvas1 = new Canvas()),
				(canvas2 = new Canvas())
			}
		}, renderer);

		canvas1.PointerMoved += (s, a) => a.Pointer.Capture(canvas1);

		// Let input know about latest device.
		SetHit(renderer, canvas1);
		impl.Input!(CreateRawPointerMovedArgs(deviceMock, root));
		CornerstoneTest.IsTrue(canvas1.IsPointerOver);
		CornerstoneTest.IsFalse(canvas2.IsPointerOver);

		SetHit(renderer, canvas2);
		RaiseSceneInvalidated(root);
		CornerstoneTest.IsFalse(canvas1.IsPointerOver);
		CornerstoneTest.IsTrue(canvas2.IsPointerOver);
	}

	[PresentationTestMethod]
	public void RenderInvalidationShouldAffectPointerOver()
	{
		using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

		var renderer = new StubHitTester();
		var deviceMock = CreatePointerDeviceMock();
		var impl = CreateTopLevelImplMock();

		var invalidateRect = new Rect(0, 0, 15, 15);
		var lastClientPosition = new Point(1, 5);

		var result = new List<(object, string, Point)>();

		void HandleEvent(object sender, PointerEventArgs e)
		{
			result.Add((sender, e.RoutedEvent!.Name, e.GetPosition(null)));
		}

		Canvas canvas;

		var root = (Window) CreateInputRoot(impl, new Panel
		{
			Children =
			{
				(canvas = new Canvas())
			}
		}, renderer);
		AddEnteredExitedHandlers(HandleEvent, root, canvas);

		// Let input know about latest device.
		SetHit(renderer, canvas);
		impl.Input!(CreateRawPointerMovedArgs(deviceMock, root, lastClientPosition));
		CornerstoneTest.IsTrue(canvas.IsPointerOver);

		SetHit(renderer, canvas);
		RaiseSceneInvalidated(root);
		CornerstoneTest.IsTrue(canvas.IsPointerOver);

		// Raise SceneInvalidated again, but now hide element from the hittest.
		SetHit(renderer, null);
		RaiseSceneInvalidated(root);
		CornerstoneTest.IsFalse(canvas.IsPointerOver);

		CornerstoneTest.AreEqual(new[]
		{
			((object) canvas, nameof(InputElement.PointerEntered), lastClientPosition),
			(root, nameof(InputElement.PointerEntered), lastClientPosition),
			(canvas, nameof(InputElement.PointerExited), lastClientPosition),
			(root, nameof(InputElement.PointerExited), lastClientPosition)
		}, result);
	}

	[PresentationTestMethod]
	public void TouchMoveShouldNotSetIsPointerOver()
	{
		using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

		var renderer = new StubHitTester();
		var device = CreatePointerDeviceMock(pointerType: PointerType.Touch);
		var impl = CreateTopLevelImplMock();

		Canvas canvas;

		var root = CreateInputRoot(impl, new Panel
		{
			Children =
			{
				(canvas = new Canvas())
			}
		}, renderer);

		SetHit(renderer, canvas);
		impl.Input!(CreateRawPointerMovedArgs(device, root));

		CornerstoneTest.IsFalse(canvas.IsPointerOver);
		CornerstoneTest.IsFalse(root.IsPointerOver);
	}

	private static void AddEnteredExitedHandlers(
		EventHandler<PointerEventArgs> handler,
		params IInputElement[] controls)
	{
		foreach (var c in controls)
		{
			c.PointerEntered += handler;
			c.PointerExited += handler;
			c.PointerMoved += handler;
		}
	}

	private void RaiseSceneInvalidated(TopLevel tl)
	{
		tl.Renderer.TriggerSceneInvalidatedForUnitTests(new Rect(0, 0, 10000, 10000));
	}

	#endregion
}