#region References

using System;
using System.ComponentModel;

#endregion

namespace Cornerstone.Internal.WeakEvents;

internal class PropertyChangedWeakEventListener<T> : WeakEventListenerBase<T, PropertyChangedEventArgs>
	where T : class, INotifyPropertyChanged
{
	#region Constructors

	public PropertyChangedWeakEventListener(T source, EventHandler<PropertyChangedEventArgs> handler)
		: base(source, handler)
	{
		source.PropertyChanged += HandleEvent;
	}

	#endregion

	#region Methods

	protected override void StopListening(T source)
	{
		source.PropertyChanged -= HandleEvent;
	}

	#endregion
}