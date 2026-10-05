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
	/// Run one sync round-trip: begin if needed, apply a push page, return a pull page, or end.
	/// Empty Changes with PullDown and EndSession false is GetChanges (pull).
	/// Non-empty Changes is ApplyChanges only (push). An end-only call does not query.
	/// A pull ends on the last page when the direction is pull-only or the client has nothing to push.
	/// </summary>
	public virtual SyncOperationResult Sync(SyncOperation operation)
	{
		if (operation == null)
		{
			throw new ArgumentNullException(nameof(operation));
		}

		var result = new SyncOperationResult();
		var startedThisCall = SyncSessionStart == null;
		var sessionStart = SyncSessionStart ?? BeginSync(operation.SessionId, operation.Settings ?? SyncSettings);
		if (startedThisCall && (operation.ResumeStatistics != null))
		{
			Statistics.UpdateWith(operation.ResumeStatistics);
		}
		result.SessionStart = sessionStart;

		var clientChanges = operation.Changes ?? new ServiceRequest<SyncObject>();
		var changeCount = clientChanges.Collection?.Count ?? 0;
		var issues = operation.Issues;

		if (issues?.Collection is { Count: > 0 })
		{
			if (changeCount > 0)
			{
				result.AppliedIssues = ApplyCorrections(operation.SessionId, clientChanges);
			}

			result.Corrections = GetCorrections(operation.SessionId, issues);
			result.Changes = new ServiceResult<SyncObject>();
		}
		else
		{
			if (changeCount > 0)
			{
				result.AppliedIssues = ApplyChanges(operation.SessionId, clientChanges);
			}

			// Pull is a separate direction from push. A non-empty Changes page is apply-only
			// so the session can pull all server pages first, then push, without echoing.
			// EndSession alone must not query; that query would count as outgoing changes.
			else if (!operation.EndSession && SyncSettings.SyncDirection.HasFlag(SyncDirection.PullDown))
			{
				var request = new SyncRequest
				{
					Since = SyncSettings.LastSyncedOnServer,
					Until = sessionStart.StartedOn,
					Skip = operation.GetChangesSkip
				};
				result.Changes = GetChanges(operation.SessionId, request);

				var clientWillNotPush = operation.ClientHasNoChanges || !SyncSettings.SyncDirection.HasFlag(SyncDirection.PushUp);
				if (clientWillNotPush && (result.Changes != null) && !result.Changes.HasMore)
				{
					result.Statistics = EndSync(operation.SessionId);
					result.SessionEnded = true;
				}
			}
		}

		if (result.SessionEnded)
		{
			return result;
		}

		if (operation.EndSession)
		{
			result.Statistics = EndSync(operation.SessionId);
			result.SessionEnded = true;
		}
		else
		{
			result.Statistics = Statistics;
		}

		return result;
	}

	/// <summary>
	/// Sends changes to a server.
	/// </summary>
	protected internal abstract ServiceResult<SyncIssue> ApplyChanges(Guid sessionId, ServiceRequest<SyncObject> changes);

	/// <summary>
	/// Apply issue-driven corrections. Same as ApplyChanges except last-write-wins is skipped.
	/// </summary>
	protected internal abstract ServiceResult<SyncIssue> ApplyCorrections(Guid sessionId, ServiceRequest<SyncObject> corrections);

	/// <summary>
	/// Starts the sync session.
	/// </summary>
	protected internal virtual SyncSessionStart BeginSync(Guid sessionId, SyncSettings settings)
	{
		if (SyncSessionStart != null)
		{
			throw new InvalidOperationException("An existing sync session is in progress.");
		}

		SyncSessionStart = new SyncSessionStart { Id = sessionId, StartedOn = DateTimeProvider.UtcNow };

		Statistics.Reset();
		SyncSettings = settings;

		SetSyncSettings();

		Converter = GetConverter();

		return SyncSessionStart;
	}

	/// <summary>
	/// Ends the sync session.
	/// </summary>
	protected internal virtual SyncStatistics EndSync(Guid sessionId)
	{
		ValidateSession(sessionId);
		SyncSessionStart = null;
		return Statistics;
	}

	/// <summary>
	/// Gets the changes from the server.
	/// </summary>
	protected internal abstract ServiceResult<SyncObject> GetChanges(Guid sessionId, SyncRequest request);

	/// <summary>
	/// Optional issue-driven outgoing objects. Default database client returns none.
	/// </summary>
	protected internal abstract ServiceResult<SyncObject> GetCorrections(Guid sessionId, ServiceRequest<SyncIssue> issues);

	protected IEnumerable<T> GetChangesQuery<T>(IEnumerable<T> collection, DateTime since, DateTime until)
		where T : ISyncEntity
	{
		return collection
			.Where(x => ((x.CreatedOn >= since) && (x.CreatedOn < until))
				|| ((x.ModifiedOn >= since) && (x.ModifiedOn < until)));
	}

	protected abstract SyncClientConverter GetConverter();

	/// <summary>
	/// BeginSync will use this to set sync settings, filters, and other values.
	/// </summary>
	protected abstract void SetSyncSettings();

	/// <summary>
	/// Validates the sync session. The SyncSession will be set on BeginSync and cleared on EndSync.
	/// </summary>
	protected virtual void ValidateSession(Guid sessionId)
	{
		if (sessionId != SyncSessionStart?.Id)
		{
			throw new InvalidOperationException("The sync session ID is invalid.");
		}
	}

	#endregion
}