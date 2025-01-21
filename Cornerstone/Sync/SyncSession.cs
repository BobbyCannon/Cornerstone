#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net;
using System.Threading;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Exceptions;
using Cornerstone.Extensions;
using Cornerstone.Logging;
using Cornerstone.Net;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// The object to track a sync session.
/// </summary>
public class SyncSession : Bindable<SyncSession>
{
	#region Fields

	private readonly SyncManager _syncManager;
	private readonly IDateTimeProvider _timeProvider;

	#endregion

	#region Constructors

	/// <summary>
	/// Initiates an instances of the sync session.
	/// </summary>
	public SyncSession(SyncManager syncManager, Guid sessionId, string syncType, IDateTimeProvider timeProvider, IDispatcher dispatcher) : base(dispatcher)
	{
		_syncManager = syncManager;
		_timeProvider = timeProvider;

		Settings = new SyncSettings(dispatcher);
		StatisticsForClient = new SyncStatistics(dispatcher);
		StatisticsForServer = new SyncStatistics(dispatcher);
		SyncClientProfilerForClient = new Profiler("Client");
		SyncClientProfilerForServer = new Profiler("Server");
		SyncIssues = new SpeedyList<SyncIssue>(dispatcher);

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
	/// The sync options.
	/// </summary>
	public SyncSettings Settings { get; }

	/// <summary>
	/// The percent of processing. This is based on the sync session <see cref="State" />.
	/// </summary>
	public decimal Percent { get; private set; }

	/// <summary>
	/// Gets the ID of the sync session.
	/// </summary>
	public Guid SessionId { get; private set; }

	/// <summary>
	/// Gets a flag to indicate progress should be shown. Will only be true if sync takes longer than the <seealso cref="ShowProgressThreshold" />.
	/// </summary>
	public bool ShowProgress => SyncRunning && (Elapsed >= ShowProgressThreshold);

	/// <summary>
	/// Gets the value to determine when to trigger <seealso cref="ShowProgress" />. Defaults to one second.
	/// </summary>
	public TimeSpan ShowProgressThreshold { get; set; }

	/// <summary>
	/// The date time the sync started on.
	/// </summary>
	public DateTime StartedOn { get; private set; }

	/// <summary>
	/// The state of the sync session.
	/// </summary>
	public SyncSessionState State { get; private set; }

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
	public DateTime StoppedOn { get; private set; }

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
	/// The sync ran to completion.
	/// </summary>
	public bool SyncCompleted => StoppedOn > DateTime.MinValue;

	/// <summary>
	/// Gets the list of issues that occurred during the last sync.
	/// </summary>
	public SpeedyList<SyncIssue> SyncIssues { get; }

	/// <summary>
	/// Gets a value indicating if the sync is running.
	/// </summary>
	public bool SyncRunning => SyncStarted && !SyncCompleted;

	/// <summary>
	/// Gets a value indicating if the last sync was started.
	/// </summary>
	public bool SyncStarted => StartedOn > DateTime.MinValue;

	/// <summary>
	/// Gets a value indicating if the sync was successful.
	/// </summary>
	public bool SyncSuccessful => State.HasFlag(SyncSessionState.Successful);

	/// <summary>
	/// The type for the sync.
	/// </summary>
	public string SyncType => Settings.SyncType;

	/// <summary>
	/// Gets the current time.
	/// </summary>
	protected DateTime CurrentTime => _timeProvider?.UtcNow ?? DateTimeProvider.RealTime.UtcNow;

	#endregion

	#region Methods

	/// <summary>
	/// Request the sync session be cancelled.
	/// </summary>
	public void Cancel()
	{
		UpdateState(SyncSessionState.Cancelled);
		StoppedOn = CurrentTime;
	}

	/// <summary>
	/// Mark the session as completed.
	/// </summary>
	public void CompleteSync()
	{
		UpdateState(SyncSessionState.Completed);
		StoppedOn = CurrentTime;
	}

	public static SyncSession CouldNotStart(SyncManager syncManager, Guid sessionId, string syncType, IDateTimeProvider timeProvider)
	{
		var session = new SyncSession(syncManager, sessionId, syncType, timeProvider, null);
		session.UpdateState(SyncSessionState.CouldNotStart);
		return session;
	}

	/// <summary>
	/// Update the SyncSession with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="settings"> The options for controlling the updating of the value. </param>
	public override bool UpdateWith(SyncSession update, IncludeExcludeSettings settings)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((settings == null) || settings.IsEmpty())
		{
			Settings.UpdateWith(update.Settings);
			Percent = update.Percent;
			SessionId = update.SessionId;
			ShowProgressThreshold = update.ShowProgressThreshold;
			StartedOn = update.StartedOn;
			State = update.State;
			StatisticsForClient.UpdateWith(update.StatisticsForClient);
			StatisticsForServer.UpdateWith(update.StatisticsForServer);
			StoppedOn = update.StoppedOn;
			SyncClientProfilerForClient.UpdateWith(update.SyncClientProfilerForClient);
			SyncClientProfilerForServer.UpdateWith(update.SyncClientProfilerForServer);
			SyncIssues.Reconcile(update.SyncIssues);
		}
		else
		{
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Settings)), x => x.Settings.UpdateWith(update.Settings));
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(Percent)), x => x.Percent = update.Percent);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(SessionId)), x => x.SessionId = update.SessionId);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(ShowProgressThreshold)), x => x.ShowProgressThreshold = update.ShowProgressThreshold);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(StartedOn)), x => x.StartedOn = update.StartedOn);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(State)), x => x.State = update.State);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(StatisticsForClient)), x => x.StatisticsForClient.UpdateWith(update.StatisticsForClient));
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(StatisticsForServer)), x => x.StatisticsForServer.UpdateWith(update.StatisticsForServer));
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(StoppedOn)), x => x.StoppedOn = update.StoppedOn);
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(SyncClientProfilerForClient)), x => x.SyncClientProfilerForClient.UpdateWith(update.SyncClientProfilerForClient));
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(SyncClientProfilerForServer)), x => x.SyncClientProfilerForServer.UpdateWith(update.SyncClientProfilerForServer));
			this.IfThen(_ => settings.ShouldProcessProperty(nameof(SyncIssues)), x => x.SyncIssues.Reconcile(update.SyncIssues));
		}

		return true;
	}

	/// <summary>
	/// Wait for the sync to complete.
	/// </summary>
	/// <param name="timeout"> The max amount of time to wait. </param>
	/// <returns> True if the sync completed otherwise false if timed out waiting. </returns>
	public bool WaitForSyncToComplete(TimeSpan timeout)
	{
		if (SyncCompleted)
		{
			return true;
		}

		var watch = Stopwatch.StartNew();

		while (!SyncCompleted)
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
	/// Wait for the sync to start.
	/// </summary>
	/// <param name="timeout"> The max amount of time to wait. </param>
	/// <returns> True if the sync was started otherwise false if timed out waiting. </returns>
	public bool WaitForSyncToStart(TimeSpan timeout)
	{
		if (SyncStarted)
		{
			return true;
		}

		var watch = Stopwatch.StartNew();

		while (SyncStarted != true)
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
	/// <param name="updateSettings"> Update options before running sync. </param>
	/// <param name="onSyncRunning"> Action to call when sync starts running. </param>
	/// <param name="onSyncCompleted"> </param>
	internal SyncSession ProcessSyncSession(Action<SyncSettings> updateSettings, Action<SyncSession> onSyncRunning, Action<SyncSession> onSyncCompleted)
	{
		try
		{
			UpdatePercent(0, 0);
			UpdateState(SyncSessionState.Initializing);

			updateSettings?.Invoke(Settings);

			var client = _syncManager.GetSyncClientForClient(StatisticsForClient, SyncClientProfilerForClient);
			var server = _syncManager.GetSyncClientForServer(StatisticsForServer, SyncClientProfilerForServer);

			if ((client == null) || (server == null))
			{
				throw new CornerstoneException("Sync client for client or server is null.");
			}

			onSyncRunning?.Invoke(this);

			UpdateState(SyncSessionState.Starting);

			var serverSession = server.BeginSync(SessionId, Settings);
			var clientSession = client.BeginSync(SessionId, Settings);
			var incoming = new Dictionary<Guid, DateTime>();

			UpdateState(SyncSessionState.Started);

			if (!SyncCancelled && Settings.SyncDirection.HasFlag(SyncDirection.PullDown))
			{
				UpdateState(SyncSessionState.Pulling);
				Process(server, client, Settings.LastSyncedOnServer, serverSession.StartedOn, incoming);
			}

			if (!SyncCancelled && Settings.SyncDirection.HasFlag(SyncDirection.PushUp))
			{
				UpdateState(SyncSessionState.Pushing);
				Process(client, server, Settings.LastSyncedOnClient, clientSession.StartedOn, incoming);
			}

			UpdateState(SyncSessionState.Completing);

			client.EndSync(SessionId);
			server.EndSync(SessionId);

			//SortLocalDatabases();

			Settings.LastSyncedOnClient = clientSession.StartedOn;
			Settings.LastSyncedOnServer = serverSession.StartedOn;

			if (!SyncCancelled && !SyncIssues.Any())
			{
				UpdateState(SyncSessionState.Successful);
			}

			UpdatePercent(100, 100);
		}
		catch (WebClientException ex)
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
		}
		catch (Exception ex)
		{
			ClearState(SyncSessionState.Successful);

			SyncIssues.Add(new SyncIssue
			{
				Id = Guid.Empty,
				IssueType = SyncIssueType.ClientException,
				Message = ex.Message,
				TypeName = string.Empty
			});
		}
		finally
		{
			// This must be the last state that must change
			StoppedOn = CurrentTime;

			// See if we have a timer for this sync type
			if (_syncManager.SyncTimers.TryGetValue(SyncType, out var syncTimer))
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

		var response = new SyncSession(null, SessionId, SyncType, _timeProvider, GetDispatcher());
		response.DisablePropertyChangeNotifications();
		response.UpdateWith(this);
		response.CompleteSync();
		response.EnablePropertyChangeNotifications();
		response.ResetHasChanges();
		onSyncCompleted.Invoke(this);

		CompleteSync();

		return response;
	}

	/// <summary>
	/// Start the sync session.
	/// </summary>
	/// <param name="sessionId"> The ID for the session. </param>
	/// <param name="syncType"> The type of the sync to start. </param>
	/// <param name="updates"> The option to update the session with. </param>
	internal void Start(Guid sessionId, string syncType, SyncSettings updates)
	{
		Reset();
		Settings.UpdateWith(updates);
		Settings.SyncType = syncType;
		SessionId = sessionId;
		StartedOn = CurrentTime;
	}

	private void ClearState(SyncSessionState flag)
	{
		State = State.ClearFlag(flag);
	}

	private void LogVerboseState(SyncSessionState state)
	{
		if (_syncManager == null)
		{
			return;
		}

		switch (state)
		{
			case SyncSessionState.Initializing:
			{
				OnLogEvent($"Syncing {SyncType} for {Settings.LastSyncedOnClient}, {Settings.LastSyncedOnServer}", EventLevel.Verbose);
				break;
			}
			case SyncSessionState.Starting:
			{
				OnLogEvent("Starting the sync session.", EventLevel.Verbose);
				break;
			}
			case SyncSessionState.Started:
			{
				OnLogEvent("The sync session has started.", EventLevel.Verbose);
				break;
			}
			case SyncSessionState.Pulling:
			{
				OnLogEvent("Starting to pull from server to client.", EventLevel.Verbose);
				break;
			}
			case SyncSessionState.Pushing:
			{
				OnLogEvent("Starting to push to server from client.", EventLevel.Verbose);
				break;
			}
			case SyncSessionState.Cancelled:
			{
				OnLogEvent("The sync session was cancelled.", EventLevel.Verbose);

				break;
			}
			case SyncSessionState.Completing:
			{
				OnLogEvent("Starting to end the session.", EventLevel.Verbose);

				break;
			}
			case SyncSessionState.Completed:
			{
				OnLogEvent($"Sync {SyncType} completed. {Elapsed:mm\\:ss\\.fff}", EventLevel.Verbose);
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
				OnLogEvent($"Unsupported sync session state... {state}", EventLevel.Critical);
				break;
			}
		}
	}

	/// <summary>
	/// Write a message to the log.
	/// </summary>
	/// <param name="message"> The message to be written. </param>
	/// <param name="level"> The level of this message. </param>
	private void OnLogEvent(string message, EventLevel level)
	{
		Logger.Instance.Write(SessionId, message, level, CurrentTime);
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
		var request = new SyncRequest { Since = since, Until = until, Skip = 0 };
		bool hasMore;

		do
		{
			// Get changes and move the request forward
			var changes = sourceClient.GetChanges(SessionId, request);
			request.Skip += changes.Collection.Count;
			hasMore = changes.HasMore;

			// Filter out any existing items that have been synced already (must have been excluded with the same modified on date/time)
			request.Collection = changes.Collection
				.Where(x => !exclude.ContainsKey(x.SyncId) || (exclude[x.SyncId] != x.ModifiedOn))
				.ToList();

			if (!request.Collection.Any())
			{
				continue;
			}

			// Apply changes and track any sync issues returned
			issues.Collection.Add(destinationClient.ApplyChanges(SessionId, request).Collection);

			// Capture all items that were synced without issue
			foreach (var syncObject in request.Collection.Where(x => issues.Collection.All(i => i.Id != x.SyncId)))
			{
				exclude.AddOrUpdate(syncObject.SyncId, syncObject.ModifiedOn);
			}

			UpdatePercent(changes.TotalCount, request.Skip);
		} while (!SyncCancelled && hasMore);

		SyncIssues.Add(issues.Collection);

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
			SyncIssues.Add(destinationClient.ApplyCorrections(SessionId, request).Collection);
		}

		results = destinationClient.GetCorrections(SessionId, issuesToProcess);

		if ((results != null) && results.Collection.Any())
		{
			RemoveIssues(SyncIssues, results.Collection);
			request.Collection = results.Collection;
			SyncIssues.Add(sourceClient.ApplyCorrections(SessionId, request).Collection);
		}
	}

	private void RemoveIssues(ICollection<SyncIssue> syncIssues, IList<SyncObject> collection)
	{
		// Remove any issue that will be processed because we'll read add any issues during processing
		syncIssues.Where(x => collection.Any(y => y.SyncId == x.Id)).ToList()
			.ForEach(x => syncIssues.Remove(syncIssues.FirstOrDefault(y => y.Id == x.Id)));
	}

	private void Reset(string syncType = default)
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

	private void UpdateState(SyncSessionState flag)
	{
		LogVerboseState(flag);
		State = State.SetFlag(flag);
	}

	#endregion
}