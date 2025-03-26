#region References

using System;
using Cornerstone.Presentation;
using Cornerstone.Threading;

#endregion

namespace Cornerstone.Profiling;

public class DebounceAndThrottleManager : WorkerManager
{
	#region Constructors

	/// <inheritdoc />
	public DebounceAndThrottleManager( IDispatcher dispatcher) : base(10, dispatcher)
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