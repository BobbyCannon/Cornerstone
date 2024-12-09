#region References

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Cornerstone;

#endregion

namespace Sample.Shared.Build;

public class SampleEventSource2<T> : INotifyCollectionChanged, INotifyPropertyChanged
{
	#region Fields

	private readonly WeakEventHandler<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs> _collectionChangedEvent;
	private readonly WeakEventHandler<PropertyChangedEventHandler, PropertyChangedEventArgs> _propertyChangedEvent;

	#endregion

	#region Constructors

	public SampleEventSource2()
	{
		_collectionChangedEvent = new WeakEventHandler<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>();
		_propertyChangedEvent = new WeakEventHandler<PropertyChangedEventHandler, PropertyChangedEventArgs>();

		Id = Guid.NewGuid();
	}

	#endregion

	#region Properties

	public Guid Id { get; }

	#endregion

	#region Methods

	public void OnCollectionChanged()
	{
		_collectionChangedEvent.Raise(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<int> { 1, 2, 3 }, 0));
	}

	public void OnPropertyChanged(string name)
	{
		_propertyChangedEvent.Raise(this, new PropertyChangedEventArgs(name));
	}

	#endregion

	#region Events

	/// <inheritdoc />
	public event NotifyCollectionChangedEventHandler CollectionChanged
	{
		add => _collectionChangedEvent.AddListener(value);
		remove => _collectionChangedEvent.RemoveListener(value);
	}

	public event PropertyChangedEventHandler PropertyChanged
	{
		add => _propertyChangedEvent.AddListener(value);
		remove => _propertyChangedEvent.RemoveListener(value);
	}

	#endregion
}