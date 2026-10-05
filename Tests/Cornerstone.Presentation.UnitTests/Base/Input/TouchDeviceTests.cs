#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Raw;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Input;

[TestClass]
public class TouchDeviceTests : PointerTestsBase
{
	#region Methods

	[PresentationTestMethod]
	public void ClickCountingShouldWorkCorrectlyWithFewTouchContacts()
	{
		using var app = UnitTestApp(new TimeSpan(200));

		var root = new TestRoot();
		var touchDevice = new TouchDevice();

		var pointerPressedExecutedTimes = 0;
		var tappedExecutedTimes = 0;
		var isDoubleTapped = false;
		var doubleTappedExecutedTimes = 0;
		root.PointerPressed += (a, e) =>
		{
			pointerPressedExecutedTimes++;
			switch (pointerPressedExecutedTimes)
			{
				case <= 2:
					CornerstoneTest.IsTrue(e.ClickCount == 1);
					break;
				case 3:
					CornerstoneTest.IsTrue(e.ClickCount == 2);
					break;
				case 4:
					CornerstoneTest.IsTrue(e.ClickCount == 3);
					break;
				case 5:
					CornerstoneTest.IsTrue(e.ClickCount == 4);
					break;
				case 6:
					CornerstoneTest.IsTrue(e.ClickCount == 5);
					break;
				case 7:
					CornerstoneTest.IsTrue(e.ClickCount == 1);
					break;
				case 8:
					CornerstoneTest.IsTrue(e.ClickCount == 1);
					break;
				case 9:
					CornerstoneTest.IsTrue(e.ClickCount == 2);
					break;
			}
		};
		root.DoubleTapped += (a, e) =>
		{
			isDoubleTapped = true;
			doubleTappedExecutedTimes++;
		};
		root.Tapped += (a, e) => { tappedExecutedTimes++; };
		var inputManager = InputManager.Instance!;
		SendXTouchContactsWithIds(inputManager, touchDevice, root, RawPointerEventType.TouchBegin, 0, 1);
		SendXTouchContactsWithIds(inputManager, touchDevice, root, RawPointerEventType.TouchEnd, 0, 1);
		TapOnce(inputManager, touchDevice, root, touchPointId: 2);
		TapOnce(inputManager, touchDevice, root, touchPointId: 3);
		TapOnce(inputManager, touchDevice, root, touchPointId: 4);
		SendXTouchContactsWithIds(inputManager, touchDevice, root, RawPointerEventType.TouchBegin, 5, 6, 7);
		SendXTouchContactsWithIds(inputManager, touchDevice, root, RawPointerEventType.TouchEnd, 5, 6, 7);
		TapOnce(inputManager, touchDevice, root, touchPointId: 8);
		CornerstoneTest.AreEqual(6, tappedExecutedTimes);
		CornerstoneTest.AreEqual(9, pointerPressedExecutedTimes);
		CornerstoneTest.IsTrue(isDoubleTapped);
		CornerstoneTest.AreEqual(3, doubleTappedExecutedTimes);
	}

	[PresentationTestMethod]
	public void DoubleTappedEventIsFiredWithTouch()
	{
		using var app = UnitTestApp(new TimeSpan(200));
		var root = new TestRoot();
		var touchDevice = new TouchDevice();

		var isDoubleTapped = false;
		var doubleTappedExecutedTimes = 0;
		var tappedExecutedTimes = 0;
		root.DoubleTapped += (a, e) =>
		{
			isDoubleTapped = true;
			doubleTappedExecutedTimes++;
		};
		root.Tapped += (a, e) => { tappedExecutedTimes++; };
		var inputManager = InputManager.Instance!;
		TapOnce(inputManager, touchDevice, root);
		TapOnce(inputManager, touchDevice, root, touchPointId: 1);
		CornerstoneTest.AreEqual(1, tappedExecutedTimes);
		CornerstoneTest.IsTrue(isDoubleTapped);
		CornerstoneTest.AreEqual(1, doubleTappedExecutedTimes);
	}

	[PresentationTestMethod]
	public void DoubleTappedNotFiredWhenClickTooLate()
	{
		using var app = UnitTestApp(new TimeSpan(0, 0, 0, 0, 20));
		var root = new TestRoot();
		var touchDevice = new TouchDevice();

		var isDoubleTapped = false;
		var doubleTappedExecutedTimes = 0;
		var tappedExecutedTimes = 0;
		root.DoubleTapped += (a, e) =>
		{
			isDoubleTapped = true;
			doubleTappedExecutedTimes++;
		};
		root.Tapped += (a, e) => { tappedExecutedTimes++; };
		var inputManager = InputManager.Instance!;
		TapOnce(inputManager, touchDevice, root);
		TapOnce(inputManager, touchDevice, root, 21, 1);
		CornerstoneTest.AreEqual(2, tappedExecutedTimes);
		CornerstoneTest.IsFalse(isDoubleTapped);
		CornerstoneTest.AreEqual(0, doubleTappedExecutedTimes);
	}

	[PresentationTestMethod]
	public void DoubleTappedNotFiredWhenSecondClickIsFromDifferentTouchContact()
	{
		using var app = UnitTestApp(new TimeSpan(200));
		var root = new TestRoot();
		var touchDevice = new TouchDevice();

		var isDoubleTapped = false;
		var doubleTappedExecutedTimes = 0;
		var tappedExecutedTimes = 0;
		root.DoubleTapped += (a, e) =>
		{
			isDoubleTapped = true;
			doubleTappedExecutedTimes++;
		};
		root.Tapped += (a, e) => { tappedExecutedTimes++; };
		var inputManager = InputManager.Instance!;
		SendXTouchContactsWithIds(inputManager, touchDevice, root, RawPointerEventType.TouchBegin, 0, 1);
		SendXTouchContactsWithIds(inputManager, touchDevice, root, RawPointerEventType.TouchEnd, 0, 1);
		CornerstoneTest.AreEqual(2, tappedExecutedTimes);
		CornerstoneTest.IsFalse(isDoubleTapped);
		CornerstoneTest.AreEqual(0, doubleTappedExecutedTimes);
	}

	[PresentationTestMethod]
	[DataRow(1)]
	[DataRow(2)]
	[DataRow(3)]
	[DataRow(4)]
	[DataRow(5)]
	public void PointerPressedCountsClicksCorrectly(int clickCount)
	{
		using var app = UnitTestApp(new TimeSpan(200));
		var root = new TestRoot();
		var touchDevice = new TouchDevice();

		var pointerPressedExecutedTimes = 0;
		var pointerPressedClicks = 0;
		root.PointerPressed += (a, e) =>
		{
			pointerPressedClicks = e.ClickCount;
			pointerPressedExecutedTimes++;
		};
		var inputManager = InputManager.Instance!;
		for (var i = 0; i < clickCount; i++)
		{
			TapOnce(inputManager, touchDevice, root, touchPointId: i);
		}

		CornerstoneTest.AreEqual(clickCount, pointerPressedExecutedTimes);
		CornerstoneTest.AreEqual(pointerPressedClicks, clickCount);
	}

	[PresentationTestMethod]
	public void TappedEventIsFiredWithTouch()
	{
		using var app = UnitTestApp(new TimeSpan(200));
		var root = new TestRoot();
		var touchDevice = new TouchDevice();

		var isTapped = false;
		var executedTimes = 0;
		root.Tapped += (a, e) =>
		{
			isTapped = true;
			executedTimes++;
		};
		TapOnce(InputManager.Instance!, touchDevice, root);
		CornerstoneTest.IsTrue(isTapped);
		CornerstoneTest.AreEqual(1, executedTimes);
	}

	[PresentationTestMethod]
	public void TouchPointerShouldSetFocusOnPointerReleased()
	{
		using var scope = PresentationLocator.EnterScope();
		using var app = UnitTestApplication.Start(
			TestServices.RealFocus);

		var impl = CreateTopLevelImplMock();

		var renderer = new StubHitTester();
		var root = new TestTopLevel(impl)
		{
			HitTesterOverride = renderer
		};
		var host = root.TopLevelHost;

		host.Focusable = true;
		var touchDevice = new TouchDevice();
		var inputManager = InputManager.Instance!;

		CornerstoneTest.IsFalse(host.IsFocused);

		Press(InputManager.Instance!, touchDevice, root.InputRoot);

		CornerstoneTest.IsFalse(host.IsFocused);
		Release(InputManager.Instance!, touchDevice, root.InputRoot);

		CornerstoneTest.IsTrue(host.IsFocused);
	}

	private static void Press(IInputManager inputManager, TouchDevice device, IInputRoot root, ulong timestamp = 0, long touchPointId = 0)
	{
		inputManager.ProcessInput(new RawPointerEventArgs(device, timestamp,
			root,
			RawPointerEventType.TouchBegin,
			new Point(0, 0),
			RawInputModifiers.None)
		{
			RawPointerId = touchPointId
		});
	}

	private static void Release(IInputManager inputManager, TouchDevice device, IInputRoot root, ulong timestamp = 0, long touchPointId = 0)
	{
		inputManager.ProcessInput(new RawPointerEventArgs(device, timestamp,
			root,
			RawPointerEventType.TouchEnd,
			new Point(0, 0),
			RawInputModifiers.None)
		{
			RawPointerId = touchPointId
		});
	}

	private static void SendXTouchContactsWithIds(IInputManager inputManager, TouchDevice device, IInputRoot root, RawPointerEventType type, params long[] touchPointIds)
	{
		for (var i = 0; i < touchPointIds.Length; i++)
		{
			inputManager.ProcessInput(new RawPointerEventArgs(device, 0,
				root,
				type,
				new Point(0, 0),
				RawInputModifiers.None)
			{
				RawPointerId = touchPointIds[i]
			});
		}
	}

	private static void TapOnce(IInputManager inputManager, TouchDevice device, IInputRoot root, ulong timestamp = 0, long touchPointId = 0)
	{
		Press(inputManager, device, root, timestamp, touchPointId);
		Release(inputManager, device, root, timestamp, touchPointId);
	}

	private IDisposable UnitTestApp(TimeSpan doubleClickTime = new())
	{
		var unitTestApp = UnitTestApplication.Start(
			new TestServices(inputManager: new InputManager()));
		var iSettingsMock = new StubPlatformSettings();
		iSettingsMock.SetDoubleTapTime(doubleClickTime);
		iSettingsMock.SetDoubleTapSize(new Size(16, 16));
		iSettingsMock.SetTapSize(new Size(16, 16));
		PresentationLocator.CurrentMutable.BindToSelf(this)
			.Bind<IPlatformSettings>().ToConstant(iSettingsMock);
		return unitTestApp;
	}

	#endregion

	#region Classes

	private class TestTopLevel(ITopLevelImpl impl) : TopLevel(impl)
	{
	}

	#endregion
}