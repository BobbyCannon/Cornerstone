#region References

using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Diagnostics;

/// <summary>
/// Optional hook invoked from the AppDispatcher worker each poll tick when registered.
/// Snapshots runtime state into diagnostics models (UI-free). Null on the host means zero cost.
/// </summary>
public interface IDiagnosticsCapture
{
	#region Methods

	/// <summary>
	/// Called on the dispatcher worker after the pending set is known for this tick
	/// (before UI ApplyModelChanges). Mutate models and mark them pending when values change.
	/// </summary>
	/// <param name="host"> The application dispatcher host. </param>
	/// <param name="pendingApplyCount"> Count of attached roots that were dirty before capture. </param>
	void Capture(ApplicationViewModel host, int pendingApplyCount);

	#endregion
}
