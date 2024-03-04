#region References

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

	public ClientMemoryDatabaseProvider()
		: base(new DatabaseOptions())
	{
		_database = new ClientMemoryDatabase();
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override IClientDatabase GetDatabaseFromProvider(DatabaseOptions options)
	{
		return _database;
	}

	#endregion
}