#region References

using System;
using System.Diagnostics;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Controls.Utils;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MouseButton = Cornerstone.Presentation.Input.MouseButton;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class PressHoldButtonTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void CaptureLostStopsTheHold()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);
		var target = Show(new PressHoldButton());
		var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);

		Press(target);
		var timer = HoldTimer(target);

		target.RaiseEvent(new PointerCaptureLostEventArgs(target, pointer));

		CornerstoneTest.IsFalse(timer.IsEnabled);
		CornerstoneTest.AreEqual(0, target.Progress);
	}

	[PresentationTestMethod]
	public void CompletedHoldExecutesTheHoldCommand()
	{
		var clock = new MockGlobalClock();
		using var app = UnitTestApplication.Start(TestServices.StyledWindow.With(globalClock: clock));
		using var sync = UnitTestSynchronizationContext.Begin();
		var parameter = new object();
		object executed = null;
		var target = new PressHoldButton
		{
			HoldDuration = TimeSpan.FromMilliseconds(15),
			HoldCommandParameter = parameter,
			HoldCommand = new TestCommand(_ => true, value => executed = value),
			Template = IndicatorTemplate()
		};
		Show(target);

		var started = DateTime.UtcNow;
		Press(target);
		WaitUntil(started, target.HoldDuration + TimeSpan.FromMilliseconds(30));
		var timer = HoldTimer(target);
		timer.ForceFire();
		clock.Pulse(TimeSpan.Zero);
		clock.Pulse(TimeSpan.FromSeconds(1));
		sync.ExecutePostedCallbacks();
		Dispatcher.UIThread.RunJobs();

		CornerstoneTest.IsTrue(ReferenceEquals(parameter, executed));
		CornerstoneTest.AreEqual(0, target.Progress);
		CornerstoneTest.IsFalse(timer.IsEnabled);
	}

	[PresentationTestMethod]
	public void DeclaredPropertiesRoundTrip()
	{
		var target = new PressHoldButton();
		var background = Brushes.Red;
		var parameter = new object();
		var command = new TestCommand();

		CornerstoneTest.IsNull(target.HoldBackground);
		CornerstoneTest.IsNull(target.HoldCommand);
		CornerstoneTest.IsNull(target.HoldCommandParameter);
		CornerstoneTest.AreEqual(TimeSpan.FromSeconds(1), target.HoldDuration);
		CornerstoneTest.AreEqual(Orientation.Horizontal, target.Orientation);
		CornerstoneTest.AreEqual(0, target.Progress);
		CornerstoneTest.IsTrue(target.Classes.Contains(":horizontal"));
		CornerstoneTest.IsFalse(target.Classes.Contains(":vertical"));

		target.HoldBackground = background;
		target.HoldCommand = command;
		target.HoldCommandParameter = parameter;
		target.HoldDuration = TimeSpan.FromMilliseconds(250);
		target.Orientation = Orientation.Vertical;
		target.Progress = 0.25;

		CornerstoneTest.IsTrue(ReferenceEquals(background, target.HoldBackground));
		CornerstoneTest.IsTrue(ReferenceEquals(command, target.HoldCommand));
		CornerstoneTest.IsTrue(ReferenceEquals(parameter, target.HoldCommandParameter));
		CornerstoneTest.AreEqual(TimeSpan.FromMilliseconds(250), target.HoldDuration);
		CornerstoneTest.AreEqual(Orientation.Vertical, target.Orientation);
		CornerstoneTest.AreEqual(0.25, target.Progress);
		CornerstoneTest.IsTrue(target.Classes.Contains(":vertical"));
		CornerstoneTest.IsFalse(target.Classes.Contains(":horizontal"));
	}

	[PresentationTestMethod]
	public void HoldThatCannotExecuteDoesNotRun()
	{
		var clock = new MockGlobalClock();
		using var app = UnitTestApplication.Start(TestServices.StyledWindow.With(globalClock: clock));
		using var sync = UnitTestSynchronizationContext.Begin();
		var executed = false;
		var target = new PressHoldButton
		{
			HoldDuration = TimeSpan.FromMilliseconds(15),
			HoldCommand = new TestCommand(_ => false, _ => executed = true),
			Template = IndicatorTemplate()
		};
		Show(target);

		var started = DateTime.UtcNow;
		Press(target);
		WaitUntil(started, target.HoldDuration + TimeSpan.FromMilliseconds(30));
		HoldTimer(target).ForceFire();
		clock.Pulse(TimeSpan.Zero);
		clock.Pulse(TimeSpan.FromSeconds(1));
		sync.ExecutePostedCallbacks();
		Dispatcher.UIThread.RunJobs();

		CornerstoneTest.IsFalse(executed);
		CornerstoneTest.AreEqual(0, target.Progress);
	}

	[PresentationTestMethod]
	public void ReleaseBeforeTheDurationDoesNotExecute()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);
		var executed = false;
		var target = new PressHoldButton
		{
			HoldDuration = TimeSpan.FromSeconds(5),
			HoldCommand = new TestCommand(_ => true, _ => executed = true)
		};
		Show(target);
		var helper = new MouseTestHelper();

		helper.Down(target, MouseButton.Left, new Point(10, 10));
		var timer = HoldTimer(target);
		CornerstoneTest.AreEqual(TimeSpan.FromMilliseconds(16), timer.Interval);

		helper.Up(target, MouseButton.Left, new Point(10, 10));

		CornerstoneTest.IsFalse(executed);
		CornerstoneTest.AreEqual(0, target.Progress);
		CornerstoneTest.IsFalse(timer.IsEnabled);
	}

	[PresentationTestMethod]
	public void RightButtonDoesNotHold()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);
		var target = Show(new PressHoldButton());

		new MouseTestHelper().Down(target, MouseButton.Right, new Point(10, 10));

		CornerstoneTest.IsFalse(Dispatcher.SnapshotTimersForUnitTests().Any(timer => timer.IsEnabled && timer.Interval == TimeSpan.FromMilliseconds(16)));
	}

	[PresentationTestMethod]
	public void SpaceAndEnterStartTheHold()
	{
		using var app = UnitTestApplication.Start(TestServices.FocusableWindow);
		var target = Show(new PressHoldButton());
		target.Focus();

		target.RaiseEvent(KeyEvent(InputElement.KeyDownEvent, Key.Space));
		var timer = HoldTimer(target);
		CornerstoneTest.IsTrue(timer.IsEnabled);

		target.RaiseEvent(KeyEvent(InputElement.KeyUpEvent, Key.Space));
		CornerstoneTest.IsFalse(timer.IsEnabled);

		target.RaiseEvent(KeyEvent(InputElement.KeyDownEvent, Key.Enter));
		CornerstoneTest.IsTrue(timer.IsEnabled);

		target.RaiseEvent(KeyEvent(InputElement.KeyUpEvent, Key.Enter));
		CornerstoneTest.IsFalse(timer.IsEnabled);
	}

	private static DispatcherTimer HoldTimer(PressHoldButton target)
	{
		var interval = TimeSpan.FromMilliseconds(16);
		return CornerstoneTest.Single(Dispatcher.SnapshotTimersForUnitTests().Where(timer => timer.Interval == interval));
	}

	private static FuncControlTemplate<PressHoldButton> IndicatorTemplate()
	{
		return new FuncControlTemplate<PressHoldButton>((_, scope) =>
			new Border { Name = "PART_Indicator" }.RegisterInNameScope(scope));
	}

	private static KeyEventArgs KeyEvent(RoutedEvent routedEvent, Key key)
	{
		return new KeyEventArgs
		{
			RoutedEvent = routedEvent,
			Key = key
		};
	}

	private static void Press(PressHoldButton target)
	{
		new MouseTestHelper().Down(target, MouseButton.Left, new Point(10, 10));
	}

	private static PressHoldButton Show(PressHoldButton target)
	{
		var window = new Window
		{
			Width = 120,
			Height = 40,
			Content = target
		};
		window.Show();
		target.ApplyTemplate();
		return target;
	}

	private static void WaitUntil(DateTime started, TimeSpan duration)
	{
		var wait = Stopwatch.StartNew();
		while ((DateTime.UtcNow - started < duration) && (wait.ElapsedMilliseconds < 500))
		{
		}
	}

	#endregion
}
