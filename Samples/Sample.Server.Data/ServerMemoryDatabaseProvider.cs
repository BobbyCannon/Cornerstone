#region References

using Cornerstone.Runtime;
using Cornerstone.Sync;
using Sample.Shared.Storage;

#endregion

namespace Sample.Server.Data;

public class ServerMemoryDatabaseProvider : SyncableDatabaseProvider<IServerDatabase>
{
	#region Fields

	private readonly ServerMemoryDatabase _database;

	#endregion

	#region Constructors

	public ServerMemoryDatabaseProvider(IDateTimeProvider dateTimeProvider) : base(dateTimeProvider)
	{
		_database = new ServerMemoryDatabase();
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override string[] GetSyncOrder()
	{
		return ServerMemoryDatabase.GetSyncOrder();
	}

	/// <inheritdoc />
	protected override IServerDatabase GetDatabaseFromProvider()
	{
		return _database;
	}

	#endregion
}