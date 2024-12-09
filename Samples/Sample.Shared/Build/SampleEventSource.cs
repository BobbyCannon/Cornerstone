#region References

using System;
using System.Collections.Specialized;
using System.ComponentModel;

#endregion

namespace Sample.Shared.Build;

public class SampleEventSource<T> : INotifyPropertyChanged
{
	#region Constructors

	public SampleEventSource()
	{
		Id = Guid.NewGuid();
	}

	#endregion

	#region Properties

	public Guid Id { get; }

	#endregion

	#region Methods

	public bool RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
	{
		var eventHandler = CollectionChanged;
		if (eventHandler == null)
		{
			return false;
		}
		eventHandler.Invoke(this, e);
		return true;
	}

	public bool RaiseEventA1()
	{
		var eventHandler = EventA;
		if (eventHandler == null)
		{
			return false;
		}
		eventHandler(this, EventArgs.Empty);
		return true;
	}

	public bool RaiseEventA2(EventArgs e)
	{
		var eventHandler = EventA;
		if (eventHandler == null)
		{
			return false;
		}
		eventHandler.Invoke(this, e);
		return true;
	}

	public bool RaiseEventB(bool value)
	{
		var eventHandler = EventB;
		if (eventHandler == null)
		{
			return false;
		}
		eventHandler.Invoke(this, new SampleEventSourceEventArgs(value));
		return true;
	}

	public bool RaiseEventC()
	{
		var eventHandler = EventC;
		if (eventHandler == null)
		{
			return false;
		}
		eventHandler.Invoke(this, EventArgs.Empty);
		return true;
	}

	public bool OnPropertyChanged(string propertyName)
	{
		var eventHandler = PropertyChanged;
		if (eventHandler == null)
		{
			return false;
		}
		eventHandler.Invoke(this, new PropertyChangedEventArgs(propertyName));
		return true;
	}

	public void Test()
	{
	}

	#endregion

	#region Events

	public event NotifyCollectionChangedEventHandler CollectionChanged;
	public event EventHandler<EventArgs> EventA;
	public event EventHandler<SampleEventSourceEventArgs> EventB;
	public event EventHandler<EventArgs> EventC;
	public event PropertyChangedEventHandler PropertyChanged;

	#endregion
}