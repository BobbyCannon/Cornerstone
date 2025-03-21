#region References

using System;
using Cornerstone.Net;
using Cornerstone.Profiling;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Web client for a sync server implemented over Web API.
/// </summary>
public class WebServerSyncClient : ServerSyncClient, ISyncServerProxy
{
	#region Fields

	private readonly string _syncUri;

	#endregion

	#region Constructors

	/// <summary>
	/// Instantiates an instance of the class.
	/// </summary>
	/// <param name="databaseProvider"> The database provider for the client </param>
	/// <param name="name"> The name of the client. </param>
	/// <param name="dateTimeProvider"> The provider of the date and time. </param>
	/// <param name="webClient"> The client to access the web. </param>
	/// <param name="syncUri"> The sync URI. Defaults to "api/Sync". </param>
	public WebServerSyncClient(
		string name,
		ISyncableDatabaseProvider databaseProvider,
		IDateTimeProvider dateTimeProvider,
		IWebClient webClient,
		string syncUri = "api/Sync"
	) : base(name, databaseProvider, dateTimeProvider, new SyncStatistics(), new Profiler(name))
	{
		_syncUri = syncUri;

		WebClient = webClient;
	}

	#endregion

	#region Properties

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
		SyncSettings.UpdateWith(settings);
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

	protected override SyncClientIncomingConverter GetIncomingConverter()
	{
		// Not for web client, the server will manage its own converters
		return null;
	}

	protected override SyncClientOutgoingConverter GetOutgoingConverter()
	{
		// Not for web client, the server will manage its own converters
		return null;
	}

	protected override void UpdateSyncSettings()
	{
		// Not for web client, the server will manage its own sync settings
	}

	#endregion
}