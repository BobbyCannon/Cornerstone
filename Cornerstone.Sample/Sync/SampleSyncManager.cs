#region References

using Cornerstone.Logging;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Sample.Sync;

/// <summary>
/// Sync manager for Sample. The client is a database sync client; the server
/// peer is WebSyncClient over SampleWebServerSyncClientProvider.
/// </summary>
[SourceReflection]
[DependencyInjected]
public class SampleSyncManager : SyncManager
{
	#region Constructors

	[DependencyInjectionConstructor]
	public SampleSyncManager(
		SampleClientSyncClientProvider clientSyncClientProvider,
		SampleServerSyncClientProvider serverSyncClientProvider,
		SyncSession syncSession,
		IRuntimeInformation runtimeInformation,
		IDateTimeProvider dateTimeProvider,
		IDispatcher dispatcher,
		Logger logger
	)
		: this(
			(ISyncClientProvider) clientSyncClientProvider,
			serverSyncClientProvider,
			syncSession,
			runtimeInformation,
			dateTimeProvider,
			dispatcher,
			logger
		)
	{
	}

	public SampleSyncManager(
		ISyncClientProvider clientSyncClientProvider,
		ISyncClientProvider serverSyncClientProvider,
		SyncSession syncSession,
		IRuntimeInformation runtimeInformation,
		IDateTimeProvider dateTimeProvider,
		IDispatcher dispatcher,
		Logger logger = null
	)
		: base(
			clientSyncClientProvider,
			serverSyncClientProvider,
			syncSession,
			runtimeInformation,
			dateTimeProvider,
			dispatcher,
			logger,
			SampleSyncClient.SyncAll
		)
	{
	}

	#endregion
}