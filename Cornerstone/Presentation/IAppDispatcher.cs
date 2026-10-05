#region References

using Cornerstone.Profiling;

#endregion

namespace Cornerstone.Presentation;

public interface IAppDispatcher
{
	#region Properties

	/// <summary>
	/// Optional system profiler. When null, AppDispatcher does no profiling work.
	/// Assign to opt into scopes such as <see cref="ApplicationViewModel.ApplyScopeName" />.
	/// </summary>
	Profiler SystemProfiler { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Apply pending Track* bindings on the UI dispatcher and wait until that apply
	/// finishes. After this returns, the ViewModel has been seeded once. Attach uses
	/// this so the first paint is not an empty bag. Later updates still come from the poll.
	/// </summary>
	void ApplyPendingTracks(DispatchableViewModel dispatchableViewModel);

	void Release(DispatchableViewModel dispatchableViewModel);

	/// <summary>
	/// Thread-safe coalescing wake: ends the current idle/active wait early and
	/// moves the worker into the high-rate active mode. Call after staging model work
	/// when low-latency projection matters; not required for correctness (idle poll still applies).
	/// </summary>
	void RequestDispatch();

	void Track(DispatchableViewModel dispatchableViewModel);

	#endregion
}