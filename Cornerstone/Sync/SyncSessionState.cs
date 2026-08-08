#region References

using System;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents the different states of syncing.
/// </summary>
[Flags]
public enum SyncSessionState
{
	/// <summary>
	/// The sync session has no state.
	/// </summary>
	Unknown = 0,

	/// <summary>
	/// The sync sessions has started.
	/// </summary>
	Started = 0b0001,

	/// <summary>
	/// The sync session has started configuring.
	/// </summary>
	Configuring = 0b0010,

	/// <summary>
	/// The sync session has been configured.
	/// </summary>
	Configured = 0b0100,

	/// <summary>
	/// The sync session is beginning sync.
	/// </summary>
	Beginning = 0b1000,

	/// <summary>
	/// The stage to pull data from the server and apply to the client.
	/// </summary>
	Pulling = 0b0001_0000,

	/// <summary>
	/// This stage is to push changes from the client and apply to the server.
	/// </summary>
	Pushing = 0b0010_0000,

	/// <summary>
	/// The sync session is ending.
	/// </summary>
	Ending = 0b0100_0000,

	/// <summary>
	/// The sync session is completed. Note: this does not mean it's successful.
	/// </summary>
	Completed = 0b1000_0000,

	/// <summary>
	/// The sync session was cancelled.
	/// </summary>
	Cancelled = 0b0001_0000_0000,

	/// <summary>
	/// The sync session was cancelled.
	/// </summary>
	Successful = 0b0010_0000_0000,

	/// <summary>
	/// The sync session was not able to start.
	/// </summary>
	CouldNotStart = 0b0100_0000_0000
}