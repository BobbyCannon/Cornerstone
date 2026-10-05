#region References

using System;
using System.Collections.Generic;
using System.Reactive;
using System.Runtime.CompilerServices;
using System.Threading;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Raw;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class ToolTipTestsPopup : ToolTipTests
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldNotCloseWhenLeavingWindowRightAfterToolTipOpenedUnderPointer()
	{
		using var app = UnitTestApplication.Start(ConfigureServices(TestServices.FocusableWindow));

		var target = new Decorator
		{
			[ToolTip.TipProperty] = "Tip",
			[ToolTip.ShowDelayProperty] = 0
		};

		var scope = SetupWindow(target);

		scope.MouseEnter(target);
		AssertToolTipOpen(target);

		// The pointer is still over the adorned control: this leave event is only caused by the tooltip window
		// being displayed on top of it (macOS case).
		scope.SendRawPointerEvent(RawPointerEventType.LeaveWindow, scope.Window.InputRoot, scope.GetPointerPosition(target));

		AssertToolTipOpen(target);
	}

	[PresentationTestMethod]
	public void ShouldNotCloseWhenPointerIsOverToolTipWindowWithoutHitTestResult()
	{
		using var app = UnitTestApplication.Start(ConfigureServices(TestServices.FocusableWindow));

		var target = new Decorator
		{
			[ToolTip.TipProperty] = "Tip",
			[ToolTip.ShowDelayProperty] = 0
		};

		var scope = SetupWindow(target);

		scope.MouseEnter(target);
		AssertToolTipOpen(target);

		var toolTip = CornerstoneTest.IsType<ToolTip>(target.GetValue(ToolTip.ToolTipProperty));
		var toolTipRoot = CornerstoneTest.IsType<PopupRoot>(toolTip.PopupHost).GetInputRoot();
		CornerstoneTest.IsNotNull(toolTipRoot);

		scope.SendRawPointerEvent(RawPointerEventType.Move, toolTipRoot, scope.GetPointerPosition(null));

		AssertToolTipOpen(target);
	}

	protected override TestServices ConfigureServices(TestServices baseServices)
	{
		return baseServices;
	}

	protected override void SetupWindowMock(StubWindowImpl windowImpl)
	{
	}

	protected override void VerifyToolTipType(Control control)
	{
		var toolTip = control.GetValue(ToolTip.ToolTipProperty);
		CornerstoneTest.IsNotNull(toolTip);
		CornerstoneTest.IsType<PopupRoot>(toolTip.PopupHost);
		CornerstoneTest.Same(TopLevel.GetTopLevel(toolTip), toolTip.PopupHost);
	}

	#endregion
}

[TestClass]
public class ToolTipTestsOverlay : ToolTipTests, IDisposable
{
	#region Fields

	private readonly IDisposable _toolTipOpenSubscription;

	#endregion

	#region Constructors

	public ToolTipTestsOverlay()
	{
		_toolTipOpenSubscription = ToolTip.IsOpenProperty.Changed.Subscribe(new AnonymousObserver<PresentationPropertyChangedEventArgs<bool>>(e =>
		{
			if (e.Sender is Visual visual && TopLevel.GetTopLevel(visual) is { } root)
			{
				PopupOverlayLayer.GetPopupOverlayLayer(visual)?.Measure(root.ClientSize);
			}
		}));
	}

	#endregion

	#region Methods

	public override void Dispose()
	{
		_toolTipOpenSubscription.Dispose();
		base.Dispose();
	}

	protected override TestServices ConfigureServices(TestServices baseServices)
	{
		return baseServices.With(windowingPlatform: new MockWindowingPlatform(popupImpl: window => null));
	}

	protected override void SetupWindowMock(StubWindowImpl windowImpl)
	{
		windowImpl.CreatePopupHandler = () => null;
	}

	protected override void VerifyToolTipType(Control control)
	{
		var toolTip = control.GetValue(ToolTip.ToolTipProperty);
		CornerstoneTest.IsNotNull(toolTip);
		CornerstoneTest.IsType<OverlayPopupHost>(toolTip.PopupHost);
		CornerstoneTest.Same(toolTip.VisualRoot, control.VisualRoot);
	}

	#endregion
}

[TestClass]
public abstract class ToolTipTests : ScopedTestBase
{
	#region Fields

	private static readonly MouseDevice smouseDevice = new(new Pointer(0, PointerType.Mouse, true));

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void ClearingIsOpenShouldRemoveOpenClass()
	{
		using (UnitTestApplication.Start(ConfigureServices(TestServices.StyledWindow)))
		{
			var toolTip = new ToolTip();

			var windowImpl = MockWindowingPlatform.CreateWindowMock();
			SetupWindowMock(windowImpl);
			var window = new Window(windowImpl);

			var decorator = new Decorator
			{
				[ToolTip.TipProperty] = toolTip
			};

			window.Content = decorator;

			window.ApplyStyling();
			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();

			ToolTip.SetIsOpen(decorator, true);
			AssertToolTipOpen(decorator);
			ToolTip.SetIsOpen(decorator, false);

			CornerstoneTest.DoesNotContain(toolTip.Classes, ":open");
		}
	}

	[PresentationTestMethod]
	public void ContentShouldUpdateWhenTipPropertyChangesAndAlreadyOpen()
	{
		using (UnitTestApplication.Start(ConfigureServices(TestServices.FocusableWindow)))
		{
			var target = new Decorator
			{
				[ToolTip.TipProperty] = "Tip",
				[ToolTip.ShowDelayProperty] = 0
			};

			SetupWindowAndActivateToolTip(target);

			AssertToolTipOpen(target);
			CornerstoneTest.AreEqual("Tip", target.GetValue(ToolTip.ToolTipProperty)?.Content);

			ToolTip.SetTip(target, "Tip1");
			CornerstoneTest.AreEqual("Tip1", target.GetValue(ToolTip.ToolTipProperty)?.Content);
		}
	}

	[PresentationTestMethod]
	public void NewToolTipReplacesOtherToolTipImmediately()
	{
		using var app = UnitTestApplication.Start(ConfigureServices(TestServices.FocusableWindow));

		var target = new Decorator
		{
			[ToolTip.TipProperty] = "Tip",
			[ToolTip.ShowDelayProperty] = 0
		};

		var other = new Decorator
		{
			[ToolTip.TipProperty] = "Tip",
			[ToolTip.ShowDelayProperty] = (int) TimeSpan.FromHours(1).TotalMilliseconds
		};

		var panel = new StackPanel
		{
			Children = { target, other }
		};

		var mouseEnter = SetupWindowAndGetMouseEnterAction(panel);

		mouseEnter(other);
		CornerstoneTest.IsFalse(ToolTip.GetIsOpen(other)); // long delay

		mouseEnter(target);
		AssertToolTipOpen(target); // no delay

		mouseEnter(other);
		CornerstoneTest.IsTrue(ToolTip.GetIsOpen(other)); // delay skipped, a tooltip was already open

		// Now disable the between-show system

		mouseEnter(target);
		AssertToolTipOpen(target);

		ToolTip.SetBetweenShowDelay(other, -1);

		mouseEnter(other);
		CornerstoneTest.IsFalse(ToolTip.GetIsOpen(other));
	}

	[PresentationTestMethod]
	public void OpenClassShouldNotInitiallyBeAdded()
	{
		using (UnitTestApplication.Start(ConfigureServices(TestServices.StyledWindow)))
		{
			var toolTip = new ToolTip();
			var window = new Window();

			var decorator = new Decorator
			{
				[ToolTip.TipProperty] = toolTip
			};

			window.Content = decorator;

			window.ApplyStyling();
			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();

			CornerstoneTest.DoesNotContain(toolTip.Classes, ":open");
		}
	}

	[PresentationTestMethod]
	public void SettingIsOpenShouldAddOpenClass()
	{
		using (UnitTestApplication.Start(ConfigureServices(TestServices.StyledWindow)))
		{
			var toolTip = new ToolTip();
			var window = new Window();

			var decorator = new Decorator
			{
				[ToolTip.TipProperty] = toolTip
			};

			window.Content = decorator;

			window.ApplyStyling();
			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();

			ToolTip.SetIsOpen(decorator, true);

			CornerstoneTest.Contains(toolTip.Classes, ":open");
			VerifyToolTipType(decorator);
		}
	}

	[PresentationTestMethod]
	public void ShouldCloseOnNullTip()
	{
		using (UnitTestApplication.Start(ConfigureServices(TestServices.FocusableWindow)))
		{
			var target = new Decorator
			{
				[ToolTip.TipProperty] = "Tip",
				[ToolTip.ShowDelayProperty] = 0
			};

			SetupWindowAndActivateToolTip(target);

			AssertToolTipOpen(target);

			target[ToolTip.TipProperty] = null;

			CornerstoneTest.IsFalse(ToolTip.GetIsOpen(target));
		}
	}

	[PresentationTestMethod]
	public void ShouldCloseWhenControlDetaches()
	{
		using (UnitTestApplication.Start(ConfigureServices(TestServices.FocusableWindow)))
		{
			var panel = new Panel();

			var target = new Decorator
			{
				[ToolTip.TipProperty] = "Tip",
				[ToolTip.ShowDelayProperty] = 0
			};

			panel.Children.Add(target);

			SetupWindowAndActivateToolTip(panel, target);

			AssertToolTipOpen(target);

			panel.Children.Remove(target);

			CornerstoneTest.IsFalse(ToolTip.GetIsOpen(target));
		}
	}

	[PresentationTestMethod]
	public void ShouldCloseWhenPointerIsMovedFromToolTipToAnotherControl()
	{
		using (UnitTestApplication.Start(ConfigureServices(TestServices.FocusableWindow)))
		{
			var target = new Decorator
			{
				[ToolTip.TipProperty] = "Tip",
				[ToolTip.ShowDelayProperty] = 0
			};

			var other = new Decorator();

			var panel = new StackPanel
			{
				Children = { target, other }
			};

			var mouseEnter = SetupWindowAndGetMouseEnterAction(panel);

			mouseEnter(target);
			AssertToolTipOpen(target);

			var tooltip = CornerstoneTest.IsType<ToolTip>(target.GetValue(ToolTip.ToolTipProperty));
			mouseEnter(tooltip);

			AssertToolTipOpen(target);

			mouseEnter(other);

			CornerstoneTest.IsFalse(ToolTip.GetIsOpen(target));
		}
	}

	[PresentationTestMethod]
	public void ShouldCloseWhenPointerLeavesWindow()
	{
		using (UnitTestApplication.Start(TestServices.FocusableWindow))
		{
			var target = new Decorator
			{
				[ToolTip.TipProperty] = "Tip",
				[ToolTip.ShowDelayProperty] = 0
			};

			var mouseEnter = SetupWindowAndGetMouseEnterAction(target);

			mouseEnter(target);
			AssertToolTipOpen(target);

			var topLevel = TopLevel.GetTopLevel(target);
			topLevel!.PlatformImpl!.Input!(new RawPointerEventArgs(smouseDevice, (ulong) DateTime.Now.Ticks, topLevel.InputRoot,
				RawPointerEventType.LeaveWindow, default(RawPointerPoint), RawInputModifiers.None));

			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.IsFalse(ToolTip.GetIsOpen(target));
		}
	}

	[PresentationTestMethod]
	public void ShouldCloseWhenTipIsOpenedAndDetachedFromVisualTree()
	{
		using (UnitTestApplication.Start(ConfigureServices(TestServices.FocusableWindow)))
		{
			var target = new Decorator
			{
				[!ToolTip.TipProperty] = new Binding("Tip"),
				[ToolTip.ShowDelayProperty] = 0
			};

			var panel = new Panel();
			panel.Children.Add(target);

			var mouseEnter = SetupWindowAndGetMouseEnterAction(panel);

			panel.DataContext = new ToolTipViewModel();

			mouseEnter(target);

			AssertToolTipOpen(target);

			panel.Children.Remove(target);

			CornerstoneTest.IsFalse(ToolTip.GetIsOpen(target));
		}
	}

	[PresentationTestMethod]
	public void ShouldNotCloseWhenPointerIsMovedFromToolTipToOriginalControl()
	{
		using (UnitTestApplication.Start(ConfigureServices(TestServices.FocusableWindow)))
		{
			var target = new Decorator
			{
				[ToolTip.TipProperty] = "Tip",
				[ToolTip.ShowDelayProperty] = 0
			};

			var mouseEnter = SetupWindowAndGetMouseEnterAction(target);

			mouseEnter(target);
			AssertToolTipOpen(target);

			var tooltip = CornerstoneTest.IsType<ToolTip>(target.GetValue(ToolTip.ToolTipProperty));
			mouseEnter(tooltip);

			AssertToolTipOpen(target);

			mouseEnter(target);

			AssertToolTipOpen(target);
		}
	}

	[PresentationTestMethod]
	public void ShouldNotCloseWhenPointerIsMovedOverToolTip()
	{
		using (UnitTestApplication.Start(ConfigureServices(TestServices.FocusableWindow)))
		{
			var target = new Decorator
			{
				[ToolTip.TipProperty] = "Tip",
				[ToolTip.ShowDelayProperty] = 0
			};

			var mouseEnter = SetupWindowAndGetMouseEnterAction(target);

			mouseEnter(target);

			AssertToolTipOpen(target);

			var tooltip = CornerstoneTest.IsType<ToolTip>(target.GetValue(ToolTip.ToolTipProperty));

			mouseEnter(tooltip);

			AssertToolTipOpen(target);
		}
	}

	[PresentationTestMethod]
	public void ShouldOpenOnPointerEnter()
	{
		using (UnitTestApplication.Start(ConfigureServices(TestServices.FocusableWindow)))
		{
			var target = new Decorator
			{
				[ToolTip.TipProperty] = "Tip",
				[ToolTip.ShowDelayProperty] = 0
			};

			SetupWindowAndActivateToolTip(target);

			AssertToolTipOpen(target);
		}
	}

	[PresentationTestMethod]
	public void ShouldOpenOnPointerEnterWithDelay()
	{
		using (UnitTestApplication.Start(ConfigureServices(TestServices.FocusableWindow)))
		{
			var target = new Decorator
			{
				[ToolTip.TipProperty] = "Tip",
				[ToolTip.ShowDelayProperty] = 1
			};

			SetupWindowAndActivateToolTip(target);

			var timer = CornerstoneTest.Single(Dispatcher.SnapshotTimersForUnitTests());
			CornerstoneTest.AreEqual(TimeSpan.FromMilliseconds(1), timer.Interval);
			CornerstoneTest.IsFalse(ToolTip.GetIsOpen(target));

			timer.ForceFire();

			AssertToolTipOpen(target);
		}
	}

	[PresentationTestMethod]
	public void ToolTipCanBeReplacedOnTheFlyViaOpeningEvent()
	{
		using var app = UnitTestApplication.Start(ConfigureServices(TestServices.FocusableWindow));

		var tip1 = new ToolTip { Content = "Hi" };
		var tip2 = new ToolTip { Content = "Bye" };
		var target = new Decorator
		{
			[ToolTip.TipProperty] = tip1,
			[ToolTip.ShowDelayProperty] = 0
		};

		ToolTip.AddToolTipOpeningHandler(target,
			(sender, args) => target[ToolTip.TipProperty] = tip2);

		SetupWindowAndActivateToolTip(target);

		AssertToolTipOpen(target);

		target[ToolTip.TipProperty] = null;

		CornerstoneTest.IsFalse(ToolTip.GetIsOpen(target));
	}

	[PresentationTestMethod]
	public void ToolTipEventsOrderIsDefined()
	{
		using var app = UnitTestApplication.Start(ConfigureServices(TestServices.FocusableWindow));

		var tip = new ToolTip { Content = "Tip" };
		var target = new Decorator
		{
			[ToolTip.TipProperty] = tip,
			[ToolTip.ShowDelayProperty] = 0
		};

		var eventsOrder = new List<(string eventName, object sender, object source)>();

		ToolTip.AddToolTipOpeningHandler(target,
			(sender, args) => eventsOrder.Add(("Opening", sender, args.Source)));
		ToolTip.AddToolTipClosingHandler(target,
			(sender, args) => eventsOrder.Add(("Closing", sender, args.Source)));

		SetupWindowAndActivateToolTip(target);

		AssertToolTipOpen(target);

		target[ToolTip.TipProperty] = null;

		CornerstoneTest.IsFalse(ToolTip.GetIsOpen(target));

		CornerstoneTest.AreEqual(new[]
		{
			("Opening", (object) target, (object) target),
			("Closing", target, target)
		}, eventsOrder);
	}

	[PresentationTestMethod]
	public void ToolTipIsNotOpenedIfOpeningEventHandled()
	{
		using var app = UnitTestApplication.Start(ConfigureServices(TestServices.FocusableWindow));

		var tip = new ToolTip { Content = "Tip" };
		var target = new Decorator
		{
			[ToolTip.TipProperty] = tip,
			[ToolTip.ShowDelayProperty] = 0
		};

		ToolTip.AddToolTipOpeningHandler(target,
			(sender, args) => args.Cancel = true);

		SetupWindowAndActivateToolTip(target);

		CornerstoneTest.IsFalse(ToolTip.GetIsOpen(target));
	}

	protected void AssertToolTipOpen(Control control)
	{
		CornerstoneTest.IsTrue(ToolTip.GetIsOpen(control));
		VerifyToolTipType(control);
	}

	protected abstract TestServices ConfigureServices(TestServices baseServices);

	protected ToolTipTestScope SetupWindow(Control windowContent, [CallerMemberName] string testName = null)
	{
		var windowImpl = MockWindowingPlatform.CreateWindowMock();
		SetupWindowMock(windowImpl);

		var hitTesterMock = new StubHitTester();

		var window = new Window(windowImpl)
		{
			HitTesterOverride = hitTesterMock,
			Content = windowContent,
			Title = testName
		};

		window.ApplyStyling();
		window.ApplyTemplate();
		window.Presenter!.ApplyTemplate();
		window.Show();

		CornerstoneTest.IsTrue(windowContent.IsAttachedToVisualTree);
		CornerstoneTest.IsTrue(windowContent.IsMeasureValid);
		CornerstoneTest.IsTrue(windowContent.IsVisible);

		return new ToolTipTestScope(
			window,
			windowImpl,
			(_, control) => hitTesterMock.SetHit(control));
	}

	protected abstract void SetupWindowMock(StubWindowImpl windowImpl);

	protected abstract void VerifyToolTipType(Control control);

	private void SetupWindowAndActivateToolTip(Control windowContent, Control targetOverride = null, [CallerMemberName] string testName = null)
	{
		SetupWindowAndGetMouseEnterAction(windowContent, testName)(targetOverride ?? windowContent);
	}

	private Action<Control> SetupWindowAndGetMouseEnterAction(Control windowContent, [CallerMemberName] string testName = null)
	{
		return SetupWindow(windowContent, testName).MouseEnter;
	}

	#endregion

	#region Classes

	protected sealed class ToolTipTestScope(Window window, StubWindowImpl windowImpl, Action<Point, Control> setupHitTest)
	{
		#region Fields

		private readonly Dictionary<Control, int> _controlIds = new();
		private IInputRoot _lastRoot;

		#endregion

		#region Properties

		public Window Window { get; } = window;

		#endregion

		#region Methods

		/// <summary>
		/// Returns a pointer position which hit tests to <paramref name="control" />, or to nothing when it's null.
		/// </summary>
		public Point GetPointerPosition(Control control)
		{
			Point point;

			if (control == null)
			{
				point = default;
			}
			else
			{
				if (!_controlIds.TryGetValue(control, out var id))
				{
					id = _controlIds[control] = _controlIds.Count;
				}
				point = new Point(id, int.MaxValue);
			}

			setupHitTest(point, control);

			return point;
		}

		/// <summary>
		/// Moves the pointer over <paramref name="control" />, leaving the previous root if it changed.
		/// </summary>
		public void MouseEnter(Control control)
		{
			var point = GetPointerPosition(control);
			var root = control?.GetInputRoot() ?? Window.InputRoot;
			var timestamp = (ulong) DateTime.Now.Ticks;

			windowImpl.Input!(new RawPointerEventArgs(smouseDevice, timestamp, root,
				RawPointerEventType.Move, point, RawInputModifiers.None));

			if ((_lastRoot != null) && (_lastRoot != root))
			{
				((PresentationSource) _lastRoot)?.PlatformImpl?.Input?.Invoke(new RawPointerEventArgs(smouseDevice, timestamp,
					_lastRoot, RawPointerEventType.LeaveWindow, new Point(-1, -1), RawInputModifiers.None));
			}

			_lastRoot = root;

			CornerstoneTest.IsTrue((control == null) || control.IsPointerOver);
		}

		/// <summary>
		/// Sends a raw pointer event, as a platform backend would do.
		/// </summary>
		public void SendRawPointerEvent(RawPointerEventType type, IInputRoot root, Point position)
		{
			windowImpl.Input!(new RawPointerEventArgs(
				smouseDevice, (ulong) DateTime.Now.Ticks, root, type, position, RawInputModifiers.None));
		}

		#endregion
	}

	#endregion
}

internal class ToolTipViewModel
{
	#region Properties

	public string Tip => "Tip";

	#endregion
}