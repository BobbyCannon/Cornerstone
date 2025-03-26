#region References

using System;
using System.Reflection;
using Cornerstone.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Internal.WeakEvents;

internal class WeakEventListener<T, T2, TArgs> : IWeakEventListener
	where T : class
	where T2 : class
{
	#region Fields

	private readonly WeakReference _destination;
	private readonly EventInfo _eventInfo;
	private readonly MethodInfo _methodInfo;
	private readonly WeakReference _source;

	#endregion

	#region Constructors

	public WeakEventListener(T source, Type type, string eventName, T2 destination, MethodInfo methodInfo)
	{
		_eventInfo = type.GetMatchingEvent(eventName, methodInfo) ?? throw new ArgumentException("Unknown Event Name", nameof(eventName));
		_eventInfo.AddEventHandler(source,
			_eventInfo.EventHandlerType == typeof(EventHandler<TArgs>)
				? new EventHandler<TArgs>(HandleEvent)
				// the event type isn't just an EventHandler<> so we have to create the delegate using reflection
				: Delegate.CreateDelegate(_eventInfo.EventHandlerType, this, nameof(HandleEvent))
		);

		_source = new WeakReference(source);
		_destination = new WeakReference(destination);
		_methodInfo = methodInfo;
	}

	#endregion

	#region Properties

	public object Destination => _destination.IsAlive ? _destination.Target : null;

	public bool IsAlive => _destination.IsAlive && _source.IsAlive;

	public object Source => _source.IsAlive ? _source.Target : null;

	#endregion

	#region Methods

	public virtual void StopListening()
	{
		if (_source.IsAlive && _source.Target is T target)
		{
			_eventInfo.RemoveEventHandler(target,
				_eventInfo.EventHandlerType == typeof(EventHandler<TArgs>)
					? new EventHandler<TArgs>(HandleEvent)
					: Delegate.CreateDelegate(_eventInfo.EventHandlerType, this, nameof(HandleEvent))
			);
		}
	}

	protected void HandleEvent(object sender, TArgs e)
	{
		if (IsAlive
			&& _source.Target is T source
			&& _destination.Target is T2 destination)
		{
			_methodInfo.Invoke(destination, [source, e]);
		}
		else
		{
			StopListening();
		}
	}

	#endregion
}

internal class WeakEventListener<T, T2, TArgs1, TArgs2> : IWeakEventListener
	where T : class
	where T2 : class
{
	#region Fields

	private readonly WeakReference _destination;
	private readonly EventInfo _eventInfo;
	private readonly MethodInfo _methodInfo;
	private readonly WeakReference _source;

	#endregion

	#region Constructors

	public WeakEventListener(T source, string eventName, T2 destination, MethodInfo methodInfo)
	{
		_eventInfo = source.GetType().GetEvent(eventName) ?? throw new ArgumentException("Unknown Event Name", nameof(eventName));
		_eventInfo.AddEventHandler(source,
			_eventInfo.EventHandlerType == typeof(EventHandler<TArgs1, TArgs2>)
				? new EventHandler<TArgs1, TArgs2>(HandleEvent)
				// the event type isn't just an EventHandler<> so we have to create the delegate using reflection
				: Delegate.CreateDelegate(_eventInfo.EventHandlerType, this, nameof(HandleEvent))
		);

		_source = new WeakReference(source);
		_destination = new WeakReference(destination);
		_methodInfo = methodInfo;
	}

	#endregion

	#region Properties

	public object Destination => _destination.IsAlive ? _destination.Target : null;

	public bool IsAlive => _destination.IsAlive && _source.IsAlive;

	public object Source => _source.IsAlive ? _source.Target : null;

	#endregion

	#region Methods

	public virtual void StopListening()
	{
		if (_source.IsAlive && _source.Target is T target)
		{
			_eventInfo.RemoveEventHandler(target,
				_eventInfo.EventHandlerType == typeof(EventHandler<TArgs1, TArgs2>)
					? new EventHandler<TArgs1, TArgs2>(HandleEvent)
					: Delegate.CreateDelegate(_eventInfo.EventHandlerType, this, nameof(HandleEvent))
			);
		}
	}

	protected void HandleEvent(object sender, (TArgs1, TArgs2) e)
	{
		if (IsAlive
			&& _source.Target is T source
			&& _destination.Target is T2 destination)
		{
			_methodInfo.Invoke(destination, [source, e]);
		}
		else
		{
			StopListening();
		}
	}

	#endregion
}