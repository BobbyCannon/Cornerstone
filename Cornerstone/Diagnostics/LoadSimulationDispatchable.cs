#region References

using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Diagnostics;

/// <summary>
/// Synthetic feature ViewModel used by diagnostics to exercise real AppDispatcher apply traffic.
/// Track and attach it like any other root; call <see cref="MarkWork" /> to dirty for the next poll.
/// </summary>
public sealed class LoadSimulationDispatchable : DispatchableViewModel
{
	#region Fields

	private readonly DispatchPending _workPending;

	#endregion

	#region Constructors

	public LoadSimulationDispatchable()
	{
		_workPending = new DispatchPending();
		TrackBinding(_workPending, ApplyWork);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Number of times this root has applied simulated work (feature apply path).
	/// </summary>
	public int ApplyCount { get; private set; }

	/// <summary>
	/// Short status for diagnostics projection.
	/// </summary>
	public string Status { get; private set; } = "Idle";

	#endregion

	#region Methods

	/// <summary>
	/// Stages one unit of work so the next feature apply increments counters.
	/// </summary>
	public void MarkWork()
	{
		_workPending.MarkPending();
	}

	public void Reset()
	{
		ApplyCount = 0;
		Status = "Idle";
		_workPending.MarkPending();
	}

	private void ApplyWork()
	{
		ApplyCount++;
		Status = $"Applied {ApplyCount}";
	}

	#endregion
}
