#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Logging;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Web;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// The object to track a sync session.
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
[DependencyInjected]
public partial class SyncSession : CornerstoneObject<SyncSession>, ISyncSessionStatus
{
	#region Fields

	private readonly Logger _logger;
	private readonly IDateTimeProvider _timeProvider;

	#endregion

	#region Constructors

	public SyncSession() : this(null, null)
	{
	}

	/// <summary>
	/// Initiates an instances of the sync session.
	/// </summary>
	public SyncSession(IDateTimeProvider timeProvider)
		: this(timeProvider, null)
	{
	}

	/// <summary>
	/// Initiates an instances of the sync session.
	/// </summary>
	[DependencyInjectionConstructor]
	public SyncSession(IDateTimeProvider timeProvider, Logger logger)
		: this(Guid.Empty, string.Empty, timeProvider, logger)
	{
	}

	/// <summary>
	/// Initiates an instances of the sync session.
	/// </summary>
	private SyncSession(Guid sessionId, string syncType, IDateTimeProvider timeProvider, Logger logger = null)
	{
		_logger = logger;
		_timeProvider = timeProvider;

		Settings = new SyncSettings();
		StatisticsForClient = new SyncStatistics();
		StatisticsForServer = new SyncStatistics();
		SyncClientProfilerForClient = new Profiler("Client");
		SyncClientProfilerForServer = new Profiler("Server");
		SyncIssues = new SpeedyList<SyncIssue>(isLongLivedBuffer: true);

		Reset(syncType);
		SessionId = sessionId;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The elapsed time for the sync.
	/// </summary>
	public TimeSpan Elapsed =>
		(StartedOn == DateTime.MinValue)
		&& (StoppedOn == DateTime.MinValue)
			? TimeSpan.Zero
			: StoppedOn == DateTime.MinValue
				? CurrentTime - StartedOn
				: StoppedOn - StartedOn;

	/// <summary>
	/// The percent of processing. This is based on the sync session <see cref="State" />.
	/// </summary>
	public partial decimal Percent { get; private set; }

	/// <summary>
	/// Gets the ID of the sync session.
	/// </summary>
	public partial Guid SessionId { get; private set; }

	/// <summary>
	/// The sync options.
	/// </summary>
	public SyncSettings Settings { get; }

	/// <summary>
	/// Gets a flag to indicate progress should be shown. Will only be true if sync takes longer than the <seealso cref="ShowProgressThreshold" />.
	/// </summary>
	public bool ShowProgress => SyncRunning && (Elapsed >= ShowProgressThreshold);

	/// <summary>
	/// Gets the value to determine when to trigger <seealso cref="ShowProgress" />. Defaults to one second.
	/// </summary>
	public partial TimeSpan ShowProgressThreshold { get; set; }

	/// <summary>
	/// The date time the sync started on.
	/// </summary>
	[AlsoNotify(nameof(Elapsed), nameof(SyncStarted), nameof(SyncRunning))]
	public partial DateTime StartedOn { get; private set; }

	/// <summary>
	/// The state of the sync session.
	/// </summary>
	[AlsoNotify(nameof(SyncCancelled), nameof(SyncCompleted), nameof(SyncRunning), nameof(SyncSuccessful))]
	public partial SyncSessionState State { get; private set; }

	/// <summary>
	/// Statistics for client
	/// </summary>
	public SyncStatistics StatisticsForClient { get; }

	/// <summary>
	/// Statistics for server
	/// </summary>
	public SyncStatistics StatisticsForServer { get; }

	/// <summary>
	/// The date time the sync stopped on.
	/// </summary>
	[AlsoNotify(nameof(Elapsed), nameof(SyncCompleted), nameof(SyncRunning))]
	public partial DateTime StoppedOn { get; private set; }

	/// <summary>
	/// Gets a value indicating if the last sync was started.
	/// </summary>
	public bool SyncCancelled => State.HasFlag(SyncSessionState.Cancelled);

	/// <summary>
	/// An optional profiler data for the client.
	/// </summary>
	public Profiler SyncClientProfilerForClient { get; }

	/// <summary>
	/// An optional profiler data for the server.
	/// </summary>
	public Profiler SyncClientProfilerForServer { get; }

	/// <summary>
	/// Gets a value indicating if the sync session is completed.
	/// </summary>
	public bool SyncCompleted => State.HasFlag(SyncSessionState.Completed);

	/// <summary>
	/// Gets a value indicating if the sync session is configured.
	/// </summary>
	public bool SyncConfigured => State.HasFlag(SyncSessionState.Configured);

	/// <summary>
	/// Gets the list of issues that occurred during the last sync.
	/// </summary>
	public SpeedyList<SyncIssue> SyncIssues { get; }

	/// <summary>
	/// Gets a value indicating if the sync session is running.
	/// </summary>
	public bool SyncRunning => SyncStarted && !SyncCompleted;

	/// <summary>
	/// Gets a value indicating if the sync session is started.
	/// </summary>
	public bool SyncStarted => State.HasFlag(SyncSessionState.Started);

	/// <summary>
	/// Gets a value indicating if the sync session is successful.
	/// </summary>
	public bool SyncSuccessful => State.HasFlag(SyncSessionState.Successful);

	/// <summary>
	/// The type for the sync.
	/// </summary>
	public string SyncType
	{
		get => Settings.SyncType;
		set
		{
			var oldValue = Settings.SyncType;
			Settings.SyncType = value;
			OnPropertyChanged(nameof(SyncType), oldValue, Settings.SyncType);
		}
	}

	/// <summary>
	/// Gets the current time.
	/// </summary>
	protected DateTime CurrentTime => _timeProvider?.UtcNow ?? DateTimeProvider.RealTime.UtcNow;

	#endregion

	#region Methods

	public static SyncSession CouldNotStart(Guid sessionId, string syncType, IDateTimeProvider timeProvider, Logger logger = null)
	{
		var session = new SyncSession(timeProvider, logger)
		{
			SessionId = sessionId,
			SyncType = syncType
		};

		session.UpdateState(SyncSessionState.CouldNotStart);
		return session;
	}

	/// <summary>
	/// Wait for a specific sync flag.
	/// </summary>
	/// <param name="state"> The state to wait for. </param>
	/// <param name="timeout"> The max amount of time to wait. </param>
	/// <returns> True if the sync state was set otherwise false if timed out waiting. </returns>
	public bool WaitForSyncState(SyncSessionState state, TimeSpan timeout)
	{
		if (State.HasFlag(state))
		{
			return true;
		}

		var watch = Stopwatch.StartNew();

		while (!State.HasFlag(state))
		{
			if (watch.Elapsed >= timeout)
			{
				return false;
			}

			Thread.Sleep(5);
		}

		return true;
	}

	/// <summary>
	/// Run the sync. This should only be called by ProcessAsync.
	/// </summary>
	/// <param name="syncManager"> The sync manager processing the session. </param>
	/// <param name="updateSettings"> Update options before running sync. </param>
	/// <param name="onSyncConfiguring"> Action to call when sync is configuring. </param>
	/// <param name="onSyncCompleted"> </param>
	internal SyncSession ProcessSyncSession(
		SyncManager syncManager,
		Action<SyncSettings> updateSettings,
		Action<SyncSession> onSyncConfiguring,
		Action<SyncSession> onSyncCompleted)
	{
		SyncClient client = null;
		SyncClient server = null;
		SyncSessionStart serverSession = null, clientSession = null;
		SyncOperationResult serverResult = null;

		try
		{
			UpdatePercent(0, 0);
			UpdateState(SyncSessionState.Configuring);
			updateSettings?.Invoke(Settings);

			client = syncManager.GetSyncClientForClient(StatisticsForClient, SyncClientProfilerForClient);
			server = syncManager.GetSyncClientForServer(StatisticsForServer, SyncClientProfilerForServer);
			if ((client == null) || (server == null))
			{
				throw new CornerstoneException("Sync client for client or server is null.");
			}

			onSyncConfiguring?.Invoke(this);
			UpdateState(SyncSessionState.Configured);

			var incoming = new Dictionary<Guid, DateTime>();

			// Pull-only will not push, even when the client database has rows.
			// Pull-then-push recounts after BeginSync. Zero outgoing rows ends the pull on the last page.
			var clientHasNoChanges = !Settings.SyncDirection.HasFlag(SyncDirection.PushUp);

			while (!SyncCancelled)
			{
				var next = NextSyncState(clientHasNoChanges, serverResult);
				if (next == SyncSessionState.Ending)
				{
					break;
				}

				UpdateState(next);
				switch (next)
				{
					case SyncSessionState.Beginning:
					{
						using (SyncClientProfilerForClient.Start("Begin"))
						{
							clientSession = client.BeginSync(SessionId, Settings);
						}

						// Hosts register the allow-list in SetSyncSettings during BeginSync.
						// An empty set must not pull, push, or count as a successful sync.
						if (!Settings.HasFilters)
						{
							OnLogEvent($"Sync {SyncType} requires at least one repository filter.", LogLevel.Debug);
							SyncIssues.Add(new SyncIssue
							{
								Id = Guid.Empty,
								IssueType = SyncIssueType.RepositoryFiltered,
								Message = "Sync requires at least one repository filter.",
								TypeName = string.Empty
							});
							break;
						}

						if (Settings.SyncDirection.HasFlag(SyncDirection.PushUp))
						{
							clientHasNoChanges = ClientHasNoOutgoingChanges(client, clientSession);
						}

						break;
					}
					case SyncSessionState.Pulling:
					{
						serverResult = PullFromServer(client, server, incoming, clientHasNoChanges, ref serverSession);
						break;
					}
					case SyncSessionState.Pushing:
					{
						serverResult = PushToServer(client, server, clientSession, incoming, ref serverSession) ?? serverResult;
						break;
					}
				}
			}
		}
		catch (Exception ex)
		{
			HandleException(ex);
		}
		finally
		{
			try
			{
				if ((clientSession != null) || (serverSession != null))
				{
					using (SyncClientProfilerForClient.Start("End"))
					{
						UpdateState(SyncSessionState.Ending);

						if (clientSession != null)
						{
							client.EndSync(SessionId);
							Settings.LastSyncedOnClient = clientSession.StartedOn;
						}

						if ((serverSession != null) && (serverResult?.SessionEnded != true))
						{
							EndServerSession(server);
						}

						if (serverSession != null)
						{
							Settings.LastSyncedOnServer = serverSession.StartedOn;
						}
					}
				}

				if (!SyncCancelled && !SyncIssues.Any())
				{
					UpdateState(SyncSessionState.Successful);
				}

				UpdatePercent(100, 100);
			}
			catch (Exception ex)
			{
				HandleException(ex);
			}

			// This must be the last state that must change
			StoppedOn = CurrentTime;

			// See if we have a timer for this sync type
			if (syncManager.SyncTimers.TryGetValue(SyncType, out var syncTimer))
			{
				if (SyncCancelled)
				{
					syncTimer.CancelledSyncs++;
					syncTimer.Reset();
				}
				else if (SyncSuccessful)
				{
					syncTimer.SuccessfulSyncs++;
					syncTimer.Stop(StoppedOn);
				}
				else
				{
					syncTimer.FailedSyncs++;
					syncTimer.Stop(StoppedOn);
				}
			}
		}

		var response = new SyncSession(SessionId, SyncType, _timeProvider, _logger);
		response.DisablePropertyChangeNotifications();
		response.UpdateWith(this);
		response.UpdateState(SyncSessionState.Completed);
		response.EnablePropertyChangeNotifications();
		response.ResetHasChanges();
		onSyncCompleted.Invoke(this);

		UpdateState(SyncSessionState.Completed);
		NotifyComputedPropertyChanged(nameof(StatisticsForClient));
		NotifyComputedPropertyChanged(nameof(StatisticsForServer));

		return response;
	}

	/// <summary>
	/// Start the sync session.
	/// </summary>
	/// <param name="sessionId"> The ID for the session. </param>
	/// <param name="syncType"> </param>
	/// <param name="settings"> The settings to update the session with. </param>
	internal void Start(Guid sessionId, string syncType, SyncSettings settings)
	{
		Reset();

		Settings.UpdateWith(settings);

		SyncType = syncType;
		SessionId = sessionId;
		StartedOn = CurrentTime;

		UpdateState(SyncSessionState.Started);

		settings.LastSyncAttemptedOn = StartedOn;
	}

	internal void UpdateState(SyncSessionState flag)
	{
		State = State.SetFlag(flag);
		LogVerboseState(flag);
	}

	/// <summary>
	/// Apply incoming objects to the local client, skipping ids already applied this session.
	/// </summary>
	private void ApplyIncoming(SyncClient destinationClient, IList<SyncObject> incoming, IDictionary<Guid, DateTime> exclude)
	{
		var excludedIds = new HashSet<Guid>(exclude.Keys);
		var filtered = incoming
			.Where(x => !excludedIds.Contains(x.SyncId)
				|| (exclude[x.SyncId] != x.ModifiedOn))
			.ToList();

		if (filtered.Count == 0)
		{
			return;
		}

		var request = new SyncRequest(filtered);
		var failed = destinationClient.ApplyChanges(SessionId, request).Collection;
		SyncIssues.AddRange(failed);

		if (failed.Count >= filtered.Count)
		{
			return;
		}

		var failedIds = failed.Select(i => i.Id).ToHashSet();
		foreach (var x in filtered)
		{
			if (failedIds.Contains(x.SyncId))
			{
				continue;
			}

			exclude[x.SyncId] = x.ModifiedOn;
		}
	}

	private void ApplyServerResult(
		SyncClient client,
		SyncOperationResult result,
		IDictionary<Guid, DateTime> incoming,
		ref SyncSessionStart serverSession)
	{
		serverSession = result.SessionStart ?? serverSession;
		if (result.Statistics != null)
		{
			StatisticsForServer.UpdateWith(result.Statistics);
		}

		if (result.AppliedIssues?.Collection?.Count > 0)
		{
			SyncIssues.AddRange(result.AppliedIssues.Collection);
		}

		if (result.Changes?.Collection?.Count > 0)
		{
			ApplyIncoming(client, result.Changes.Collection, incoming);
		}

		if (result.Corrections?.Collection?.Count > 0)
		{
			ApplyIncoming(client, result.Corrections.Collection, incoming);
		}
	}

	private void ClearState(SyncSessionState flag)
	{
		State = State.ClearFlag(flag);
	}

	/// <summary>
	/// End the server session without requesting changes.
	/// </summary>
	private void EndServerSession(SyncClient server)
	{
		var endResult = server.Sync(new SyncOperation
		{
			SessionId = SessionId,
			Settings = Settings,
			EndSession = true
		});

		if (endResult.Statistics != null)
		{
			StatisticsForServer.UpdateWith(endResult.Statistics);
		}
	}

	private void HandleException(Exception exception)
	{
		switch (exception)
		{
			case AggregateException sValue:
			{
				HandleException(sValue.InnerException);
				break;
			}
			case WebClientException ex:
			{
				ClearState(SyncSessionState.Successful);

				switch (ex.Code)
				{
					case HttpStatusCode.Unauthorized:
					{
						SyncIssues.Add(new SyncIssue
						{
							Id = Guid.Empty,
							IssueType = SyncIssueType.Unauthorized,
							Message = "Unauthorized: please update your credentials in settings or contact support.",
							TypeName = string.Empty
						});
						break;
					}
					case HttpStatusCode.ServiceUnavailable:
					{
						SyncIssues.Add(new SyncIssue
						{
							Id = Guid.Empty,
							IssueType = SyncIssueType.ServiceUnavailable,
							Message = "Service unavailable. Please try again later.",
							TypeName = string.Empty
						});
						break;
					}
					default:
					{
						SyncIssues.Add(new SyncIssue
						{
							Id = Guid.Empty,
							IssueType = SyncIssueType.ClientException,
							Message = ex.Message,
							TypeName = string.Empty
						});
						break;
					}
				}
				break;
			}
			case not null:
			{
				ClearState(SyncSessionState.Successful);

				SyncIssues.Add(new SyncIssue
				{
					Id = Guid.Empty,
					IssueType = SyncIssueType.ClientException,
					Message = exception.Message,
					TypeName = string.Empty
				});
				break;
			}
		}
	}

	private void LogVerboseState(SyncSessionState state)
	{
		switch (state)
		{
			case SyncSessionState.Started:
			{
				OnLogEvent($"Sync {SyncType} has started.", LogLevel.Debug);
				break;
			}
			case SyncSessionState.Configuring:
			{
				OnLogEvent($"Sync {SyncType} is being configured.", LogLevel.Debug);
				break;
			}
			case SyncSessionState.Configured:
			{
				OnLogEvent($"Sync {SyncType} has been configured for {Settings.LastSyncedOnClient}, {Settings.LastSyncedOnServer}", LogLevel.Debug);
				break;
			}
			case SyncSessionState.Beginning:
			{
				OnLogEvent($"Sync {SyncType} is beginning.", LogLevel.Debug);
				break;
			}
			case SyncSessionState.Pulling:
			{
				OnLogEvent($"Sync {SyncType} pulling from server to client.", LogLevel.Debug);
				break;
			}
			case SyncSessionState.Pushing:
			{
				OnLogEvent($"Sync {SyncType} pushing to server from client.", LogLevel.Debug);
				break;
			}
			case SyncSessionState.Cancelled:
			{
				OnLogEvent($"Sync {SyncType} was cancelled.", LogLevel.Debug);

				break;
			}
			case SyncSessionState.Ending:
			{
				OnLogEvent($"Sync {SyncType} is ending session.", LogLevel.Debug);

				break;
			}
			case SyncSessionState.Completed:
			{
				OnLogEvent($"Sync {SyncType} completed.", LogLevel.Debug);
				break;
			}
			case SyncSessionState.CouldNotStart:
			case SyncSessionState.Unknown:
			case SyncSessionState.Successful:
			{
				// Ignore these
				break;
			}
			default:
			{
				OnLogEvent($"Unsupported sync session state... {state}", LogLevel.Critical);
				break;
			}
		}
	}

	/// <summary>
	/// Write a message to the log.
	/// </summary>
	/// <param name="message"> The message to be written. </param>
	/// <param name="level"> The level of this message. </param>
	private void OnLogEvent(string message, LogLevel level)
	{
		_logger?.Write(level, SessionId, message, CurrentTime);
	}

	private SyncOperationResult ProcessCorrections(SyncClient server, SyncClient client, IDictionary<Guid, DateTime> incoming)
	{
		var issuesToProcess = new ServiceRequest<SyncIssue>
		{
			Collection = SyncIssues.Take(Settings.ItemsPerSyncRequest).ToList()
		};
		var correctionResult = server.Sync(new SyncOperation
		{
			SessionId = SessionId,
			Settings = Settings,
			Issues = issuesToProcess
		});
		if (correctionResult.Statistics != null)
		{
			StatisticsForServer.UpdateWith(correctionResult.Statistics);
		}

		if (correctionResult.Corrections?.Collection?.Count > 0)
		{
			RemoveIssues(SyncIssues, correctionResult.Corrections.Collection);
			ApplyIncoming(client, correctionResult.Corrections.Collection, incoming);
		}

		if (correctionResult.AppliedIssues?.Collection?.Count > 0)
		{
			SyncIssues.AddRange(correctionResult.AppliedIssues.Collection);
		}

		var localCorrections = client.GetCorrections(SessionId, issuesToProcess);
		if (localCorrections?.Collection?.Count > 0)
		{
			RemoveIssues(SyncIssues, localCorrections.Collection);
			var applied = server.Sync(new SyncOperation
			{
				SessionId = SessionId,
				Settings = Settings,
				Issues = issuesToProcess,
				Changes = new ServiceRequest<SyncObject>(localCorrections.Collection)
			});
			if (applied.AppliedIssues?.Collection?.Count > 0)
			{
				SyncIssues.AddRange(applied.AppliedIssues.Collection);
			}

			return applied;
		}

		return correctionResult;
	}

	/// <summary>
	/// The next phase this session has not run. Ending means the work loop is finished.
	/// </summary>
	private SyncSessionState NextSyncState(bool clientHasNoChanges, SyncOperationResult serverResult)
	{
		if (!Settings.HasFilters && State.HasFlag(SyncSessionState.Beginning))
		{
			return SyncSessionState.Ending;
		}

		if (!State.HasFlag(SyncSessionState.Beginning))
		{
			return SyncSessionState.Beginning;
		}

		if (Settings.SyncDirection.HasFlag(SyncDirection.PullDown) && !State.HasFlag(SyncSessionState.Pulling))
		{
			return SyncSessionState.Pulling;
		}

		if (Settings.SyncDirection.HasFlag(SyncDirection.PushUp)
			&& !clientHasNoChanges
			&& (serverResult?.SessionEnded != true)
			&& !State.HasFlag(SyncSessionState.Pushing))
		{
			return SyncSessionState.Pushing;
		}

		return SyncSessionState.Ending;
	}

	/// <summary>
	/// True when the local client has nothing to push for this session window.
	/// </summary>
	private bool ClientHasNoOutgoingChanges(SyncClient client, SyncSessionStart clientSession)
	{
		if (client is not SyncClientForDatabase databaseClient)
		{
			return false;
		}

		var count = databaseClient.CountOutgoingChanges(SessionId, new SyncRequest
		{
			Since = Settings.LastSyncedOnClient,
			Until = clientSession.StartedOn
		});
		return count == 0;
	}

	/// <summary>
	/// Pull server pages and apply them locally before the client looks for its own changes.
	/// A row applied here is not pushed back. A newer client row is left in place and can still be pushed.
	/// The first call begins the server session. The last page ends it when the client will not push,
	/// including pull-only. That end is inside the pull call. There is no later end-only call.
	/// </summary>
	private SyncOperationResult PullFromServer(
		SyncClient client,
		SyncClient server,
		IDictionary<Guid, DateTime> incoming,
		bool clientHasNoChanges,
		ref SyncSessionStart serverSession)
	{
		using (SyncClientProfilerForClient.Start("Pull"))
		{
			var serverSkip = 0;
			var serverProcessed = 0;
			SyncOperationResult result = null;

			while (!SyncCancelled)
			{
				result = server.Sync(new SyncOperation
				{
					SessionId = SessionId,
					Settings = Settings,
					GetChangesSkip = serverSkip,
					ClientHasNoChanges = clientHasNoChanges
				});
				ApplyServerResult(client, result, incoming, ref serverSession);
				if (result.Changes?.Collection?.Count > 0)
				{
					serverSkip += result.Changes.Collection.Count;
					serverProcessed += result.Changes.Collection.Count;
					UpdatePercent(
						serverProcessed + (result.Changes.HasMore ? 1 : 0),
						serverProcessed);
				}

				if (result.SessionEnded || result.Changes is not { HasMore: true })
				{
					break;
				}
			}

			if (!SyncCancelled && SyncIssues.Any() && (serverSession != null) && (result?.SessionEnded == false))
			{
				result = ProcessCorrections(server, client, incoming) ?? result;
			}

			return result;
		}
	}

	/// <summary>
	/// Push local pages that were not just applied from the server.
	/// A short page ends the server session on that call. A full page does not, because the next
	/// local page might exist. When that next page is empty, this loop does not query the server.
	/// The session then makes one end-only call.
	/// The local request sets Take to the page size. SyncRequest defaults Take to 1000, and
	/// GetChanges keeps a positive Take that does not exceed that page size, so a larger page
	/// would otherwise look short and end the session before the remaining rows are sent.
	/// </summary>
	private SyncOperationResult PushToServer(
		SyncClient client,
		SyncClient server,
		SyncSessionStart clientSession,
		IDictionary<Guid, DateTime> incoming,
		ref SyncSessionStart serverSession)
	{
		using (SyncClientProfilerForClient.Start("Push"))
		{
			var clientSkip = 0;
			SyncOperationResult result = null;
			while (!SyncCancelled && (result?.SessionEnded != true))
			{
				var localRequest = new SyncRequest
				{
					Since = Settings.LastSyncedOnClient,
					Until = clientSession.StartedOn,
					Skip = clientSkip,
					Take = Settings.ItemsPerSyncRequest
				};
				var page = client.GetChanges(SessionId, localRequest);
				var pageCount = page.Collection?.Count ?? 0;
				clientSkip += pageCount;

				var outgoing = page.Collection
					.Where(x => !incoming.ContainsKey(x.SyncId) || (incoming[x.SyncId] != x.ModifiedOn))
					.ToList();

				if (outgoing.Count > 0)
				{
					var lastPage = pageCount < Settings.ItemsPerSyncRequest;
					result = server.Sync(new SyncOperation
					{
						SessionId = SessionId,
						Settings = Settings,
						Changes = new ServiceRequest<SyncObject>(outgoing),
						EndSession = lastPage && !SyncIssues.Any()
					});
					ApplyServerResult(client, result, incoming, ref serverSession);
				}

				if ((result?.SessionEnded == true) || (pageCount < Settings.ItemsPerSyncRequest))
				{
					break;
				}
			}

			if (!SyncCancelled && SyncIssues.Any() && (serverSession != null) && (result?.SessionEnded != true))
			{
				result = ProcessCorrections(server, client, incoming) ?? result;
			}

			return result;
		}
	}

	private void RemoveIssues(ICollection<SyncIssue> syncIssues, IList<SyncObject> collection)
	{
		// Remove any issue that will be processed because we'll read add any issues during processing
		syncIssues.Where(x => collection.Any(y => y.SyncId == x.Id)).ToList()
			.ForEach(x => syncIssues.Remove(syncIssues.FirstOrDefault(y => y.Id == x.Id)));
	}

	private void Reset(string syncType = null)
	{
		SessionId = Guid.Empty;
		State = SyncSessionState.Unknown;
		StartedOn = DateTime.MinValue;
		StoppedOn = DateTime.MinValue;
		SyncIssues.Clear();
		StatisticsForClient.Reset();
		StatisticsForServer.Reset();
		Settings.Reset();
		Settings.SyncType = syncType;
	}

	private void UpdatePercent(decimal total, decimal count)
	{
		Percent = total <= 0 ? 0 : Math.Round((count / total) * 100, 2);
	}

	#endregion
}