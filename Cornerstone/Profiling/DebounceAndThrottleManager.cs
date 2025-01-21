#region References

using System;
using System.Threading;
using Cornerstone.Presentation;
using Cornerstone.Threading;

#endregion

namespace Cornerstone.Profiling;

public class DebounceAndThrottleManager : WorkerManager
{
	#region Constructors

	/// <inheritdoc />
	public DebounceAndThrottleManager(IWeakEventManager weakEventManager, IDispatcher dispatcher)
		: base(10, weakEventManager, dispatcher)
	{
		
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void Work(TimeSpan elapsed)
	{
	}

	#endregion
}