#region References

using Cornerstone.Runtime;
using Cornerstone.Sync;
using Sample.Shared.Storage;

#endregion

namespace Sample.Server.Data;

public abstract class ServerDatabaseProvider : SyncableDatabaseProvider<IServerDatabase>
{
	#region Constructors

	/// <inheritdoc />
	protected ServerDatabaseProvider(IDateTimeProvider dateTimeProvider) : base(dateTimeProvider)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override string[] GetSyncOrder()
	{
		return ServerMemoryDatabase.GetSyncOrder();
	}

	#endregion
}