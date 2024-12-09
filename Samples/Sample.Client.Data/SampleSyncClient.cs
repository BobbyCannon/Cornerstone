#region References

using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sync;
using Sample.Shared.Storage.Client;
using Sample.Shared.Sync;

#endregion

namespace Sample.Client.Data;

public class SampleSyncClient : SyncClient
{
	#region Constructors

	/// <inheritdoc />
	public SampleSyncClient(ISyncableDatabaseProvider provider, IDateTimeProvider timeProvider,
		SyncStatistics syncStatistics, Profiler syncClientProfiler)
		: base(provider, timeProvider, syncStatistics, syncClientProfiler)
	{
	}

	#endregion

	#region Methods

	protected override SyncClientIncomingConverter GetIncomingConverter()
	{
		return new SyncClientIncomingConverter(
			new SyncObjectIncomingConverter<AccountSync, ClientAccount>()
		);
	}

	protected override SyncClientOutgoingConverter GetOutgoingConverter()
	{
		return new SyncClientOutgoingConverter(
			new SyncObjectOutgoingConverter<ClientAccount, AccountSync>()
		);
	}

	/// <inheritdoc />
	protected override void UpdateSyncSettings(SyncSettings syncSettings, SyncDevice syncDevice)
	{
	}

	#endregion
}