#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Logging;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Web;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents a sync client.
/// </summary>
public abstract class SyncClient
{
	#region Constructors

	/// <summary>
	/// Initializes a sync client.
	/// </summary>
	protected SyncClient(
		string name,
		IDateTimeProvider dateTimeProvider,
		SyncStatistics syncStatistics,
		Profiler syncClientProfiler,
		Logger logger = null)
	{
		DateTimeProvider = dateTimeProvider;
		Logger = logger;
		Name = name;
		Profiler = syncClientProfiler ?? new Profiler(name);
		Statistics = syncStatistics ?? new SyncStatistics();
		SyncDevice = new SyncDevice();
		SyncSettings = new SyncSettings();
	}

	#endregion

	#region Properties

	/// <summary>
	/// An optional converter to process sync objects from Server to Client
	/// </summary>
	public SyncClientConverter Converter { get; private set; }

	/// <summary>
	/// Gets or sets the name of the sync client.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Profiler for tracking specific points during sync client processing.
	/// </summary>
	public Profiler Profiler { get; }

	/// <summary>
	/// The communication statistics for this sync client.
	/// </summary>
	public SyncStatistics Statistics { get; }

	/// <summary>
	/// The device for the sync.
	/// </summary>
	public SyncDevice SyncDevice { get; private set; }

	/// <summary>
	/// The options for the sync.
	/// </summary>
	public SyncSettings SyncSettings { get; private set; }

	/// <summary>
	/// The date and time provider.
	/// </summary>
	protected IDateTimeProvider DateTimeProvider { get; }

	/// <summary>
	/// Optional in-memory logger for operational messages.
	/// </summary>
	protected Logger Logger { get; }

	/// <summary>
	/// The start of the sync session.
	/// </summary>
	protected SyncSessionStart SyncSessionStart { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Sends changes to a server.
	/// </summary>
	/// <param name="sessionId"> The ID of the sync session. </param>
	/// <param name="changes"> The changes to write to the server. </param>
	/// <returns> A list of sync issues if there were any. </returns>
	public abstract ServiceResult<SyncIssue> ApplyChanges(Guid sessionId, ServiceRequest<SyncObject> changes);

	/// <summary>
	/// Sends issue corrections to a server.
	/// </summary>
	/// <param name="sessionId"> The ID of the sync session. </param>
	/// <param name="corrections"> The corrections to write to the server. </param>
	/// <returns> A list of sync issues if there were any. </returns>
	public abstract ServiceResult<SyncIssue> ApplyCorrections(Guid sessionId, ServiceRequest<SyncObject> corrections);

	/// <summary>
	/// Starts the sync session.
	/// </summary>
	/// <param name="sessionId"> The ID of the sync session. </param>
	/// <param name="settings"> The settings for the sync session. </param>
	public virtual SyncSessionStart BeginSync(Guid sessionId, SyncSettings settings)
	{
		if (SyncSessionStart != null)
		{
			throw new InvalidOperationException("An existing sync session is in progress.");
		}

		SyncSessionStart = new SyncSessionStart { Id = sessionId, StartedOn = DateTimeProvider.UtcNow };

		Statistics.Reset();
		SyncSettings = settings;

		UpdateSyncSettings();
		
		Converter = GetConverter();

		return SyncSessionStart;
	}

	/// <summary>
	/// Ends the sync session.
	/// </summary>
	/// <param name="sessionId"> The ID of the sync session. </param>
	public virtual SyncStatistics EndSync(Guid sessionId)
	{
		ValidateSession(sessionId);
		SyncSessionStart = null;
		return Statistics;
	}

	/// <summary>
	/// Gets the changes from the server.
	/// </summary>
	/// <param name="sessionId"> The ID of the sync session. </param>
	/// <param name="request"> The details for the request. </param>
	/// <returns> The list of changes from the server. </returns>
	public abstract ServiceResult<SyncObject> GetChanges(Guid sessionId, SyncRequest request);

	/// <summary>
	/// Gets the list of sync objects to try and resolve the issue list.
	/// </summary>
	/// <param name="sessionId"> The ID of the sync session. </param>
	/// <param name="issues"> The issues to process. </param>
	/// <returns> The sync objects to resolve the issues. </returns>
	public abstract ServiceResult<SyncObject> GetCorrections(Guid sessionId, ServiceRequest<SyncIssue> issues);

	protected IQueryable<T> GetChangesQuery<T>(IEnumerable<T> collection, DateTime since, DateTime until)
		where T : ISyncEntity
	{
		return collection
			.Where(x =>
				((x.CreatedOn >= since) && (x.CreatedOn < until))
				|| ((x.ModifiedOn >= since) && (x.ModifiedOn < until))
			)
			.AsQueryable();
	}

	protected abstract SyncClientConverter GetConverter();

	/// <summary>
	/// Update sync settings filter and other such on BeginSync.
	/// </summary>
	protected abstract void UpdateSyncSettings();

	/// <summary>
	/// Validates the sync session. The SyncSession will be set on BeginSync and cleared on EndSync.
	/// </summary>
	/// <param name="sessionId"> </param>
	protected virtual void ValidateSession(Guid sessionId)
	{
		if (sessionId != SyncSessionStart?.Id)
		{
			throw new InvalidOperationException("The sync session ID is invalid.");
		}
	}

	#endregion
}