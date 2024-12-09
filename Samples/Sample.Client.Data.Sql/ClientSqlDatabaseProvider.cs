#region References

using Cornerstone.Storage;
using Sample.Shared.Storage;

#endregion

namespace Sample.Client.Data.Sql;

public class ClientSqlDatabaseProvider : DatabaseProvider<IClientDatabase>
{
	#region Fields

	private readonly string _connectionString;

	#endregion

	#region Constructors

	public ClientSqlDatabaseProvider(string connectionString)
	{
		_connectionString = connectionString;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override IClientDatabase GetDatabaseFromProvider()
	{
		return ClientSqlDatabase.UseSqlServer(_connectionString);
	}

	#endregion
}