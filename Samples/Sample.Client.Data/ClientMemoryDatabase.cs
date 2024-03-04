#region References

using Cornerstone.EntityFramework;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Sample.Shared.Storage;
using Sample.Shared.Storage.Client;
using Sample.Shared.Sync;

#endregion

namespace Sample.Client.Data;

public class ClientMemoryDatabase : SyncableDatabase, IClientDatabase
{
	#region Constructors

	/// <inheritdoc />
	public ClientMemoryDatabase() : this(null, null)
	{
	}

	/// <inheritdoc />
	public ClientMemoryDatabase(DatabaseOptions options, DatabaseKeyCache keyCache) : base(options, keyCache)
	{
		Accounts = GetSyncableRepository<ClientAccount, int>();
		Addresses = GetSyncableRepository<ClientAddress, long>();

		this.ConfigureModelViaMapping();
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public ISyncableRepository<ClientAccount, int> Accounts { get; }

	/// <inheritdoc />
	public ISyncableRepository<ClientAddress, long> Addresses { get; }

	#endregion
}