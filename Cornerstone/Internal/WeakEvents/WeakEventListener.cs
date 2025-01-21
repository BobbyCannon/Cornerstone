#region References

using System;
using System.Reflection;
using Cornerstone.Presentation;

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
		_eventInfo.AddEventHandler(source,
			_eventInfo.EventHandlerType == typeof(EventHandler<TArgs>)
				? new EventHandler<TArgs>(HandleEvent)
				// the event type isn't just an EventHandler<> so we have to create the delegate using reflection
				: Delegate.CreateDelegate(_eventInfo.EventHandlerType, this, nameof(HandleEvent))
		);
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

internal class WeakEventListener<T, TArgs1, TArgs2> : WeakEventListenerBase<T, TArgs1, TArgs2>
	where T : class
{
	#region Fields

	private readonly EventInfo _eventInfo;

	#endregion

	#region Constructors

	public WeakEventListener(T source, string eventName, EventHandler<TArgs1, TArgs2> handler)
		: base(source, handler)
	{
		_eventInfo = source.GetType().GetEvent(eventName) ?? throw new ArgumentException("Unknown Event Name", nameof(eventName));

		var eventHandler = _eventInfo.EventHandlerType == typeof(EventHandler<TArgs1, TArgs2>)
			? new EventHandler<TArgs1, TArgs2>(HandleEvent)
			// the event type isn't just an EventHandler<> so we have to create the delegate using reflection
			: Delegate.CreateDelegate(_eventInfo.EventHandlerType, this, nameof(HandleEvent));

		_eventInfo.AddEventHandler(source, eventHandler);
	}

	#endregion

	#region Methods

	protected override void StopListening(T source)
	{
		if (_eventInfo.EventHandlerType == typeof(EventHandler<TArgs1, TArgs2>))
		{
			_eventInfo.RemoveEventHandler(source, new EventHandler<TArgs1, TArgs2>(HandleEvent));
		}
		else
		{
			_eventInfo.RemoveEventHandler(source, Delegate.CreateDelegate(_eventInfo.EventHandlerType, this, nameof(HandleEvent)));
		}
	}

	#endregion
}