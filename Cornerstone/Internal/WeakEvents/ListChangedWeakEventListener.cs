#region References

using System;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Internal.WeakEvents;

internal class ListChangedWeakEventListener<T, T2, T3>
	: WeakEventListener<T, T3, PresentationListUpdatedEventArg<T2>>
	where T : class, IPresentationList<T2>
	where T3 : class
{
	#region Constructors

	public ListChangedWeakEventListener(T source, T3 destination, EventHandler<PresentationListUpdatedEventArg<T2>> handler)
		: base(source, typeof(IPresentationList<T2>), nameof(source.ListUpdated), destination, handler.Method)
	{
	}

	#endregion
}