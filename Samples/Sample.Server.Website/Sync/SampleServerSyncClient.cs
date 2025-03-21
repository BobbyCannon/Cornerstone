#region References

using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sync;
using Sample.Shared.Storage.Server;
using Sample.Shared.Storage.Sync;

#endregion

namespace Sample.Server.Website.Sync;

public class SampleServerSyncClient : ServerSyncClient
{
	#region Constructors

	/// <inheritdoc />
	public SampleServerSyncClient(string name, AccountEntity authenticatedAccount, ISyncableDatabaseProvider provider,
		IDateTimeProvider timeProvider, SyncStatistics syncStatistics, Profiler profiler)
		: base(name, provider, timeProvider, syncStatistics, profiler)
	{
		AuthenticatedAccount = authenticatedAccount;
	}

	#endregion

	#region Properties

	public AccountEntity AuthenticatedAccount { get; }

	#endregion

	#region Methods

	protected override SyncClientIncomingConverter GetIncomingConverter()
	{
		return new SyncClientIncomingConverter(
			new SyncObjectIncomingConverter<Account, AccountEntity>(),
			new SyncObjectIncomingConverter<Address, AddressEntity>()
		);
	}

	protected override SyncClientOutgoingConverter GetOutgoingConverter()
	{
		return new SyncClientOutgoingConverter(
			new SyncObjectOutgoingConverter<AccountEntity, Account>(),
			new SyncObjectOutgoingConverter<AddressEntity, Address>()
		);
	}

	/// <inheritdoc />
	protected override void UpdateSyncSettings()
	{
		switch (SyncSettings.SyncType)
		{
			case "All":
			{
				SyncSettings.AddFilter(new SyncRepositoryFilter<AccountEntity>());
				SyncSettings.AddFilter(new SyncRepositoryFilter<AddressEntity>());
				break;
			}
		}
	}

	#endregion
}