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
/// Session direction, one server call per page, and the pull-then-push conflict order.
/// The server slot is the sync client these tests count.
/// </summary>
[TestClass]
[DoNotParallelize]
public class SyncDirectionWorkflowTests : SyncRecordingTest
{
	#region Methods

	[TestMethod]
	public void EqualModifiedOnStaysOnEachSideAndIsNotPushed()
	{
		WithRecording((client, server, manager, calls, clientProvider, serverProvider) =>
		{
			clientProvider.Settings.MaintainCreatedOn = false;
			clientProvider.Settings.MaintainModifiedOn = false;
			serverProvider.Settings.MaintainCreatedOn = false;
			serverProvider.Settings.MaintainModifiedOn = false;

			var when = new DateTime(2020, 6, 1, 12, 0, 0, DateTimeKind.Utc);
			var syncId = Guid.NewGuid();
			SetTime(when);
			AddAddress(client, "FromClient", syncId: syncId);
			SetTime(when);
			AddAddress(server, "FromServer", syncId: syncId);

			using var clientCheck = (ISampleDatabase) clientProvider.GetSyncableDatabase();
			using var serverCheck = (ISampleDatabase) serverProvider.GetSyncableDatabase();
			AreEqual(when, ((AddressEntity) clientCheck.Addresses.Read(syncId)).ModifiedOn);
			AreEqual(when, ((AddressEntity) serverCheck.Addresses.Read(syncId)).ModifiedOn);

			// A tie is not applied. The pull still records that SyncId and ModifiedOn, so the client copy is not pushed.
			var session = SyncOk(manager);
			AssertPhases(session, pulling: true, pushing: true);
			AreEqual(2, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: false, clientHasNoChanges: false);
			AssertEndOnly(calls[1]);
			AreEqual(1, calls[1].OutgoingChanges);
			AreEqual(0, calls[1].AppliedChanges);
			AreEqual("FromClient", ((AddressEntity) client.Addresses.Read(syncId)).Line1);
			AreEqual("FromServer", ((AddressEntity) server.Addresses.Read(syncId)).Line1);
			AreEqual(when, ((AddressEntity) client.Addresses.Read(syncId)).ModifiedOn);
			AreEqual(when, ((AddressEntity) server.Addresses.Read(syncId)).ModifiedOn);
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void PullDownHitsTheServerOnce()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			AddAddress(client, "ClientOnly");
			AddAddress(server, "ServerOnly");

			var session = SyncOk(manager, settings => settings.SyncDirection = SyncDirection.PullDown);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			AreEqual(1, calls[0].OutgoingChanges);
			AreEqual(0, calls[0].AppliedChanges);
			CollectionAssert.AreEquivalent(new[] { "ClientOnly", "ServerOnly" }, Lines(client));
			CollectionAssert.AreEquivalent(new[] { "ServerOnly" }, Lines(server));
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void PullDownIncomingFilterRejectsOnApplyAndStillQueriesOnce()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			AddAddress(server, "ServerSc", "SC");
			AddAddress(server, "ServerGa", "GA");

			var session = manager.Sync(SampleSyncClient.SyncAll, settings =>
			{
				settings.SyncDirection = SyncDirection.PullDown;
				settings.AddFilter<AddressEntity>(incomingFilter: x => x.State == "SC");
			}, TimeSpan.FromSeconds(30));

			IsFalse(session.SyncSuccessful);
			IsTrue(session.State.HasFlag(SyncSessionState.Pulling));
			IsFalse(session.State.HasFlag(SyncSessionState.Pushing));
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 2, sessionEnded: true, clientHasNoChanges: true);
			AreEqual(1, session.SyncIssues.Count);
			AreEqual(SyncIssueType.SyncEntityFiltered, session.SyncIssues[0].IssueType);
			CollectionAssert.AreEquivalent(new[] { "ServerSc" }, Lines(client));
			CollectionAssert.AreEquivalent(new[] { "ServerGa", "ServerSc" }, Lines(server));
			AreEqual(DateTime.MinValue, manager.GetSyncSettings(SampleSyncClient.SyncAll).LastSyncedOnClient);
			AreEqual(DateTime.MinValue, manager.GetSyncSettings(SampleSyncClient.SyncAll).LastSyncedOnServer);
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 1, serverChanges: 2, serverApplied: 0);
		});
	}

	[TestMethod]
	public void PullDownOutgoingFilterLimitsThePullAndDoesNotPush()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			AddAddress(client, "ClientGa", "GA");
			AddAddress(server, "ServerSc", "SC");
			AddAddress(server, "ServerGa", "GA");

			var session = SyncOk(manager, settings =>
			{
				settings.SyncDirection = SyncDirection.PullDown;
				settings.AddFilter<AddressEntity>(outgoingFilter: x => x.State == "SC");
			});

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			CollectionAssert.AreEquivalent(new[] { "ClientGa", "ServerSc" }, Lines(client));
			CollectionAssert.AreEquivalent(new[] { "ServerGa", "ServerSc" }, Lines(server));
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void PullDownPagesWithoutAnExtraEndCall()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			AddAddress(client, "ClientOnly");
			AddAddress(server, "One");
			AddAddress(server, "Two");

			var session = SyncOk(manager, settings =>
			{
				settings.SyncDirection = SyncDirection.PullDown;
				settings.ItemsPerSyncRequest = 1;
			});

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(2, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: false, clientHasNoChanges: true);
			AssertPull(calls[1], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			AreEqual(0, calls[0].GetChangesSkip);
			AreEqual(1, calls[1].GetChangesSkip);
			AreEqual(2, calls[1].OutgoingChanges);
			AreEqual(0, calls[1].AppliedChanges);
			CollectionAssert.AreEquivalent(new[] { "ClientOnly", "One", "Two" }, Lines(client));
			CollectionAssert.AreEquivalent(new[] { "One", "Two" }, Lines(server));
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 2, serverChanges: 2, serverApplied: 0);
		});
	}

	[TestMethod]
	public void PullThenPushDropsTheAppliedEchoAndEndsWithoutAnotherQuery()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Guid.NewGuid();
			AddAddress(client, "FromClient", syncId: syncId);
			AddAddress(server, "FromServer", syncId: syncId);

			var session = SyncOk(manager);

			AssertPhases(session, pulling: true, pushing: true);
			AreEqual(2, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: false, clientHasNoChanges: false);
			AssertEndOnly(calls[1]);
			AreEqual(calls[0].OutgoingChanges, calls[1].OutgoingChanges);
			AreEqual(0, calls[1].AppliedChanges);
			AreEqual("FromServer", ((AddressEntity) client.Addresses.Read(syncId)).Line1);
			AreEqual("FromServer", ((AddressEntity) server.Addresses.Read(syncId)).Line1);
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void PullThenPushKeepsNewerClientRowAndStillPullsServerRows()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Guid.NewGuid();
			AddAddress(server, "FromServer", syncId: syncId);
			var clientShared = AddAddress(client, "FromClient", syncId: syncId);
			AddAddress(server, "ServerOnly");
			var clientOnly = AddAddress(client, "ClientOnly");

			var session = SyncOk(manager);

			AssertPhases(session, pulling: true, pushing: true);
			AreEqual(2, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 2, sessionEnded: false, clientHasNoChanges: false);
			AssertPush(calls[1], changeCount: 2, endSession: true);
			CollectionAssert.AreEquivalent(new[] { clientShared.SyncId, clientOnly.SyncId }, calls[1].ChangeIds);
			AreEqual(2, calls[1].AppliedChanges);
			CollectionAssert.AreEquivalent(new[] { "ClientOnly", "FromClient", "ServerOnly" }, Lines(client));
			CollectionAssert.AreEquivalent(new[] { "ClientOnly", "FromClient", "ServerOnly" }, Lines(server));
			AssertStatistics(session, manager, clientChanges: 3, clientApplied: 1, serverChanges: 2, serverApplied: 2);
		});
	}

	[TestMethod]
	public void PullThenPushPagesTheServerThenPushesTheClient()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			AddAddress(server, "One");
			AddAddress(server, "Two");
			var clientOnly = AddAddress(client, "ClientOnly");

			var session = SyncOk(manager, settings => settings.ItemsPerSyncRequest = 1);

			AssertPhases(session, pulling: true, pushing: true);
			AreEqual(4, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: false, clientHasNoChanges: false);
			AssertPull(calls[1], resultChanges: 1, sessionEnded: false, clientHasNoChanges: false);
			AssertPush(calls[2], changeCount: 1, endSession: false);
			AssertEndOnly(calls[3]);
			AreEqual(0, calls[0].GetChangesSkip);
			AreEqual(1, calls[1].GetChangesSkip);
			AreEqual(clientOnly.SyncId, calls[2].ChangeIds[0]);
			AreEqual(2, calls[3].OutgoingChanges);
			AreEqual(1, calls[3].AppliedChanges);
			CollectionAssert.AreEquivalent(new[] { "ClientOnly", "One", "Two" }, Lines(client));
			CollectionAssert.AreEquivalent(new[] { "ClientOnly", "One", "Two" }, Lines(server));
			AssertStatistics(session, manager, clientChanges: 3, clientApplied: 2, serverChanges: 2, serverApplied: 1);
		});
	}

	[TestMethod]
	public void PullThenPushSyncsBothWaysAndServerReplacesOlderClientRow()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Guid.NewGuid();
			AddAddress(client, "FromClient", syncId: syncId);
			AddAddress(server, "FromServer", syncId: syncId);
			AddAddress(server, "ServerOnly");
			var clientOnly = AddAddress(client, "ClientOnly");

			var session = SyncOk(manager);

			AssertPhases(session, pulling: true, pushing: true);
			AreEqual(2, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 2, sessionEnded: false, clientHasNoChanges: false);
			AssertPush(calls[1], changeCount: 1, endSession: true);
			AreEqual(clientOnly.SyncId, calls[1].ChangeIds[0]);
			AreEqual(2, calls[1].OutgoingChanges);
			AreEqual(1, calls[1].AppliedChanges);
			CollectionAssert.AreEquivalent(new[] { "ClientOnly", "FromServer", "ServerOnly" }, Lines(client));
			CollectionAssert.AreEquivalent(new[] { "ClientOnly", "FromServer", "ServerOnly" }, Lines(server));
			AssertStatistics(session, manager, clientChanges: 3, clientApplied: 2, serverChanges: 2, serverApplied: 1);
		});
	}

	[TestMethod]
	public void PullThenPushWithNothingOnTheServerPullsThenPushes()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var clientOnly = AddAddress(client, "ClientOnly");

			var session = SyncOk(manager);

			AssertPhases(session, pulling: true, pushing: true);
			AreEqual(2, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 0, sessionEnded: false, clientHasNoChanges: false);
			AssertPush(calls[1], changeCount: 1, endSession: true);
			AreEqual(clientOnly.SyncId, calls[1].ChangeIds[0]);
			AreEqual(0, calls[1].OutgoingChanges);
			AreEqual(1, calls[1].AppliedChanges);
			CollectionAssert.AreEquivalent(new[] { "ClientOnly" }, Lines(client));
			CollectionAssert.AreEquivalent(new[] { "ClientOnly" }, Lines(server));
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 1);
		});
	}

	[TestMethod]
	public void PullThenPushWithNothingToPushEndsAfterThePull()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			AddAddress(server, "ServerOnly");

			var session = SyncOk(manager);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			AreEqual(0, calls[0].AppliedChanges);
			CollectionAssert.AreEquivalent(new[] { "ServerOnly" }, Lines(client));
			CollectionAssert.AreEquivalent(new[] { "ServerOnly" }, Lines(server));
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void PushUpHitsTheServerOnceAndDoesNotQuery()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var clientOnly = AddAddress(client, "ClientOnly");
			AddAddress(server, "ServerOnly");

			var session = SyncOk(manager, settings => settings.SyncDirection = SyncDirection.PushUp);

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: 1, endSession: true);
			AreEqual(clientOnly.SyncId, calls[0].ChangeIds[0]);
			AreEqual(0, calls[0].OutgoingChanges);
			AreEqual(1, calls[0].AppliedChanges);
			CollectionAssert.AreEquivalent(new[] { "ClientOnly" }, Lines(client));
			CollectionAssert.AreEquivalent(new[] { "ClientOnly", "ServerOnly" }, Lines(server));
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 1);
		});
	}

	[TestMethod]
	public void PushUpOutgoingFilterLimitsThePushAndDoesNotPull()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var clientSc = AddAddress(client, "ClientSc", "SC");
			AddAddress(client, "ClientGa", "GA");
			AddAddress(server, "ServerOnly", "GA");

			var session = SyncOk(manager, settings =>
			{
				settings.SyncDirection = SyncDirection.PushUp;
				settings.AddFilter<AddressEntity>(outgoingFilter: x => x.State == "SC");
			});

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: 1, endSession: true);
			AreEqual(clientSc.SyncId, calls[0].ChangeIds[0]);
			AreEqual(0, calls[0].OutgoingChanges);
			CollectionAssert.AreEquivalent(new[] { "ClientGa", "ClientSc" }, Lines(client));
			CollectionAssert.AreEquivalent(new[] { "ClientSc", "ServerOnly" }, Lines(server));
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 1);
		});
	}

	[TestMethod]
	public void PushUpOutgoingFilterThatExcludesEveryRowDoesNotCallTheServer()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			AddAddress(client, "ClientGa", "GA");
			AddAddress(server, "ServerOnly");

			var session = SyncOk(manager, settings =>
			{
				settings.SyncDirection = SyncDirection.PushUp;
				settings.AddFilter<AddressEntity>(outgoingFilter: x => x.State == "SC");
			});

			AssertPhases(session, pulling: false, pushing: false);
			AreEqual(0, calls.Count, () => FormatCalls(calls));
			CollectionAssert.AreEquivalent(new[] { "ClientGa" }, Lines(client));
			CollectionAssert.AreEquivalent(new[] { "ServerOnly" }, Lines(server));
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 0, serverChanges: 0, serverApplied: 0);
		});
	}

	[TestMethod]
	public void PushUpPagesWithoutQueryingTheServer()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			AddAddress(client, "One");
			AddAddress(client, "Two");
			AddAddress(server, "ServerOnly");

			var session = SyncOk(manager, settings =>
			{
				settings.SyncDirection = SyncDirection.PushUp;
				settings.ItemsPerSyncRequest = 1;
			});

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(3, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: 1, endSession: false);
			AssertPush(calls[1], changeCount: 1, endSession: false);
			AssertEndOnly(calls[2]);
			AreEqual(0, calls[0].OutgoingChanges);
			AreEqual(0, calls[2].OutgoingChanges);
			AreEqual(2, calls[2].AppliedChanges);
			CollectionAssert.AreEquivalent(new[] { "One", "Two" }, Lines(client));
			CollectionAssert.AreEquivalent(new[] { "One", "ServerOnly", "Two" }, Lines(server));
			AssertStatistics(session, manager, clientChanges: 2, clientApplied: 0, serverChanges: 0, serverApplied: 2);
		});
	}

	[TestMethod]
	public void PushUpAboveTheDefaultTakeSendsEveryRow()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			const int count = 1001;
			for (var i = 0; i < count; i++)
			{
				client.Addresses.Add(new AddressEntity
				{
					City = "City",
					CreatedOn = UtcNow,
					ExternalId = Guid.NewGuid(),
					Line1 = "Row" + i,
					ModifiedOn = UtcNow,
					Postal = "29640",
					State = "SC",
					SyncId = Guid.NewGuid()
				});
			}

			Save(client);

			var session = manager.Sync(SampleSyncClient.SyncAll, settings =>
			{
				settings.SyncDirection = SyncDirection.PushUp;
				settings.ItemsPerSyncRequest = count;
			}, TimeSpan.FromMinutes(2));
			IsTrue(session.SyncSuccessful, () => string.Join("; ", session.SyncIssues.Select(x => $"{x.IssueType}:{x.Message}")));

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(2, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: count, endSession: false);
			AssertEndOnly(calls[1]);
			AreEqual(count, ReadAll<AddressEntity>(server.Addresses).Count);
			AssertStatistics(session, manager, clientChanges: count, clientApplied: 0, serverChanges: 0, serverApplied: count);
		});
	}

	[TestMethod]
	public void PushUpWithNothingToSendDoesNotCallTheServer()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			AddAddress(server, "ServerOnly");

			var session = SyncOk(manager, settings => settings.SyncDirection = SyncDirection.PushUp);

			AssertPhases(session, pulling: false, pushing: false);
			AreEqual(0, calls.Count, () => FormatCalls(calls));
			AreEqual(0, Lines(client).Count);
			CollectionAssert.AreEquivalent(new[] { "ServerOnly" }, Lines(server));
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 0, serverChanges: 0, serverApplied: 0);
		});
	}

	#endregion
}
