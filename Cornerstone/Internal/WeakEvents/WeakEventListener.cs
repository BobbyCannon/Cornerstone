#region References

using System;
using System.Reflection;
using System.Threading.Tasks;
using Cornerstone.Extensions;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Internal.WeakEvents;

internal class WeakEventListener<T, T2> : IWeakEventListener
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
		_eventInfo = type.GetMatchingEvent(eventName, methodInfo)
			?? throw new ArgumentException($"Failed to find event: {type.ToAssemblyName()}:{eventName}", nameof(eventName));
		_eventInfo.AddEventHandler(source,
			_eventInfo.EventHandlerType == typeof(EventHandler)
				? new EventHandler(HandleEvent)

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
				_eventInfo.EventHandlerType == typeof(EventHandler)
					? new EventHandler(HandleEvent)
					: Delegate.CreateDelegate(_eventInfo.EventHandlerType, this, nameof(HandleEvent))
			);
		}
	}

	protected void HandleEvent(EventArgs args)
	{
		if (IsAlive
			&& _source.Target is T
			&& _destination.Target is T2 destination)
		{
			_methodInfo.Invoke(destination, [args]);
		}
		else
		{
			StopListening();
		}
	}

	protected void HandleEvent(object sender, EventArgs e)
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
		_eventInfo = type.GetMatchingEvent(eventName, methodInfo)
			?? throw new ArgumentException($"Failed to find event: {type.ToAssemblyName()}:{eventName}", nameof(eventName));
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

	protected void HandleEvent(TArgs e)
	{
		if (IsAlive
			&& _source.Target is T
			&& _destination.Target is T2 destination)
		{
			_methodInfo.Invoke(destination, [e]);
		}
		else
		{
			StopListening();
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

internal sealed class WeakEventListener<T> : IWeakEventListener
{
	#region Fields

	private readonly bool _isAsync;
	private readonly MethodInfo _method;
	private WeakReference _weakTarget;

	#endregion

	#region Constructors

	// Constructor for synchronous handlers
	public WeakEventListener(object target, Action<T> handler)
	{
		if (target == null)
		{
			throw new ArgumentNullException(nameof(target));
		}
		if (handler == null)
		{
			throw new ArgumentNullException(nameof(handler));
		}

		_weakTarget = new WeakReference(target);
		_method = handler.Method;
		_isAsync = false;
	}

	// Constructor for asynchronous handlers
	public WeakEventListener(object target, Func<T, Task> handler)
	{
		if (target == null)
		{
			throw new ArgumentNullException(nameof(target));
		}
		if (handler == null)
		{
			throw new ArgumentNullException(nameof(handler));
		}

		_weakTarget = new WeakReference(target);
		_method = handler.Method;
		_isAsync = true;
	}

	#endregion

	#region Properties

	public object Destination => _weakTarget.Target;

	public bool IsAlive => _weakTarget.IsAlive;

	public object Source => null;

	#endregion

	#region Methods

	/// <summary>
	/// Synchronous fallback (optional, if you want to support both)
	/// </summary>
	public void DeliverMessage(T message)
	{
		// Fire-and-forget the async version
		_ = DeliverMessageAsync(message);
	}

	/// <summary>
	/// Delivers the message synchronously or asynchronously depending on the handler type.
	/// Fire-and-forget: exceptions are observed but not propagated (common pattern).
	/// </summary>
	public async Task DeliverMessageAsync(T message)
	{
		if (!_weakTarget.IsAlive)
		{
			return;
		}

		var target = _weakTarget.Target;
		if (target == null)
		{
			return;
		}

		try
		{
			var result = _method.Invoke(target, [message]);

			// If it's an async handler, await the Task
			if (_isAsync && result is Task task)
			{
				await task.ConfigureAwait(false);
			}

			// Otherwise: synchronous handler, already done
		}
		catch (Exception ex)
		{
			// Handle invocation errors (e.g., TargetInvocationException unwrap)
			var inner = ex.InnerException ?? ex;

			// Optionally log or report
			// In production: consider a global unhandled async exception handler
			// For now: fire-and-forget safe — do not rethrow
		}
	}

	public void StopListening()
	{
		_weakTarget = null;
	}

	#endregion
}