#region References

using System;
using System.ComponentModel;

#endregion

namespace Cornerstone.TestAssembly;

public class PropertyChangeEventTracker<T>
	where T : INotifyPropertyChanged
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

	public void UnsubscribeEvents()
	{
		_source.PropertyChanged -= SourceOnPropertyChanged;
	}

	private void SourceOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		_eventTracer(e.PropertyName);
	}

	#endregion
}