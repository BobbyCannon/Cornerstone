#region References

using Sample.Shared.Storage;

#endregion

namespace Sample.Server.Data.Sqlite;

public class ServerSqliteDatabaseProvider : ServerDatabaseProvider
{
	#region Fields

	private readonly string _connectionString;

	#endregion

	#region Constructors

	public ServerSqliteDatabaseProvider(string connectionString)
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

	#endregion
}