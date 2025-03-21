#region References

using Cornerstone.Runtime;
using Cornerstone.Storage;
using Sample.Shared.Storage;

#endregion

namespace Sample.Server.Data.Sqlite;

public class ServerSqliteDatabaseProvider : ServerDatabaseProvider
{
	#region Fields

	private readonly string _connectionString;

	#endregion

	#region Constructors

	public ServerSqliteDatabaseProvider(string connectionString, IDateTimeProvider dateTimeProvider, DatabaseKeyCache keyCache)
		: base(dateTimeProvider, keyCache)
	{
		_connectionString = connectionString;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override IServerDatabase GetDatabaseFromProvider()
	{
		return ServerSqliteDatabase.UseSqlite(_connectionString);
	}

	protected override IServerDatabase GetDatabaseFromProvider(DatabaseSettings settings, DatabaseKeyCache keyCache)
	{
		return ServerSqliteDatabase.UseSqlite(_connectionString, settings, keyCache);
	}

	#endregion
}