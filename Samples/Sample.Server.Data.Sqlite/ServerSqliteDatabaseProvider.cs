#region References

using Cornerstone.Storage;
using Sample.Shared.Storage;

#endregion

namespace Sample.Server.Data.Sqlite;

public class ServerSqliteDatabaseProvider : DatabaseProvider<IServerDatabase>
{
	#region Fields

	private readonly string _connectionString;

	#endregion

	#region Constructors

	public ServerSqliteDatabaseProvider(string connectionString)
		: base(new DatabaseOptions())
	{
		_connectionString = connectionString;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override IServerDatabase GetDatabaseFromProvider(DatabaseOptions options)
	{
		return ServerSqliteDatabase.UseSqlite(_connectionString, options);
	}

	#endregion
}