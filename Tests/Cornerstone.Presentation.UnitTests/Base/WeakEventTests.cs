#region References

using System;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class WeakEventTests
{
	#region Methods

	[PresentationTestMethod]
	public void EventHandlerShouldNotBeKeptAlive()
	{
		var handled = false;
		var source = new EventSource();
		AddSubscriber(source, () => handled = true);
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
		EventSource.WeakEv.Subscribe(source, subscriber);

		source.Fire();
		CornerstoneTest.IsTrue(handled);
	}

	private static void AddSubscriber(EventSource source, Action func)
	{
		EventSource.WeakEv.Subscribe(source, new Subscriber(func));
	}

	#endregion

	#region Classes

	private class EventSource
	{
		#region Fields

		public static readonly WeakEvent<EventSource, EventArgs> WeakEv = WeakEvent.Register<EventSource>(
			(t, s) => t.Event += s,
			(t, s) => t.Event -= s);

		#endregion

		#region Methods

		public void Fire()
		{
			Event?.Invoke(this, EventArgs.Empty);
		}

		#endregion

		#region Events

		public event EventHandler Event;

		#endregion
	}

	private class Subscriber : IWeakEventSubscriber<EventArgs>
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

		public void OnEvent(object sender, WeakEvent ev, EventArgs args)
		{
			_onEvent?.Invoke();
		}

		#endregion
	}

	#endregion
}