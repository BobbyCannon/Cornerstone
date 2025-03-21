#region References

using Cornerstone.Runtime;
using Cornerstone.Storage;
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

	public ClientSqliteDatabaseProvider(string connectionString, IDateTimeProvider dateTimeProvider, DatabaseKeyCache keyCache)
		: base(dateTimeProvider, keyCache, IClientDatabase.GetDefaultDatabaseSettings())
	{
		_connectionString = connectionString;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override IClientDatabase GetDatabaseFromProvider()
	{
		return ClientSqliteDatabase.UseSqlite(_connectionString, Settings, KeyCache);
	}

	protected override IClientDatabase GetDatabaseFromProvider(DatabaseSettings settings, DatabaseKeyCache keyCache)
	{
		return ClientSqliteDatabase.UseSqlite(_connectionString, settings, keyCache);
	}

	#endregion
}