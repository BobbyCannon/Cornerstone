#region References

using System;
using System.Data;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sample.Models;
using Cornerstone.Sample.Sync;
using Cornerstone.Sync;
using Cornerstone.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
[DoNotParallelize]
public class SyncManagerConcurrencyTests : SyncScenarioTest
{
	#region Methods

	[TestMethod]
	public void CancelSyncDuringConfigure()
	{
		WithEachPair((_, _, manager) =>
		{
			using var entered = new ManualResetEventSlim(false);
			using var release = new ManualResetEventSlim(false);
			var first = manager.SyncAsync(
				SampleSyncClient.SyncAll,
				_ =>
				{
					entered.Set();
					release.Wait(TimeSpan.FromSeconds(15));
				},
				TimeSpan.FromSeconds(30)
			);
			IsTrue(entered.Wait(TimeSpan.FromSeconds(15)));
			manager.CancelSync();
			release.Set();
			var result = first.GetAwaiter().GetResult();
			IsTrue(result.SyncCompleted);
			IsTrue(
				result.SyncCancelled
				|| manager.SyncSession.SyncCancelled
				|| (manager.SyncTimers[SampleSyncClient.SyncAll].CancelledSyncs >= 1)
			);
		});
	}

	[TestMethod]
	public void CancelledSyncIncrementsCancelledTimer()
	{
		WithEachPair((_, _, manager) =>
		{
			manager.CancelSync();
			var session = manager.Sync(SampleSyncClient.SyncAll, waitFor: TimeSpan.FromSeconds(5));
			IsTrue(session.SyncSuccessful || session.SyncCompleted);
		});
	}

	[TestMethod]
	public void ConcurrentSyncWaitTimeoutCannotStart()
	{
		WithEachPair((_, _, manager) =>
		{
			using var entered = new ManualResetEventSlim(false);
			using var release = new ManualResetEventSlim(false);
			var first = manager.SyncAsync(
				SampleSyncClient.SyncAll,
				_ =>
				{
					entered.Set();
					release.Wait(TimeSpan.FromSeconds(15));
				},
				TimeSpan.FromSeconds(30)
			);
			IsTrue(entered.Wait(TimeSpan.FromSeconds(15)));
			var secondTask = manager.SyncAsync(SampleSyncClient.SyncAll, waitFor: TimeSpan.FromSeconds(2));
			while (!secondTask.IsCompleted)
			{
				IncrementTime(seconds: 1);
				Thread.Sleep(5);
			}
			var second = secondTask.GetAwaiter().GetResult();
			IsTrue(second.State.HasFlag(SyncSessionState.CouldNotStart));
			release.Set();
			first.GetAwaiter().GetResult();
		});
	}

	[TestMethod]
	public void ConcurrentSyncWithWaitQueuesAndSucceeds()
	{
		WithEachPair((_, _, manager) =>
		{
			using var entered = new ManualResetEventSlim(false);
			using var release = new ManualResetEventSlim(false);
			var first = manager.SyncAsync(
				SampleSyncClient.SyncAll,
				_ =>
				{
					entered.Set();
					release.Wait(TimeSpan.FromSeconds(15));
				},
				TimeSpan.FromSeconds(30)
			);
			IsTrue(entered.Wait(TimeSpan.FromSeconds(15)));
			var secondTask = manager.SyncAsync(SampleSyncClient.SyncAll, waitFor: TimeSpan.FromSeconds(30));
			release.Set();
			var firstResult = first.GetAwaiter().GetResult();
			var secondResult = secondTask.GetAwaiter().GetResult();
			IsTrue(firstResult.SyncCompleted);
			IsTrue(secondResult.SyncSuccessful || secondResult.SyncCompleted);
		});
	}

	[TestMethod]
	public void ConcurrentSyncWithoutWaitCannotStart()
	{
		WithEachPair((_, _, manager) =>
		{
			using var entered = new ManualResetEventSlim(false);
			using var release = new ManualResetEventSlim(false);
			var first = manager.SyncAsync(
				SampleSyncClient.SyncAll,
				_ =>
				{
					entered.Set();
					release.Wait(TimeSpan.FromSeconds(15));
				},
				TimeSpan.FromSeconds(30)
			);
			IsTrue(entered.Wait(TimeSpan.FromSeconds(15)));
			var second = manager.SyncAsync(SampleSyncClient.SyncAll).GetAwaiter().GetResult();
			IsTrue(second.State.HasFlag(SyncSessionState.CouldNotStart));
			IsFalse((second.SyncIssues.Count > 0) && (second.SyncIssues[0].IssueType == SyncIssueType.SyncManagerDisabled));
			release.Set();
			var firstResult = first.GetAwaiter().GetResult();
			IsTrue(firstResult.SyncCompleted, () => $"state={firstResult.State} issues={firstResult.SyncIssues.Count}");
		});
	}

	[TestMethod]
	public void DisabledManagerDoesNotStartAndCallsPostAction()
	{
		WithEachPair((_, _, manager) =>
		{
			manager.IsEnabled = false;
			var postCalled = false;
			SyncSession postSession = null;
			var session = manager.SyncAsync(
				SampleSyncClient.SyncAll,
				postAction: x =>
				{
					postCalled = true;
					postSession = x;
				}
			).GetAwaiter().GetResult();

			IsTrue(postCalled);
			IsNotNull(postSession);
			IsTrue(session.State.HasFlag(SyncSessionState.CouldNotStart));
			AreEqual(1, session.SyncIssues.Count);
			AreEqual(SyncIssueType.SyncManagerDisabled, session.SyncIssues[0].IssueType);
		});
	}

	[TestMethod]
	public void FailedSyncIncrementsFailedTimer()
	{
		WithEachPair((client, server, _) =>
		{
			AddAddress(client, "Home");
			var manager = new SampleSyncManager(
				new SampleSyncClientProvider("Client", ScenarioClientProvider, this),
				new RejectIncomingServerProvider(ScenarioServerProvider, this),
				new SyncSession(this),
				RuntimeInformation,
				this,
				Dispatcher
			);
			manager.GetSyncSettings(SampleSyncClient.SyncAll).IncludeIssueDetails = true;
			var session = manager.Sync(SampleSyncClient.SyncAll, waitFor: TimeSpan.FromSeconds(30));
			DetachScenarioDatabases();
			IsTrue(session.SyncCompleted);
			IsTrue(session.SyncIssues.Count > 0);
			AreEqual(1, manager.SyncTimers[SampleSyncClient.SyncAll].FailedSyncs);
		});
	}

	[TestMethod]
	public void GetSyncClientsFromProviders()
	{
		WithEachPair((_, _, manager) =>
		{
			var client = manager.GetSyncClientForClient(new SyncStatistics(), new Profiler("C"));
			var server = manager.GetSyncClientForServer(new SyncStatistics(), new Profiler("S"));
			IsNotNull(client);
			IsNotNull(server);
			AreEqual("Client", client.Name);
			AreEqual("Server", server.Name);
		});
	}

	[TestMethod]
	public void GetSyncSettingsUnknownTypeIsNull()
	{
		WithEachPair((_, _, manager) =>
		{
			IsNull(manager.GetSyncSettings("Missing"));
			manager.UpdateLastSyncedOn("Missing", UtcNow, UtcNow);
		});
	}

	[TestMethod]
	public void NewSettingsDefaultPageSizeIsSixHundred()
	{
		WithEachPair((_, _, manager) => { AreEqual(600, manager.GetSyncSettings(SampleSyncClient.SyncAll).ItemsPerSyncRequest); });
	}

	[TestMethod]
	public void NullSyncClientIsClientException()
	{
		WithEachPair((_, _, _) =>
		{
			var manager = new SampleSyncManager(
				new NullSyncClientProvider(ScenarioClientProvider),
				new SampleSyncClientProvider("Server", ScenarioServerProvider, this),
				new SyncSession(this),
				RuntimeInformation,
				this,
				Dispatcher
			);
			var session = manager.Sync(SampleSyncClient.SyncAll, waitFor: TimeSpan.FromSeconds(30));
			IsTrue(session.SyncCompleted);
			IsTrue(session.SyncIssues.Count > 0);
			AreEqual(SyncIssueType.ClientException, session.SyncIssues[0].IssueType);
		});
	}

	[TestMethod]
	public void UnsupportedSyncClientIsClientException()
	{
		WithEachPair((_, _, _) =>
		{
			var manager = new SampleSyncManager(
				new SampleSyncClientProvider("Client", ScenarioClientProvider, this),
				new RejectingSyncClientProvider(ScenarioServerProvider, this),
				new SyncSession(this),
				RuntimeInformation,
				this,
				Dispatcher
			);
			var session = manager.Sync(SampleSyncClient.SyncAll, waitFor: TimeSpan.FromSeconds(30));
			IsTrue(session.SyncCompleted);
			IsFalse(session.SyncSuccessful);
			AreEqual(1, session.SyncIssues.Count);
			AreEqual(SyncIssueType.ClientException, session.SyncIssues[0].IssueType);
			AreEqual("The sync client is not supported.", session.SyncIssues[0].Message);
			AreEqual(1, manager.SyncTimers[SampleSyncClient.SyncAll].FailedSyncs);
			AreEqual(DateTime.MinValue, manager.GetSyncSettings(SampleSyncClient.SyncAll).LastSyncedOnClient);
		});
	}

	[TestMethod]
	public void OtherWebExceptionIsClientException()
	{
		WithEachPair((_, _, _) =>
		{
			var manager = new SampleSyncManager(
				new SampleSyncClientProvider("Client", ScenarioClientProvider, this),
				new ThrowingWebClientProvider(ScenarioServerProvider, this, HttpStatusCode.BadGateway),
				new SyncSession(this),
				RuntimeInformation,
				this,
				Dispatcher
			);
			var session = manager.Sync(SampleSyncClient.SyncAll, waitFor: TimeSpan.FromSeconds(30));
			IsTrue(session.SyncCompleted);
			AreEqual(SyncIssueType.ClientException, session.SyncIssues[0].IssueType);
		});
	}

	[TestMethod]
	public void PostActionRunsAfterSuccessfulSync()
	{
		WithEachPair((client, _, manager) =>
		{
			AddAddress(client, "Home");
			var postCalled = false;
			var session = manager.Sync(
				SampleSyncClient.SyncAll,
				waitFor: TimeSpan.FromSeconds(30),
				postAction: _ => postCalled = true
			);
			IsTrue(session.SyncSuccessful);
			IsTrue(postCalled);
			AreEqual(1, manager.SyncTimers[SampleSyncClient.SyncAll].SuccessfulSyncs);
		});
	}

	[TestMethod]
	public void ReadyToSyncUsesLastSyncedAndAttempted()
	{
		WithEachPair((client, _, _) =>
		{
			var manager = new ReadySyncManager(
				new SampleSyncClientProvider("Client", ScenarioClientProvider, this),
				new SampleSyncClientProvider("Server", ScenarioServerProvider, this),
				new SyncSession(this),
				RuntimeInformation,
				this,
				Dispatcher
			);
			IsTrue(manager.IsReady(SampleSyncClient.SyncAll, TimeSpan.FromHours(1)));
			IsTrue(manager.IsReady("UnknownType", TimeSpan.FromHours(1)));
			AddAddress(client, "Home");
			var session = manager.Sync(SampleSyncClient.SyncAll, waitFor: TimeSpan.FromSeconds(30));
			IsTrue(session.SyncSuccessful);
			IsFalse(manager.IsReady(SampleSyncClient.SyncAll, TimeSpan.FromHours(1)));
			IncrementTime(hours: 2);
			IsTrue(manager.IsReady(SampleSyncClient.SyncAll, TimeSpan.FromHours(1)));
		});
	}

	[TestMethod]
	public void SequentialSyncsBothSucceed()
	{
		WithEachPair((client, _, manager) =>
		{
			AddAddress(client, "Home");
			var first = RunSync(manager);
			var second = RunSync(manager);
			IsTrue(first.SyncSuccessful);
			IsTrue(second.SyncSuccessful);
		});
	}

	[TestMethod]
	public void ServiceUnavailableWebExceptionIsServiceUnavailableIssue()
	{
		WithEachPair((_, _, _) =>
		{
			var manager = new SampleSyncManager(
				new SampleSyncClientProvider("Client", ScenarioClientProvider, this),
				new ThrowingWebClientProvider(ScenarioServerProvider, this, HttpStatusCode.ServiceUnavailable),
				new SyncSession(this),
				RuntimeInformation,
				this,
				Dispatcher
			);
			var session = manager.Sync(SampleSyncClient.SyncAll, waitFor: TimeSpan.FromSeconds(30));
			IsTrue(session.SyncCompleted);
			AreEqual(SyncIssueType.ServiceUnavailable, session.SyncIssues[0].IssueType);
		});
	}

	[TestMethod]
	public void StartSyncCommandRunsNamedType()
	{
		WithEachPair((client, _, manager) =>
		{
			AddAddress(client, "Home");
			manager.StartSyncCommand.Execute(SampleSyncClient.SyncAll);
			IsTrue(manager.WaitForSyncsToComplete(TimeSpan.FromSeconds(30)));
			IsTrue(manager.SyncSession.SyncCompleted);
		});
	}

	[TestMethod]
	public void StopSyncCancelsRunningSession()
	{
		WithEachPair((_, _, manager) =>
		{
			using var entered = new ManualResetEventSlim(false);
			using var release = new ManualResetEventSlim(false);
			var first = manager.SyncAsync(
				SampleSyncClient.SyncAll,
				_ =>
				{
					entered.Set();
					release.Wait(TimeSpan.FromSeconds(15));
				},
				TimeSpan.FromSeconds(30)
			);
			IsTrue(entered.Wait(TimeSpan.FromSeconds(15)));
			var stop = Task.Run(() => manager.StopSync(TimeSpan.FromSeconds(15)));
			IsTrue(SpinWait.SpinUntil(() => manager.SyncSession.SyncCancelled, TimeSpan.FromSeconds(15)));
			release.Set();
			stop.GetAwaiter().GetResult();
			var result = first.GetAwaiter().GetResult();
			IsTrue(result.SyncCompleted);
			IsTrue(result.SyncCancelled || (manager.SyncTimers[SampleSyncClient.SyncAll].CancelledSyncs >= 1));
		});
	}

	[TestMethod]
	public void SuccessfulSessionEndsAtOneHundredPercent()
	{
		WithEachPair((client, _, manager) =>
		{
			AddAddress(client, "Home");
			var session = RunSync(manager);
			AreEqual(100m, session.Percent);
			IsTrue(session.SyncConfigured);
			IsTrue(session.SyncSuccessful);
		});
	}

	[TestMethod]
	public void SyncCompletedEventFires()
	{
		WithEachPair((_, _, manager) =>
		{
			var fired = false;
			manager.SyncCompleted += (_, _) => fired = true;
			RunSync(manager);
			IsTrue(fired);
		});
	}

	[TestMethod]
	public void SyncCompletedSeesStoredStampsBeforeDispatcherRuns()
	{
		var dispatcher = new QueuedDispatcher();
		var manager = NewCompletingManager(dispatcher);
		var client = new DateTime(2024, 6, 1, 8, 0, 0, DateTimeKind.Utc);
		var server = new DateTime(2024, 6, 1, 8, 1, 0, DateTimeKind.Utc);
		var session = SuccessfulSession(client, server);
		DateTime seenClient = DateTime.MinValue;
		DateTime seenServer = DateTime.MinValue;
		var fired = false;

		manager.SyncCompleted += (_, completed) =>
		{
			fired = true;
			var settings = manager.GetSyncSettings(completed.SyncType);
			seenClient = settings.LastSyncedOnClient;
			seenServer = settings.LastSyncedOnServer;
		};

		manager.RaiseCompleted(session);

		IsTrue(fired);
		AreEqual(client, seenClient);
		AreEqual(server, seenServer);
		AreEqual(client, manager.GetSyncSettings(SampleSyncClient.SyncAll).LastSyncedOnClient);
		AreEqual(server, manager.GetSyncSettings(SampleSyncClient.SyncAll).LastSyncedOnServer);
		AreEqual(1, dispatcher.Posted);
	}

	[TestMethod]
	public void UnsuccessfulCompletionDoesNotStoreLastSynced()
	{
		var dispatcher = new QueuedDispatcher();
		var manager = NewCompletingManager(dispatcher);
		var session = new SyncSession(this);
		session.SyncType = SampleSyncClient.SyncAll;
		session.Settings.LastSyncedOnClient = new DateTime(2024, 6, 1, 8, 0, 0, DateTimeKind.Utc);
		session.Settings.LastSyncedOnServer = new DateTime(2024, 6, 1, 8, 1, 0, DateTimeKind.Utc);
		session.UpdateState(SyncSessionState.Completed);
		var fired = false;

		manager.SyncCompleted += (_, _) => fired = true;
		manager.RaiseCompleted(session);

		IsTrue(fired);
		AreEqual(DateTime.MinValue, manager.GetSyncSettings(SampleSyncClient.SyncAll).LastSyncedOnClient);
		AreEqual(DateTime.MinValue, manager.GetSyncSettings(SampleSyncClient.SyncAll).LastSyncedOnServer);
		AreEqual(1, dispatcher.Posted);
	}

	[TestMethod]
	public void UnauthorizedWebExceptionIsUnauthorizedIssue()
	{
		WithEachPair((_, _, _) =>
		{
			var manager = new SampleSyncManager(
				new SampleSyncClientProvider("Client", ScenarioClientProvider, this),
				new ThrowingWebClientProvider(ScenarioServerProvider, this, HttpStatusCode.Unauthorized),
				new SyncSession(this),
				RuntimeInformation,
				this,
				Dispatcher
			);
			var session = manager.Sync(SampleSyncClient.SyncAll, waitFor: TimeSpan.FromSeconds(30));
			IsTrue(session.SyncCompleted);
			AreEqual(SyncIssueType.Unauthorized, session.SyncIssues[0].IssueType);
		});
	}

	[TestMethod]
	public void UninitializeLifecycleResetsLastSynced()
	{
		WithEachPair((client, _, manager) =>
		{
			AddAddress(client, "Home");
			RunSync(manager);
			IsTrue(manager.GetSyncSettings(SampleSyncClient.SyncAll).LastSyncedOnClient > DateTime.MinValue);
			manager.UninitializeLifecycle();
			AreEqual(DateTime.MinValue, manager.GetSyncSettings(SampleSyncClient.SyncAll).LastSyncedOnClient);
		});
	}

	[TestMethod]
	public void UnsupportedSyncTypeThrows()
	{
		WithEachPair((_, _, manager) =>
		{
			ExpectedException<ConstraintException>(
				() => manager.Sync("NotAType"),
				"The sync type is not supported by this sync manager."
			);
		});
	}

	[TestMethod]
	public void UpdateLastSyncedOnAndReset()
	{
		WithEachPair((client, _, manager) =>
		{
			AddAddress(client, "Home");
			var session = RunSync(manager);
			IsTrue(manager.GetSyncSettings(SampleSyncClient.SyncAll).LastSyncedOnClient > DateTime.MinValue);
			IsTrue(manager.GetSyncSettings(SampleSyncClient.SyncAll).LastSyncedOnServer > DateTime.MinValue);
			AreEqual(session.Settings.LastSyncedOnClient, manager.GetSyncSettings(SampleSyncClient.SyncAll).LastSyncedOnClient);

			manager.Reset();
			AreEqual(DateTime.MinValue, manager.GetSyncSettings(SampleSyncClient.SyncAll).LastSyncedOnClient);
			AreEqual(DateTime.MinValue, manager.GetSyncSettings(SampleSyncClient.SyncAll).LastSyncedOnServer);
		});
	}

	[TestMethod]
	public void WaitForSyncsToCompleteTimesOutWhileRunning()
	{
		WithEachPair((_, _, manager) =>
		{
			using var entered = new ManualResetEventSlim(false);
			using var release = new ManualResetEventSlim(false);
			var first = manager.SyncAsync(
				SampleSyncClient.SyncAll,
				_ =>
				{
					entered.Set();
					release.Wait(TimeSpan.FromSeconds(15));
				},
				TimeSpan.FromSeconds(30)
			);
			IsTrue(entered.Wait(TimeSpan.FromSeconds(15)));
			var waitTask = Task.Run(() => manager.WaitForSyncsToComplete(TimeSpan.FromSeconds(2)));
			while (!waitTask.IsCompleted)
			{
				IncrementTime(seconds: 1);
				Thread.Sleep(5);
			}
			IsFalse(waitTask.GetAwaiter().GetResult());
			release.Set();
			first.GetAwaiter().GetResult();
		});
	}

	[TestMethod]
	public void WaitForSyncsToCompleteWhenIdle()
	{
		WithEachPair((_, _, manager) => { IsTrue(manager.WaitForSyncsToComplete(TimeSpan.FromSeconds(1))); });
	}

	private CompletingSyncManager NewCompletingManager(IDispatcher dispatcher)
	{
		return new CompletingSyncManager(
			new UnusedSyncClientProvider(),
			new UnusedSyncClientProvider(),
			new SyncSession(this),
			RuntimeInformation,
			this,
			dispatcher
		);
	}

	private SyncSession SuccessfulSession(DateTime client, DateTime server)
	{
		var session = new SyncSession(this);
		session.SyncType = SampleSyncClient.SyncAll;
		session.Settings.LastSyncedOnClient = client;
		session.Settings.LastSyncedOnServer = server;
		session.UpdateState(SyncSessionState.Successful);
		return session;
	}

	#endregion

	#region Classes

	private sealed class CompletingSyncManager : SampleSyncManager
	{
		#region Constructors

		public CompletingSyncManager(
			ISyncClientProvider clientSyncClientProvider,
			ISyncClientProvider serverSyncClientProvider,
			SyncSession syncSession,
			IRuntimeInformation runtimeInformation,
			IDateTimeProvider dateTimeProvider,
			IDispatcher dispatcher
		) : base(
			clientSyncClientProvider,
			serverSyncClientProvider,
			syncSession,
			runtimeInformation,
			dateTimeProvider,
			dispatcher
		)
		{
		}

		#endregion

		#region Methods

		public void RaiseCompleted(SyncSession session)
		{
			OnSyncCompleted(session);
		}

		#endregion
	}

	private sealed class NullSyncClientProvider : ISyncClientProvider
	{
		#region Fields

		private readonly ISyncableDatabaseProvider _databaseProvider;

		#endregion

		#region Constructors

		public NullSyncClientProvider(ISyncableDatabaseProvider databaseProvider)
		{
			_databaseProvider = databaseProvider;
		}

		#endregion

		#region Methods

		public SyncClient GetSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
		{
			return null;
		}

		public ISyncableDatabase GetSyncableDatabase()
		{
			return _databaseProvider.GetSyncableDatabase();
		}

		#endregion
	}

	private sealed class ReadySyncManager : SampleSyncManager
	{
		#region Constructors

		public ReadySyncManager(
			ISyncClientProvider clientSyncClientProvider,
			ISyncClientProvider serverSyncClientProvider,
			SyncSession syncSession,
			IRuntimeInformation runtimeInformation,
			IDateTimeProvider dateTimeProvider,
			IDispatcher dispatcher
		) : base(
			clientSyncClientProvider,
			serverSyncClientProvider,
			syncSession,
			runtimeInformation,
			dateTimeProvider,
			dispatcher
		)
		{
		}

		#endregion

		#region Methods

		public bool IsReady(string syncType, TimeSpan interval)
		{
			return ReadyToSync(syncType, interval);
		}

		#endregion
	}

	private sealed class QueuedDispatcher : IDispatcher
	{
		#region Properties

		public int Posted { get; private set; }

		#endregion

		#region Methods

		public bool CheckAccess()
		{
			return false;
		}

		public void Post(Action action, DispatcherPriority priority = default)
		{
			Posted++;
		}

		public void VerifyAccess()
		{
		}

		#endregion
	}

	private sealed class RejectingServerSyncClient : SampleServerSyncClient
	{
		#region Constructors

		public RejectingServerSyncClient(
			string name,
			ISyncableDatabaseProvider databaseProvider,
			IDateTimeProvider dateTimeProvider,
			SyncStatistics syncStatistics,
			Profiler syncClientProfiler
		) : base(name, databaseProvider, dateTimeProvider, syncStatistics, syncClientProfiler)
		{
		}

		#endregion

		#region Methods

		protected override bool ValidateSyncClient()
		{
			return false;
		}

		#endregion
	}

	private sealed class RejectingSyncClientProvider : ISyncClientProvider
	{
		#region Fields

		private readonly ISyncableDatabaseProvider _databaseProvider;
		private readonly IDateTimeProvider _dateTimeProvider;

		#endregion

		#region Constructors

		public RejectingSyncClientProvider(ISyncableDatabaseProvider databaseProvider, IDateTimeProvider dateTimeProvider)
		{
			_databaseProvider = databaseProvider;
			_dateTimeProvider = dateTimeProvider;
		}

		#endregion

		#region Methods

		public SyncClient GetSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
		{
			return new RejectingServerSyncClient("Server", _databaseProvider, _dateTimeProvider, syncStatistics, syncClientProfiler);
		}

		public ISyncableDatabase GetSyncableDatabase()
		{
			return _databaseProvider.GetSyncableDatabase();
		}

		#endregion
	}

	private sealed class RejectIncomingServerClient : SampleSyncClient
	{
		#region Constructors

		public RejectIncomingServerClient(
			string name,
			ISyncableDatabaseProvider databaseProvider,
			IDateTimeProvider dateTimeProvider,
			SyncStatistics syncStatistics,
			Profiler syncClientProfiler
		) : base(name, databaseProvider, dateTimeProvider, syncStatistics, syncClientProfiler)
		{
		}

		#endregion

		#region Methods

		protected override void SetSyncSettings()
		{
			SampleSyncFilters.ApplyAll(SyncSettings);
			SyncSettings.AddFilter<AddressEntity>(incomingFilter: _ => false);
		}

		#endregion
	}

	private sealed class RejectIncomingServerProvider : ISyncClientProvider
	{
		#region Fields

		private readonly ISyncableDatabaseProvider _databaseProvider;
		private readonly IDateTimeProvider _dateTimeProvider;

		#endregion

		#region Constructors

		public RejectIncomingServerProvider(ISyncableDatabaseProvider databaseProvider, IDateTimeProvider dateTimeProvider)
		{
			_databaseProvider = databaseProvider;
			_dateTimeProvider = dateTimeProvider;
		}

		#endregion

		#region Methods

		public SyncClient GetSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
		{
			return new RejectIncomingServerClient("Server", _databaseProvider, _dateTimeProvider, syncStatistics, syncClientProfiler);
		}

		public ISyncableDatabase GetSyncableDatabase()
		{
			return _databaseProvider.GetSyncableDatabase();
		}

		#endregion
	}

	private sealed class UnusedSyncClientProvider : ISyncClientProvider
	{
		#region Methods

		public SyncClient GetSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
		{
			return null;
		}

		public ISyncableDatabase GetSyncableDatabase()
		{
			return null;
		}

		#endregion
	}

	private sealed class ThrowingWebClientProvider : ISyncClientProvider
	{
		#region Fields

		private readonly HttpStatusCode _code;
		private readonly ISyncableDatabaseProvider _databaseProvider;
		private readonly IDateTimeProvider _dateTimeProvider;

		#endregion

		#region Constructors

		public ThrowingWebClientProvider(
			ISyncableDatabaseProvider databaseProvider,
			IDateTimeProvider dateTimeProvider,
			HttpStatusCode code
		)
		{
			_databaseProvider = databaseProvider;
			_dateTimeProvider = dateTimeProvider;
			_code = code;
		}

		#endregion

		#region Methods

		public SyncClient GetSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
		{
			return new ThrowingWebSyncClient("Server", _databaseProvider, _dateTimeProvider, syncStatistics, syncClientProfiler, _code);
		}

		public ISyncableDatabase GetSyncableDatabase()
		{
			return _databaseProvider.GetSyncableDatabase();
		}

		#endregion
	}

	private sealed class ThrowingWebSyncClient : SampleSyncClient
	{
		#region Fields

		private readonly HttpStatusCode _code;

		#endregion

		#region Constructors

		public ThrowingWebSyncClient(
			string name,
			ISyncableDatabaseProvider databaseProvider,
			IDateTimeProvider dateTimeProvider,
			SyncStatistics syncStatistics,
			Profiler syncClientProfiler,
			HttpStatusCode code
		) : base(name, databaseProvider, dateTimeProvider, syncStatistics, syncClientProfiler)
		{
			_code = code;
		}

		#endregion

		#region Methods

		public override SyncOperationResult Sync(SyncOperation operation)
		{
			throw new WebClientException(_code, _code.ToString());
		}

		#endregion
	}

	#endregion
}