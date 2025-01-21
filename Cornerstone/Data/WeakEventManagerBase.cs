#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// WeakEventManager base class. Inspired by the WPF WeakEventManager class and the code in
/// https://social.msdn.microsoft.com/Forums/silverlight/en-US/34d85c3f-52ea-4adc-bb32-8297f5549042/command-binding-memory-leak?forum=silverlightbugs
/// </summary>
/// <remarks> Copied here from ReactiveUI due to bugs in its design (singleton instance for multiple events). </remarks>
/// <typeparam name="TEventManager"> </typeparam>
/// <typeparam name="TEventSource"> The type of the event source. </typeparam>
/// <typeparam name="TEventHandler"> The type of the event handler. </typeparam>
/// <typeparam name="TEventArgs"> The type of the event arguments. </typeparam>
public abstract class WeakEventManagerBase<TEventManager, TEventSource, TEventHandler, TEventArgs>
	where TEventManager : WeakEventManagerBase<TEventManager, TEventSource, TEventHandler, TEventArgs>, new()
{
	#region Fields

	/// <summary>
	/// Mapping from the source of the event to the list of handlers. This is a CWT to ensure it does not leak the source of the event.
	/// </summary>
	private readonly ConditionalWeakTable<object, WeakHandlerList> _sourceToWeakHandlers = new();

	/// <summary>
	/// Mapping between the target of the delegate (for example a Button) and the handler (EventHandler).
	/// Windows Phone needs this, otherwise the event handler gets garbage collected.
	/// </summary>
	private readonly ConditionalWeakTable<object, List<Delegate>> _targetToEventHandler = new();

	private static readonly Lazy<TEventManager> CurrentLazy = new(() => new TEventManager());

	// ReSharper disable once StaticMemberInGenericType
	private static readonly object StaticSource = new();

	#endregion

	#region Properties

	private static TEventManager Current => CurrentLazy.Value;

	#endregion

	#region Methods

	/// <summary>
	/// Adds a weak reference to the handler and associates it with the source.
	/// </summary>
	/// <param name="source"> The source. </param>
	/// <param name="handler"> The handler. </param>
	public static void AddHandler(TEventSource source, TEventHandler handler)
	{
		Current.PrivateAddHandler(source, handler);
	}

	/// <summary>
	/// Removes the association between the source and the handler.
	/// </summary>
	/// <param name="source"> The source. </param>
	/// <param name="handler"> The handler. </param>
	public static void RemoveHandler(TEventSource source, TEventHandler handler)
	{
		Current.PrivateRemoveHandler(source, handler);
	}

	/// <summary>
	/// Delivers the event to the handlers registered for the source.
	/// </summary>
	/// <param name="sender"> The sender. </param>
	/// <param name="args"> The event args containing the event data. </param>
	protected static void DeliverEvent(object sender, TEventArgs args)
	{
		Current.PrivateDeliverEvent(sender, args);
	}

	protected void PrivateAddHandler(TEventSource source, TEventHandler handler)
	{
		if (source == null)
		{
			throw new ArgumentNullException(nameof(source));
		}
		if (handler == null)
		{
			throw new ArgumentNullException(nameof(handler));
		}

		if (!typeof(TEventHandler).GetTypeInfo().IsSubclassOf(typeof(Delegate)))
		{
			throw new ArgumentException("Handler must be Delegate type");
		}

		AddWeakHandler(source, handler);
		AddTargetHandler(handler);
	}

	protected void PrivateRemoveHandler(TEventSource source, TEventHandler handler)
	{
		if (source == null)
		{
			throw new ArgumentNullException(nameof(source));
		}
		if (handler == null)
		{
			throw new ArgumentNullException(nameof(handler));
		}

		if (!typeof(TEventHandler).GetTypeInfo().IsSubclassOf(typeof(Delegate)))
		{
			throw new ArgumentException("handler must be Delegate type");
		}

		RemoveWeakHandler(source, handler);
		RemoveTargetHandler(handler);
	}

	/// <summary>
	/// Override this method to attach to an event.
	/// </summary>
	/// <param name="source"> The source. </param>
	protected abstract void StartListening(TEventSource source);

	/// <summary>
	/// Override this method to detach from an event.
	/// </summary>
	/// <param name="source"> The source. </param>
	protected abstract void StopListening(TEventSource source);

	private void AddTargetHandler(TEventHandler handler)
	{
		var @delegate = handler as Delegate;
		var key = @delegate?.Target ?? StaticSource;

		if (_targetToEventHandler.TryGetValue(key, out var delegates))
		{
			delegates.Add(@delegate);
		}
		else
		{
			delegates = [@delegate];

			_targetToEventHandler.Add(key, delegates);
		}
	}

	private void AddWeakHandler(TEventSource source, TEventHandler handler)
	{
		if (_sourceToWeakHandlers.TryGetValue(source, out var weakHandlers))
		{
			// clone list if we are currently delivering an event
			if (weakHandlers.IsDeliverActive)
			{
				weakHandlers = weakHandlers.Clone();
				_sourceToWeakHandlers.Remove(source);
				_sourceToWeakHandlers.Add(source, weakHandlers);
			}
			weakHandlers.AddWeakHandler(source, handler);
		}
		else
		{
			weakHandlers = new WeakHandlerList();
			weakHandlers.AddWeakHandler(source, handler);

			_sourceToWeakHandlers.Add(source, weakHandlers);
			StartListening(source);
		}

		Purge(source);
	}

	private void PrivateDeliverEvent(object sender, TEventArgs args)
	{
		var source = sender ?? StaticSource;

		var hasStaleEntries = false;

		if (_sourceToWeakHandlers.TryGetValue(source, out var weakHandlers))
		{
			using (weakHandlers.DeliverActive())
			{
				hasStaleEntries = weakHandlers.DeliverEvent(source, args);
			}
		}

		if (hasStaleEntries)
		{
			Purge(source);
		}
	}

	private void Purge(object source)
	{
		if (_sourceToWeakHandlers.TryGetValue(source, out var weakHandlers))
		{
			if (weakHandlers.IsDeliverActive)
			{
				weakHandlers = weakHandlers.Clone();
				_sourceToWeakHandlers.Remove(source);
				_sourceToWeakHandlers.Add(source, weakHandlers);
			}
			else
			{
				weakHandlers.Purge();
			}
		}
	}

	private void RemoveTargetHandler(TEventHandler handler)
	{
		var @delegate = handler as Delegate;
		var key = @delegate?.Target ?? StaticSource;

		if (_targetToEventHandler.TryGetValue(key, out var delegates))
		{
			delegates.Remove(@delegate);

			if (delegates.Count == 0)
			{
				_targetToEventHandler.Remove(key);
			}
		}
	}

	private void RemoveWeakHandler(TEventSource source, TEventHandler handler)
	{
		if (_sourceToWeakHandlers.TryGetValue(source, out var weakHandlers))
		{
			// clone list if we are currently delivering an event
			if (weakHandlers.IsDeliverActive)
			{
				weakHandlers = weakHandlers.Clone();
				_sourceToWeakHandlers.Remove(source);
				_sourceToWeakHandlers.Add(source, weakHandlers);
			}

			if (weakHandlers.RemoveWeakHandler(source, handler) && (weakHandlers.Count == 0))
			{
				_sourceToWeakHandlers.Remove(source);
				StopListening(source);
			}
		}
	}

	#endregion

	#region Classes

	internal class WeakHandler
	{
		#region Fields

		private readonly WeakReference _originalHandler;
		private readonly WeakReference _source;

		#endregion

		#region Constructors

		public WeakHandler(object source, TEventHandler originalHandler)
		{
			_source = new WeakReference(source);
			_originalHandler = new WeakReference(originalHandler);
		}

		#endregion

		#region Properties

		public TEventHandler Handler
		{
			get
			{
				if (_originalHandler == null)
				{
					return default;
				}
				return (TEventHandler) _originalHandler.Target;
			}
		}

		public bool IsActive => (_source != null) && _source.IsAlive && (_originalHandler != null) && _originalHandler.IsAlive;

		#endregion

		#region Methods

		public bool Matches(object o, TEventHandler handler)
		{
			return (_source != null) &&
				ReferenceEquals(_source.Target, o) &&
				(_originalHandler != null) &&
				(ReferenceEquals(_originalHandler.Target, handler) ||
					(_originalHandler.Target is TEventHandler &&
						handler is TEventHandler &&
						handler is Delegate del && _originalHandler.Target is Delegate origDel && Equals(del.Target, origDel.Target)));
		}

		#endregion
	}

	internal class WeakHandlerList
	{
		#region Fields

		private int _deliveries;
		private readonly List<WeakHandler> _handlers;

		#endregion

		#region Constructors

		public WeakHandlerList()
		{
			_handlers = [];
		}

		#endregion

		#region Properties

		public int Count => _handlers.Count;

		public bool IsDeliverActive => _deliveries > 0;

		#endregion

		#region Methods

		public void AddWeakHandler(TEventSource source, TEventHandler handler)
		{
			var handlerSink = new WeakHandler(source, handler);
			_handlers.Add(handlerSink);
		}

		public WeakHandlerList Clone()
		{
			var newList = new WeakHandlerList();
			newList._handlers.AddRange(_handlers.Where(h => h.IsActive));

			return newList;
		}

		public IDisposable DeliverActive()
		{
			Interlocked.Increment(ref _deliveries);

			return new Disposable(() => Interlocked.Decrement(ref _deliveries));
		}

		// ReSharper disable once MemberHidesStaticFromOuterClass
		public virtual bool DeliverEvent(object sender, TEventArgs args)
		{
			var hasStaleEntries = false;

			foreach (var handler in _handlers)
			{
				if (handler.IsActive)
				{
					var @delegate = handler.Handler as Delegate;
					@delegate?.DynamicInvoke(sender, args);
				}
				else
				{
					hasStaleEntries = true;
				}
			}

			return hasStaleEntries;
		}

		public void Purge()
		{
			for (var i = _handlers.Count - 1; i >= 0; i--)
			{
				if (!_handlers[i].IsActive)
				{
					_handlers.RemoveAt(i);
				}
			}
		}

		public bool RemoveWeakHandler(TEventSource source, TEventHandler handler)
		{
			foreach (var weakHandler in _handlers)
			{
				if (weakHandler.Matches(source, handler))
				{
					return _handlers.Remove(weakHandler);
				}
			}

			return false;
		}

		#endregion
	}

	#endregion
}

internal sealed class Disposable : IDisposable
{
	#region Fields

	private volatile Action _dispose;

	#endregion

	#region Constructors

	public Disposable(Action dispose)
	{
		_dispose = dispose;
	}

	#endregion

	#region Properties

	public bool IsDisposed => _dispose == null;

	#endregion

	#region Methods

	public void Dispose()
	{
		Interlocked.Exchange(ref _dispose, null)?.Invoke();
	}

	#endregion
}