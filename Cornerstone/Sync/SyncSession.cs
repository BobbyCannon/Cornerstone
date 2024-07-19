#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Exceptions;
using Cornerstone.Extensions;
using Cornerstone.Logging;
using Cornerstone.Net;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// The object to track a sync session.
/// </summary>
public class SyncSession : Bindable<SyncSession>, ILoggingSession
{
	#region Fields

	private readonly SyncManager _syncManager;

	#endregion

	#region Constructors

	/// <summary>
	/// Initiates an instances of the sync session.
	/// </summary>
	public SyncSession() : this(null, default, null)
	{
	}

	/// <summary>
	/// Initiates an instances of the sync session.
	/// </summary>
	public SyncSession(SyncManager syncManager, string syncType, IDispatcher dispatcher) : base(dispatcher)
	{
		_syncManager = syncManager;

		Options = new SyncOptions(dispatcher);
		SessionId = Guid.NewGuid();
		StatisticsForClient = new SyncStatistics(dispatcher);
		StatisticsForServer = new SyncStatistics(dispatcher);
		//SyncIssues = new ThreadSafeObservableList<SyncIssue>(null, null, dispatcher);
		SyncIssues = new SpeedyList<SyncIssue>(dispatcher);

		Reset(syncType);
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
				? TimeService.CurrentTime.UtcNow - StartedOn
				: StoppedOn - StartedOn;

	/// <summary>
	/// The sync options.
	/// </summary>
	public SyncOptions Options { get; }

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
	public bool SyncCancelled { get; private set; }

	/// <summary>
	/// The sync ran to completion.
	/// </summary>
	public bool SyncCompleted { get; private set; }

	/// <summary>
	/// Gets the list of issues that occurred during the last sync.
	/// </summary>
	public SpeedyList<SyncIssue> SyncIssues { get; }

	/// <summary>
	/// Gets a value indicating if the sync is running.
	/// </summary>
	public bool SyncRunning => (StartedOn > DateTime.MinValue) && !SyncCompleted;

	/// <summary>
	/// Gets a value indicating if the last sync was started.
	/// </summary>
	public bool SyncStarted => StartedOn > DateTime.MinValue;

	/// <summary>
	/// Gets a value indicating if the sync was successful.
	/// </summary>
	public bool SyncSuccessful { get; private set; }

	/// <summary>
	/// The Type for the sync.
	/// </summary>
	public string SyncType { get; private set; }

	#endregion

	#region Methods

	/// <summary>
	/// Request the sync session be cancelled.
	/// </summary>
	public void Cancel()
	{
		SyncCancelled = true;
	}

	/// <summary>
	/// Update the SyncSession with an update.
	/// </summary>
	/// <param name="update"> The update to be applied. </param>
	/// <param name="options"> The options for controlling the updating of the value. </param>
	public override bool UpdateWith(SyncSession update, IncludeExcludeOptions options)
	{
		// If the update is null then there is nothing to do.
		if (update == null)
		{
			return false;
		}

		// ****** You can use GenerateUpdateWith to update this ******

		if ((options == null) || options.IsEmpty())
		{
			Options.UpdateWith(update.Options);
			Percent = update.Percent;
			SessionId = update.SessionId;
			ShowProgressThreshold = update.ShowProgressThreshold;
			StartedOn = update.StartedOn;
			State = update.State;
			StatisticsForClient.UpdateWith(update.StatisticsForClient);
			StatisticsForServer.UpdateWith(update.StatisticsForServer);
			StoppedOn = update.StoppedOn;
			SyncCancelled = update.SyncCancelled;
			SyncCompleted = update.SyncCompleted;
			SyncIssues.Reconcile(update.SyncIssues);
			SyncSuccessful = update.SyncSuccessful;
			SyncType = update.SyncType;
		}
		else
		{
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Options)), x => x.Options.UpdateWith(update.Options));
			this.IfThen(_ => options.ShouldProcessProperty(nameof(Percent)), x => x.Percent = update.Percent);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(SessionId)), x => x.SessionId = update.SessionId);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(ShowProgressThreshold)), x => x.ShowProgressThreshold = update.ShowProgressThreshold);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(StartedOn)), x => x.StartedOn = update.StartedOn);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(State)), x => x.State = update.State);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(StatisticsForClient)), x => x.StatisticsForClient.UpdateWith(update.StatisticsForClient));
			this.IfThen(_ => options.ShouldProcessProperty(nameof(StatisticsForServer)), x => x.StatisticsForServer.UpdateWith(update.StatisticsForServer));
			this.IfThen(_ => options.ShouldProcessProperty(nameof(StoppedOn)), x => x.StoppedOn = update.StoppedOn);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(SyncCancelled)), x => x.SyncCancelled = update.SyncCancelled);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(SyncCompleted)), x => x.SyncCompleted = update.SyncCompleted);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(SyncIssues)), x => x.SyncIssues.Reconcile(update.SyncIssues));
			this.IfThen(_ => options.ShouldProcessProperty(nameof(SyncSuccessful)), x => x.SyncSuccessful = update.SyncSuccessful);
			this.IfThen(_ => options.ShouldProcessProperty(nameof(SyncType)), x => x.SyncType = update.SyncType);
		}

		return true;
	}

	/// <inheritdoc />
	protected override void OnPropertyChangedInDispatcher(string propertyName)
	{
		switch (propertyName)
		{
			case nameof(State):
			{
				LogVerboseState(State);
				break;
			}
		}

		base.OnPropertyChangedInDispatcher(propertyName);
	}

	/// <summary>
	/// Write a message to the log.
	/// </summary>
	/// <param name="message"> The message to be written. </param>
	/// <param name="level"> The level of this message. </param>
	internal void OnLogEvent(string message, EventLevel level)
	{
		Logger.Instance.Write(SessionId, message, level);
	}

	/// <summary>
	/// Run the sync. This should only be called by ProcessAsync.
	/// </summary>
	/// <param name="updateOptions"> Update options before running sync. </param>
	/// <param name="onSyncRunning"> Action to call when sync starts running. </param>
	/// <param name="onSyncCompleted"> </param>
	internal SyncSession RunSync(Action<SyncOptions> updateOptions, Action<SyncSession> onSyncRunning, Action<SyncSession> onSyncCompleted)
	{
		try
		{
			UpdatePercent(0, 0);

			State = SyncSessionState.Initializing;

			updateOptions?.Invoke(Options);

			var client = _syncManager.GetSyncClientForClient();
			var server = _syncManager.GetSyncClientForServer();

			if ((client == null) || (server == null))
			{
				throw new CornerstoneException("Sync client for client or server is null.");
			}

			onSyncRunning?.Invoke(this);

			State = SyncSessionState.Starting;

			var serverSession = server.BeginSync(SessionId, Options);
			var clientSession = client.BeginSync(SessionId, Options);
			var incoming = new Dictionary<Guid, DateTime>();

			State = SyncSessionState.Started;

			if (!SyncCancelled && Options.SyncDirection.HasFlag(SyncDirection.PullDown))
			{
				State = SyncSessionState.Pulling;
				Process(server, client, Options.LastSyncedOnServer, serverSession.StartedOn, incoming);
			}

			if (!SyncCancelled && Options.SyncDirection.HasFlag(SyncDirection.PushUp))
			{
				State = SyncSessionState.Pushing;
				Process(client, server, Options.LastSyncedOnClient, clientSession.StartedOn, incoming);
			}

			State = SyncSessionState.Ending;

			client.EndSync(SessionId);
			server.EndSync(SessionId);

			//SortLocalDatabases();

			Options.LastSyncedOnClient = clientSession.StartedOn;
			Options.LastSyncedOnServer = serverSession.StartedOn;

			StatisticsForClient.UpdateWith(client.Statistics);
			StatisticsForServer.UpdateWith(server.Statistics);

			SyncSuccessful = !SyncCancelled && !SyncIssues.Any();

			UpdatePercent(100, 100);
		}
		catch (WebClientException ex)
		{
			SyncSuccessful = false;

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
			SyncSuccessful = false;
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
			StoppedOn = TimeService.CurrentTime.UtcNow;

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

			State = SyncSessionState.Completed;
		}

		var response = new SyncSession();
		response.DisablePropertyChangeNotifications();
		response.UpdateWith(this);
		response.SyncCompleted = true;
		response.EnablePropertyChangeNotifications();
		response.ResetHasChanges();
		onSyncCompleted.Invoke(this);
		SyncCompleted = true;
		return response;
	}

	/// <summary>
	/// Start the sync session.
	/// </summary>
	/// <param name="syncType"> The type of the sync to start. </param>
	/// <param name="updates"> The option to update the session with. </param>
	internal void Start(string syncType, SyncOptions updates)
	{
		Reset();
		Options.UpdateWith(updates);
		SessionId = Guid.NewGuid();
		SyncType = syncType;
		StartedOn = TimeService.CurrentTime.UtcNow;
	}

	private void LogVerboseState(SyncSessionState state)
	{
		switch (State)
		{
			case SyncSessionState.Initializing:
			{
				OnLogEvent($"Syncing {SyncType} for {Options.LastSyncedOnClient}, {Options.LastSyncedOnServer}", EventLevel.Verbose);
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
			case SyncSessionState.Ending:
			{
				OnLogEvent("Starting to end the session.", EventLevel.Verbose);

				break;
			}
			case SyncSessionState.Completed:
			{
				OnLogEvent($"Sync {SyncType} completed. {Elapsed:mm\\:ss\\.fff}", EventLevel.Verbose);
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
	/// Get changes from one client and apply them to another client.
	/// </summary>
	/// <param name="sourceClient"> The source to get changes from. </param>
	/// <param name="destinationClient"> The destination to apply changes to. </param>
	/// <param name="since"> The start date and time to get changes for. </param>
	/// <param name="until"> The end date and time to get changes for. </param>
	/// <param name="exclude"> The optional collection of items to exclude. </param>
	private void Process(ISyncClient sourceClient, ISyncClient destinationClient, DateTime since, DateTime until, IDictionary<Guid, DateTime> exclude)
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
			issues.Collection.AddRange(destinationClient.ApplyChanges(SessionId, request).Collection);

			// Capture all items that were synced without issue
			foreach (var syncObject in request.Collection.Where(x => issues.Collection.All(i => i.Id != x.SyncId)))
			{
				exclude.AddOrUpdate(syncObject.SyncId, syncObject.ModifiedOn);
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
			Collection = issues.Collection.Take(Options.ItemsPerSyncRequest).ToList()
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

	private void Reset(string syncType = default)
	{
		Options.Reset();
		SessionId = Guid.Empty;
		StartedOn = DateTime.MinValue;
		StoppedOn = DateTime.MinValue;
		SyncCancelled = false;
		SyncCompleted = false;
		SyncIssues.Clear();
		SyncSuccessful = false;
		SyncType = syncType;
	}

	private void UpdatePercent(decimal total, decimal count)
	{
		Percent = total <= 0 ? 0 : Math.Round((total / count) * 100, 2);
	}

	#endregion
}