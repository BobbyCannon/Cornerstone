#region References

using Cornerstone.EntityFramework;
using Cornerstone.Extensions;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Sample.Shared.Storage;
using Sample.Shared.Storage.Client;

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
	public ClientMemoryDatabase(DatabaseSettings settings, DatabaseKeyCache keyCache) : base(settings, keyCache)
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

	/// <inheritdoc />
	public override string[] SyncOrder => GetSyncOrder();

	#endregion

	#region Methods

	public static string[] GetSyncOrder()
	{
		return
		[
			typeof(ClientAccount).ToAssemblyName(),
			typeof(ClientAddress).ToAssemblyName()
		];
	}

	#endregion
}