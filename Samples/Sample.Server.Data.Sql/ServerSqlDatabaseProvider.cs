#region References

using Cornerstone.Storage;
using Sample.Shared.Storage;

#endregion

namespace Sample.Server.Data.Sql;

public class ServerSqlDatabaseProvider : DatabaseProvider<IServerDatabase>
{
	#region Fields

	private readonly string _connectionString;

	#endregion

	#region Constructors

	public ServerSqlDatabaseProvider(string connectionString)
		: base(new DatabaseOptions())
	{
		_connectionString = connectionString;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override IServerDatabase GetDatabaseFromProvider(DatabaseOptions options)
	{
		return ServerSqlDatabase.UseSqlServer(_connectionString, options);
	}

	#endregion
}