#region References

using Cornerstone.Attributes;
using Cornerstone.Net;
using Cornerstone.Profiling;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Provides sync provider and some web interfaces.
/// </summary>
public class WebServerSyncClientProvider : IServerSyncClientProvider
{
	#region Fields

	private readonly ISyncableDatabaseProvider _databaseProvider;
	private readonly IDateTimeProvider _dateTimeProvider;
	private readonly IWebClient _webClient;

	#endregion

	#region Constructors

	/// <summary>
	/// Create an instance of a provider for a web sync client.
	/// </summary>
	/// <param name="webClient"> The web client to use. </param>
	/// <param name="dateTimeProvider"> The provider of the date and time. </param>
	[DependencyInjectionConstructor]
	public WebServerSyncClientProvider(IWebClient webClient, IDateTimeProvider dateTimeProvider)
		: this(webClient, null, dateTimeProvider)
	{
	}

	/// <summary>
	/// Create an instance of a provider for a web sync client.
	/// </summary>
	/// <param name="webClient"> The web client to use. </param>
	/// <param name="databaseProvider"> The syncable database provider. </param>
	/// <param name="dateTimeProvider"> The provider of the date and time. </param>
	internal WebServerSyncClientProvider(
		IWebClient webClient,
		ISyncableDatabaseProvider databaseProvider,
		IDateTimeProvider dateTimeProvider)
	{
		_webClient = webClient;
		_databaseProvider = databaseProvider;
		_dateTimeProvider = dateTimeProvider;
	}

	#endregion

	#region Methods

	public ServerSyncClient GetServerSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
	{
		return new WebServerSyncClient("Web Client", _databaseProvider, _dateTimeProvider, _webClient);
	}

	public ISyncableDatabase GetSyncableDatabase()
	{
		return _databaseProvider.GetSyncableDatabase();
	}

	public SyncClient GetSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
	{
		return GetServerSyncClient(syncStatistics, syncClientProfiler);
	}

	#endregion
}