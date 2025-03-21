#region References

using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Logging;

/// <summary>
/// In memory tracker path repository.
/// </summary>
public class MemoryTrackerRepository : Bindable, ITrackerRepository
{
	#region Constructors

	/// <summary>
	/// Instantiates an instance of the path repository.
	/// </summary>
	public MemoryTrackerRepository(IDispatcher dispatcher) : base(dispatcher)
	{
		Paths = new SpeedyList<TrackerPath>(dispatcher, new OrderBy<TrackerPath>(x => x.StartedOn, true)) { Limit = 20 };
	}

	#endregion

	#region Properties

	/// <summary>
	/// The paths for this repository.
	/// </summary>
	public SpeedyList<TrackerPath> Paths { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Clears the paths from the repository.
	/// </summary>
	public void Clear()
	{
		Paths.Clear();
	}

	/// <summary>
	/// Writes paths to this repository.
	/// </summary>
	/// <param name="paths"> The paths to be added. </param>
	public void Write(params TrackerPath[] paths)
	{
		Paths.Add(paths);
	}

	#endregion
}