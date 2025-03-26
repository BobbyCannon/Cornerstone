#region References

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Internal.WeakEvents;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone;

/// <summary>
/// Provides methods for registering and unregistering event handlers that
/// don't cause memory "leaks" when the lifetime of the listener is longer
/// than the lifetime of the object being listened to.
/// </summary>
public static class WeakEventManager
{
	#region Fields

	private static readonly SpeedyList<IWeakEventListener> _listeners;

	#endregion

	#region Constructors

	static WeakEventManager()
	{
		_listeners = new();

		Listeners = new ReadOnlySpeedyList<IWeakEventListener>(_listeners);
	}

	#endregion

	#region Properties

	public static ReadOnlySpeedyList<IWeakEventListener> Listeners { get; }

	#endregion

	#region Methods

	public static void Add<T, T2, TArgs>(T source, string eventName, T2 destination, EventHandler<TArgs> handler)
		where T : class
		where T2 : class
	{
		_listeners.Add(new WeakEventListener<T, T2, TArgs>(source, typeof(T), eventName, destination, handler.Method));
	}

	/// <summary>
	/// Registers the given delegate as a handler for the event specified by [eventName] on the given source.
	/// </summary>
	public static void Add<T, T2, TArgs1, TArgs2>(T source, string eventName, T2 destination, EventHandler<TArgs1, TArgs2> handler)
		where T : class
		where T2 : class
	{
		_listeners.Add(new WeakEventListener<T, T2, TArgs1, TArgs2>(source, eventName, destination, handler.Method));
	}

	/// <summary>
	/// Registers the given delegate as a handler for the INotifyCollectionChanged.CollectionChanged event
	/// </summary>
	public static void AddCollectionChanged<T, T2>(T source, T2 destination, NotifyCollectionChangedEventHandler handler)
		where T : class, INotifyCollectionChanged
		where T2 : class
	{
		_listeners.Add(new CollectionChangedWeakEventListener<T, T2>(source, destination, handler));
	}

	/// <summary>
	/// Registers the given delegate as a handler for the INotifyPropertyChanged.PropertyChanged event
	/// </summary>
	public static void AddPropertyChanged<T, T2>(T source, T2 destination, PropertyChangedEventHandler handler)
		where T : class, INotifyPropertyChanged
		where T2 : class
	{
		_listeners.Add(new PropertyChangedWeakEventListener<T, T2>(source, destination, handler));
	}

	public static void AddSpeedyListUpdated<T, TArgs, T2>(T source, T2 destination, EventHandler<SpeedyListUpdatedEventArg<TArgs>> handler)
		where T : class, ISpeedyList<TArgs>
		where T2 : class
	{
		_listeners.Add(new ListChangedWeakEventListener<T, TArgs, T2>(source, destination, handler));
	}

	public static void CleanUp()
	{
		var deadListeners = _listeners.Where(x => !x.IsAlive).ToList();

		foreach (var weakEventListener in deadListeners)
		{
			weakEventListener.StopListening();

			_listeners.Remove(weakEventListener);
		}
	}

	/// <summary>
	/// Unregisters any previously registered weak event handlers on the given source object
	/// </summary>
	public static void Remove<T>(T source)
		where T : class
	{
		var toRemove = new List<IWeakEventListener>();
		foreach (var listener in _listeners)
		{
			if (!listener.IsAlive)
			{
				toRemove.Add(listener);
			}
			else if (listener.Source == source)
			{
				listener.StopListening();
				toRemove.Add(listener);
			}
		}
		foreach (var item in toRemove)
		{
			_listeners.Remove(item);
		}
	}

	/// <summary>
	/// Unregisters all weak event listeners that have been registered by this weak event manager instance
	/// </summary>
	public static void Reset()
	{
		foreach (var listener in _listeners)
		{
			if (listener.IsAlive)
			{
				listener.StopListening();
			}
		}
		_listeners.Clear();
	}

	#endregion
}

public interface IWeakEventListener
{
	#region Properties

	bool IsAlive { get; }

	object Source { get; }

	#endregion

	#region Methods

	void StopListening();

	#endregion
}