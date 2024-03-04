#region References

using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Extensions;
using Cornerstone.Logging;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Managers;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents a sync manager for syncing two clients.
/// </summary>
public abstract class SyncManager : Manager
{
	#region Fields

	private readonly object _processLock;
	private readonly string[] _supportedSyncTypes;
	private readonly ConcurrentDictionary<string, SyncOptions> _syncOptions;
	private readonly ConcurrentDictionary<string, SyncTimer> _syncTimers;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a sync manager for syncing two clients.
	/// </summary>
	/// <param name="databaseProvider"> The database provider for the client. </param>
	/// <param name="runtimeInformation"> The runtime information for the client. </param>
	/// <param name="serverSyncClientProvider"> The server provider to get a sync client. </param>
	/// <param name="logListener"> The listener to capture logs. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	/// <param name="supportedSyncTypes"> The types this sync manager supports. </param>
	protected SyncManager(ISyncableDatabaseProvider databaseProvider, IRuntimeInformation runtimeInformation,
		SyncClientProvider serverSyncClientProvider, LogListener logListener, IDispatcher dispatcher,
		params string[] supportedSyncTypes)
		: base(dispatcher)
	{
		_processLock = new object();
		_supportedSyncTypes = supportedSyncTypes;
		_syncOptions = new ConcurrentDictionary<string, SyncOptions>();
		_syncTimers = new ConcurrentDictionary<string, SyncTimer>();

		DatabaseProvider = databaseProvider;
		LogListener = logListener;
		IsEnabled = true;
		RuntimeInformation = runtimeInformation;
		ServerSyncClientProvider = serverSyncClientProvider;
		SyncSession = new SyncSession(this, default, dispatcher);
		SyncWaitTimeout = TimeSpan.FromMilliseconds(60000);
	}

	#endregion

	#region Properties

	/// <summary>
	/// The database provider for the client.
	/// </summary>
	public ISyncableDatabaseProvider DatabaseProvider { get; }

	/// <summary>
	/// Gets a value indicating the sync manager is enabled.
	/// </summary>
	public bool IsEnabled { get; set; }

	/// <summary>
	/// The log listener for the sync manager.
	/// </summary>
	public LogListener LogListener { get; }

	/// <summary>
	/// The runtime information provider.
	/// </summary>
	public IRuntimeInformation RuntimeInformation { get; }

	/// <summary>
	/// The server provider to get a sync client.
	/// </summary>
	public SyncClientProvider ServerSyncClientProvider { get; }

	/// <summary>
	/// The configure sync options for the sync manager.
	/// </summary>
	/// <seealso cref="GetOrAddSyncOptions" />
	public ReadOnlyDictionary<string, SyncOptions> SyncOptions => new(_syncOptions);

	/// <summary>
	/// The current sync session.
	/// </summary>
	public SyncSession SyncSession { get; }

	/// <summary>
	/// The configure sync timers for the sync manager.
	/// </summary>
	public ReadOnlyDictionary<string, SyncTimer> SyncTimers => new(_syncTimers);

	/// <summary>
	/// The timeout to be used when synchronously syncing.
	/// </summary>
	public TimeSpan SyncWaitTimeout { get; set; }

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

		OnLogEvent($"Cancelling running Sync {syncSession.SyncType}...", EventLevel.Verbose);

		syncSession.Cancel();
	}

	/// <summary>
	/// Gets an optional incoming converter to convert incoming sync data. The converter is applied to the local sync client.
	/// </summary>
	public abstract SyncClientIncomingConverter GetIncomingConverter();

	/// <summary>
	/// Gets an optional outgoing converter to convert incoming sync data. The converter is applied to the local sync client.
	/// </summary>
	public abstract SyncClientOutgoingConverter GetOutgoingConverter();

	/// <summary>
	/// Gets the sync client to be used in the client sid of the sync.
	/// </summary>
	/// <returns> The sync client for the client. </returns>
	public virtual ISyncClient GetSyncClientForClient()
	{
		var client = new SyncClient("Client (local)", DatabaseProvider)
		{
			IncomingConverter = GetIncomingConverter(),
			OutgoingConverter = GetOutgoingConverter(),
			Options = { EnablePrimaryKeyCache = true, IsServerClient = false }
		};
		return client;
	}

	/// <summary>
	/// Gets the sync client to be used in the server side of the sync.
	/// </summary>
	/// <returns> The sync client for the server. </returns>
	public virtual ISyncClient GetSyncClientForServer()
	{
		var client = ServerSyncClientProvider.GetClient();
		if (client?.Options?.IsServerClient == false)
		{
			client.Options.IsServerClient = true;
		}
		return client;
	}

	/// <summary>
	/// Gets the sync options by the provide sync type.
	/// </summary>
	/// <param name="syncType"> The sync type to get options for. </param>
	/// <returns> The sync options for the type </returns>
	public virtual SyncOptions GetSyncOptions(string syncType)
	{
		// ReSharper disable once CanSimplifyDictionaryTryGetValueWithGetValueOrDefault
		return SyncOptions.TryGetValue(syncType, out var option) ? option : null;
	}

	/// <summary>
	/// Processes a sync request.
	/// </summary>
	/// <param name="syncType"> The type of the sync to process. </param>
	/// <param name="updateOptions"> Optional action to possibly update options when the sync starts. </param>
	/// <param name="waitFor"> Optional timeout to wait for the active sync to complete. </param>
	/// <param name="postAction">
	/// An optional action to run after sync is completed but before notification goes out. If the sync cannot
	/// start then the options will be null as they were never read or set.
	/// </param>
	/// <returns> The task for the process. </returns>
	public Task<SyncSession> ProcessAsync(string syncType, Action<SyncOptions> updateOptions = null,
		TimeSpan? waitFor = null, Action<SyncSession> postAction = null)
	{
		ValidateSyncType(syncType);

		if (!IsEnabled)
		{
			OnLogEvent($"Sync Manager is not enabled so Sync {syncType} not started.", EventLevel.Verbose);
			postAction?.Invoke(null);
			return Task.FromResult(new SyncSession());
		}

		var currentSyncSession = SyncSession;

		if (currentSyncSession.SyncRunning)
		{
			if (waitFor == null)
			{
				OnLogEvent($"Sync {currentSyncSession.SyncType} is already running so Sync {syncType} not started.", EventLevel.Verbose);
				postAction?.Invoke(null);
				return Task.FromResult(new SyncSession());
			}

			// See if we are going to force current sync to stop
			OnLogEvent($"Waiting for Sync {currentSyncSession.SyncType} to complete...", EventLevel.Verbose);
		}

		// Start the sync before we start the task
		var syncSession = StartSyncSession(syncType, waitFor, postAction);
		if (syncSession == null)
		{
			OnLogEvent("Timed out waiting for current sync to complete.", EventLevel.Verbose);
			return Task.FromResult<SyncSession>(null);
		}

		// Start the sync in a background thread.
		return Task.Run(() =>
		{
			var response = syncSession.RunSync(updateOptions, OnSyncRunning, OnSyncCompleted);
			postAction?.Invoke(response);
			return response;
		});
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

		WaitForSyncToComplete(timeout);
	}

	/// <summary>
	/// Update the sync dates on all sync options.
	/// </summary>
	/// <param name="lastSyncedOnClient"> The last time when synced on the client. </param>
	/// <param name="lastSyncedOnServer"> The last time when synced on the server. </param>
	public virtual void UpdateAllLastSyncedOn(DateTime lastSyncedOnClient, DateTime lastSyncedOnServer)
	{
		foreach (var collectionOptions in _syncOptions.Values)
		{
			collectionOptions.LastSyncedOnClient = lastSyncedOnClient;
			collectionOptions.LastSyncedOnServer = lastSyncedOnServer;
		}
	}

	/// <summary>
	/// Update the LastSyncedOn Server/Client for a sync type.
	/// </summary>
	/// <param name="type"> The type to get options for. </param>
	/// <param name="lastSyncedOnClient"> The date and time of the client of the last sync. </param>
	/// <param name="lastSyncedOnServer"> The date and time of the server of the last sync. </param>
	public virtual void UpdateLastSyncedOn(string type, DateTime lastSyncedOnClient, DateTime lastSyncedOnServer)
	{
		var options = GetSyncOptions(type);
		if (options == null)
		{
			return;
		}

		options.LastSyncedOnClient = lastSyncedOnClient;
		options.LastSyncedOnServer = lastSyncedOnServer;
	}

	/// <summary>
	/// Wait for the sync to complete.
	/// </summary>
	/// <param name="timeout"> An optional max amount of time to wait. SyncWaitTimeout will be used it no timeout provided. </param>
	/// <returns> True if the sync completed otherwise false if timed out waiting. </returns>
	public bool WaitForSyncToComplete(TimeSpan? timeout = null)
	{
		if (SyncSession.SyncCompleted)
		{
			return true;
		}

		var watch = Stopwatch.StartNew();
		timeout ??= SyncWaitTimeout;

		while (!SyncSession.SyncCompleted)
		{
			if (watch.Elapsed >= timeout)
			{
				return false;
			}

			Thread.Sleep(10);
		}

		return true;
	}

	/// <summary>
	/// Wait for the sync to start.
	/// </summary>
	/// <param name="timeout"> An optional max amount of time to wait. SyncWaitTimeout will be used it no timeout provided. </param>
	/// <returns> True if the sync was started otherwise false if timed out waiting. </returns>
	public bool WaitForSyncToStart(TimeSpan? timeout = null)
	{
		if (SyncSession.SyncStarted)
		{
			return true;
		}

		var watch = Stopwatch.StartNew();
		timeout ??= SyncWaitTimeout;

		while (SyncSession.SyncStarted != true)
		{
			if (watch.Elapsed >= timeout)
			{
				return false;
			}

			Thread.Sleep(10);
		}

		return true;
	}

	/// <summary>
	/// Wait for the sync to start running.
	/// </summary>
	/// <param name="timeout"> An optional max amount of time to wait. SyncWaitTimeout will be used it no timeout provided. </param>
	/// <returns> True if the sync was started to process otherwise false if timed out waiting. </returns>
	public bool WaitForSyncToStartRunning(TimeSpan? timeout = null)
	{
		if (SyncSession == null)
		{
			return true;
		}

		var watch = Stopwatch.StartNew();
		timeout ??= SyncWaitTimeout;

		while (!SyncSession.SyncRunning)
		{
			if (watch.Elapsed >= timeout)
			{
				return false;
			}

			Thread.Sleep(10);
		}

		return true;
	}

	/// <summary>
	/// Gets the default sync options for a sync manager.
	/// </summary>
	/// <param name="syncType"> The type of sync these options are for. </param>
	/// <param name="update"> Optional update action to change provided defaults. </param>
	/// <returns> The default set of options. </returns>
	/// <remarks>
	/// This should only be use in the sync manager constructor.
	/// </remarks>
	protected SyncOptions GetOrAddSyncOptions(string syncType, Action<SyncOptions> update = null)
	{
		return _syncOptions.GetOrAdd(syncType, key =>
		{
			if (SyncOptions.TryGetValue(key, out var syncOptions))
			{
				return syncOptions;
			}

			var options = new SyncOptions
			{
				LastSyncedOnClient = DateTime.MinValue,
				LastSyncedOnServer = DateTime.MinValue,
				// note: everything below is a request, the sync clients (web sync controller)
				// has the options to override. Ex: you may request 600 items then the sync
				// client may reduce it to only 100 items.
				PermanentDeletions = false,
				ItemsPerSyncRequest = 600,
				IncludeIssueDetails = false
			};

			options.Values.AddOrUpdate(Sync.SyncOptions.SyncKey, ((int) (object) syncType).ToString());
			//options.AddOrUpdateSyncClientDetails(RuntimeInformation);

			// optional update to modify sync options
			update?.Invoke(options);

			_syncOptions.GetOrAdd(syncType, options);

			return options;
		});
	}

	/// <summary>
	/// Gets or adds an average sync timer for a sync type. This will track the average time spent syncing for the provided type.
	/// </summary>
	/// <param name="syncType"> The type of sync these options are for. </param>
	/// <param name="limit"> Optional limit of syncs to average. </param>
	/// <returns> The timer for tracking the time spent syncing. </returns>
	/// <remarks>
	/// This should only be use in the sync manager constructor.
	/// </remarks>
	protected SyncTimer GetOrAddSyncTimer(string syncType, int limit = 10)
	{
		return _syncTimers.GetOrAdd(syncType, new SyncTimer(limit, GetDispatcher()));
	}

	/// <summary>
	/// Write a message to the log.
	/// </summary>
	/// <param name="message"> The message to be written. </param>
	/// <param name="level"> The level of this message. </param>
	protected virtual void OnLogEvent(string message, EventLevel level)
	{
		SyncSession.OnLogEvent(message, level);
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
			UpdateLastSyncedOn(session.SyncType, session.Options.LastSyncedOnClient, session.Options.LastSyncedOnServer);
		}

		SyncCompleted?.Invoke(this, session);
	}

	/// <summary>
	/// Indicate the sync is running
	/// </summary>
	protected virtual void OnSyncRunning(SyncSession session)
	{
	}

	/// <summary>
	/// Validates the provided sync type is supported by this sync manager.
	/// </summary>
	/// <param name="syncType"> The type of the sync to validate. </param>
	/// <exception cref="ConstraintException"> The sync type is not supported by this sync manager. </exception>
	protected void ValidateSyncType(string syncType)
	{
		if (!_supportedSyncTypes.Contains(syncType))
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
		// todo: should the second wait timeout be less?
		WaitForSyncToStart(timeout ?? SyncWaitTimeout);
		WaitForSyncToComplete(timeout ?? SyncWaitTimeout);
		return task.Result;
	}

	private SyncSession StartSyncSession(string syncType, TimeSpan? waitFor, Action<SyncSession> postAction)
	{
		// Lock the sync before we start, wait until 
		var syncSession = WaitForSyncAvailableThenStart(syncType, waitFor ?? TimeSpan.Zero);
		if (syncSession == null)
		{
			OnLogEvent($"Failed to Sync {syncType} because current Sync {syncType} never completed while waiting.", EventLevel.Verbose);
			postAction?.Invoke(null);
			return null;
		}

		if (LogListener != null)
		{
			// Update the listener with the current session.
			LogListener.SessionId = syncSession.SessionId;
		}

		return syncSession;
	}

	private SyncSession WaitForSyncAvailableThenStart(string syncType, TimeSpan timeout)
	{
		var watch = Stopwatch.StartNew();

		do
		{
			// Lock to see if we can start a sync
			if (!Monitor.TryEnter(_processLock))
			{
				Thread.Sleep(10);
				continue;
			}

			try
			{
				// Check to see if a sync is already running
				if (!SyncSession.SyncRunning)
				{
					// No sync running, start a new sync by starting the watch
					SyncSession.Start(syncType, GetSyncOptions(syncType));

					// See if we have a timer for this sync type
					if (SyncTimers.TryGetValue(SyncSession.SyncType, out var syncTimer))
					{
						syncTimer.Start(SyncSession.StartedOn);
					}

					OnLogEvent($"Sync {SyncSession.SyncType} started", EventLevel.Verbose);

					return SyncSession;
				}
			}
			finally
			{
				// Free up the lock
				Monitor.Exit(_processLock);
			}

			// Pause to allow locks to be handed out
			Thread.Sleep(10);

			// Wait for an existing sync to completed until the provided timeout
		} while (watch.Elapsed < timeout);

		// The sync is still running so return false
		return null;
	}

	#endregion

	#region Events

	/// <summary>
	/// Indicates the sync is completed.
	/// </summary>
	public event EventHandler<SyncSession> SyncCompleted;

	#endregion
}