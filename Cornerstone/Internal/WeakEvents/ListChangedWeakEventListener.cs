#region References

using System;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Internal.WeakEvents;

internal class ListChangedWeakEventListener<T, T2, T3>
	: WeakEventListener<T, T3, SpeedyListUpdatedEventArg<T2>>
	where T : class, ISpeedyList<T2>
	where T3 : class
{
	#region Constructors

	public ListChangedWeakEventListener(T source, T3 destination, EventHandler<SpeedyListUpdatedEventArg<T2>> handler)
		: base(source, nameof(source.ListUpdated), destination, handler.Method)
	{
	}

	#endregion
}