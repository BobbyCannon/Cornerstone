#region References

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Cornerstone.Collections;
using Cornerstone.Internal.WeakEvents;

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

	internal readonly SpeedyDictionary<IWeakEventListener, Delegate> _listeners;

	#endregion

	#region Constructors

	public WeakEventManager()
	{
		_listeners = new();
	}

	#endregion

	#region Methods

	/// <summary>
	/// Registers the given delegate as a handler for the event specified by [eventName] on the given source.
	/// </summary>
	public void Add<T, TArgs>(T source, string eventName, EventHandler<TArgs> handler)
		where T : class
	{
		_listeners.Add(new WeakEventListener<T, TArgs>(source, eventName, handler), handler);
	}

	/// <summary>
	/// Registers the given delegate as a handler for the INotifyCollectionChanged.CollectionChanged event
	/// </summary>
	public void AddCollectionChanged<T>(T source, EventHandler<NotifyCollectionChangedEventArgs> handler)
		where T : class, INotifyCollectionChanged
	{
		_listeners.Add(new CollectionChangedWeakEventListener<T>(source, handler), handler);
	}

	/// <summary>
	/// Registers the given delegate as a handler for the INotifyPropertyChanged.PropertyChanged event
	/// </summary>
	public void AddPropertyChanged<T>(T source, EventHandler<PropertyChangedEventArgs> handler)
		where T : class, INotifyPropertyChanged
	{
		_listeners.Add(new PropertyChangedWeakEventListener<T>(source, handler), handler);
	}

	/// <summary>
	/// Unregisters any previously registered weak event handlers on the given source object
	/// </summary>
	public void Remove<T>(T source)
		where T : class
	{
		var toRemove = new List<IWeakEventListener>();
		foreach (var listener in _listeners.Keys)
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
		foreach (var listener in _listeners.Keys)
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
	void Add<T, TArgs>(T source, string eventName, EventHandler<TArgs> handler)
		where T : class;

	/// <summary>
	/// Registers the given delegate as a handler for the INotifyCollectionChanged.CollectionChanged event
	/// </summary>
	void AddCollectionChanged<T>(T source, EventHandler<NotifyCollectionChangedEventArgs> handler)
		where T : class, INotifyCollectionChanged;

	/// <summary>
	/// Registers the given delegate as a handler for the INotifyPropertyChanged.PropertyChanged event
	/// </summary>
	void AddPropertyChanged<T>(T source, EventHandler<PropertyChangedEventArgs> handler)
		where T : class, INotifyPropertyChanged;

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