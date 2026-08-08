#region References

using System;
using Cornerstone.Location;

#endregion

namespace Cornerstone.Sync;

public interface IServerSyncSession : ISyncSession
{
	#region Properties

	public bool IsCompleted { get; set; }

	public Guid SessionId { get; set; }

	public DateTime StartedOn { get; set; }

	public DateTime StoppedOn { get; set; }

	public SyncDirection SyncDirection { get; set; }

	public string SyncIssues { get; set; }

	public string SyncSettings { get; set; }

	public string SyncStatistics { get; set; }

	public string SyncType { get; set; }

	#endregion
}

/// <summary>
/// Represents a sync session
/// </summary>
public interface ISyncSession : IBasicLocation, ISyncClientDetails
{
	#region Properties

	/// <summary>
	/// The last date and time the location was updated.
	/// </summary>
	DateTime LocationUpdatedOn { get; set; }

	#endregion
}