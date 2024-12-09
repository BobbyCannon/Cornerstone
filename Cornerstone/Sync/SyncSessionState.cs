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
	/// The sync session is initializing.
	/// </summary>
	Initializing = 1 << 1,

	/// <summary>
	/// The sync setting is starting.
	/// </summary>
	Starting = 1 << 2,

	/// <summary>
	/// The sync sessions has started.
	/// </summary>
	Started = 1 << 3,

	/// <summary>
	/// The stage to pull data from the server and apply to the client.
	/// </summary>
	Pulling = 1 << 4,

	/// <summary>
	/// This stage is to push changes from the client and apply to the server.
	/// </summary>
	Pushing = 1 << 5,

	/// <summary>
	/// The sync session is starting to complete.
	/// </summary>
	Completing = 1 << 6,

	/// <summary>
	/// The sync session is completed. Note: this does not mean it's successful.
	/// </summary>
	Completed = 1 << 7,

	/// <summary>
	/// The sync session was cancelled.
	/// </summary>
	Cancelled = 1 << 8,

	/// <summary>
	/// The sync session was cancelled.
	/// </summary>
	Successful = 1 << 9,

	/// <summary>
	/// The sync session was not able to start.
	/// </summary>
	CouldNotStart = 1 << 10
}