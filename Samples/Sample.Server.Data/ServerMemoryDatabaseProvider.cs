#region References

using Cornerstone.Storage;
using Cornerstone.Sync;
using Sample.Shared.Storage;

#endregion

namespace Sample.Server.Data;

public class ServerMemoryDatabaseProvider : SyncableDatabaseProvider<IServerDatabase>
{
	#region Fields

	private readonly ServerMemoryDatabase _database;

	#endregion

	#region Constructors

	public ServerMemoryDatabaseProvider()
		: base(new DatabaseOptions())
	{
		_database = new ServerMemoryDatabase();
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override IServerDatabase GetDatabaseFromProvider(DatabaseOptions options)
	{
		return _database;
	}

	#endregion
}