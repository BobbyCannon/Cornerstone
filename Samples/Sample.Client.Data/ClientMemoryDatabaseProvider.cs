#region References

using Cornerstone.Data;
using Cornerstone.Runtime;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Sample.Shared.Storage;

#endregion

namespace Sample.Client.Data;

public class ClientMemoryDatabaseProvider : SyncableDatabaseProvider<IClientDatabase>
{
	#region Fields

	private readonly ClientMemoryDatabase _database;

	#endregion

	#region Constructors

	public ClientMemoryDatabaseProvider(IDateTimeProvider dateTimeProvider, DatabaseKeyCache keyCache)
		: base(dateTimeProvider, keyCache, IClientDatabase.GetDefaultDatabaseSettings())
	{
		_database = new ClientMemoryDatabase(dateTimeProvider, Settings, keyCache);
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override IClientDatabase GetDatabaseFromProvider()
	{
		_database.DatabaseSettings.UpdateWith(Settings, IncludeExcludeSettings.Empty);
		return _database;
	}

	protected override IClientDatabase GetDatabaseFromProvider(DatabaseSettings settings, DatabaseKeyCache keyCache)
	{
		_database.DatabaseSettings.UpdateWith(settings, IncludeExcludeSettings.Empty);
		return _database;
	}

	#endregion
}