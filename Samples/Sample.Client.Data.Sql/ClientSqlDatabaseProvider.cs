#region References

using Cornerstone.Runtime;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Sample.Shared.Storage;

#endregion

namespace Sample.Client.Data.Sql;

public class ClientSqlDatabaseProvider : SyncableDatabaseProvider<IClientDatabase>
{
	#region Fields

	private readonly string _connectionString;

	#endregion

	#region Constructors

	public ClientSqlDatabaseProvider(string connectionString, IDateTimeProvider dateTimeProvider, DatabaseKeyCache keyCache) 
		: base(dateTimeProvider, keyCache, IClientDatabase.GetDefaultDatabaseSettings())
	{
		_connectionString = connectionString;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override IClientDatabase GetDatabaseFromProvider()
	{
		return ClientSqlDatabase.UseSqlServer(_connectionString, Settings, KeyCache);
	}

	protected override IClientDatabase GetDatabaseFromProvider(DatabaseSettings settings, DatabaseKeyCache keyCache)
	{
		return ClientSqlDatabase.UseSqlServer(_connectionString, settings, keyCache);
	}

	#endregion
}