#region References

using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Logging;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Managers;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Timer = Cornerstone.Profiling.Timer;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents a sync manager for syncing two clients.
/// </summary>
public class SyncManager : Manager
{
	#region Fields

	private readonly SpeedyQueue<Guid> _syncQueue;
	private readonly ConcurrentDictionary<string, SyncSettings> _syncSettingsForSyncType;
	private readonly ConcurrentDictionary<string, SyncTimer> _syncTimers;
	private readonly IDateTimeProvider _timeProvider;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a sync manager for syncing two clients.
	/// </summary>
	/// <param name="clientSyncClientProvider"> The client provider to get a sync client. </param>
	/// <param name="serverSyncClientProvider"> The server provider to get a sync client. </param>
	/// <param name="runtimeInformation"> The runtime information for the client. </param>
	/// <param name="timeProvider"> An optional time provider. Defaults to DateTimeProvider.RealTime if not provided. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	/// <param name="supportedSyncTypes"> The types this sync manager supports. </param>
	public SyncManager(
		ISyncClientProvider clientSyncClientProvider,
		IServerSyncClientProvider serverSyncClientProvider,
		IRuntimeInformation runtimeInformation,
		IDateTimeProvider timeProvider,
		IDispatcher dispatcher,
		params string[] supportedSyncTypes)
		: base(dispatcher)
	{
		_timeProvider = timeProvider;
		_syncSettingsForSyncType = new ConcurrentDictionary<string, SyncSettings>();
		_syncTimers = new ConcurrentDictionary<string, SyncTimer>();
		_syncQueue = new SpeedyQueue<Guid>();

		ClientSyncClientProvider = clientSyncClientProvider;
		IsEnabled = true;
		RuntimeInformation = runtimeInformation;
		SyncClientProfilerForClient = new Profiler("Client Profiler");
		SyncClientProfilerForServer = new Profiler("Server Profiler");
		ServerSyncClientProvider = serverSyncClientProvider;
		SupportedSyncTypes = supportedSyncTypes;
		SyncSession = new SyncSession(this, Guid.Empty, null, timeProvider, dispatcher);
		SyncWaitTimeout = TimeSpan.FromMilliseconds(60000);

		foreach (var type in SupportedSyncTypes)
		{
			GetOrAddSyncSettings(type);
			GetOrAddSyncTimer(type);
		}

		StartSyncCommand = new RelayCommand(x => ProcessAsync(x.ToString()));
	}

	#endregion

	#region Properties

	/// <summary>
	/// The client provider to get a sync client.
	/// </summary>
	public ISyncClientProvider ClientSyncClientProvider { get; }

	/// <summary>
	/// Gets a value indicating the sync manager is enabled.
	/// </summary>
	public bool IsEnabled { get; set; }

	/// <summary>
	/// The runtime information provider.
	/// </summary>
	public IRuntimeInformation RuntimeInformation { get; }

	/// <summary>
	/// The server provider to get a sync client.
	/// </summary>
	public IServerSyncClientProvider ServerSyncClientProvider { get; }

	/// <summary>
	/// Start a sync based on the command parameter.
	/// </summary>
	public ICommand StartSyncCommand { get; }

	/// <summary>
	/// The profiler for the client.
	/// </summary>
	public Profiler SyncClientProfilerForClient { get; }

	/// <summary>
	/// The profiler for the server.
	/// </summary>
	public Profiler SyncClientProfilerForServer { get; }

	/// <summary>
	/// The current sync session.
	/// </summary>
	public SyncSession SyncSession { get; }

	/// <summary>
	/// The configure sync settings for the sync manager.
	/// </summary>
	/// <seealso cref="GetOrAddSyncSettings" />
	public ReadOnlyDictionary<string, SyncSettings> SyncSettingsForSyncType => new(_syncSettingsForSyncType);

	/// <summary>
	/// The configure sync timers for the sync manager.
	/// </summary>
	public ReadOnlyDictionary<string, SyncTimer> SyncTimers => new(_syncTimers);

	/// <summary>
	/// The timeout to be used when synchronously syncing.
	/// </summary>
	public TimeSpan SyncWaitTimeout { get; set; }

	/// <summary>
	/// The sync types that are supported by this manager.
	/// </summary>
	protected string[] SupportedSyncTypes { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Cancels the current running sync.
	/// </summary>
	/// <remarks>
	/// See <seealso cref="WaitForSyncToComplete" /> if you want to wait for the sync to complete.
	/// </remarks>
	public void CancelSync()
	{
		var syncSession = SyncSession;
		if (syncSession == null)
		{
			return;
		}

		OnLogEvent(syncSession.SessionId, $"Cancelling running Sync {syncSession.SyncType}...", EventLevel.Verbose);

		syncSession.Cancel();
	}

	/// <summary>
	/// Gets the sync client to be used in the client sid of the sync.
	/// </summary>
	/// <returns> The sync client for the client. </returns>
	public virtual SyncClient GetSyncClientForClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
	{
		var client = ClientSyncClientProvider.GetSyncClient(syncStatistics, syncClientProfiler);
		return client;
	}

	/// <summary>
	/// Gets the sync client to be used in the server side of the sync.
	/// </summary>
	/// <returns> The sync client for the server. </returns>
	public virtual ServerSyncClient GetSyncClientForServer(SyncStatistics syncStatistics, Profiler syncClientProfiler)
	{
		var client = ServerSyncClientProvider.GetServerSyncClient(syncStatistics, syncClientProfiler);
		return client;
	}

	/// <summary>
	/// Gets the sync settings by the provide sync type.
	/// </summary>
	/// <param name="syncType"> The sync type to get settings for. </param>
	/// <returns> The sync settings for the type </returns>
	public virtual SyncSettings GetSyncSettings(string syncType)
	{
		// ReSharper disable once CanSimplifyDictionaryTryGetValueWithGetValueOrDefault
		return SyncSettingsForSyncType.TryGetValue(syncType, out var settings) ? settings : null;
	}

	/// <summary>
	/// Processes a sync request.
	/// </summary>
	/// <param name="syncType"> The type of the sync to process. </param>
	/// <param name="updateSettings"> Optional action to possibly update settings when the sync starts. </param>
	/// <param name="waitFor"> Optional timeout to wait for the active sync to complete. </param>
	/// <param name="postAction">
	/// An optional action to run after sync is completed but before notification goes out. If the sync cannot
	/// start then the settings will be null as they were never read or set.
	/// </param>
	/// <returns> The task for the process. </returns>
	public SyncSession Process(string syncType, Action<SyncSettings> updateSettings = null,
		TimeSpan? waitFor = null, Action<SyncSession> postAction = null)
	{
		this.Dispatch(StartSyncCommand.Refresh);

		ValidateSyncType(syncType);

		var sessionId = Guid.NewGuid();

		if (!IsEnabled)
		{
			OnLogEvent(sessionId, $"Sync Manager is not enabled so Sync {syncType} not started.", EventLevel.Verbose);
			postAction?.Invoke(null);
			return new SyncSession(this, sessionId, syncType, _timeProvider, GetDispatcher());
		}

		var currentSyncSession = SyncSession;

		if (currentSyncSession.SyncRunning)
		{
			if (waitFor == null)
			{
				OnLogEvent(sessionId, $"Sync {currentSyncSession.SyncType} is already running so Sync {syncType} not started.", EventLevel.Verbose);
				postAction?.Invoke(null);
				var session = SyncSession.CouldNotStart(this, sessionId, syncType, _timeProvider);
				return session;
			}

			// See if we are going to force current sync to stop
			OnLogEvent(sessionId, $"Sync {syncType} waiting for Sync {currentSyncSession.SyncType} to complete...", EventLevel.Verbose);
		}

		_syncQueue.Enqueue(sessionId);

		// Start the sync before we start the task
		var syncSession = StartSyncSession(sessionId, syncType, waitFor);
		if (syncSession == null)
		{
			OnLogEvent(sessionId, "Timed out waiting for current sync to complete.", EventLevel.Verbose);
			var session = SyncSession.CouldNotStart(this, sessionId, syncType, _timeProvider);
			postAction?.Invoke(session);
			return null;
		}

		OnLogEvent(sessionId, $"Starting to {syncSession.SyncType} sync...", EventLevel.Verbose);

		var response = syncSession.ProcessSyncSession(updateSettings, OnSyncRunning, OnSyncCompleted);

		if (_syncQueue.TryDequeue(out var dequeuedId)
			&& (dequeuedId != sessionId))
		{
			OnLogEvent(sessionId, "The dequeued ID does not match expected session!", EventLevel.Critical);
		}

		postAction?.Invoke(response);

		return response;
	}

	/// <summary>
	/// Processes a sync request.
	/// </summary>
	/// <param name="syncType"> The type of the sync to process. </param>
	/// <param name="updateSettings"> Optional action to possibly update settings when the sync starts. </param>
	/// <param name="waitFor"> Optional timeout to wait for the active sync to complete. </param>
	/// <param name="postAction">
	/// An optional action to run after sync is completed but before notification goes out. If the sync cannot
	/// start then the settings will be null as they were never read or set.
	/// </param>
	/// <returns> The task for the process. </returns>
	public Task<SyncSession> ProcessAsync(string syncType, Action<SyncSettings> updateSettings = null,
		TimeSpan? waitFor = null, Action<SyncSession> postAction = null)
	{
		return Task.Run(() => Process(syncType, updateSettings, waitFor, postAction));
	}

	/// <summary>
	/// Cancel the current running sync and wait for it to stop.
	/// </summary>
	public void StopSync(TimeSpan? timeout = null)
	{
		var syncSession = SyncSession;
		if (syncSession == null)
		{
			return;
		}

		OnLogEvent($"Stopping running Sync {syncSession.SyncType}...", EventLevel.Verbose);

		syncSession.Cancel();
		syncSession.WaitForSyncToComplete(timeout ?? SyncWaitTimeout);
	}

	/// <summary>
	/// Update the sync dates on all sync settings.
	/// </summary>
	/// <param name="lastSyncedOnClient"> The last time when synced on the client. </param>
	/// <param name="lastSyncedOnServer"> The last time when synced on the server. </param>
	public virtual void UpdateAllLastSyncedOn(DateTime lastSyncedOnClient, DateTime lastSyncedOnServer)
	{
		foreach (var collectionSettings in _syncSettingsForSyncType.Values)
		{
			collectionSettings.LastSyncedOnClient = lastSyncedOnClient;
			collectionSettings.LastSyncedOnServer = lastSyncedOnServer;
		}
	}

	/// <summary>
	/// Update the LastSyncedOn Server/Client for a sync type.
	/// </summary>
	/// <param name="syncType"> The type name to get settings for. </param>
	/// <param name="lastSyncedOnClient"> The date and time of the client of the last sync. </param>
	/// <param name="lastSyncedOnServer"> The date and time of the server of the last sync. </param>
	public virtual void UpdateLastSyncedOn(string syncType, DateTime lastSyncedOnClient, DateTime lastSyncedOnServer)
	{
		var settings = GetSyncSettings(syncType);
		if (settings == null)
		{
			return;
		}

		settings.LastSyncedOnClient = lastSyncedOnClient;
		settings.LastSyncedOnServer = lastSyncedOnServer;
	}

	/// <summary>
	/// Wait for the sync to complete.
	/// </summary>
	/// <param name="timeout"> An optional max amount of time to wait. SyncWaitTimeout will be used it no timeout provided. </param>
	/// <returns> True if the sync completed otherwise false if timed out waiting. </returns>
	public bool WaitForSyncToComplete(TimeSpan? timeout = null)
	{
		return SyncSession.WaitForSyncToComplete(timeout ?? SyncWaitTimeout);
	}

	/// <summary>
	/// Wait for the sync to start.
	/// </summary>
	/// <param name="timeout"> An optional max amount of time to wait. SyncWaitTimeout will be used it no timeout provided. </param>
	/// <returns> True if the sync was started otherwise false if timed out waiting. </returns>
	public bool WaitForSyncToStart(TimeSpan? timeout = null)
	{
		return SyncSession.WaitForSyncToStart(timeout ?? SyncWaitTimeout);
	}

	/// <summary>
	/// Gets the default sync settings for a sync manager.
	/// </summary>
	/// <param name="syncType"> The type of sync these settings are for. </param>
	/// <param name="update"> Optional update action to change provided defaults. </param>
	/// <returns> The default set of settings. </returns>
	/// <remarks>
	/// This should only be use in the sync manager constructor.
	/// </remarks>
	protected SyncSettings GetOrAddSyncSettings(string syncType, Action<SyncSettings> update = null)
	{
		return _syncSettingsForSyncType.GetOrAdd(syncType, key =>
		{
			var settings = new SyncSettings
			{
				LastSyncedOnClient = DateTime.MinValue,
				LastSyncedOnServer = DateTime.MinValue,
				// note: everything below is a request, the sync clients (web sync controller)
				// has the settings to override. Ex: you may request 600 items then the sync
				// client may reduce it to only 100 items.
				PermanentDeletions = false,
				ItemsPerSyncRequest = 600,
				IncludeIssueDetails = false
			};

			// optional update to modify sync settings
			update?.Invoke(settings);

			return settings;
		});
	}

	/// <summary>
	/// Gets or adds an average sync timer for a sync type. This will track the average time spent syncing for the provided type.
	/// </summary>
	/// <param name="syncType"> The type of sync these settings are for. </param>
	/// <param name="limit"> Optional limit of syncs to average. </param>
	/// <returns> The timer for tracking the time spent syncing. </returns>
	/// <remarks>
	/// This should only be use in the sync manager constructor.
	/// </remarks>
	protected SyncTimer GetOrAddSyncTimer(string syncType, int limit = 10)
	{
		return _syncTimers.GetOrAdd(syncType, new SyncTimer(limit, null, GetDispatcher()));
	}

	/// <summary>
	/// Write a message to the log.
	/// </summary>
	/// <param name="message"> The message to be written. </param>
	/// <param name="level"> The level of this message. </param>
	protected virtual void OnLogEvent(string message, EventLevel level)
	{
		Logger.Instance.Write(SyncSession.SessionId, message, level, _timeProvider.UtcNow);
	}

	/// <summary>
	/// Write a message to the log.
	/// </summary>
	/// <param name="sessionId"> The ID for the session. </param>
	/// <param name="message"> The message to be written. </param>
	/// <param name="level"> The level of this message. </param>
	protected virtual void OnLogEvent(Guid sessionId, string message, EventLevel level)
	{
		Logger.Instance.Write(sessionId, message, level, _timeProvider.UtcNow);
	}

	/// <summary>
	/// Indicate the sync is complete.
	/// </summary>
	/// <param name="session"> The results of the completed sync. </param>
	protected virtual void OnSyncCompleted(SyncSession session)
	{
		// If no issues we'll store last synced on
		if (session.SyncSuccessful)
		{
			this.DispatchAsync(() =>
			{
				UpdateLastSyncedOn(session.SyncType, session.Settings.LastSyncedOnClient, session.Settings.LastSyncedOnServer);
				SyncClientProfilerForClient.UpdateWith(session.SyncClientProfilerForClient);
				SyncClientProfilerForServer.UpdateWith(session.SyncClientProfilerForServer);
			});
		}

		this.Dispatch(StartSyncCommand.Refresh);

		SyncCompleted?.Invoke(this, session);
	}

	/// <summary>
	/// Indicate the sync is running
	/// </summary>
	protected virtual void OnSyncRunning(SyncSession session)
	{
	}

	protected bool ReadyToSync(string syncType, TimeSpan interval)
	{
		if (!SyncSettingsForSyncType.TryGetValue(syncType, out var settings))
		{
			// Never synced
			return true;
		}

		return ((_timeProvider.UtcNow - settings.LastSyncedOnClient) >= interval)
			&& ((settings.LastSyncAttemptedOn <= DateTime.MinValue)
				|| ((_timeProvider.UtcNow - settings.LastSyncAttemptedOn) >= interval)
			);
	}

	/// <summary>
	/// Validates the provided sync type is supported by this sync manager.
	/// </summary>
	/// <param name="syncType"> The type of the sync to validate. </param>
	/// <exception cref="ConstraintException"> The sync type is not supported by this sync manager. </exception>
	protected void ValidateSyncType(string syncType)
	{
		if (!SupportedSyncTypes.Contains(syncType))
		{
			throw new ConstraintException("The sync type is not supported by this sync manager.");
		}
	}

	/// <summary>
	/// Wait for a sync to complete.
	/// </summary>
	/// <param name="task"> The task to wait for. </param>
	/// <param name="timeout"> The optional timeout. </param>
	/// <returns> The sync session. </returns>
	protected SyncSession WaitForSync(Task<SyncSession> task, TimeSpan? timeout = null)
	{
		if (task.IsCompleted)
		{
			return task.Result;
		}

		// bug: this is broke, SyncSession is not guaranteed to be your session!

		// todo: should the second wait timeout be less?
		SyncSession.WaitForSyncToStart(timeout ?? SyncWaitTimeout);
		SyncSession.WaitForSyncToComplete(timeout ?? SyncWaitTimeout);
		return task.Result;
	}

	private SyncSession StartSyncSession(Guid sessionId, string syncType, TimeSpan? waitFor)
	{
		var watch = new Timer(_timeProvider);
		waitFor ??= TimeSpan.Zero;

		// Lock to see if we can start a sync
		while (!_syncQueue.TryPeek(out var peek)
				|| (peek != sessionId))
		{
			Thread.Sleep(10);

			if (watch.Elapsed > waitFor)
			{
				// The sync is still running so return false
				OnLogEvent(sessionId, $"Failed to Sync {syncType} because current Sync {syncType} never completed while waiting.", EventLevel.Verbose);
				return null;
			}
		}

		// No sync running, start a new sync by starting the watch
		SyncSession.Start(sessionId, syncType, GetSyncSettings(syncType));

		// See if we have a timer for this sync type
		if (SyncTimers.TryGetValue(SyncSession.SyncType, out var syncTimer))
		{
			syncTimer.Start(SyncSession.StartedOn);
		}

		OnLogEvent(sessionId, $"Sync {SyncSession.SyncType} started", EventLevel.Verbose);
		return SyncSession;
	}

	#endregion

	#region Events

	/// <summary>
	/// Indicates the sync is completed.
	/// </summary>
	public event EventHandler<SyncSession> SyncCompleted;

	#endregion
}