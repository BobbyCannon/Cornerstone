#region References

using System;

#endregion

namespace Cornerstone.Internal.WeakEvents;

internal interface IWeakEventListener
{
	#region Properties

	Delegate Handler { get; }
	
	bool IsAlive { get; }
	
	object Source { get; }

	#endregion

	#region Methods

	void StopListening();

	#endregion
}