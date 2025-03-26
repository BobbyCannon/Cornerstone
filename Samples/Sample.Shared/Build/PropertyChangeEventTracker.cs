#region References

using System;
using System.ComponentModel;
using Cornerstone;

#endregion

namespace Sample.Shared.Build;

public class PropertyChangeEventTracker<T>
	where T : class, INotifyPropertyChanged
{
	#region Fields

	private readonly Action<string> _eventTracer;
	private readonly T _source;

	#endregion

	#region Constructors

	public PropertyChangeEventTracker(T source, Action<string> eventTracer)
	{
		_source = source;
		_eventTracer = eventTracer;
	}

	#endregion

	#region Methods

	public void SubscribeEvents()
	{
		_source.PropertyChanged += SourceOnPropertyChanged;
	}

	public void SubscribeEventsUsingWeak()
	{
		//_source.PropertyChanged += SourceOnPropertyChanged;
		WeakEventManager.AddPropertyChanged(_source, this, SourceOnPropertyChanged);
	}

	public void UnsubscribeEvents()
	{
		_source.PropertyChanged -= SourceOnPropertyChanged;
	}

	public void UnsubscribeEventsUsingWeak()
	{
		//_source.PropertyChanged -= SourceOnPropertyChanged;
		WeakEventManager.Remove(_source);
	}

	private void SourceOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		_eventTracer(e.PropertyName);
	}

	#endregion
}