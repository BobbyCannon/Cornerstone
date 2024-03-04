#region References

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

	public ClientSqliteDatabaseProvider(string connectionString)
		: base(new DatabaseOptions())
	{
		_connectionString = connectionString;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override IClientDatabase GetDatabaseFromProvider(DatabaseOptions options)
	{
		return ClientSqliteDatabase.UseSqlite(_connectionString, options);
	}

	#endregion
}