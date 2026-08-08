#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Logging;
using Cornerstone.Presentation;
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
[DependencyInjected]
public partial class SyncSession : CornerstoneObject<SyncSession>
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
		SyncIssues = new PresentationList<SyncIssue>();

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
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial decimal Percent { get; private set; }

	/// <summary>
	/// Gets the ID of the sync session.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial Guid SessionId { get; private set; }

	/// <summary>
	/// The sync options.
	/// </summary>
	[UpdateableAction(UpdateableAction.All)]
	public SyncSettings Settings { get; }

	/// <summary>
	/// Gets a flag to indicate progress should be shown. Will only be true if sync takes longer than the <seealso cref="ShowProgressThreshold" />.
	/// </summary>
	public bool ShowProgress => SyncRunning && (Elapsed >= ShowProgressThreshold);

	/// <summary>
	/// Gets the value to determine when to trigger <seealso cref="ShowProgress" />. Defaults to one second.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial TimeSpan ShowProgressThreshold { get; set; }

	/// <summary>
	/// The date time the sync started on.
	/// </summary>
	[Notify]
	[AlsoNotify(nameof(Elapsed), nameof(SyncStarted), nameof(SyncRunning))]
	[UpdateableAction(UpdateableAction.All)]
	public partial DateTime StartedOn { get; private set; }

	/// <summary>
	/// The state of the sync session.
	/// </summary>
	[Notify]
	[AlsoNotify(nameof(SyncCancelled), nameof(SyncCompleted), nameof(SyncRunning), nameof(SyncSuccessful))]
	[UpdateableAction(UpdateableAction.All)]
	public partial SyncSessionState State { get; private set; }

	/// <summary>
	/// Statistics for client
	/// </summary>
	[UpdateableAction(UpdateableAction.All)]
	public SyncStatistics StatisticsForClient { get; }

	/// <summary>
	/// Statistics for server
	/// </summary>
	[UpdateableAction(UpdateableAction.All)]
	public SyncStatistics StatisticsForServer { get; }

	/// <summary>
	/// The date time the sync stopped on.
	/// </summary>
	[Notify]
	[AlsoNotify(nameof(Elapsed), nameof(SyncCompleted), nameof(SyncRunning))]
	[UpdateableAction(UpdateableAction.All)]
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
	[UpdateableAction(UpdateableAction.All)]
	public PresentationList<SyncIssue> SyncIssues { get; }

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
		try
		{
			UpdatePercent(0, 0);
			UpdateState(SyncSessionState.Configuring);
			updateSettings?.Invoke(Settings);

			var client = syncManager.GetSyncClientForClient(StatisticsForClient, SyncClientProfilerForClient);
			var server = syncManager.GetSyncClientForServer(StatisticsForServer, SyncClientProfilerForServer);
			if ((client == null) || (server == null))
			{
				throw new CornerstoneException("Sync client for client or server is null.");
			}

			onSyncConfiguring?.Invoke(this);
			UpdateState(SyncSessionState.Configured);
			SyncSessionStart serverSession = null, clientSession = null;

			if (!SyncCancelled)
			{
				UpdateState(SyncSessionState.Beginning);
				serverSession = server.BeginSync(SessionId, Settings);
				clientSession = client.BeginSync(SessionId, Settings);
			}

			var incoming = new Dictionary<Guid, DateTime>();

			if (!SyncCancelled
				&& Settings.SyncDirection.HasFlag(SyncDirection.PullDown)
				&& (serverSession != null))
			{
				UpdateState(SyncSessionState.Pulling);
				Process(server, client, Settings.LastSyncedOnServer, serverSession.StartedOn, incoming);
			}

			if (!SyncCancelled
				&& Settings.SyncDirection.HasFlag(SyncDirection.PushUp)
				&& (clientSession != null))
			{
				UpdateState(SyncSessionState.Pushing);
				Process(client, server, Settings.LastSyncedOnClient, clientSession.StartedOn, incoming);
			}

			UpdateState(SyncSessionState.Ending);

			client.EndSync(SessionId);
			server.EndSync(SessionId);

			if (clientSession != null)
			{
				Settings.LastSyncedOnClient = clientSession.StartedOn;
			}

			if (serverSession != null)
			{
				Settings.LastSyncedOnServer = serverSession.StartedOn;
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
		finally
		{
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

	private void ClearState(SyncSessionState flag)
	{
		State = State.ClearFlag(flag);
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
							Message = "Unauthorized: please update your credentials in settings or contact support.",
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

	/// <summary>
	/// Get changes from one client and apply them to another client.
	/// </summary>
	/// <param name="sourceClient"> The source to get changes from. </param>
	/// <param name="destinationClient"> The destination to apply changes to. </param>
	/// <param name="since"> The start date and time to get changes for. </param>
	/// <param name="until"> The end date and time to get changes for. </param>
	/// <param name="exclude"> The optional collection of items to exclude. </param>
	private void Process(SyncClient sourceClient, SyncClient destinationClient, DateTime since, DateTime until, IDictionary<Guid, DateTime> exclude)
	{
		var issues = new ServiceRequest<SyncIssue>();
		var request = new SyncRequest { Since = since, Until = until };
		bool hasMore;

		var excludedIds = new HashSet<Guid>(exclude.Keys);

		do
		{
			var changes = sourceClient.GetChanges(SessionId, request);
			request.Skip += changes.Collection.Count;
			hasMore = changes.HasMore;

			var filtered = changes.Collection
				.Where(x => !excludedIds.Contains(x.SyncId)
					|| (exclude[x.SyncId] != x.ModifiedOn))
				.ToList();

			if (filtered.Count == 0)
			{
				continue;
			}

			request.Collection = filtered;
			var failed = destinationClient.ApplyChanges(SessionId, request).Collection;
			issues.Collection.AddRange(failed);

			// Everything NOT in failed succeeded → mark as synced
			if (failed.Count < filtered.Count)
			{
				var failedIds = failed.Select(i => i.Id).ToHashSet();
				foreach (var x in filtered)
				{
					if (failedIds.Contains(x.SyncId))
					{
						continue;
					}

					excludedIds.Add(x.SyncId);
					exclude[x.SyncId] = x.ModifiedOn;
				}
			}

			UpdatePercent(changes.TotalCount, request.Skip);
		} while (!SyncCancelled && hasMore);

		SyncIssues.AddRange(issues.Collection);

		if (SyncCancelled || !issues.Collection.Any())
		{
			return;
		}

		var issuesToProcess = new ServiceRequest<SyncIssue>
		{
			Collection = issues.Collection.Take(Settings.ItemsPerSyncRequest).ToList()
		};

		var results = sourceClient.GetCorrections(SessionId, issuesToProcess);

		if ((results != null) && results.Collection.Any())
		{
			RemoveIssues(SyncIssues, results.Collection);
			request.Collection = results.Collection;
			SyncIssues.AddRange(destinationClient.ApplyCorrections(SessionId, request).Collection);
		}

		results = destinationClient.GetCorrections(SessionId, issuesToProcess);

		if ((results != null) && results.Collection.Any())
		{
			RemoveIssues(SyncIssues, results.Collection);
			request.Collection = results.Collection;
			SyncIssues.AddRange(sourceClient.ApplyCorrections(SessionId, request).Collection);
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
		Settings.Reset();
		Settings.SyncType = syncType;
	}

	private void UpdatePercent(decimal total, decimal count)
	{
		Percent = total <= 0 ? 0 : Math.Round((count / total) * 100, 2);
	}

	#endregion
}