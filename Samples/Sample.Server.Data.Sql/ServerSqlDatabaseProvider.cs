#region References

using Cornerstone.Runtime;
using Cornerstone.Storage;
using Sample.Shared.Storage;

#endregion

namespace Sample.Server.Data.Sql;

public class ServerSqlDatabaseProvider : ServerDatabaseProvider
{
	#region Fields

	private readonly string _connectionString;

	#endregion

	#region Constructors

	public ServerSqlDatabaseProvider(string connectionString, IDateTimeProvider dateTimeProvider, DatabaseKeyCache keyCache)
		: base(dateTimeProvider, keyCache)
	{
		_connectionString = connectionString;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override IServerDatabase GetDatabaseFromProvider()
	{
		return ServerSqlDatabase.UseSqlServer(_connectionString);
	}

	protected override IServerDatabase GetDatabaseFromProvider(DatabaseSettings settings, DatabaseKeyCache keyCache)
	{
		return ServerSqlDatabase.UseSqlServer(_connectionString, settings, keyCache);
	}

	#endregion
}