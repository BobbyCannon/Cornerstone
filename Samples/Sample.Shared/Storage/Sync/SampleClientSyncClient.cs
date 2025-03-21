#region References

using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sync;
using Sample.Shared.Storage.Client;

#endregion

namespace Sample.Shared.Storage.Sync;

public class SampleClientSyncClient : SyncClient
{
	#region Constructors

	/// <inheritdoc />
	public SampleClientSyncClient(string name, ISyncableDatabaseProvider provider,
		IDateTimeProvider timeProvider, SyncStatistics syncStatistics, Profiler syncClientProfiler)
		: base(name, provider, timeProvider, syncStatistics, syncClientProfiler)
	{
	}

	#endregion

	#region Methods

	protected override SyncClientIncomingConverter GetIncomingConverter()
	{
		return new SyncClientIncomingConverter(
			new SyncObjectIncomingConverter<Account, ClientAccount>()
		);
	}

	protected override SyncClientOutgoingConverter GetOutgoingConverter()
	{
		return new SyncClientOutgoingConverter(
			new SyncObjectOutgoingConverter<ClientAccount, Account>()
		);
	}

	/// <inheritdoc />
	protected override void UpdateSyncSettings()
	{
	}

	#endregion
}