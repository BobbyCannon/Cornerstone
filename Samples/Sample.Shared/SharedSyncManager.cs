#region References

using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using Cornerstone.Logging;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Sync;
using Sample.Shared.Storage.Client;
using Sample.Shared.Storage.Server;
using Sample.Shared.Sync;

#endregion

namespace Sample.Shared;

public class SharedSyncManager : SyncManager
{
	#region Constructors

	/// <inheritdoc />
	public SharedSyncManager(ISyncableDatabaseProvider databaseProvider, IRuntimeInformation runtimeInformation,
		SyncClientProvider serverSyncClientProvider, LogListener logListener, IDispatcher dispatcher, params string[] supportedSyncTypes)
		: base(databaseProvider, runtimeInformation, serverSyncClientProvider, logListener, dispatcher, supportedSyncTypes)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override SyncClientIncomingConverter GetIncomingConverter()
	{
		return new SyncClientIncomingConverter(
		[
			new SyncObjectIncomingConverter<AccountSync, ClientAccount, int>(),
			new SyncObjectIncomingConverter<AddressSync, ClientAddress, long>(),
			new SyncObjectIncomingConverter<LogEventSync, LogEventEntity, long>()
		]);
	}

	/// <inheritdoc />
	public override SyncClientOutgoingConverter GetOutgoingConverter()
	{
		return new SyncClientOutgoingConverter(
		[
			new SyncObjectOutgoingConverter<ClientAccount, int, AccountSync>(),
			new SyncObjectOutgoingConverter<ClientAddress, long, AddressSync>(),
			new SyncObjectOutgoingConverter<ClientLogEvent, long, LogEventSync>()
		]);
	}

	public SyncSession SyncAll(TimeSpan? timeout = null, TimeSpan? waitFor = null, Action<SyncSession> postAction = null)
	{
		return WaitForSync(SyncAllAsync(waitFor, postAction), timeout);
	}

	public Task<SyncSession> SyncAllAsync(TimeSpan? waitFor = null, Action<SyncSession> postAction = null)
	{
		OnLogEvent("Starting to sync all...", EventLevel.Verbose);

		return ProcessAsync("All", options =>
			{
				options.ResetFilters();
				options.AddSyncableFilter(new SyncRepositoryFilter<ClientAccount>());
				options.AddSyncableFilter(new SyncRepositoryFilter<ClientAddress>());
				options.AddSyncableFilter(new SyncRepositoryFilter<ClientLogEvent>());
				OnLogEvent("Sync all started", EventLevel.Verbose);
			},
			waitFor,
			postAction);
	}

	#endregion
}