#region References

using System;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Get-only sync session fields for AppDispatcher projection.
/// </summary>
public interface ISyncSessionStatus
{
	#region Properties

	TimeSpan Elapsed { get; }

	decimal Percent { get; }

	DateTime StartedOn { get; }

	DateTime StoppedOn { get; }

	bool SyncCancelled { get; }

	bool SyncCompleted { get; }

	bool SyncRunning { get; }

	bool SyncSuccessful { get; }

	string SyncType { get; }

	#endregion
}