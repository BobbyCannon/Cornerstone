#region References

using System;
using System.Reflection;

#endregion

namespace Cornerstone.Internal.WeakEvents;

internal class WeakEventListener<T, TArgs> : WeakEventListenerBase<T, TArgs>
	where T : class
{
	#region Fields

	private readonly EventInfo _eventInfo;

	#endregion

	#region Constructors

	public WeakEventListener(T source, string eventName, EventHandler<TArgs> handler)
		: base(source, handler)
	{
		_eventInfo = source.GetType().GetEvent(eventName) ?? throw new ArgumentException("Unknown Event Name", nameof(eventName));
		if (_eventInfo.EventHandlerType == typeof(EventHandler<TArgs>))
		{
			_eventInfo.AddEventHandler(source, new EventHandler<TArgs>(HandleEvent));
		}
		else //the event type isn't just an EventHandler<> so we have to create the delegate using reflection
		{
			_eventInfo.AddEventHandler(source, Delegate.CreateDelegate(_eventInfo.EventHandlerType, this, nameof(HandleEvent)));
		}
	}

	#endregion

	#region Methods

	protected override void StopListening(T source)
	{
		if (_eventInfo.EventHandlerType == typeof(EventHandler<TArgs>))
		{
			_eventInfo.RemoveEventHandler(source, new EventHandler<TArgs>(HandleEvent));
		}
		else
		{
			_eventInfo.RemoveEventHandler(source, Delegate.CreateDelegate(_eventInfo.EventHandlerType, this, nameof(HandleEvent)));
		}
	}

	#endregion
}