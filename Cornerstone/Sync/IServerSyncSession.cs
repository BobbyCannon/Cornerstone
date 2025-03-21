#region References

using System;

#endregion

namespace Cornerstone.Sync;

public interface IServerSyncSession : ISyncDevice
{
	#region Properties

	public bool IsCompleted { get; set; }

	public Guid SessionId { get; set; }

	public DateTime StartedOn { get; set; }

	public DateTime StoppedOn { get; set; }

	public SyncDirection SyncDirection { get; set; }

	public string SyncIssues { get; set; }

	public string SyncSettings { get; set; }

	public string SyncType { get; set; }

	#endregion
}