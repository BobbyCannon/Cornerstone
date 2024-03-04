#region References

using Cornerstone.Sync;

#endregion

namespace Sample.Server.Data;

public class ServerSyncClient : SyncClient
{
	#region Constructors

	/// <inheritdoc />
	public ServerSyncClient(ISyncableDatabaseProvider provider)
		: base("Server Sync Client", provider)
	{
	}

	#endregion
}