#region References

// ReSharper disable RedundantArgumentDefaultValue
using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Overlays;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.GestureRecognizers;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Input;

[TestClass]
public class GesturesTests
{
	#region Fields

	private readonly MouseTestHelper _mouse = new();

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void CancelledHoldGestureShouldCancelContextFlyout()
	{
		using var scope = PresentationLocator.EnterScope();
		var iSettingsMock = new StubPlatformSettings();
		iSettingsMock.SetHoldWaitDuration(TimeSpan.FromMilliseconds(300));
		iSettingsMock.SetTapSize(new Size(16, 16));
		PresentationLocator.CurrentMutable.BindToSelf(this)
			.Bind<IPlatformSettings>().ToConstant(iSettingsMock);

		using var app = UnitTestApplication.Start();

		var flyout = new Flyout();
		var border = new Border
		{
			ContextFlyout = flyout
		};
		InputElement.SetIsHoldWithMouseEnabled(border, true);
		var root = new TestRoot
		{
			Child = border
		};

		var contextRequested = false;
		var contextCanceled = false;

		flyout.Opened += (s, e) => contextRequested = true;
		flyout.Closed += (s, e) => contextCanceled = true;

		_mouse.Down(border);
		var timer = CornerstoneTest.Single(Dispatcher.SnapshotTimersForUnitTests());
		timer.ForceFire();
		_mouse.Move(border, new Point(100, 100));

		CornerstoneTest.IsTrue(contextRequested);
		CornerstoneTest.IsTrue(contextCanceled);
	}

	[PresentationTestMethod]
	public void DoubleTappedShouldBeRaisedEvenWhenPressedReleasedHandled()
	{
		var border = new Border();
		var root = new TestRoot
		{
			Child = border
		};
		var result = new List<string>();

		AddHandlers(root, border, result, true);

		_mouse.Click(border);
		_mouse.Down(border, clickCount: 2);

		CornerstoneTest.AreEqual(new[] { "bp", "dp", "br", "dr", "bt", "dt", "bp", "dp", "bdt", "ddt" }, result);
	}

	[PresentationTestMethod]
	public void DoubleTappedShouldFollowPointerPressedReleasedPressed()
	{
		var border = new Border();
		var root = new TestRoot
		{
			Child = border
		};
		var result = new List<string>();

		AddHandlers(root, border, result, false);

		_mouse.Click(border);
		_mouse.Down(border, clickCount: 2);

		CornerstoneTest.AreEqual(new[] { "bp", "dp", "br", "dr", "bt", "dt", "bp", "dp", "bdt", "ddt" }, result);
	}

	[PresentationTestMethod]
	public void DoubleTappedShouldNotBeRaisedForMiddleButton()
	{
		var border = new Border();
		var root = new TestRoot
		{
			Child = border
		};
		var raised = false;

		root.AddHandler(InputElement.DoubleTappedEvent, (_, _) => raised = true);

		_mouse.Click(border, MouseButton.Middle);
		_mouse.Down(border, MouseButton.Middle, clickCount: 2);

		CornerstoneTest.IsFalse(raised);
	}

	[PresentationTestMethod]
	public void DoubleTappedShouldNotBeRaisedForRightButton()
	{
		var border = new Border();
		var root = new TestRoot
		{
			Child = border
		};
		var raised = false;

		root.AddHandler(InputElement.DoubleTappedEvent, (_, _) => raised = true);

		_mouse.Click(border, MouseButton.Right);
		_mouse.Down(border, MouseButton.Right, clickCount: 2);

		CornerstoneTest.IsFalse(raised);
	}

	[PresentationTestMethod]
	public void GesturesShouldBeCancelledWhenPointerCaptureIsLost()
	{
		var border = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Colors.Red)
		};
		border.GestureRecognizers.Add(new PinchGestureRecognizer());
		var root = new TestRoot
		{
			Child = border
		};
		var raised = false;

		root.AddHandler(InputElement.PinchEvent, (_, _) => raised = true);

		var firstPoint = new Point(5, 5);
		var secondPoint = new Point(10, 10);

		var firstTouch = new TouchTestHelper();
		var secondTouch = new TouchTestHelper();

		firstTouch.Down(border, firstPoint);

		firstTouch.Cancel();

		secondTouch.Down(border, secondPoint);
		secondTouch.Move(border, new Point(20, 20));

		CornerstoneTest.IsFalse(raised);
	}

	[PresentationTestMethod]
	public void HoldShouldBeCancelledWhenPointerMovesTooFar()
	{
		using var scope = PresentationLocator.EnterScope();
		var iSettingsMock = new StubPlatformSettings();
		iSettingsMock.SetHoldWaitDuration(TimeSpan.FromMilliseconds(300));
		iSettingsMock.SetTapSize(new Size(16, 16));
		PresentationLocator.CurrentMutable.BindToSelf(this)
			.Bind<IPlatformSettings>().ToConstant(iSettingsMock);

		using var app = UnitTestApplication.Start();

		var border = new Border();
		InputElement.SetIsHoldWithMouseEnabled(border, true);
		var root = new TestRoot
		{
			Child = border
		};
		var cancelled = false;

		root.AddHandler(InputElement.HoldingEvent, (_, e) => cancelled = e.HoldingState == HoldingState.Canceled);

		_mouse.Down(border);

		var timer = CornerstoneTest.Single(Dispatcher.SnapshotTimersForUnitTests());
		CornerstoneTest.AreEqual(iSettingsMock.HoldWaitDuration, timer.Interval);
		timer.ForceFire();

		_mouse.Move(border, new Point(3, 3));

		CornerstoneTest.IsFalse(cancelled);

		_mouse.Move(border, new Point(20, 20));

		CornerstoneTest.IsTrue(cancelled);
	}

	[PresentationTestMethod]
	public void HoldShouldBeCancelledWhenSecondContactIsDetected()
	{
		using var scope = PresentationLocator.EnterScope();
		var iSettingsMock = new StubPlatformSettings();
		iSettingsMock.SetHoldWaitDuration(TimeSpan.FromMilliseconds(300));
		PresentationLocator.CurrentMutable.BindToSelf(this)
			.Bind<IPlatformSettings>().ToConstant(iSettingsMock);

		using var app = UnitTestApplication.Start();

		var border = new Border();
		InputElement.SetIsHoldWithMouseEnabled(border, true);
		var root = new TestRoot
		{
			Child = border
		};
		var cancelled = false;

		root.AddHandler(InputElement.HoldingEvent, (_, e) => cancelled = e.HoldingState == HoldingState.Canceled);

		_mouse.Down(border);
		CornerstoneTest.IsFalse(cancelled);

		var timer = CornerstoneTest.Single(Dispatcher.SnapshotTimersForUnitTests());
		CornerstoneTest.AreEqual(iSettingsMock.HoldWaitDuration, timer.Interval);
		timer.ForceFire();

		var secondMouse = new MouseTestHelper();

		secondMouse.Down(border);

		CornerstoneTest.IsTrue(cancelled);
	}

	[PresentationTestMethod]
	public void HoldShouldBeRaisedAfterHoldDuration()
	{
		using var scope = PresentationLocator.EnterScope();
		var iSettingsMock = new StubPlatformSettings();
		iSettingsMock.SetHoldWaitDuration(TimeSpan.FromMilliseconds(300));
		iSettingsMock.SetTapSize(new Size(16, 16));
		PresentationLocator.CurrentMutable.BindToSelf(this)
			.Bind<IPlatformSettings>().ToConstant(iSettingsMock);

		using var app = UnitTestApplication.Start();

		var border = new Border();
		InputElement.SetIsHoldWithMouseEnabled(border, true);
		var root = new TestRoot
		{
			Child = border
		};
		var holding = HoldingState.Canceled;

		root.AddHandler(InputElement.HoldingEvent, (_, e) => holding = e.HoldingState);

		_mouse.Down(border);
		CornerstoneTest.IsFalse(holding != HoldingState.Canceled);

		// Verify timer duration, but execute it immediately.
		var timer = CornerstoneTest.Single(Dispatcher.SnapshotTimersForUnitTests());
		CornerstoneTest.AreEqual(iSettingsMock.HoldWaitDuration, timer.Interval);
		timer.ForceFire();

		CornerstoneTest.IsTrue(holding == HoldingState.Started);

		_mouse.Up(border);

		CornerstoneTest.IsTrue(holding == HoldingState.Completed);
	}

	[PresentationTestMethod]
	public void HoldShouldNotBeRaisedForMultipleContacts()
	{
		using var scope = PresentationLocator.EnterScope();
		var iSettingsMock = new StubPlatformSettings();
		iSettingsMock.SetHoldWaitDuration(TimeSpan.FromMilliseconds(300));
		PresentationLocator.CurrentMutable.BindToSelf(this)
			.Bind<IPlatformSettings>().ToConstant(iSettingsMock);

		using var app = UnitTestApplication.Start();

		var border = new Border();
		InputElement.SetIsHoldWithMouseEnabled(border, true);
		var testRoot = new TestRoot
		{
			Child = border
		};
		var raised = false;

		testRoot.AddHandler(InputElement.HoldingEvent, (_, e) => raised = e.HoldingState == HoldingState.Completed);

		var secondMouse = new MouseTestHelper();

		_mouse.Down(border, MouseButton.Left);

		// Verify timer duration, but execute it immediately.
		var timer = CornerstoneTest.Single(Dispatcher.SnapshotTimersForUnitTests());
		CornerstoneTest.AreEqual(iSettingsMock.HoldWaitDuration, timer.Interval);
		timer.ForceFire();

		secondMouse.Down(border, MouseButton.Left);

		CornerstoneTest.IsFalse(raised);
	}

	[PresentationTestMethod]
	public void HoldShouldNotRaisedWhenPointerIsMovedBeforeTimer()
	{
		using var scope = PresentationLocator.EnterScope();
		var iSettingsMock = new StubPlatformSettings();
		iSettingsMock.SetHoldWaitDuration(TimeSpan.FromMilliseconds(300));
		PresentationLocator.CurrentMutable.BindToSelf(this)
			.Bind<IPlatformSettings>().ToConstant(iSettingsMock);

		using var app = UnitTestApplication.Start();

		var border = new Border();
		InputElement.SetIsHoldWithMouseEnabled(border, true);
		var root = new TestRoot
		{
			Child = border
		};
		var raised = false;

		root.AddHandler(InputElement.HoldingEvent, (_, e) => raised = e.HoldingState == HoldingState.Completed);

		_mouse.Down(border);
		CornerstoneTest.IsFalse(raised);

		_mouse.Move(border, new Point(20, 20));
		CornerstoneTest.IsFalse(raised);

		// Verify timer duration, but execute it immediately.
		var timer = CornerstoneTest.Single(Dispatcher.SnapshotTimersForUnitTests());
		CornerstoneTest.AreEqual(iSettingsMock.HoldWaitDuration, timer.Interval);
		timer.ForceFire();

		CornerstoneTest.IsFalse(raised);
	}

	[PresentationTestMethod]
	public void HoldShouldNotRaisedWhenPointerReleasedBeforeTimer()
	{
		using var scope = PresentationLocator.EnterScope();
		var iSettingsMock = new StubPlatformSettings();
		iSettingsMock.SetHoldWaitDuration(TimeSpan.FromMilliseconds(300));
		PresentationLocator.CurrentMutable.BindToSelf(this)
			.Bind<IPlatformSettings>().ToConstant(iSettingsMock);

		using var app = UnitTestApplication.Start();

		var border = new Border();
		InputElement.SetIsHoldWithMouseEnabled(border, true);
		var root = new TestRoot
		{
			Child = border
		};
		var raised = false;

		root.AddHandler(InputElement.HoldingEvent, (_, e) => raised = e.HoldingState == HoldingState.Started);

		_mouse.Down(border);
		CornerstoneTest.IsFalse(raised);

		_mouse.Up(border);
		CornerstoneTest.IsFalse(raised);

		// Verify timer duration, but execute it immediately.
		var timer = CornerstoneTest.Single(Dispatcher.SnapshotTimersForUnitTests());
		CornerstoneTest.AreEqual(iSettingsMock.HoldWaitDuration, timer.Interval);
		timer.ForceFire();

		CornerstoneTest.IsFalse(raised);
	}

	[PresentationTestMethod]
	public void PinchedShouldBeRaisedForTwoPointersMoving()
	{
		var border = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Colors.Red)
		};
		border.GestureRecognizers.Add(new PinchGestureRecognizer());
		var root = new TestRoot
		{
			Child = border
		};
		var raised = false;

		root.AddHandler(InputElement.PinchEvent, (_, _) => raised = true);

		var firstPoint = new Point(5, 5);
		var secondPoint = new Point(10, 10);

		var firstTouch = new TouchTestHelper();
		var secondTouch = new TouchTestHelper();

		firstTouch.Down(border, firstPoint);
		secondTouch.Down(border, secondPoint);
		secondTouch.Move(border, new Point(20, 20));

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void PinchedShouldNotBeRaisedForSamePointer()
	{
		var touch = new TouchTestHelper();

		var border = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Colors.Red)
		};
		border.GestureRecognizers.Add(new PinchGestureRecognizer());
		var root = new TestRoot
		{
			Child = border
		};
		var raised = false;

		root.AddHandler(InputElement.PinchEvent, (_, _) => raised = true);

		var firstPoint = new Point(5, 5);
		var secondPoint = new Point(10, 10);

		touch.Down(border, firstPoint);
		touch.Down(border, secondPoint);
		touch.Down(border, new Point(20, 20));

		CornerstoneTest.IsFalse(raised);
	}

	[PresentationTestMethod]
	public void RightTappedShouldBeRaisedForRightButton()
	{
		var border = new Border();
		var root = new TestRoot
		{
			Child = border
		};
		var raised = false;

		root.AddHandler(InputElement.RightTappedEvent, (_, _) => raised = true);

		_mouse.Click(border, MouseButton.Right);

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void ScrollingShouldStartAfterStartDistanceIsExceeded()
	{
		var border = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Colors.Red)
		};
		border.GestureRecognizers.Add(new ScrollGestureRecognizer
		{
			CanHorizontallyScroll = true,
			CanVerticallyScroll = true,
			ScrollStartDistance = 50
		});
		var root = new TestRoot
		{
			Child = border
		};
		var raised = false;

		root.AddHandler(InputElement.ScrollGestureEvent, (_, _) => raised = true);

		var firstTouch = new TouchTestHelper();

		firstTouch.Down(border, new Point(5, 5));
		firstTouch.Move(border, new Point(20, 20));

		CornerstoneTest.IsFalse(raised);

		firstTouch.Move(border, new Point(70, 20));

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void StartedHoldGestureShouldRaiseContextRequestedEvent()
	{
		using var scope = PresentationLocator.EnterScope();
		var iSettingsMock = new StubPlatformSettings();
		iSettingsMock.SetHoldWaitDuration(TimeSpan.FromMilliseconds(300));
		iSettingsMock.SetTapSize(new Size(16, 16));
		PresentationLocator.CurrentMutable.BindToSelf(this)
			.Bind<IPlatformSettings>().ToConstant(iSettingsMock);

		using var app = UnitTestApplication.Start();

		var flyout = new Flyout();
		var border = new Border
		{
			ContextFlyout = flyout
		};
		InputElement.SetIsHoldWithMouseEnabled(border, true);
		var root = new TestRoot
		{
			Child = border
		};

		var contextRequested = false;

		flyout.Opened += (s, e) => contextRequested = true;

		_mouse.Down(border);
		var timer = CornerstoneTest.Single(Dispatcher.SnapshotTimersForUnitTests());
		timer.ForceFire();
		_mouse.Up(border);

		CornerstoneTest.IsTrue(contextRequested);
	}

	[PresentationTestMethod]
	public void TappedShouldBeRaisedEvenWhenPressedReleasedHandled()
	{
		var border = new Border();
		var root = new TestRoot
		{
			Child = border
		};
		var result = new List<string>();

		AddHandlers(root, border, result, true);

		_mouse.Click(border);

		CornerstoneTest.AreEqual(new[] { "bp", "dp", "br", "dr", "bt", "dt" }, result);
	}

	[PresentationTestMethod]
	public void TappedShouldBeRaisedFromCapturedControl()
	{
		var inner = new Border
		{
			Focusable = true,
			Name = "Inner"
		};
		var border = new Border
		{
			Focusable = true,
			Child = inner,
			Name = "Parent"
		};
		var root = new TestRoot
		{
			Child = border
		};
		var raised = false;

		border.PointerPressed += (s, e) => { e.Pointer.Capture(inner); };
		_mouse.Click(border, MouseButton.Left);

		root.AddHandler(InputElement.TappedEvent, (_, _) => raised = true);

		_mouse.Click(border, MouseButton.Left);

		CornerstoneTest.IsTrue(raised);
	}

	[PresentationTestMethod]
	public void TappedShouldFollowPointerPressedReleased()
	{
		var border = new Border();
		var root = new TestRoot
		{
			Child = border
		};
		var result = new List<string>();

		AddHandlers(root, border, result, false);

		_mouse.Click(border);

		CornerstoneTest.AreEqual(new[] { "bp", "dp", "br", "dr", "bt", "dt" }, result);
	}

	[PresentationTestMethod]
	public void TappedShouldNotBeRaisedForMiddleButton()
	{
		var border = new Border();
		var root = new TestRoot
		{
			Child = border
		};
		var raised = false;

		root.AddHandler(InputElement.TappedEvent, (_, _) => raised = true);

		_mouse.Click(border, MouseButton.Middle);

		CornerstoneTest.IsFalse(raised);
	}

	[PresentationTestMethod]
	public void TappedShouldNotBeRaisedForRightButton()
	{
		var border = new Border();
		var root = new TestRoot
		{
			Child = border
		};
		var raised = false;

		root.AddHandler(InputElement.TappedEvent, (_, _) => raised = true);

		_mouse.Click(border, MouseButton.Right);

		CornerstoneTest.IsFalse(raised);
	}

	private static void AddHandlers(
		TestRoot root,
		Border border,
		IList<string> result,
		bool markHandled)
	{
		root.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
		{
			result.Add("dp");

			if (markHandled)
			{
				e.Handled = true;
			}
		});

		root.AddHandler(InputElement.PointerReleasedEvent, (_, e) =>
		{
			result.Add("dr");

			if (markHandled)
			{
				e.Handled = true;
			}
		});

		border.AddHandler(InputElement.PointerPressedEvent, (_, _) => result.Add("bp"));
		border.AddHandler(InputElement.PointerReleasedEvent, (_, _) => result.Add("br"));

		root.AddHandler(InputElement.TappedEvent, (_, _) => result.Add("dt"));
		root.AddHandler(InputElement.DoubleTappedEvent, (_, _) => result.Add("ddt"));
		border.AddHandler(InputElement.TappedEvent, (_, _) => result.Add("bt"));
		border.AddHandler(InputElement.DoubleTappedEvent, (_, _) => result.Add("bdt"));
	}

	#endregion
}