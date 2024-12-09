#region References

using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sync;
using Sample.Shared.Storage.Server;
using Sample.Shared.Sync;

#endregion

namespace Sample.Server.Data;

public class SampleServerSyncClient : ServerSyncClient
{
	#region Constructors

	/// <inheritdoc />
	public SampleServerSyncClient(ISyncableDatabaseProvider provider, IDateTimeProvider timeProvider,
		SyncStatistics syncStatistics, Profiler syncClientProfiler)
		: base(provider, timeProvider, syncStatistics, syncClientProfiler)
	{
	}

	#endregion

	#region Methods

	protected override SyncClientIncomingConverter GetIncomingConverter()
	{
		return new SyncClientIncomingConverter(
			new SyncObjectIncomingConverter<AccountSync, AccountEntity>()
		);
	}

	protected override SyncClientOutgoingConverter GetOutgoingConverter()
	{
		return new SyncClientOutgoingConverter(
			new SyncObjectOutgoingConverter<AccountEntity, AccountSync>()
		);
	}

	/// <inheritdoc />
	protected override void UpdateSyncSettings(SyncSettings syncSettings, SyncDevice syncDevice)
	{
		switch (syncSettings.SyncType)
		{
			case "All":
			{
				syncSettings.AddSyncableFilter(new SyncRepositoryFilter<AccountEntity>());
				break;
			}
		}
	}

	#endregion
}