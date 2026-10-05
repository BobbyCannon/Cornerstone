#region References

using System;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class WeakEventHandlerManagerTests
{
	#region Methods

	[PresentationTestMethod]
	public void EventHandlerShouldNotBeKeptAlive()
	{
		var handled = false;
		var source = new EventSource();
		AddCollectableSubscriber(source, "Event", () => handled = true);
		for (var c = 0; c < 10; c++)
		{
			GC.Collect();
			GC.Collect(3, GCCollectionMode.Forced, true);
		}
		source.Fire();
		CornerstoneTest.IsFalse(handled);
	}

	[PresentationTestMethod]
	public void EventShouldBePassedToSubscriber()
	{
		var handled = false;
		var subscriber = new Subscriber(() => handled = true);
		var source = new EventSource();
		WeakEventHandlerManager.Subscribe<EventSource, EventArgs, Subscriber>(source, "Event",
			subscriber.OnEvent);
		source.Fire();
		CornerstoneTest.IsTrue(handled);
	}

	[PresentationTestMethod]
	public void EventShouldNotBeRaisedAfterUnsubscribe()
	{
		var handled = false;
		var subscriber = new Subscriber(() => handled = true);
		var source = new EventSource();
		WeakEventHandlerManager.Subscribe<EventSource, EventArgs, Subscriber>(source, "Event",
			subscriber.OnEvent);

		WeakEventHandlerManager.Unsubscribe<EventArgs, Subscriber>(source, "Event",
			subscriber.OnEvent);

		source.Fire();

		CornerstoneTest.IsFalse(handled);
	}

	private static void AddCollectableSubscriber(EventSource source, string name, Action func)
	{
		WeakEventHandlerManager.Subscribe<EventSource, EventArgs, Subscriber>(source, name, new Subscriber(func).OnEvent);
	}

	#endregion

	#region Classes

	private class EventSource
	{
		#region Methods

		public void Fire()
		{
			Event?.Invoke(this, EventArgs.Empty);
		}

		#endregion

		#region Events

		public event EventHandler<EventArgs> Event;

		#endregion
	}

	private class Subscriber
	{
		#region Fields

		private readonly Action _onEvent;

		#endregion

		#region Constructors

		public Subscriber(Action onEvent)
		{
			_onEvent = onEvent;
		}

		#endregion

		#region Methods

		public void OnEvent(object sender, EventArgs ev)
		{
			_onEvent?.Invoke();
		}

		#endregion
	}

	#endregion
}