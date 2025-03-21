#region References

using Cornerstone.Runtime;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Sample.Shared.Storage;

#endregion

namespace Sample.Server.Data;

public class ServerMemoryDatabaseProvider : ServerDatabaseProvider
{
	#region Fields

	private readonly ServerMemoryDatabase _database;

	#endregion

	#region Constructors

	public ServerMemoryDatabaseProvider(IDateTimeProvider dateTimeProvider, DatabaseKeyCache keyCache)
		: base(dateTimeProvider, keyCache)
	{
		_database = new ServerMemoryDatabase(dateTimeProvider, Settings, KeyCache);
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override IServerDatabase GetDatabaseFromProvider()
	{
		return _database;
	}

	protected override IServerDatabase GetDatabaseFromProvider(DatabaseSettings settings, DatabaseKeyCache keyCache)
	{
		return _database;
	}

	#endregion
}