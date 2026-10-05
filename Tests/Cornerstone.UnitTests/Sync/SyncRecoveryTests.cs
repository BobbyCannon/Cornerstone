#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sample.Models;
using Cornerstone.Sample.Storage;
using Cornerstone.Sample.Sync;
using Cornerstone.Sync;
using Cornerstone.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

/// <summary>
/// Cancel finishes the server call already in flight and then stops.
/// A batch save that throws is retried one row at a time.
/// Corrections run only when that server session is still open.
/// </summary>
[TestClass]
[DoNotParallelize]
public class SyncRecoveryTests : SyncRecordingTest
{
	#region Methods

	[TestMethod]
	public void CancelDuringPullAppliesThatPageAndDoesNotPush()
	{
		var gate = new SyncGate();
		WithGate(gate, (client, server, manager) =>
		{
			AddAddress(server, "FromServer");
			var local = AddAddress(client, "FromClient");
			var task = manager.SyncAsync(SampleSyncClient.SyncAll, null, TimeSpan.FromSeconds(30));
			IsTrue(gate.Entered.Wait(TimeSpan.FromSeconds(15)));
			manager.CancelSync();
			gate.Release.Set();
			var session = task.GetAwaiter().GetResult();

			IsTrue(session.SyncCancelled || manager.SyncSession.SyncCancelled, () => $"returned={session.State} live={manager.SyncSession.State}");
			IsTrue(session.SyncCompleted);
			IsFalse(session.SyncSuccessful);
			AreEqual(2, gate.Calls.Count, () => FormatCalls(gate.Calls));
			AssertPull(gate.Calls[0], resultChanges: 1, sessionEnded: false, clientHasNoChanges: false);
			AssertEndOnly(gate.Calls[1]);
			IsNotNull(client.Addresses.Read(gate.Calls[0].ResultChangeIds[0]));
			IsNull(server.Addresses.Read(local.SyncId));
			AreEqual(DateTime.MinValue, Stamps(manager).Client);
			AreEqual(DateTime.MinValue, Stamps(manager).Server);
			IsTrue(manager.SyncTimers[SampleSyncClient.SyncAll].CancelledSyncs >= 1);
		});
	}

	[TestMethod]
	public void CancelDuringPushStillAppliesThatPage()
	{
		var gate = new SyncGate();
		WithGate(gate, (client, server, manager) =>
		{
			var local = AddAddress(client, "FromClient");
			var task = manager.SyncAsync(
				SampleSyncClient.SyncAll,
				settings => settings.SyncDirection = SyncDirection.PushUp,
				TimeSpan.FromSeconds(30));
			IsTrue(gate.Entered.Wait(TimeSpan.FromSeconds(15)));
			manager.CancelSync();
			gate.Release.Set();
			var session = task.GetAwaiter().GetResult();

			IsTrue(session.SyncCancelled || manager.SyncSession.SyncCancelled, () => $"returned={session.State} live={manager.SyncSession.State}");
			IsFalse(session.SyncSuccessful);
			AreEqual(1, gate.Calls.Count, () => FormatCalls(gate.Calls));
			AssertPush(gate.Calls[0], changeCount: 1, endSession: true);
			IsNotNull(server.Addresses.Read(local.SyncId));
			AreEqual(DateTime.MinValue, Stamps(manager).Client);
			AreEqual(DateTime.MinValue, Stamps(manager).Server);

			var retry = manager.Sync(SampleSyncClient.SyncAll, settings => settings.SyncDirection = SyncDirection.PushUp, TimeSpan.FromSeconds(30));
			IsTrue(retry.SyncSuccessful, () => string.Join("; ", retry.SyncIssues.Select(x => x.Message)));
			IsTrue(Stamps(manager).Client > DateTime.MinValue);
		});
	}

	[TestMethod]
	public void ClientCorrectionRepairsARejectedPush()
	{
		WithRepair((client, server, manager, calls) =>
		{
			var syncId = Guid.NewGuid();
			AddAddress(client, "Bad", syncId: syncId);
			var repair = SyncObject.ToSyncObject(new Address
			{
				City = "City",
				CreatedOn = UtcNow,
				Line1 = "Fixed",
				ModifiedOn = UtcNow,
				Postal = "29640",
				State = "SC",
				SyncId = syncId
			});

			var session = manager.Sync(SampleSyncClient.SyncAll, settings =>
			{
				settings.SyncDirection = SyncDirection.PushUp;
				settings.ItemsPerSyncRequest = 1;
				settings.AddFilter<AddressEntity>(incomingFilter: x => x.Line1 != "Bad");
				((RepairingClientProvider) manager.ClientSyncClientProvider).Repair = repair;
			}, TimeSpan.FromSeconds(30));

			IsTrue(session.SyncSuccessful, () => string.Join("; ", session.SyncIssues.Select(x => $"{x.IssueType}:{x.Message}")));
			AreEqual(4, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: 1, endSession: false);
			AreEqual(1, calls[1].IssueCount);
			AreEqual(0, calls[1].ChangeCount);
			AreEqual(1, calls[2].IssueCount);
			AreEqual(1, calls[2].ChangeCount);
			AreEqual("Fixed", ((AddressEntity) server.Addresses.Read(syncId)).Line1);
			AreEqual(0, session.StatisticsForServer.AppliedChanges);
			AreEqual(1, session.StatisticsForServer.AppliedCorrections);
			AreEqual(0, session.StatisticsForServer.Corrections);
			AreEqual(0, session.StatisticsForClient.AppliedCorrections);
			IsTrue(Stamps(manager).Client > DateTime.MinValue);
		});
	}

	[TestMethod]
	public void EmptyCorrectionsLeaveTheIssueAndDoNotMoveLastSyncedOn()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Guid.NewGuid();
			AddAddress(client, "Bad", syncId: syncId);

			var session = manager.Sync(SampleSyncClient.SyncAll, settings =>
			{
				settings.SyncDirection = SyncDirection.PushUp;
				settings.ItemsPerSyncRequest = 1;
				settings.AddFilter<AddressEntity>(incomingFilter: x => x.Line1 != "Bad");
			}, TimeSpan.FromSeconds(30));

			IsFalse(session.SyncSuccessful);
			AreEqual(SyncIssueType.SyncEntityFiltered, session.SyncIssues[0].IssueType);
			AreEqual(3, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: 1, endSession: false);
			AreEqual(1, calls[1].IssueCount);
			AreEqual(0, calls[1].ChangeCount);
			IsFalse(calls[1].EndSession);
			AssertEndOnly(calls[2]);
			IsNull(server.Addresses.Read(syncId));
			AreEqual(0, session.StatisticsForServer.Corrections);
			AreEqual(0, session.StatisticsForServer.AppliedCorrections);
			AreEqual(0, session.StatisticsForServer.IndividualProcessCount);
			AreEqual(DateTime.MinValue, Stamps(manager).Client);
		});
	}

	[TestMethod]
	public void CorrectionRequestSendsOnePageOfIssues()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var first = AddAddress(client, "Bad");
			IncrementTime(seconds: 1);
			var second = AddAddress(client, "Bad");

			var session = manager.Sync(SampleSyncClient.SyncAll, settings =>
			{
				settings.SyncDirection = SyncDirection.PushUp;
				settings.ItemsPerSyncRequest = 1;
				settings.AddFilter<AddressEntity>(incomingFilter: x => x.Line1 != "Bad");
			}, TimeSpan.FromSeconds(30));

			IsFalse(session.SyncSuccessful);
			AreEqual(2, session.SyncIssues.Count);
			AreEqual(4, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: 1, endSession: false);
			AssertPush(calls[1], changeCount: 1, endSession: false);
			AreEqual(1, calls[2].IssueCount);
			AreEqual(0, calls[2].ChangeCount);
			IsFalse(calls[2].EndSession);
			AssertEndOnly(calls[3]);
			IsNull(server.Addresses.Read(first.SyncId));
			IsNull(server.Addresses.Read(second.SyncId));
			AreEqual(DateTime.MinValue, Stamps(manager).Client);
			AreEqual(DateTime.MinValue, Stamps(manager).Server);
		});
	}

	[TestMethod]
	public void PullBatchFailureSavesTheGoodRowOnTheClient()
	{
		var badId = Guid.NewGuid();
		WithInjectedServer(BadAddress(badId), (client, server, manager, calls) =>
		{
			var good = AddAddress(server, "Good");

			var session = manager.Sync(SampleSyncClient.SyncAll, settings => settings.SyncDirection = SyncDirection.PullDown, TimeSpan.FromSeconds(30));

			IsFalse(session.SyncSuccessful);
			IsNotNull(client.Addresses.Read(good.SyncId));
			IsNull(client.Addresses.Read(badId));
			IsTrue(session.StatisticsForClient.IndividualProcessCount >= 1);
			AreEqual(1, session.StatisticsForClient.AppliedChanges);
			AreEqual(0, session.StatisticsForServer.IndividualProcessCount);
			AreEqual(DateTime.MinValue, Stamps(manager).Client);
			IsTrue(session.SyncIssues.Any(x => x.Id == badId));
		});
	}

	[TestMethod]
	public void PushBatchFailureSavesTheGoodRowAndCountsTheIndividualRetry()
	{
		var badId = Guid.NewGuid();
		WithInjectedClient(BadAddress(badId), (client, server, manager, calls) =>
		{
			var good = AddAddress(client, "Good");

			var session = manager.Sync(SampleSyncClient.SyncAll, settings => settings.SyncDirection = SyncDirection.PushUp, TimeSpan.FromSeconds(30));

			IsFalse(session.SyncSuccessful);
			IsNotNull(server.Addresses.Read(good.SyncId));
			IsNull(server.Addresses.Read(badId));
			IsTrue(session.StatisticsForServer.IndividualProcessCount >= 1);
			AreEqual(1, session.StatisticsForServer.AppliedChanges);
			AreEqual(1, session.StatisticsForClient.Changes);
			AreEqual(0, session.StatisticsForClient.IndividualProcessCount);
			AreEqual(DateTime.MinValue, Stamps(manager).Client);
			IsTrue(session.SyncIssues.Any(x => x.Id == badId));
		});
	}

	[TestMethod]
	public void ServerCorrectionRepairsARejectedPullBeforeThePush()
	{
		var badId = Guid.NewGuid();
		var repair = SyncObject.ToSyncObject(new Address
		{
			City = "City",
			CreatedOn = UtcNow,
			Line1 = "Fixed",
			ModifiedOn = UtcNow,
			Postal = "29640",
			State = "SC",
			SyncId = badId
		});
		WithRepairServer(repair, (client, server, manager, calls) =>
		{
			AddAddress(server, "Bad", syncId: badId);
			var mine = AddAddress(client, "Mine");

			var session = manager.Sync(SampleSyncClient.SyncAll, settings =>
			{
				settings.AddFilter<AddressEntity>(incomingFilter: x => x.Line1 != "Bad");
			}, TimeSpan.FromSeconds(30));

			IsTrue(session.SyncSuccessful, () => string.Join("; ", session.SyncIssues.Select(x => $"{x.IssueType}:{x.Message}")));
			AreEqual("Fixed", ((AddressEntity) client.Addresses.Read(badId)).Line1);
			AreEqual("Bad", ((AddressEntity) server.Addresses.Read(badId)).Line1);
			IsNotNull(server.Addresses.Read(mine.SyncId));
			AreEqual(1, session.StatisticsForClient.AppliedChanges);
			AreEqual(2, session.StatisticsForClient.Changes);
			AreEqual(1, session.StatisticsForServer.Changes);
			AreEqual(1, session.StatisticsForServer.AppliedChanges);
			AreEqual(0, session.StatisticsForServer.Corrections);
			IsTrue(Stamps(manager).Client > DateTime.MinValue);
		});
	}

	private static SyncObject BadAddress(Guid syncId)
	{
		return SyncObject.ToSyncObject(new Address
		{
			City = "City",
			CreatedOn = DateTime.UtcNow,
			Line1 = null,
			ModifiedOn = DateTime.UtcNow,
			Postal = "29640",
			State = "SC",
			SyncId = syncId
		});
	}

	private (DateTime Client, DateTime Server) Stamps(SampleSyncManager manager)
	{
		var settings = manager.GetSyncSettings(SampleSyncClient.SyncAll);
		return (settings.LastSyncedOnClient, settings.LastSyncedOnServer);
	}

	private void WithGate(SyncGate gate, Action<ISampleDatabase, ISampleDatabase, SampleSyncManager> test)
	{
		var clientProvider = NewMemoryProvider();
		var serverProvider = NewMemoryProvider();
		using var clientDatabase = (ISampleDatabase) clientProvider.GetSyncableDatabase();
		using var serverDatabase = (ISampleDatabase) serverProvider.GetSyncableDatabase();
		clientDatabase.Migrate();
		serverDatabase.Migrate();
		var manager = new SampleSyncManager(
			new SampleSyncClientProvider("Client", clientProvider, this),
			new GatedServerProvider(serverProvider, this, gate),
			new SyncSession(this),
			RuntimeInformation,
			this,
			Dispatcher
		);
		manager.GetSyncSettings(SampleSyncClient.SyncAll).IncludeIssueDetails = true;
		test(clientDatabase, serverDatabase, manager);
	}

	private void WithInjectedClient(SyncObject extra, Action<ISampleDatabase, ISampleDatabase, SampleSyncManager, List<ServerCall>> test)
	{
		var calls = new List<ServerCall>();
		var clientProvider = NewMemoryProvider();
		var serverProvider = NewMemoryProvider();
		using var clientDatabase = (ISampleDatabase) clientProvider.GetSyncableDatabase();
		using var serverDatabase = (ISampleDatabase) serverProvider.GetSyncableDatabase();
		clientDatabase.Migrate();
		serverDatabase.Migrate();
		var manager = new SampleSyncManager(
			new InjectingClientProvider(clientProvider, this, extra),
			new RecordingHubProvider(serverProvider, this, calls),
			new SyncSession(this),
			RuntimeInformation,
			this,
			Dispatcher
		);
		manager.GetSyncSettings(SampleSyncClient.SyncAll).IncludeIssueDetails = true;
		test(clientDatabase, serverDatabase, manager, calls);
	}

	private void WithInjectedServer(SyncObject extra, Action<ISampleDatabase, ISampleDatabase, SampleSyncManager, List<ServerCall>> test)
	{
		var calls = new List<ServerCall>();
		var clientProvider = NewMemoryProvider();
		var serverProvider = NewMemoryProvider();
		using var clientDatabase = (ISampleDatabase) clientProvider.GetSyncableDatabase();
		using var serverDatabase = (ISampleDatabase) serverProvider.GetSyncableDatabase();
		clientDatabase.Migrate();
		serverDatabase.Migrate();
		var manager = new SampleSyncManager(
			new SampleSyncClientProvider("Client", clientProvider, this),
			new InjectingServerProvider(serverProvider, this, calls, extra),
			new SyncSession(this),
			RuntimeInformation,
			this,
			Dispatcher
		);
		manager.GetSyncSettings(SampleSyncClient.SyncAll).IncludeIssueDetails = true;
		test(clientDatabase, serverDatabase, manager, calls);
	}

	private void WithRepair(Action<ISampleDatabase, ISampleDatabase, SampleSyncManager, List<ServerCall>> test)
	{
		var calls = new List<ServerCall>();
		var clientProvider = NewMemoryProvider();
		var serverProvider = NewMemoryProvider();
		using var clientDatabase = (ISampleDatabase) clientProvider.GetSyncableDatabase();
		using var serverDatabase = (ISampleDatabase) serverProvider.GetSyncableDatabase();
		clientDatabase.Migrate();
		serverDatabase.Migrate();
		var repairing = new RepairingClientProvider(clientProvider, this);
		var manager = new SampleSyncManager(
			repairing,
			new RecordingHubProvider(serverProvider, this, calls),
			new SyncSession(this),
			RuntimeInformation,
			this,
			Dispatcher
		);
		manager.GetSyncSettings(SampleSyncClient.SyncAll).IncludeIssueDetails = true;
		test(clientDatabase, serverDatabase, manager, calls);
	}

	private void WithRepairServer(SyncObject repair, Action<ISampleDatabase, ISampleDatabase, SampleSyncManager, List<ServerCall>> test)
	{
		var calls = new List<ServerCall>();
		var clientProvider = NewMemoryProvider();
		var serverProvider = NewMemoryProvider();
		using var clientDatabase = (ISampleDatabase) clientProvider.GetSyncableDatabase();
		using var serverDatabase = (ISampleDatabase) serverProvider.GetSyncableDatabase();
		clientDatabase.Migrate();
		serverDatabase.Migrate();
		var manager = new SampleSyncManager(
			new SampleSyncClientProvider("Client", clientProvider, this),
			new RepairingServerProvider(serverProvider, this, calls, repair),
			new SyncSession(this),
			RuntimeInformation,
			this,
			Dispatcher
		);
		manager.GetSyncSettings(SampleSyncClient.SyncAll).IncludeIssueDetails = true;
		test(clientDatabase, serverDatabase, manager, calls);
	}

	#endregion

	#region Classes

	private sealed class GatedServerProvider : ISyncClientProvider
	{
		#region Fields

		private readonly ISyncableDatabaseProvider _databaseProvider;
		private readonly IDateTimeProvider _dateTimeProvider;
		private readonly SyncGate _gate;

		#endregion

		#region Constructors

		public GatedServerProvider(ISyncableDatabaseProvider databaseProvider, IDateTimeProvider dateTimeProvider, SyncGate gate)
		{
			_databaseProvider = databaseProvider;
			_dateTimeProvider = dateTimeProvider;
			_gate = gate;
		}

		#endregion

		#region Methods

		public SyncClient GetSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
		{
			return new GatedServerSyncClient("Server", _databaseProvider, _dateTimeProvider, syncStatistics, syncClientProfiler, _gate);
		}

		public ISyncableDatabase GetSyncableDatabase()
		{
			return _databaseProvider.GetSyncableDatabase();
		}

		#endregion
	}

	private sealed class GatedServerSyncClient : SampleServerSyncClient
	{
		#region Fields

		private readonly SyncGate _gate;

		#endregion

		#region Constructors

		public GatedServerSyncClient(
			string name,
			ISyncableDatabaseProvider databaseProvider,
			IDateTimeProvider dateTimeProvider,
			SyncStatistics statistics,
			Profiler profiler,
			SyncGate gate
		) : base(name, databaseProvider, dateTimeProvider, statistics, profiler)
		{
			_gate = gate;
		}

		#endregion

		#region Methods

		public override SyncOperationResult Sync(SyncOperation operation)
		{
			_gate.Entered.Set();
			_gate.Release.Wait(TimeSpan.FromSeconds(15));
			var result = base.Sync(operation);
			var call = new ServerCall();
			call.ChangeCount = operation.Changes?.Collection?.Count ?? 0;
			call.ChangeIds = operation.Changes?.Collection?.Select(x => x.SyncId).ToList() ?? [];
			call.ClientHasNoChanges = operation.ClientHasNoChanges;
			call.EndSession = operation.EndSession;
			call.IssueCount = operation.Issues?.Collection?.Count ?? 0;
			call.ResultChangeCount = result.Changes?.Collection?.Count ?? 0;
			call.ResultChangeIds = result.Changes?.Collection?.Select(x => x.SyncId).ToList() ?? [];
			call.SessionEnded = result.SessionEnded;
			_gate.Calls.Add(call);
			return result;
		}

		#endregion
	}

	private sealed class InjectingClient : SampleSyncClient
	{
		#region Fields

		private readonly SyncObject _extra;

		#endregion

		#region Constructors

		public InjectingClient(
			string name,
			ISyncableDatabaseProvider databaseProvider,
			IDateTimeProvider dateTimeProvider,
			SyncStatistics statistics,
			Profiler profiler,
			SyncObject extra
		) : base(name, databaseProvider, dateTimeProvider, statistics, profiler)
		{
			_extra = extra;
		}

		#endregion

		#region Methods

		protected internal override ServiceResult<SyncObject> GetChanges(Guid sessionId, SyncRequest request)
		{
			var page = base.GetChanges(sessionId, request);
			if ((request.Skip == 0) && (_extra != null))
			{
				page.Collection.Add(_extra);
			}

			return page;
		}

		#endregion
	}

	private sealed class InjectingClientProvider : ISyncClientProvider
	{
		#region Fields

		private readonly ISyncableDatabaseProvider _databaseProvider;
		private readonly IDateTimeProvider _dateTimeProvider;
		private readonly SyncObject _extra;

		#endregion

		#region Constructors

		public InjectingClientProvider(ISyncableDatabaseProvider databaseProvider, IDateTimeProvider dateTimeProvider, SyncObject extra)
		{
			_databaseProvider = databaseProvider;
			_dateTimeProvider = dateTimeProvider;
			_extra = extra;
		}

		#endregion

		#region Methods

		public SyncClient GetSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
		{
			return new InjectingClient("Client", _databaseProvider, _dateTimeProvider, syncStatistics, syncClientProfiler, _extra);
		}

		public ISyncableDatabase GetSyncableDatabase()
		{
			return _databaseProvider.GetSyncableDatabase();
		}

		#endregion
	}

	private sealed class InjectingServerProvider : ISyncClientProvider
	{
		#region Fields

		private readonly List<ServerCall> _calls;
		private readonly ISyncableDatabaseProvider _databaseProvider;
		private readonly IDateTimeProvider _dateTimeProvider;
		private readonly SyncObject _extra;

		#endregion

		#region Constructors

		public InjectingServerProvider(
			ISyncableDatabaseProvider databaseProvider,
			IDateTimeProvider dateTimeProvider,
			List<ServerCall> calls,
			SyncObject extra)
		{
			_databaseProvider = databaseProvider;
			_dateTimeProvider = dateTimeProvider;
			_calls = calls;
			_extra = extra;
		}

		#endregion

		#region Methods

		public SyncClient GetSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
		{
			return new InjectingServerSyncClient("Server", _databaseProvider, _dateTimeProvider, syncStatistics, syncClientProfiler, _calls, _extra);
		}

		public ISyncableDatabase GetSyncableDatabase()
		{
			return _databaseProvider.GetSyncableDatabase();
		}

		#endregion
	}

	private sealed class InjectingServerSyncClient : SampleServerSyncClient
	{
		#region Fields

		private readonly List<ServerCall> _calls;
		private readonly SyncObject _extra;

		#endregion

		#region Constructors

		public InjectingServerSyncClient(
			string name,
			ISyncableDatabaseProvider databaseProvider,
			IDateTimeProvider dateTimeProvider,
			SyncStatistics statistics,
			Profiler profiler,
			List<ServerCall> calls,
			SyncObject extra
		) : base(name, databaseProvider, dateTimeProvider, statistics, profiler)
		{
			_calls = calls;
			_extra = extra;
		}

		#endregion

		#region Methods

		protected internal override ServiceResult<SyncObject> GetChanges(Guid sessionId, SyncRequest request)
		{
			var page = base.GetChanges(sessionId, request);
			if ((request.Skip == 0) && (_extra != null))
			{
				page.Collection.Add(_extra);
			}

			return page;
		}

		#endregion
	}

	private sealed class RecordingHubProvider : ISyncClientProvider
	{
		#region Fields

		private readonly List<ServerCall> _calls;
		private readonly ISyncableDatabaseProvider _databaseProvider;
		private readonly IDateTimeProvider _dateTimeProvider;

		#endregion

		#region Constructors

		public RecordingHubProvider(ISyncableDatabaseProvider databaseProvider, IDateTimeProvider dateTimeProvider, List<ServerCall> calls)
		{
			_databaseProvider = databaseProvider;
			_dateTimeProvider = dateTimeProvider;
			_calls = calls;
		}

		#endregion

		#region Methods

		public SyncClient GetSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
		{
			return new RecordingHubSyncClient("Server", _databaseProvider, _dateTimeProvider, syncStatistics, syncClientProfiler, _calls);
		}

		public ISyncableDatabase GetSyncableDatabase()
		{
			return _databaseProvider.GetSyncableDatabase();
		}

		#endregion
	}

	private sealed class RecordingHubSyncClient : SampleServerSyncClient
	{
		#region Fields

		private readonly List<ServerCall> _calls;

		#endregion

		#region Constructors

		public RecordingHubSyncClient(
			string name,
			ISyncableDatabaseProvider databaseProvider,
			IDateTimeProvider dateTimeProvider,
			SyncStatistics statistics,
			Profiler profiler,
			List<ServerCall> calls
		) : base(name, databaseProvider, dateTimeProvider, statistics, profiler)
		{
			_calls = calls;
		}

		#endregion

		#region Methods

		public override SyncOperationResult Sync(SyncOperation operation)
		{
			var result = base.Sync(operation);
			var call = new ServerCall();
			call.ChangeCount = operation.Changes?.Collection?.Count ?? 0;
			call.ChangeIds = operation.Changes?.Collection?.Select(x => x.SyncId).ToList() ?? [];
			call.ClientHasNoChanges = operation.ClientHasNoChanges;
			call.EndSession = operation.EndSession;
			call.IssueCount = operation.Issues?.Collection?.Count ?? 0;
			call.ResultChangeCount = result.Changes?.Collection?.Count ?? 0;
			call.ResultChangeIds = result.Changes?.Collection?.Select(x => x.SyncId).ToList() ?? [];
			call.SessionEnded = result.SessionEnded;
			_calls.Add(call);
			return result;
		}

		#endregion
	}

	private sealed class RepairingClient : SampleSyncClient
	{
		#region Fields

		private readonly RepairingClientProvider _provider;

		#endregion

		#region Constructors

		public RepairingClient(
			string name,
			ISyncableDatabaseProvider databaseProvider,
			IDateTimeProvider dateTimeProvider,
			SyncStatistics statistics,
			Profiler profiler,
			RepairingClientProvider provider
		) : base(name, databaseProvider, dateTimeProvider, statistics, profiler)
		{
			_provider = provider;
		}

		#endregion

		#region Methods

		protected internal override ServiceResult<SyncObject> GetCorrections(Guid sessionId, ServiceRequest<SyncIssue> issues)
		{
			ValidateSession(sessionId);
			if (_provider.Repair == null)
			{
				return new ServiceResult<SyncObject>();
			}

			return new ServiceResult<SyncObject> { Collection = [_provider.Repair], TotalCount = 1 };
		}

		#endregion
	}

	private sealed class RepairingClientProvider : ISyncClientProvider
	{
		#region Fields

		private readonly ISyncableDatabaseProvider _databaseProvider;
		private readonly IDateTimeProvider _dateTimeProvider;

		#endregion

		#region Constructors

		public RepairingClientProvider(ISyncableDatabaseProvider databaseProvider, IDateTimeProvider dateTimeProvider)
		{
			_databaseProvider = databaseProvider;
			_dateTimeProvider = dateTimeProvider;
		}

		#endregion

		#region Properties

		public SyncObject Repair { get; set; }

		#endregion

		#region Methods

		public SyncClient GetSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
		{
			return new RepairingClient("Client", _databaseProvider, _dateTimeProvider, syncStatistics, syncClientProfiler, this);
		}

		public ISyncableDatabase GetSyncableDatabase()
		{
			return _databaseProvider.GetSyncableDatabase();
		}

		#endregion
	}

	private sealed class RepairingServerProvider : ISyncClientProvider
	{
		#region Fields

		private readonly List<ServerCall> _calls;
		private readonly ISyncableDatabaseProvider _databaseProvider;
		private readonly IDateTimeProvider _dateTimeProvider;
		private readonly SyncObject _repair;

		#endregion

		#region Constructors

		public RepairingServerProvider(
			ISyncableDatabaseProvider databaseProvider,
			IDateTimeProvider dateTimeProvider,
			List<ServerCall> calls,
			SyncObject repair)
		{
			_databaseProvider = databaseProvider;
			_dateTimeProvider = dateTimeProvider;
			_calls = calls;
			_repair = repair;
		}

		#endregion

		#region Methods

		public SyncClient GetSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
		{
			return new RepairingServerSyncClient("Server", _databaseProvider, _dateTimeProvider, syncStatistics, syncClientProfiler, _calls, _repair);
		}

		public ISyncableDatabase GetSyncableDatabase()
		{
			return _databaseProvider.GetSyncableDatabase();
		}

		#endregion
	}

	private sealed class RepairingServerSyncClient : SampleServerSyncClient
	{
		#region Fields

		private readonly List<ServerCall> _calls;
		private readonly SyncObject _repair;

		#endregion

		#region Constructors

		public RepairingServerSyncClient(
			string name,
			ISyncableDatabaseProvider databaseProvider,
			IDateTimeProvider dateTimeProvider,
			SyncStatistics statistics,
			Profiler profiler,
			List<ServerCall> calls,
			SyncObject repair
		) : base(name, databaseProvider, dateTimeProvider, statistics, profiler)
		{
			_calls = calls;
			_repair = repair;
		}

		#endregion

		#region Methods

		protected internal override ServiceResult<SyncObject> GetCorrections(Guid sessionId, ServiceRequest<SyncIssue> issues)
		{
			ValidateSession(sessionId);
			return new ServiceResult<SyncObject> { Collection = [_repair], TotalCount = 1 };
		}

		#endregion
	}

	private sealed class SyncGate
	{
		#region Constructors

		public SyncGate()
		{
			Calls = [];
			Entered = new ManualResetEventSlim(false);
			Release = new ManualResetEventSlim(false);
		}

		#endregion

		#region Properties

		public List<ServerCall> Calls { get; }

		public ManualResetEventSlim Entered { get; }

		public ManualResetEventSlim Release { get; }

		#endregion
	}

	#endregion
}
