#region References

using System;
using System.Collections.Specialized;

#endregion

namespace Cornerstone.Internal.WeakEvents;

internal class CollectionChangedWeakEventListener<T, T2>
	: WeakEventListener<T, T2, NotifyCollectionChangedEventArgs>
	where T : class, INotifyCollectionChanged
	where T2 : class
{
	#region Constructors

	public CollectionChangedWeakEventListener(T source, T2 destination, NotifyCollectionChangedEventHandler handler)
		: base(source, nameof(source.CollectionChanged), destination, handler.Method)
	{
	}

	#endregion
}