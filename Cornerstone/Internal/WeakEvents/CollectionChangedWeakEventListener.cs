#region References

using System;
using System.Collections.Specialized;

#endregion

namespace Cornerstone.Internal.WeakEvents;

internal class CollectionChangedWeakEventListener<T> : WeakEventListenerBase<T, NotifyCollectionChangedEventArgs>
	where T : class, INotifyCollectionChanged
{
	#region Constructors

	public CollectionChangedWeakEventListener(T source, EventHandler<NotifyCollectionChangedEventArgs> handler)
		: base(source, handler)
	{
		source.CollectionChanged += HandleEvent;
	}

	#endregion

	#region Methods

	protected override void StopListening(T source)
	{
		source.CollectionChanged -= HandleEvent;
	}

	#endregion
}