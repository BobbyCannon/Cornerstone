namespace Cornerstone.Sync;

/// <summary>
/// Represents the different states of syncing.
/// </summary>
public enum SyncSessionState
{
	/// <summary>
	/// The sync session is initializing.
	/// </summary>
	Initializing = 0,

	/// <summary>
	/// The sync setting is starting.
	/// </summary>
	Starting = 1,

	/// <summary>
	/// The sync sessions has started.
	/// </summary>
	Started = 2,

	/// <summary>
	/// The stage to pull data from the server and apply to the client.
	/// </summary>
	Pulling = 3,

	/// <summary>
	/// This stage is to push changes from the client and apply to the server.
	/// </summary>
	Pushing = 4,

	/// <summary>
	/// The sync session is ending.
	/// </summary>
	Ending = 5,

	/// <summary>
	/// The sync session is completed. Note: this does not mean it's successful.
	/// </summary>
	Completed = 6
}