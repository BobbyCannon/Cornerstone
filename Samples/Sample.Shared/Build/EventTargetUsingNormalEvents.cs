#region References

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using Cornerstone.Extensions;

#endregion

namespace Sample.Shared.Build;

public class EventTargetUsingNormalEvents<T> : IEventTarget
{
	#region Fields

	private readonly Action<string> _eventTracer;
	private readonly SampleEventSource<T> _source;

	#endregion

	#region Constructors

	public EventTargetUsingNormalEvents(SampleEventSource<T> source, Action<string> eventTracer)
	{
		_source = source;
		_eventTracer = eventTracer;
	}

	~EventTargetUsingNormalEvents()
	{
	}

	#endregion

	#region Methods

	public void SubscribeEvents()
	{
		_source.CollectionChanged += SourceOnCollectionChanged;
		_source.EventA += SourceEventA;
		_source.EventB += SourceEventB;
		_source.EventC += SourceEventA;
		_source.PropertyChanged += Source_PropertyChanged;
	}

	public void UnsubscribeAll()
	{
		UnsubscribeEvents();
	}

	public void UnsubscribeEvents()
	{
		_source.CollectionChanged -= SourceOnCollectionChanged;
		_source.EventA -= SourceEventA;
		_source.EventB -= SourceEventB;
		_source.EventC -= SourceEventA;
		_source.PropertyChanged -= Source_PropertyChanged;
	}

	private void Source_PropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		_eventTracer("PropertyChanged: " + e.PropertyName);
	}

	private void SourceEventA(object sender, EventArgs e)
	{
		_eventTracer("EventA");
	}

	private void SourceEventB(object sender, SampleEventSourceEventArgs e)
	{
		_eventTracer("EventB " + e.Cancel);
	}

	private void SourceOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.NewItems != null)
		{
			_eventTracer($"CollectionChanged: {e.Action}({string.Join(",", e.NewItems.ToObjectArray())}) : {e.NewStartingIndex}");
		}
		
		if (e.OldItems != null)
		{
			_eventTracer($"CollectionChanged: {e.Action}({string.Join(",", e.OldItems.ToObjectArray())}) : {e.OldStartingIndex}");
		}
	}

	#endregion
}