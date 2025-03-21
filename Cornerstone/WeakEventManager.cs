#region References

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
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
public class WeakEventManager : IWeakEventManager
{
	#region Fields

	private readonly SpeedyList<IWeakEventListener> _listeners;

	#endregion

	#region Constructors

	public WeakEventManager()
	{
		_listeners = new();
	}

	#endregion

	#region Methods

	public IWeakEventListener Add<T, T2, TArgs>(T source, string eventName, T2 destination, EventHandler<TArgs> handler)
		where T : class
		where T2 : class
	{
		return _listeners.Add(new WeakEventListener<T, T2, TArgs>(source, eventName, destination, handler.Method));
	}

	/// <summary>
	/// Registers the given delegate as a handler for the event specified by [eventName] on the given source.
	/// </summary>
	public IWeakEventListener Add<T, T2, TArgs1, TArgs2>(T source, string eventName, T2 destination, EventHandler<TArgs1, TArgs2> handler)
		where T : class
		where T2 : class
	{
		return _listeners.Add(new WeakEventListener<T, T2, TArgs1, TArgs2>(source, eventName, destination, handler.Method));
	}

	/// <summary>
	/// Registers the given delegate as a handler for the INotifyCollectionChanged.CollectionChanged event
	/// </summary>
	public IWeakEventListener AddCollectionChanged<T, T2>(T source, T2 destination, NotifyCollectionChangedEventHandler handler)
		where T : class, INotifyCollectionChanged
		where T2 : class
	{
		return _listeners.Add(new CollectionChangedWeakEventListener<T, T2>(source, destination, handler));
	}

	/// <summary>
	/// Registers the given delegate as a handler for the INotifyPropertyChanged.PropertyChanged event
	/// </summary>
	public IWeakEventListener AddPropertyChanged<T, T2>(T source, T2 destination, PropertyChangedEventHandler handler)
		where T : class, INotifyPropertyChanged
		where T2 : class
	{
		return _listeners.Add(new PropertyChangedWeakEventListener<T, T2>(source, destination, handler));
	}

	public IWeakEventListener AddSpeedyListUpdated<T, TArgs, T2>(T source, T2 destination, EventHandler<SpeedyListUpdatedEventArg<TArgs>> handler)
		where T : class, ISpeedyList<TArgs>
		where T2 : class
	{
		return _listeners.Add(new ListChangedWeakEventListener<T, TArgs, T2>(source, destination, handler));
	}

	/// <summary>
	/// Unregisters any previously registered weak event handlers on the given source object
	/// </summary>
	public void Remove<T>(T source)
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
	public void Reset()
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

public interface IWeakEventManager
{
	#region Methods

	/// <summary>
	/// Registers the given delegate as a handler for the event specified by `eventName` on the given source.
	/// </summary>
	IWeakEventListener Add<T, T2, TArgs>(T source, string eventName, T2 destination, EventHandler<TArgs> handler)
		where T : class
		where T2 : class;

	/// <summary>
	/// Registers the given delegate as a handler for the event specified by [eventName] on the given source.
	/// </summary>
	IWeakEventListener Add<T, T2, TArgs1, TArgs2>(T source, string eventName, T2 destination, EventHandler<TArgs1, TArgs2> handler)
		where T : class
		where T2 : class;

	/// <summary>
	/// Registers the given delegate as a handler for the INotifyCollectionChanged.CollectionChanged event
	/// </summary>
	IWeakEventListener AddCollectionChanged<T, T2>(T source, T2 destination, NotifyCollectionChangedEventHandler handler)
		where T : class, INotifyCollectionChanged
		where T2 : class;

	/// <summary>
	/// Registers the given delegate as a handler for the INotifyPropertyChanged.PropertyChanged event
	/// </summary>
	IWeakEventListener AddPropertyChanged<T, T2>(T source, T2 destination, PropertyChangedEventHandler handler)
		where T : class, INotifyPropertyChanged
		where T2 : class;

	/// <summary>
	/// Registers the given delegate as a handler for the ISpeedyList.ListChanged event
	/// </summary>
	IWeakEventListener AddSpeedyListUpdated<T, TArgs, T2>(T source, T2 destination, EventHandler<SpeedyListUpdatedEventArg<TArgs>> handler)
		where T : class, ISpeedyList<TArgs>
		where T2 : class;

	/// <summary>
	/// Unregisters any previously registered weak event handlers on the given source object
	/// </summary>
	void Remove<T>(T source)
		where T : class;

	/// <summary>
	/// Unregisters all weak event listeners that have been registered by this weak event manager instance
	/// </summary>
	void Reset();

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