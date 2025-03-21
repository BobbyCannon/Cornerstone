#region References

using System;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Client / Server Sync Times
/// </summary>
public struct SyncTimes : ISyncTimes
{
	#region Constructors

	/// <summary>
	/// Instantiates an instance of the class.
	/// </summary>
	/// <param name="type"> The type name of the sync. </param>
	/// <param name="client"> The date and time for the client. </param>
	/// <param name="server"> The date and time for the server. </param>
	public SyncTimes(string type, DateTime client, DateTime server)
	{
		SyncType = type;
		LastSyncedOnClient = client;
		LastSyncedOnServer = server;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public DateTime LastSyncedOnClient { get; set; }

	/// <inheritdoc />
	public DateTime LastSyncedOnServer { get; set; }

	/// <inheritdoc />
	public string SyncType { get; set; }

	#endregion
}

/// <summary>
/// Client / Server Sync Times
/// </summary>
public interface ISyncTimes
{
	#region Properties

	/// <summary>
	/// Gets or sets the last synced on date and time for the client.
	/// </summary>
	DateTime LastSyncedOnClient { get; set; }

	/// <summary>
	/// Gets or sets the last synced on date and time for the server.
	/// </summary>
	DateTime LastSyncedOnServer { get; set; }

	/// <summary>
	/// The type name of the sync.
	/// </summary>
	string SyncType { get; set; }

	#endregion
}