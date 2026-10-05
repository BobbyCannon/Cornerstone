#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Interactivity;

[TestClass]
public class InteractiveTests
{
	#region Methods

	[PresentationTestMethod]
	public void BubblingClassHandlersShouldBeCalled()
	{
		var ev = new RoutedEvent(
			"test",
			RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
			typeof(RoutedEventArgs),
			typeof(TestInteractive));
		var invoked = new List<string>();
		EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive) s!).Name);

		var target = CreateTree(ev, null, 0);

		ev.AddClassHandler(typeof(TestInteractive), handler, RoutingStrategies.Bubble);

		var args = new RoutedEventArgs(ev, target);
		target.RaiseEvent(args);

		CornerstoneTest.AreEqual(new[] { "2b", "1" }, invoked);
	}

	[PresentationTestMethod]
	public void BubblingEventShouldBubbleUp()
	{
		var ev = new RoutedEvent("test", RoutingStrategies.Bubble, typeof(RoutedEventArgs), typeof(TestInteractive));
		var invoked = new List<string>();
		EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive) s!).Name);
		var target = CreateTree(ev, handler, RoutingStrategies.Bubble | RoutingStrategies.Tunnel);

		var args = new RoutedEventArgs(ev, target);
		target.RaiseEvent(args);

		CornerstoneTest.AreEqual(new[] { "2b", "1" }, invoked);
	}

	[PresentationTestMethod]
	public void BubblingSubscriptionShouldNotCatchTunneling()
	{
		var ev = new RoutedEvent(
			"test",
			RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
			typeof(RoutedEventArgs),
			typeof(TestInteractive));
		var count = 0;

		EventHandler<RoutedEventArgs> handler = (s, e) =>
		{
			CornerstoneTest.AreEqual(RoutingStrategies.Bubble, e.Route);
			++count;
		};

		var target = CreateTree(ev, handler, RoutingStrategies.Bubble);

		var args = new RoutedEventArgs(ev, target);
		target.RaiseEvent(args);

		CornerstoneTest.AreEqual(2, count);
	}

	[PresentationTestMethod]
	public void DirectClassHandlersShouldBeCalled()
	{
		var ev = new RoutedEvent(
			"test",
			RoutingStrategies.Direct,
			typeof(RoutedEventArgs),
			typeof(TestInteractive));
		var invoked = new List<string>();
		EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive) s!).Name);

		var target = CreateTree(ev, null, 0);

		ev.AddClassHandler(typeof(TestInteractive), handler, RoutingStrategies.Direct);

		var args = new RoutedEventArgs(ev, target);
		target.RaiseEvent(args);

		CornerstoneTest.AreEqual(new[] { "2b" }, invoked);
	}

	[PresentationTestMethod]
	public void DirectEventShouldGoStraightToSource()
	{
		var ev = new RoutedEvent("test", RoutingStrategies.Direct, typeof(RoutedEventArgs), typeof(TestInteractive));
		var invoked = new List<string>();
		EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive) s!).Name);
		var target = CreateTree(ev, handler, RoutingStrategies.Direct);

		var args = new RoutedEventArgs(ev, target);
		target.RaiseEvent(args);

		CornerstoneTest.AreEqual(new[] { "2b" }, invoked);
	}

	[PresentationTestMethod]
	public void DirectEventShouldHaveRouteSetToDirect()
	{
		var ev = new RoutedEvent("test", RoutingStrategies.Direct, typeof(RoutedEventArgs), typeof(TestInteractive));
		var called = false;

		EventHandler<RoutedEventArgs> handler = (s, e) =>
		{
			CornerstoneTest.AreEqual(RoutingStrategies.Direct, e.Route);
			called = true;
		};

		var target = CreateTree(ev, handler, RoutingStrategies.Direct);

		var args = new RoutedEventArgs(ev, target);
		target.RaiseEvent(args);

		CornerstoneTest.IsTrue(called);
	}

	[PresentationTestMethod]
	public void DirectSubscriptionShouldNotCatchTunnelingOrBubbling()
	{
		var ev = new RoutedEvent(
			"test",
			RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
			typeof(RoutedEventArgs),
			typeof(TestInteractive));
		var count = 0;

		EventHandler<RoutedEventArgs> handler = (s, e) => { ++count; };

		var target = CreateTree(ev, handler, RoutingStrategies.Direct);

		var args = new RoutedEventArgs(ev, target);
		target.RaiseEvent(args);

		CornerstoneTest.AreEqual(0, count);
	}

	[PresentationTestMethod]
	public void EventShouldShouldKeepPropogatingToHandedEventsTooHandlers()
	{
		var ev = new RoutedEvent(
			"test",
			RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
			typeof(RoutedEventArgs),
			typeof(TestInteractive));
		var invoked = new List<string>();

		EventHandler<RoutedEventArgs> handler = (s, e) =>
		{
			invoked.Add(((TestInteractive) s!).Name);
			e.Handled = true;
		};

		var target = CreateTree(ev, handler, RoutingStrategies.Bubble | RoutingStrategies.Tunnel, true);

		var args = new RoutedEventArgs(ev, target);
		target.RaiseEvent(args);

		CornerstoneTest.AreEqual(new[] { "1", "2b", "2b", "1" }, invoked);
	}

	[PresentationTestMethod]
	public void EventsShouldHaveRouteSet()
	{
		var ev = new RoutedEvent(
			"test",
			RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
			typeof(RoutedEventArgs),
			typeof(TestInteractive));
		var invoked = new List<RoutingStrategies>();
		EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(e.Route);
		var target = CreateTree(ev, handler, RoutingStrategies.Bubble | RoutingStrategies.Tunnel);

		var args = new RoutedEventArgs(ev, target);
		target.RaiseEvent(args);

		CornerstoneTest.AreEqual(new[]
		{
			RoutingStrategies.Tunnel,
			RoutingStrategies.Tunnel,
			RoutingStrategies.Bubble,
			RoutingStrategies.Bubble
		}, invoked);
	}

	[PresentationTestMethod]
	public void GetObservableShouldListenToEvent()
	{
		var ev = new RoutedEvent<RoutedEventArgs>("test", RoutingStrategies.Direct, typeof(TestInteractive));
		var target = new TestInteractive();
		var called = 0;
		var subscription = target.GetObservable(ev).Subscribe(_ => ++called);

		var args = new RoutedEventArgs(ev, target);
		target.RaiseEvent(args);

		subscription.Dispose();

		target.RaiseEvent(args);

		CornerstoneTest.AreEqual(1, called);
	}

	[PresentationTestMethod]
	public void HandledBubbledEventShouldNotPropogateFurther()
	{
		var ev = new RoutedEvent("test", RoutingStrategies.Bubble, typeof(RoutedEventArgs), typeof(TestInteractive));
		var invoked = new List<string>();

		EventHandler<RoutedEventArgs> handler = (s, e) =>
		{
			var t = (TestInteractive) s!;
			invoked.Add(t.Name);
			e.Handled = t.Name == "2b";
		};

		var target = CreateTree(ev, handler, RoutingStrategies.Bubble);

		var args = new RoutedEventArgs(ev, target);
		target.RaiseEvent(args);

		CornerstoneTest.AreEqual(new[] { "2b" }, invoked);
	}

	[PresentationTestMethod]
	public void HandledTunnelledEventShouldNotPropogateFurther()
	{
		var ev = new RoutedEvent(
			"test",
			RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
			typeof(RoutedEventArgs),
			typeof(TestInteractive));
		var invoked = new List<string>();

		EventHandler<RoutedEventArgs> handler = (s, e) =>
		{
			var t = (TestInteractive) s!;
			invoked.Add(t.Name);
			e.Handled = t.Name == "2b";
		};

		var target = CreateTree(ev, handler, RoutingStrategies.Bubble | RoutingStrategies.Tunnel);

		var args = new RoutedEventArgs(ev, target);
		target.RaiseEvent(args);

		CornerstoneTest.AreEqual(new[] { "1", "2b" }, invoked);
	}

	[PresentationTestMethod]
	public void RemovingControlInHandlerShouldNotStopEvent()
	{
		// Issue #3176
		var ev = new RoutedEvent("test", RoutingStrategies.Bubble, typeof(RoutedEventArgs), typeof(TestInteractive));
		var invoked = new List<string>();
		EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive) s!).Name);
		var parent = CreateTree(ev, handler, RoutingStrategies.Bubble | RoutingStrategies.Tunnel);
		var target = (Interactive) parent.GetVisualChildren().Single();

		EventHandler<RoutedEventArgs> removeHandler = (s, e) => { parent.Children = Array.Empty<Visual>(); };

		target.AddHandler(ev, removeHandler);

		var args = new RoutedEventArgs(ev, target);
		target.RaiseEvent(args);

		CornerstoneTest.AreEqual(new[] { "3", "2b", "1" }, invoked);
	}

	[PresentationTestMethod]
	public void TunnelingBubblingEventShouldTunnelThenBubbleUp()
	{
		var ev = new RoutedEvent(
			"test",
			RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
			typeof(RoutedEventArgs),
			typeof(TestInteractive));
		var invoked = new List<string>();
		EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive) s!).Name);
		var target = CreateTree(ev, handler, RoutingStrategies.Bubble | RoutingStrategies.Tunnel);

		var args = new RoutedEventArgs(ev, target);
		target.RaiseEvent(args);

		CornerstoneTest.AreEqual(new[] { "1", "2b", "2b", "1" }, invoked);
	}

	[PresentationTestMethod]
	public void TunnelingClassHandlersShouldBeCalled()
	{
		var ev = new RoutedEvent(
			"test",
			RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
			typeof(RoutedEventArgs),
			typeof(TestInteractive));
		var invoked = new List<string>();
		EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive) s!).Name);

		var target = CreateTree(ev, null, 0);

		ev.AddClassHandler(typeof(TestInteractive), handler, RoutingStrategies.Tunnel);

		var args = new RoutedEventArgs(ev, target);
		target.RaiseEvent(args);

		CornerstoneTest.AreEqual(new[] { "1", "2b" }, invoked);
	}

	[PresentationTestMethod]
	public void TunnelingEventShouldTunnel()
	{
		var ev = new RoutedEvent("test", RoutingStrategies.Tunnel, typeof(RoutedEventArgs), typeof(TestInteractive));
		var invoked = new List<string>();
		EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive) s!).Name);
		var target = CreateTree(ev, handler, RoutingStrategies.Bubble | RoutingStrategies.Tunnel);

		var args = new RoutedEventArgs(ev, target);
		target.RaiseEvent(args);

		CornerstoneTest.AreEqual(new[] { "1", "2b" }, invoked);
	}

	[PresentationTestMethod]
	public void TunnelingSubscriptionShouldNotCatchBubbling()
	{
		var ev = new RoutedEvent(
			"test",
			RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
			typeof(RoutedEventArgs),
			typeof(TestInteractive));
		var count = 0;

		EventHandler<RoutedEventArgs> handler = (s, e) =>
		{
			CornerstoneTest.AreEqual(RoutingStrategies.Tunnel, e.Route);
			++count;
		};

		var target = CreateTree(ev, handler, RoutingStrategies.Tunnel);

		var args = new RoutedEventArgs(ev, target);
		target.RaiseEvent(args);

		CornerstoneTest.AreEqual(2, count);
	}

	[PresentationTestMethod]
	public void TypedClassHandlersShouldBeCalled()
	{
		var ev = new RoutedEvent<RoutedEventArgs>(
			"test",
			RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
			typeof(TestInteractive));

		var target = CreateTree(ev, null, 0);

		ev.AddClassHandler<TestInteractive>((x, e) => x.ClassHandler(e), RoutingStrategies.Bubble);

		var args = new RoutedEventArgs(ev, target);
		target.RaiseEvent(args);

		CornerstoneTest.IsTrue(target.ClassHandlerInvoked);
		var interactive = target.GetVisualParent<TestInteractive>();
		CornerstoneTest.IsNotNull(interactive);
		CornerstoneTest.IsTrue(interactive.ClassHandlerInvoked);
	}

	[PresentationTestMethod]
	public void TypedClassHandlersShouldBeCalledForHandledEvents()
	{
		var ev = new RoutedEvent<RoutedEventArgs>(
			"test",
			RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
			typeof(TestInteractive));

		var target = CreateTree(ev, null, 0);

		ev.AddClassHandler<TestInteractive>((x, e) => x.MarkEventAsHandled(e), RoutingStrategies.Bubble);
		ev.AddClassHandler<TestInteractive>((x, e) => x.ClassHandler(e), RoutingStrategies.Bubble, true);

		var args = new RoutedEventArgs(ev, target);
		target.RaiseEvent(args);

		CornerstoneTest.IsTrue(args.Handled);
		CornerstoneTest.IsTrue(target.ClassHandlerInvoked);
		var interactive = target.GetVisualParent<TestInteractive>();
		CornerstoneTest.IsNotNull(interactive);
		CornerstoneTest.IsTrue(interactive.ClassHandlerInvoked);
	}

	private static TestInteractive CreateTree(
		RoutedEvent ev,
		EventHandler<RoutedEventArgs> handler,
		RoutingStrategies handlerRoutes,
		bool handledEventsToo = false)
	{
		TestInteractive target;

		var tree = new TestInteractive
		{
			Name = "1",
			Children = new[]
			{
				new TestInteractive
				{
					Name = "2a"
				},
				target = new TestInteractive
				{
					Name = "2b",
					Children = new[]
					{
						new TestInteractive
						{
							Name = "3"
						}
					}
				}
			}
		};

		if (handler != null)
		{
			foreach (var i in tree.GetSelfAndVisualDescendants().Cast<Interactive>())
			{
				i.AddHandler(ev, handler, handlerRoutes, handledEventsToo);
			}
		}

		return target;
	}

	#endregion

	#region Classes

	private class TestInteractive : Interactive
	{
		#region Properties

		public IEnumerable<Visual> Children
		{
			get => VisualChildren.AsEnumerable();

			set
			{
				VisualChildren.Clear();
				VisualChildren.AddRange(value);
			}
		}

		public bool ClassHandlerInvoked { get; private set; }
		public new string Name { get; set; }

		#endregion

		#region Methods

		public void ClassHandler(RoutedEventArgs e)
		{
			ClassHandlerInvoked = true;
		}

		public void MarkEventAsHandled(RoutedEventArgs e)
		{
			e.Handled = true;
		}

		#endregion
	}

	#endregion
}