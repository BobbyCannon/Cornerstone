#region References

using Cornerstone.Logging;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Web;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Web client for a sync server implemented over Web API.
/// </summary>
public class WebSyncClient : ServerSyncClient
{
	#region Fields

	private readonly string _syncUri;

	#endregion

	#region Constructors

	/// <summary>
	/// Instantiates an instance of the class.
	/// </summary>
	public WebSyncClient(
		string name,
		IDateTimeProvider dateTimeProvider,
		ISyncableDatabaseProvider provider,
		IWebClient webClient,
		string syncUri = "api/Sync",
		Logger logger = null,
		Profiler profiler = null)
		: base(name, provider, dateTimeProvider, new SyncStatistics(), profiler, logger)
	{
		_syncUri = syncUri;

		Settings = new SyncClientSettings();
		WebClient = webClient;
	}

	#endregion

	#region Properties

	public SyncClientSettings Settings { get; set; }

	/// <summary>
	/// The web client to use to connect to the server.
	/// </summary>
	public IWebClient WebClient { get; }

	#endregion

	#region Methods

	public override SyncOperationResult Sync(SyncOperation operation)
	{
		using var webSync = Profiler.Start("WebSync");
		return WebClient.Post<SyncOperation, SyncOperationResult>(_syncUri, operation);
	}

	protected override SyncClientConverter GetConverter()
	{
		return new SyncClientConverter();
	}

	protected override void SetSyncSettings()
	{
	}

	#endregion
}