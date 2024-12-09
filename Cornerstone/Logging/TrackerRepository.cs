#region References

using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Logging;

/// <summary>
/// In memory tracker path repository.
/// </summary>
public class TrackerRepository : Bindable, ITrackerRepository
{
	#region Constructors

	/// <summary>
	/// Instantiates an instance of the path repository.
	/// </summary>
	public TrackerRepository(IDispatcher dispatcher) : base(dispatcher)
	{
		Paths = new SpeedyList<TrackerPath>(dispatcher, new OrderBy<TrackerPath>(x => x.StartedOn, true)) { Limit = 20 };
	}

	#endregion

	#region Properties

	/// <summary>
	/// The paths for this repository.
	/// </summary>
	public ISpeedyList<TrackerPath> Paths { get; }

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

/// <summary>
/// Interface for storing tracker path data.
/// </summary>
public interface ITrackerRepository
{
	#region Methods

	/// <summary>
	/// Writes a collection of tracker paths.
	/// </summary>
	/// <param name="paths"> The tracker paths to write. </param>
	void Write(params TrackerPath[] paths);

	#endregion
}