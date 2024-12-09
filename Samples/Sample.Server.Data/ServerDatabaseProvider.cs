#region References

using Cornerstone.Sync;
using Sample.Shared.Storage;

#endregion

namespace Sample.Server.Data;

public abstract class ServerDatabaseProvider : SyncableDatabaseProvider<IServerDatabase>
{
	#region Methods

	/// <inheritdoc />
	public override string[] GetSyncOrder()
	{
		return ServerMemoryDatabase.GetSyncOrder();
	}

	#endregion
}