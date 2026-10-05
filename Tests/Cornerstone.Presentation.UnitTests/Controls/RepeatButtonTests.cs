#region References

using System;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MouseButton = Cornerstone.Presentation.Input.MouseButton;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class RepeatButtonTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void DeclaredPropertiesRoundTrip()
	{
		var target = new RepeatButton();

		CornerstoneTest.AreEqual(300, target.Delay);
		CornerstoneTest.AreEqual(100, target.Interval);

		target.Delay = 20;
		target.Interval = 5;

		CornerstoneTest.AreEqual(20, target.Delay);
		CornerstoneTest.AreEqual(5, target.Interval);
	}

	[PresentationTestMethod]
	public void DisablingTheButtonStopsTheRepeat()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);
		var target = new RepeatButton { Focusable = false, Delay = 321 };
		var window = new Window { Content = target };
		window.Show();

		Press(target);
		var timer = RepeatTimer(target);

		target.IsEnabled = false;

		CornerstoneTest.IsFalse(Dispatcher.SnapshotTimersForUnitTests().Contains(timer));
		CornerstoneTest.IsFalse(target.IsPressed);
	}

	[PresentationTestMethod]
	public void HoldRepeatsAfterDelayThenUsesInterval()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);
		var target = new RepeatButton
		{
			Delay = 321,
			Interval = 40
		};
		var clicks = 0;
		target.Click += (_, _) => clicks++;
		var window = new Window { Content = target };
		window.Show();

		Press(target);
		var timer = RepeatTimer(target);
		CornerstoneTest.AreEqual(TimeSpan.FromMilliseconds(321), timer.Interval);
		CornerstoneTest.AreEqual(0, clicks);

		timer.ForceFire();

		CornerstoneTest.AreEqual(1, clicks);
		CornerstoneTest.AreEqual(TimeSpan.FromMilliseconds(40), timer.Interval);

		target.Interval = 15;
		timer.ForceFire();

		CornerstoneTest.AreEqual(2, clicks);
		CornerstoneTest.AreEqual(TimeSpan.FromMilliseconds(15), timer.Interval);
	}

	[PresentationTestMethod]
	public void PointerReleaseStopsTheRepeat()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);
		var target = new RepeatButton { Delay = 321 };
		var window = new Window { Content = target };
		window.Show();

		var helper = new MouseTestHelper();
		helper.Down(target, MouseButton.Left, new Point(10, 10));
		var timer = RepeatTimer(target);

		helper.Up(target, MouseButton.Left, new Point(10, 10));

		CornerstoneTest.IsFalse(Dispatcher.SnapshotTimersForUnitTests().Contains(timer));
		CornerstoneTest.IsFalse(target.IsPressed);
	}

	[PresentationTestMethod]
	public void RightButtonDoesNotRepeat()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);
		var target = new RepeatButton { Delay = 321 };
		var window = new Window { Content = target };
		window.Show();

		new MouseTestHelper().Down(target, MouseButton.Right, new Point(10, 10));

		CornerstoneTest.IsFalse(Dispatcher.SnapshotTimersForUnitTests().Any(timer => timer.Interval == TimeSpan.FromMilliseconds(target.Delay)));
		CornerstoneTest.IsFalse(target.IsPressed);
	}

	[PresentationTestMethod]
	public void SpaceStartsRepeatAndKeyUpStopsIt()
	{
		using var app = UnitTestApplication.Start(TestServices.FocusableWindow);
		var target = new RepeatButton();
		var clicks = 0;
		target.Click += (_, _) => clicks++;
		var window = new Window { Content = target };
		window.Show();
		target.Focus();

		target.RaiseEvent(KeyEvent(InputElement.KeyDownEvent, Key.Space));

		CornerstoneTest.IsTrue(target.IsPressed);
		CornerstoneTest.AreEqual(0, clicks);
		var timer = CornerstoneTest.Single(Dispatcher.SnapshotTimersForUnitTests());

		timer.ForceFire();
		CornerstoneTest.AreEqual(1, clicks);

		target.RaiseEvent(KeyEvent(InputElement.KeyUpEvent, Key.Space));

		CornerstoneTest.AreEqual(0, Dispatcher.SnapshotTimersForUnitTests().Count);
		CornerstoneTest.IsFalse(target.IsPressed);
		CornerstoneTest.AreEqual(2, clicks);
	}

	private static KeyEventArgs KeyEvent(RoutedEvent routedEvent, Key key)
	{
		return new KeyEventArgs
		{
			RoutedEvent = routedEvent,
			Key = key
		};
	}

	private static void Press(RepeatButton target)
	{
		new MouseTestHelper().Down(target, MouseButton.Left, new Point(10, 10));
	}

	private static DispatcherTimer RepeatTimer(RepeatButton target)
	{
		var delay = TimeSpan.FromMilliseconds(target.Delay);
		return CornerstoneTest.Single(Dispatcher.SnapshotTimersForUnitTests().Where(timer => timer.Interval == delay));
	}

	#endregion
}