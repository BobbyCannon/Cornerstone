#region References

using Cornerstone.Presentation;
using Cornerstone.Profiling;

#endregion

namespace Cornerstone.Collections;

/// <inheritdoc />
public class SpeedyListProfiler : DetailedTimer
{
	#region Constructors

	/// <inheritdoc />
	public SpeedyListProfiler(IDispatcher dispatcher) : base("Speedy List Profiler", dispatcher)
	{
		AddedCount = new Counter(dispatcher);
		OrderCount = new Counter(dispatcher);
		RemovedCount = new Counter(dispatcher);
	}

	#endregion

	#region Properties

	/// <summary>
	/// The number of items added to the list.
	/// </summary>
	public Counter AddedCount { get; }

	/// <summary>
	/// The amount of times the list was ordered.
	/// </summary>
	public Counter OrderCount { get; }

	/// <summary>
	/// The number of items removed the list.
	/// </summary>
	public Counter RemovedCount { get; }

	#endregion
}