#region References

using Cornerstone.Sync;
using Sample.Shared.Storage;

#endregion

namespace Sample.Client.Data.Sqlite;

public class ClientSqliteDatabaseProvider : SyncableDatabaseProvider<IClientDatabase>
{
	#region Fields

	private readonly string _connectionString;

	#endregion

	#region Constructors

	public ClientSqliteDatabaseProvider(string connectionString)
	{
		_connectionString = connectionString;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override string[] GetSyncOrder()
	{
		return ClientMemoryDatabase.GetSyncOrder();
	}

	/// <inheritdoc />
	protected override IClientDatabase GetDatabaseFromProvider()
	{
		return ClientSqliteDatabase.UseSqlite(_connectionString);
	}

	#endregion
}