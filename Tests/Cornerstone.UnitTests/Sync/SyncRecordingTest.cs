#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sample.Models;
using Cornerstone.Sample.Storage;
using Cornerstone.Sample.Sync;
using Cornerstone.Storage.Sql;
using Cornerstone.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

/// <summary>
/// In-process sync pair whose server slot records every Sync call.
/// A hub server sanitizes settings. A peer server is an ordinary sync client.
/// </summary>
public abstract class SyncRecordingTest : SyncScenarioTest
{
	#region Methods

	protected static void AssertEndOnly(ServerCall call)
	{
		AreEqual(0, call.ChangeCount);
		IsTrue(call.EndSession);
		AreEqual(0, call.IssueCount);
		AreEqual(0, call.ResultChangeCount);
		IsTrue(call.SessionEnded);
	}

	protected static void AssertPhases(SyncSession session, bool pulling, bool pushing)
	{
		AreEqual(pulling, session.State.HasFlag(SyncSessionState.Pulling));
		AreEqual(pushing, session.State.HasFlag(SyncSessionState.Pushing));
		IsTrue(session.State.HasFlag(SyncSessionState.Successful));
	}

	protected static void AssertPull(ServerCall call, int resultChanges, bool sessionEnded, bool clientHasNoChanges)
	{
		AreEqual(0, call.ChangeCount);
		IsFalse(call.EndSession);
		AreEqual(0, call.IssueCount);
		AreEqual(clientHasNoChanges, call.ClientHasNoChanges);
		AreEqual(resultChanges, call.ResultChangeCount);
		AreEqual(sessionEnded, call.SessionEnded);
	}

	protected static void AssertPush(ServerCall call, int changeCount, bool endSession)
	{
		AreEqual(changeCount, call.ChangeCount);
		AreEqual(endSession, call.EndSession);
		AreEqual(0, call.IssueCount);
		AreEqual(0, call.ResultChangeCount);
		AreEqual(endSession, call.SessionEnded);
	}

	protected static void AssertStatistics(
		SyncSession session,
		SampleSyncManager manager,
		int clientChanges,
		int clientApplied,
		int serverChanges,
		int serverApplied)
	{
		AssertSide(session.StatisticsForClient, clientChanges, clientApplied, "returned client");
		AssertSide(session.StatisticsForServer, serverChanges, serverApplied, "returned server");
		AssertSide(manager.SyncSession.StatisticsForClient, clientChanges, clientApplied, "live client");
		AssertSide(manager.SyncSession.StatisticsForServer, serverChanges, serverApplied, "live server");
	}

	protected static string FormatCalls(IReadOnlyList<ServerCall> calls)
	{
		return string.Join(" | ", calls.Select(x =>
			$"changes={x.ChangeCount} end={x.EndSession} skip={x.GetChangesSkip} noPush={x.ClientHasNoChanges} result={x.ResultChangeCount} ended={x.SessionEnded} out={x.OutgoingChanges} applied={x.AppliedChanges}"));
	}

	protected static List<string> Lines(ISampleDatabase database)
	{
		return ReadAll<AddressEntity>(database.Addresses).Select(x => x.Line1).ToList();
	}

	protected SampleSqlDatabaseProvider NewMemoryProvider()
	{
		return new SampleSqlDatabaseProvider(
			$"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared;",
			SqlProvider.Sqlite,
			this
		);
	}

	protected SyncSession SyncOk(SampleSyncManager manager, Action<SyncSettings> update = null)
	{
		var session = manager.Sync(SampleSyncClient.SyncAll, update, TimeSpan.FromSeconds(30));
		IsTrue(session.SyncSuccessful, () => string.Join("; ", session.SyncIssues.Select(x => $"{x.IssueType}:{x.Message}")));
		return session;
	}

	protected void WithRecording(
		Action<ISampleDatabase, ISampleDatabase, SampleSyncManager, List<ServerCall>, SampleSqlDatabaseProvider, SampleSqlDatabaseProvider> test,
		bool asHub = true)
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
			new RecordingServerProvider(serverProvider, this, calls, asHub),
			new SyncSession(this),
			RuntimeInformation,
			this,
			Dispatcher
		);
		manager.GetSyncSettings(SampleSyncClient.SyncAll).IncludeIssueDetails = true;
		test(clientDatabase, serverDatabase, manager, calls, clientProvider, serverProvider);
	}

	private static void AssertSide(SyncStatistics statistics, int changes, int applied, string name)
	{
		AreEqual(changes, statistics.Changes, () => $"{name} {statistics}");
		AreEqual(applied, statistics.AppliedChanges, () => $"{name} {statistics}");
		AreEqual(0, statistics.Corrections, () => $"{name} {statistics}");
		AreEqual(0, statistics.AppliedCorrections, () => $"{name} {statistics}");
		AreEqual(0, statistics.IndividualProcessCount, () => $"{name} {statistics}");
	}

	private static void Record(List<ServerCall> calls, SyncOperation operation, SyncStatistics statistics, SyncOperationResult result)
	{
		var call = new ServerCall();
		call.ChangeCount = operation.Changes?.Collection?.Count ?? 0;
		call.ChangeIds = operation.Changes?.Collection?.Select(x => x.SyncId).ToList() ?? new List<Guid>();
		call.ClientHasNoChanges = operation.ClientHasNoChanges;
		call.EndSession = operation.EndSession;
		call.GetChangesSkip = operation.GetChangesSkip;
		call.IssueCount = operation.Issues?.Collection?.Count ?? 0;
		call.AppliedChanges = statistics.AppliedChanges;
		call.OutgoingChanges = statistics.Changes;
		call.ResultChangeCount = result.Changes?.Collection?.Count ?? 0;
		call.ResultChangeIds = result.Changes?.Collection?.Select(x => x.SyncId).ToList() ?? new List<Guid>();
		call.SessionEnded = result.SessionEnded;
		calls.Add(call);
	}

	#endregion

	#region Classes

	protected sealed class ServerCall
	{
		#region Properties

		public int AppliedChanges { get; set; }

		public int ChangeCount { get; set; }

		public List<Guid> ChangeIds { get; set; }

		public bool ClientHasNoChanges { get; set; }

		public bool EndSession { get; set; }

		public int GetChangesSkip { get; set; }

		public int IssueCount { get; set; }

		public int OutgoingChanges { get; set; }

		public int ResultChangeCount { get; set; }

		public List<Guid> ResultChangeIds { get; set; }

		public bool SessionEnded { get; set; }

		public string Uri { get; set; }

		#endregion
	}

	private sealed class RecordingPeerSyncClient : SampleSyncClient
	{
		#region Fields

		private readonly List<ServerCall> _calls;

		#endregion

		#region Constructors

		public RecordingPeerSyncClient(
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
			Record(_calls, operation, Statistics, result);
			return result;
		}

		#endregion
	}

	private sealed class RecordingServerProvider : ISyncClientProvider
	{
		#region Fields

		private readonly bool _asHub;
		private readonly List<ServerCall> _calls;
		private readonly ISyncableDatabaseProvider _databaseProvider;
		private readonly IDateTimeProvider _dateTimeProvider;

		#endregion

		#region Constructors

		public RecordingServerProvider(
			ISyncableDatabaseProvider databaseProvider,
			IDateTimeProvider dateTimeProvider,
			List<ServerCall> calls,
			bool asHub)
		{
			_asHub = asHub;
			_calls = calls;
			_databaseProvider = databaseProvider;
			_dateTimeProvider = dateTimeProvider;
		}

		#endregion

		#region Methods

		public SyncClient GetSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
		{
			if (_asHub)
			{
				return new RecordingServerSyncClient("Server", _databaseProvider, _dateTimeProvider, syncStatistics, syncClientProfiler, _calls);
			}

			return new RecordingPeerSyncClient("Peer", _databaseProvider, _dateTimeProvider, syncStatistics, syncClientProfiler, _calls);
		}

		public ISyncableDatabase GetSyncableDatabase()
		{
			return _databaseProvider.GetSyncableDatabase();
		}

		#endregion
	}

	private sealed class RecordingServerSyncClient : SampleServerSyncClient
	{
		#region Fields

		private readonly List<ServerCall> _calls;

		#endregion

		#region Constructors

		public RecordingServerSyncClient(
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
			Record(_calls, operation, Statistics, result);
			return result;
		}

		#endregion
	}

	#endregion
}
