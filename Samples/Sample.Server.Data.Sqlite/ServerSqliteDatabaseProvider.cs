#region References

using Cornerstone.Runtime;
using Sample.Shared.Storage;

#endregion

namespace Sample.Server.Data.Sqlite;

public class ServerSqliteDatabaseProvider : ServerDatabaseProvider
{
	#region Fields

	private readonly string _connectionString;

	#endregion

	#region Constructors

	public ServerSqliteDatabaseProvider(string connectionString, IDateTimeProvider dateTimeProvider) : base(dateTimeProvider)
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