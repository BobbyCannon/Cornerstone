#region References

using System;
using System.Collections.Generic;
using System.Linq;
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
/// Session server calls go through WebSyncClient. Each call is one post, and each post is one inner server Sync.
/// </summary>
[TestClass]
[DoNotParallelize]
public class SyncWebHopTests : SyncRecordingTest
{
	#region Methods

	[TestMethod]
	public void IncludeIssueDetailsIsClearedOnTheServer()
	{
		WithWebHop((client, server, manager, posts, inner) =>
		{
			AddAddress(client, "Home");

			var session = RunDirection(manager, SyncDirection.PushUp, settings => settings.IncludeIssueDetails = true);

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(1, posts.Count);
			AreEqual(1, inner.Count);
			IsFalse(session.Settings.IncludeIssueDetails);
			IsNotNull(server.Addresses.Read(posts[0].ChangeIds[0]));
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 1);
		});
	}

	[TestMethod]
	public void PagedPullIsOnePostPerPage()
	{
		WithWebHop((client, server, manager, posts, inner) =>
		{
			AddAddress(server, "One");
			AddAddress(server, "Two");
			AddAddress(client, "Local");

			var session = RunDirection(manager, SyncDirection.PullDown, settings => settings.ItemsPerSyncRequest = 1);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(2, posts.Count, () => FormatCalls(posts));
			AreEqual(2, inner.Count);
			AssertPull(posts[0], resultChanges: 1, sessionEnded: false, clientHasNoChanges: true);
			AssertPull(posts[1], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			AreEqual(posts[0].ChangeCount, inner[0].ChangeCount);
			AreEqual(posts[1].SessionEnded, inner[1].SessionEnded);
			AreEqual(3, ReadAll<AddressEntity>(client.Addresses).Count);
			AreEqual(2, ReadAll<AddressEntity>(server.Addresses).Count);
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 2, serverChanges: 2, serverApplied: 0);
		});
	}

	[TestMethod]
	public void PermanentDeleteStaysSoftThroughTheHop()
	{
		WithWebHop((client, server, manager, posts, inner) =>
		{
			var address = AddAddress(client, "Home");
			RunDirection(manager, SyncDirection.PushUp);
			posts.Clear();
			inner.Clear();
			var stored = (AddressEntity) client.Addresses.Read(address.SyncId);
			stored.IsDeleted = true;
			stored.ModifiedOn = UtcNow;
			Save(client, stored);

			var session = RunDirection(manager, SyncDirection.PushUp, settings => settings.PermanentDeletions = true);

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(1, posts.Count);
			AreEqual(1, inner.Count);
			IsFalse(session.Settings.PermanentDeletions);
			var serverAddress = (AddressEntity) server.Addresses.Read(address.SyncId);
			IsNotNull(serverAddress);
			IsTrue(serverAddress.IsDeleted);
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 1);
		});
	}

	[TestMethod]
	public void PullDownIsOnePostAndOneInnerSync()
	{
		WithWebHop((client, server, manager, posts, inner) =>
		{
			AddAddress(server, "FromServer");
			AddAddress(client, "FromClient");

			var session = RunDirection(manager, SyncDirection.PullDown);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, posts.Count, () => FormatCalls(posts));
			AreEqual(1, inner.Count);
			AreEqual("api/Sync", posts[0].Uri);
			AssertPull(posts[0], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			AssertPull(inner[0], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			AreEqual(0, inner[0].AppliedChanges);
			AreEqual(1, inner[0].OutgoingChanges);
			CollectionAssert.AreEquivalent(new[] { "FromClient", "FromServer" }, Lines(client));
			CollectionAssert.AreEquivalent(new[] { "FromServer" }, Lines(server));
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void PullThenPushIsTwoPostsWhenBothSidesChanged()
	{
		WithWebHop((client, server, manager, posts, inner) =>
		{
			AddAddress(server, "FromServer");
			var clientOnly = AddAddress(client, "FromClient");

			var session = RunDirection(manager, SyncDirection.PullDownThenPushUp);

			AssertPhases(session, pulling: true, pushing: true);
			AreEqual(2, posts.Count, () => FormatCalls(posts));
			AreEqual(2, inner.Count);
			AssertPull(posts[0], resultChanges: 1, sessionEnded: false, clientHasNoChanges: false);
			AssertPush(posts[1], changeCount: 1, endSession: true);
			AreEqual(clientOnly.SyncId, posts[1].ChangeIds[0]);
			AreEqual(posts[0].ResultChangeCount, inner[0].ResultChangeCount);
			AreEqual(posts[1].ChangeCount, inner[1].ChangeCount);
			AreEqual(1, inner[0].OutgoingChanges);
			AreEqual(inner[0].OutgoingChanges, inner[1].OutgoingChanges);
			AreEqual(1, inner[1].AppliedChanges);
			CollectionAssert.AreEquivalent(new[] { "FromClient", "FromServer" }, Lines(client));
			CollectionAssert.AreEquivalent(new[] { "FromClient", "FromServer" }, Lines(server));
			AssertStatistics(session, manager, clientChanges: 2, clientApplied: 1, serverChanges: 1, serverApplied: 1);
		});
	}

	[TestMethod]
	public void PullThenPushWithNothingToPushIsOnePost()
	{
		WithWebHop((client, server, manager, posts, inner) =>
		{
			AddAddress(server, "FromServer");

			var session = RunDirection(manager, SyncDirection.PullDownThenPushUp);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, posts.Count, () => FormatCalls(posts));
			AreEqual(1, inner.Count);
			AssertPull(posts[0], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			AreEqual(1, ReadAll<AddressEntity>(client.Addresses).Count);
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void PushUpIsOnePostAndDoesNotQuery()
	{
		WithWebHop((client, server, manager, posts, inner) =>
		{
			var local = AddAddress(client, "FromClient");
			AddAddress(server, "FromServer");

			var session = RunDirection(manager, SyncDirection.PushUp);

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(1, posts.Count, () => FormatCalls(posts));
			AreEqual(1, inner.Count);
			AreEqual("api/Sync", posts[0].Uri);
			AssertPush(posts[0], changeCount: 1, endSession: true);
			AreEqual(local.SyncId, inner[0].ChangeIds[0]);
			AreEqual(0, inner[0].OutgoingChanges);
			AreEqual(1, inner[0].AppliedChanges);
			CollectionAssert.AreEquivalent(new[] { "FromClient" }, Lines(client));
			CollectionAssert.AreEquivalent(new[] { "FromClient", "FromServer" }, Lines(server));
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 1);
		});
	}

	[TestMethod]
	public void PushUpWithNothingToSendDoesNotPost()
	{
		WithWebHop((client, server, manager, posts, inner) =>
		{
			AddAddress(server, "FromServer");

			var session = RunDirection(manager, SyncDirection.PushUp);

			AssertPhases(session, pulling: false, pushing: false);
			AreEqual(0, posts.Count);
			AreEqual(0, inner.Count);
			AreEqual(0, ReadAll<AddressEntity>(client.Addresses).Count);
			AreEqual(1, ReadAll<AddressEntity>(server.Addresses).Count);
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 0, serverChanges: 0, serverApplied: 0);
		});
	}

	private SyncSession RunDirection(SampleSyncManager manager, SyncDirection direction, Action<SyncSettings> update = null)
	{
		var session = manager.Sync(SampleSyncClient.SyncAll, settings =>
		{
			settings.SyncDirection = direction;
			update?.Invoke(settings);
		}, TimeSpan.FromSeconds(30));
		IsTrue(session.SyncSuccessful, () => string.Join("; ", session.SyncIssues.Select(x => $"{x.IssueType}:{x.Message}")));
		return session;
	}

	private void WithWebHop(Action<ISampleDatabase, ISampleDatabase, SampleSyncManager, List<ServerCall>, List<ServerCall>> test)
	{
		var posts = new List<ServerCall>();
		var inner = new List<ServerCall>();
		var clientProvider = NewMemoryProvider();
		var serverProvider = NewMemoryProvider();
		using var clientDatabase = (ISampleDatabase) clientProvider.GetSyncableDatabase();
		using var serverDatabase = (ISampleDatabase) serverProvider.GetSyncableDatabase();
		clientDatabase.Migrate();
		serverDatabase.Migrate();
		var manager = new SampleSyncManager(
			new SampleSyncClientProvider("Client", clientProvider, this),
			new CountingWebServerProvider(serverProvider, this, posts, inner),
			new SyncSession(this),
			RuntimeInformation,
			this,
			Dispatcher
		);
		manager.GetSyncSettings(SampleSyncClient.SyncAll).IncludeIssueDetails = true;
		test(clientDatabase, serverDatabase, manager, posts, inner);
	}

	#endregion

	#region Classes

	private sealed class CountingInnerSyncClient : SampleServerSyncClient
	{
		#region Fields

		private readonly List<ServerCall> _calls;

		#endregion

		#region Constructors

		public CountingInnerSyncClient(
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
			var call = Describe(operation, result, null);
			call.AppliedChanges = Statistics.AppliedChanges;
			call.OutgoingChanges = Statistics.Changes;
			_calls.Add(call);
			return result;
		}

		#endregion
	}

	private sealed class CountingWebClient : SampleWebClient
	{
		#region Fields

		private readonly List<ServerCall> _posts;

		#endregion

		#region Constructors

		public CountingWebClient(SampleServerSyncClient serverClient, List<ServerCall> posts) : base(serverClient)
		{
			_posts = posts;
		}

		#endregion

		#region Methods

		public override TResult Post<TContent, TResult>(string uri, TContent content, TimeSpan? timeout = null)
		{
			var operation = content as SyncOperation;
			var result = base.Post<TContent, TResult>(uri, content, timeout);
			if ((operation != null) && (result is SyncOperationResult typed))
			{
				_posts.Add(Describe(operation, typed, uri));
			}

			return result;
		}

		#endregion
	}

	private sealed class CountingWebServerProvider : ISyncClientProvider
	{
		#region Fields

		private readonly ISyncableDatabaseProvider _databaseProvider;
		private readonly IDateTimeProvider _dateTimeProvider;
		private readonly List<ServerCall> _inner;
		private readonly List<ServerCall> _posts;

		#endregion

		#region Constructors

		public CountingWebServerProvider(
			ISyncableDatabaseProvider databaseProvider,
			IDateTimeProvider dateTimeProvider,
			List<ServerCall> posts,
			List<ServerCall> inner)
		{
			_databaseProvider = databaseProvider;
			_dateTimeProvider = dateTimeProvider;
			_posts = posts;
			_inner = inner;
		}

		#endregion

		#region Methods

		public SyncClient GetSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
		{
			var server = new CountingInnerSyncClient(
				"Server",
				_databaseProvider,
				_dateTimeProvider,
				syncStatistics,
				syncClientProfiler,
				_inner
			);
			return new WebSyncClient(
				"Web",
				_dateTimeProvider,
				_databaseProvider,
				new CountingWebClient(server, _posts),
				profiler: syncClientProfiler
			);
		}

		public ISyncableDatabase GetSyncableDatabase()
		{
			return _databaseProvider.GetSyncableDatabase();
		}

		#endregion
	}

	private static ServerCall Describe(SyncOperation operation, SyncOperationResult result, string uri)
	{
		var call = new ServerCall();
		call.ChangeCount = operation.Changes?.Collection?.Count ?? 0;
		call.ChangeIds = operation.Changes?.Collection?.Select(x => x.SyncId).ToList() ?? [];
		call.ClientHasNoChanges = operation.ClientHasNoChanges;
		call.EndSession = operation.EndSession;
		call.GetChangesSkip = operation.GetChangesSkip;
		call.IssueCount = operation.Issues?.Collection?.Count ?? 0;
		call.ResultChangeCount = result.Changes?.Collection?.Count ?? 0;
		call.ResultChangeIds = result.Changes?.Collection?.Select(x => x.SyncId).ToList() ?? [];
		call.SessionEnded = result.SessionEnded;
		call.Uri = uri;
		if (result.Statistics != null)
		{
			call.AppliedChanges = result.Statistics.AppliedChanges;
			call.OutgoingChanges = result.Statistics.Changes;
		}

		return call;
	}

	#endregion
}
