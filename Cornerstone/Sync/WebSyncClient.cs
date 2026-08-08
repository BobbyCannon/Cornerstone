#region References

using System;
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
	/// <param name="dateTimeProvider"> </param>
	/// <param name="provider"> The database provider for the client </param>
	/// <param name="name"> The name of the client. </param>
	/// <param name="webClient"> The client to access the web. </param>
	/// <param name="syncUri"> The sync URI. Defaults to "api/Sync". </param>
	/// <param name="logger"> Optional in-memory logger. </param>
	public WebSyncClient(
		string name,
		IDateTimeProvider dateTimeProvider,
		ISyncableDatabaseProvider provider,
		IWebClient webClient,
		string syncUri = "api/Sync",
		Logger logger = null)
		: base(name, provider, dateTimeProvider, new SyncStatistics(), new Profiler(name), logger)
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

	public override ServiceResult<SyncIssue> ApplyChanges(Guid sessionId, ServiceRequest<SyncObject> changes)
	{
		return WebClient.Post<ServiceRequest<SyncObject>, ServiceResult<SyncIssue>>($"{_syncUri}/{nameof(ApplyChanges)}/{sessionId}", changes);
	}

	public override ServiceResult<SyncIssue> ApplyCorrections(Guid sessionId, ServiceRequest<SyncObject> corrections)
	{
		return WebClient.Post<ServiceRequest<SyncObject>, ServiceResult<SyncIssue>>($"{_syncUri}/{nameof(ApplyCorrections)}/{sessionId}", corrections);
	}

	public override SyncSessionStart BeginSync(Guid sessionId, SyncSettings settings)
	{
		return WebClient.Post<SyncSettings, SyncSessionStart>($"{_syncUri}/{nameof(BeginSync)}/{sessionId}", settings);
	}

	public override SyncStatistics EndSync(Guid sessionId)
	{
		var statistics = WebClient.Post<string, SyncStatistics>($"{_syncUri}/{nameof(EndSync)}/{sessionId}", string.Empty);
		Statistics.UpdateWith(statistics);
		return Statistics;
	}

	public override ServiceResult<SyncObject> GetChanges(Guid sessionId, SyncRequest request)
	{
		return WebClient.Post<SyncRequest, ServiceResult<SyncObject>>($"{_syncUri}/{nameof(GetChanges)}/{sessionId}", request);
	}

	public override ServiceResult<SyncObject> GetCorrections(Guid sessionId, ServiceRequest<SyncIssue> issues)
	{
		return WebClient.Post<ServiceRequest<SyncIssue>, ServiceResult<SyncObject>>($"{_syncUri}/{nameof(GetCorrections)}/{sessionId}", issues);
	}

	protected override SyncClientConverter GetConverter()
	{
		return new SyncClientConverter();
	}

	protected override void UpdateSyncSettings()
	{
	}

	#endregion
}