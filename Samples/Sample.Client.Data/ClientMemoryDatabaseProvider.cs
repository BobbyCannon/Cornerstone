#region References

using Cornerstone.Runtime;
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

	public ClientMemoryDatabaseProvider(IDateTimeProvider dateTimeProvider) : base(dateTimeProvider)
	{
		_database = new ClientMemoryDatabase();
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override string[] GetSyncOrder()
	{
		return ClientMemoryDatabase.GetSyncOrder();
	}

	/// <inheritdoc />
	protected override IClientDatabase GetDatabaseFromProvider()
	{
		return _database;
	}

	#endregion
}