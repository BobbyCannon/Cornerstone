#region References

using Cornerstone.Logging;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Web;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Provides sync provider and some web interfaces.
/// </summary>
public class WebServerSyncClientProvider : IServerSyncClientProvider
{
	#region Fields

	private readonly IDateTimeProvider _dateTimeProvider;
	private readonly Logger _logger;
	private readonly string _name;
	private readonly IWebClient _webClient;

	#endregion

	#region Constructors

	/// <summary>
	/// Create an instance of a provider for a web sync client.
	/// </summary>
	[DependencyInjectionConstructor]
	public WebServerSyncClientProvider(
		IDateTimeProvider dateTimeProvider,
		ISyncableDatabaseProvider provider,
		IWebClient webClient,
		Logger logger = null)
	{
		_name = "Web Server Client";
		_dateTimeProvider = dateTimeProvider;
		_logger = logger;
		_webClient = webClient;

		Provider = provider;
	}

	#endregion

	#region Properties

	public ISyncableDatabaseProvider Provider { get; }

	#endregion

	#region Methods

	public ServerSyncClient GetServerSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
	{
		return new WebSyncClient(_name, _dateTimeProvider, Provider, _webClient, logger: _logger);
	}

	public ISyncableDatabase GetSyncableDatabase()
	{
		return Provider.GetSyncableDatabase();
	}

	public SyncClient GetSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
	{
		return new WebSyncClient(_name, _dateTimeProvider, Provider, _webClient, logger: _logger);
	}

	#endregion
}