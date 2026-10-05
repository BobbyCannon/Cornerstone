#region References

using System;
using System.Collections.Generic;
using Cornerstone.Sample.Models;
using Cornerstone.Sample.Storage;
using Cornerstone.Sample.Sync;
using Cornerstone.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

/// <summary>
/// Tombstones, permanent deletes, and undeletes for pull, push, and pull-then-push.
/// A hub skips an older incoming tombstone. A pull applies a tombstone even when the local row is newer.
/// </summary>
[TestClass]
[DoNotParallelize]
public class SyncDeleteDirectionTests : SyncRecordingTest
{
	#region Methods

	[TestMethod]
	public void BothTombstonesStayDeletedOnPullThenPush()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Seed(server, manager, calls);
			MarkDeleted(client, syncId);
			MarkDeleted(server, syncId, "ServerDrop");

			var session = RunDirection(manager, SyncDirection.PullDownThenPushUp);

			AssertPhases(session, pulling: true, pushing: true);
			AreEqual(2, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: false, clientHasNoChanges: false);
			AssertEndOnly(calls[1]);
			AssertRow(client, syncId, deleted: true, line: "ServerDrop");
			AssertRow(server, syncId, deleted: true, line: "ServerDrop");
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void ClientTombstoneDoesNotDeleteTheKeptRow()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var keep = AddAddress(server, "Keep").SyncId;
			var drop = AddAddress(server, "Drop").SyncId;
			SyncOk(manager);
			calls.Clear();
			MarkDeleted(client, drop);

			var session = RunDirection(manager, SyncDirection.PullDownThenPushUp);

			AssertPhases(session, pulling: true, pushing: true);
			AreEqual(2, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 0, sessionEnded: false, clientHasNoChanges: false);
			AssertPush(calls[1], changeCount: 1, endSession: true);
			AssertRow(client, keep, deleted: false, line: "Keep");
			AssertRow(server, keep, deleted: false, line: "Keep");
			AssertRow(client, drop, deleted: true, line: "Drop");
			AssertRow(server, drop, deleted: true, line: "Drop");
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 1);
		});
	}

	[TestMethod]
	public void ClientTombstonePullDownLeavesTheServerLive()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Seed(server, manager, calls);
			MarkDeleted(client, syncId);

			var session = RunDirection(manager, SyncDirection.PullDown);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 0, sessionEnded: true, clientHasNoChanges: true);
			AssertRow(client, syncId, deleted: true, line: "Home");
			AssertRow(server, syncId, deleted: false, line: "Home");
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 0, serverChanges: 0, serverApplied: 0);
		});
	}

	[TestMethod]
	public void ClientTombstonePullThenPushDeletesTheServer()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Seed(server, manager, calls);
			MarkDeleted(client, syncId);

			var session = RunDirection(manager, SyncDirection.PullDownThenPushUp);

			AssertPhases(session, pulling: true, pushing: true);
			AreEqual(2, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 0, sessionEnded: false, clientHasNoChanges: false);
			AssertPush(calls[1], changeCount: 1, endSession: true);
			AssertRow(client, syncId, deleted: true, line: "Home");
			AssertRow(server, syncId, deleted: true, line: "Home");
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 1);
		});
	}

	[TestMethod]
	public void ClientTombstonePushUpDeletesTheServerWithoutAQuery()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Seed(server, manager, calls);
			MarkDeleted(client, syncId);

			var session = RunDirection(manager, SyncDirection.PushUp);

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: 1, endSession: true);
			AssertRow(client, syncId, deleted: true, line: "Home");
			AssertRow(server, syncId, deleted: true, line: "Home");
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 1);
		});
	}

	[TestMethod]
	public void ClientUndeletePullDownLeavesTheServerDeleted()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = DeletedOnBoth(client, server, manager, calls);
			MarkLive(client, syncId, "Back");

			var session = RunDirection(manager, SyncDirection.PullDown);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 0, sessionEnded: true, clientHasNoChanges: true);
			AssertRow(client, syncId, deleted: false, line: "Back");
			AssertRow(server, syncId, deleted: true, line: "Home");
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 0, serverChanges: 0, serverApplied: 0);
		});
	}

	[TestMethod]
	public void ClientUndeletePullThenPushRestoresTheServer()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = DeletedOnBoth(client, server, manager, calls);
			MarkLive(client, syncId, "Back");

			var session = RunDirection(manager, SyncDirection.PullDownThenPushUp);

			AssertPhases(session, pulling: true, pushing: true);
			AreEqual(2, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 0, sessionEnded: false, clientHasNoChanges: false);
			AssertPush(calls[1], changeCount: 1, endSession: true);
			AssertRow(client, syncId, deleted: false, line: "Back");
			AssertRow(server, syncId, deleted: false, line: "Back");
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 1);
		});
	}

	[TestMethod]
	public void ClientUndeletePushUpRestoresTheServer()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = DeletedOnBoth(client, server, manager, calls);
			MarkLive(client, syncId, "Back");

			var session = RunDirection(manager, SyncDirection.PushUp);

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: 1, endSession: true);
			AssertRow(client, syncId, deleted: false, line: "Back");
			AssertRow(server, syncId, deleted: false, line: "Back");
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 1);
		});
	}

	[TestMethod]
	public void EqualTimestampClientTombstoneDeletesThePeerOnPushUp()
	{
		WithRecording((client, server, manager, calls, clientProvider, serverProvider) =>
		{
			var syncId = Seed(server, manager, calls, "Edited");
			Stamp(clientProvider, serverProvider, client, server, syncId, clientDeleted: true, serverDeleted: false, clientLine: "Edited", serverLine: "Edited");

			var session = RunDirection(manager, SyncDirection.PushUp);

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: 1, endSession: true);
			AssertRow(client, syncId, deleted: true, line: "Edited");
			AssertRow(server, syncId, deleted: true, line: "Edited");
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 1);
		}, asHub: false);
	}

	[TestMethod]
	public void EqualTimestampClientTombstoneDoesNotDeleteTheHubOnPushUp()
	{
		WithRecording((client, server, manager, calls, clientProvider, serverProvider) =>
		{
			var syncId = Seed(server, manager, calls, "Edited");
			Stamp(clientProvider, serverProvider, client, server, syncId, clientDeleted: true, serverDeleted: false, clientLine: "Edited", serverLine: "Edited");

			var session = RunDirection(manager, SyncDirection.PushUp);

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: 1, endSession: true);
			AssertRow(client, syncId, deleted: true, line: "Edited");
			AssertRow(server, syncId, deleted: false, line: "Edited");
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 0);
		});
	}

	[TestMethod]
	public void EqualTimestampServerTombstoneStillDeletesOnPullDown()
	{
		WithRecording((client, server, manager, calls, clientProvider, serverProvider) =>
		{
			var syncId = Seed(server, manager, calls);
			Stamp(clientProvider, serverProvider, client, server, syncId, clientDeleted: false, serverDeleted: true, clientLine: "Edited", serverLine: "Home");

			var session = RunDirection(manager, SyncDirection.PullDown);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			AssertRow(client, syncId, deleted: true, line: "Home");
			AssertRow(server, syncId, deleted: true, line: "Home");
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void HubCreatesATombstoneForAnUnknownRowWhenPermanentDeleteIsCleared()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = AddAddress(client, "Only").SyncId;
			MarkDeleted(client, syncId);

			var session = RunDirection(manager, SyncDirection.PushUp, settings =>
			{
				settings.PermanentDeletions = true;
				settings.AddFilter<AddressEntity>(skipDeletedItemsOnInitialSync: false);
			});

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: 1, endSession: true);
			IsFalse(session.Settings.PermanentDeletions);
			AssertRow(server, syncId, deleted: true, line: "Only");
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 1);
		});
	}

	[TestMethod]
	public void HubPermanentDeleteOnPullKeepsTheClientTombstone()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Seed(server, manager, calls);
			MarkDeleted(server, syncId);

			var session = RunDirection(manager, SyncDirection.PullDown, settings => settings.PermanentDeletions = true);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			IsFalse(session.Settings.PermanentDeletions);
			AssertRow(client, syncId, deleted: true, line: "Home");
			AssertRow(server, syncId, deleted: true, line: "Home");
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void HubPermanentDeleteStillSoftDeletesOnPullThenPush()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Seed(server, manager, calls);
			MarkDeleted(client, syncId);

			var session = RunDirection(manager, SyncDirection.PullDownThenPushUp, settings => settings.PermanentDeletions = true);

			AssertPhases(session, pulling: true, pushing: true);
			AreEqual(2, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 0, sessionEnded: false, clientHasNoChanges: false);
			AssertPush(calls[1], changeCount: 1, endSession: true);
			IsFalse(session.Settings.PermanentDeletions);
			AssertRow(client, syncId, deleted: true, line: "Home");
			AssertRow(server, syncId, deleted: true, line: "Home");
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 1);
		});
	}

	[TestMethod]
	public void HubPermanentDeleteStillSoftDeletesOnPushUp()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Seed(server, manager, calls);
			MarkDeleted(client, syncId);

			var session = RunDirection(manager, SyncDirection.PushUp, settings => settings.PermanentDeletions = true);

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: 1, endSession: true);
			IsFalse(session.Settings.PermanentDeletions);
			AssertRow(server, syncId, deleted: true, line: "Home");
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 1);
		});
	}

	[TestMethod]
	public void InitialClientTombstoneIsCreatedOnTheServerWhenSkipIsOff()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = AddAddress(client, "Gone").SyncId;
			MarkDeleted(client, syncId);

			var session = RunDirection(manager, SyncDirection.PullDownThenPushUp, SkipDeletedOff);

			AssertPhases(session, pulling: true, pushing: true);
			AreEqual(2, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 0, sessionEnded: false, clientHasNoChanges: false);
			AssertPush(calls[1], changeCount: 1, endSession: true);
			AssertRow(server, syncId, deleted: true, line: "Gone");
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 1);
		});
	}

	[TestMethod]
	public void InitialClientTombstoneIsNotPushedOnPullDownWhenSkipIsOff()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = AddAddress(client, "Gone").SyncId;
			MarkDeleted(client, syncId);

			var session = RunDirection(manager, SyncDirection.PullDown, SkipDeletedOff);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 0, sessionEnded: true, clientHasNoChanges: true);
			AssertGone(server, syncId);
			AssertRow(client, syncId, deleted: true, line: "Gone");
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 0, serverChanges: 0, serverApplied: 0);
		});
	}

	[TestMethod]
	public void InitialClientTombstoneIsSkippedOnPullDown()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = AddAddress(client, "Gone").SyncId;
			MarkDeleted(client, syncId);

			var session = RunDirection(manager, SyncDirection.PullDown);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 0, sessionEnded: true, clientHasNoChanges: true);
			AssertGone(server, syncId);
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 0, serverChanges: 0, serverApplied: 0);
		});
	}

	[TestMethod]
	public void InitialClientTombstoneIsSkippedOnPullThenPush()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = AddAddress(client, "Gone").SyncId;
			MarkDeleted(client, syncId);

			var session = RunDirection(manager, SyncDirection.PullDownThenPushUp);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 0, sessionEnded: true, clientHasNoChanges: true);
			AssertGone(server, syncId);
			AssertRow(client, syncId, deleted: true, line: "Gone");
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 0, serverChanges: 0, serverApplied: 0);
		});
	}

	[TestMethod]
	public void InitialClientTombstoneIsSkippedOnPushUp()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = AddAddress(client, "Gone").SyncId;
			MarkDeleted(client, syncId);

			var session = RunDirection(manager, SyncDirection.PushUp);

			AssertPhases(session, pulling: false, pushing: false);
			AreEqual(0, calls.Count, () => FormatCalls(calls));
			AssertGone(server, syncId);
			AssertRow(client, syncId, deleted: true, line: "Gone");
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 0, serverChanges: 0, serverApplied: 0);
		});
	}

	[TestMethod]
	public void InitialServerTombstoneIsCreatedOnTheClientWhenSkipIsOff()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = AddAddress(server, "Gone").SyncId;
			MarkDeleted(server, syncId);

			var session = RunDirection(manager, SyncDirection.PullDownThenPushUp, SkipDeletedOff);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			AssertRow(client, syncId, deleted: true, line: "Gone");
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void IncomingDeleteTombstoneKeepsCreatedOn()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var address = AddAddress(server, "Gone");
			var createdOn = address.CreatedOn;
			MarkDeleted(server, address.SyncId);

			RunDirection(manager, SyncDirection.PullDown, SkipDeletedOff);

			var stored = (AddressEntity) client.Addresses.Read(address.SyncId);
			IsNotNull(stored);
			IsTrue(stored.IsDeleted);
			AreEqual(createdOn, stored.CreatedOn);
		});
	}

	[TestMethod]
	public void InitialServerTombstoneIsNotPulledOnPushUpWhenSkipIsOff()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = AddAddress(server, "Gone").SyncId;
			MarkDeleted(server, syncId);

			var session = RunDirection(manager, SyncDirection.PushUp, SkipDeletedOff);

			AssertPhases(session, pulling: false, pushing: false);
			AreEqual(0, calls.Count, () => FormatCalls(calls));
			AssertGone(client, syncId);
			AssertRow(server, syncId, deleted: true, line: "Gone");
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 0, serverChanges: 0, serverApplied: 0);
		});
	}

	[TestMethod]
	public void InitialServerTombstoneIsSkippedOnPullDown()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = AddAddress(server, "Gone").SyncId;
			MarkDeleted(server, syncId);

			var session = RunDirection(manager, SyncDirection.PullDown);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 0, sessionEnded: true, clientHasNoChanges: true);
			AssertGone(client, syncId);
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 0, serverChanges: 0, serverApplied: 0);
		});
	}

	[TestMethod]
	public void InitialServerTombstoneIsSkippedOnPullThenPush()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = AddAddress(server, "Gone").SyncId;
			MarkDeleted(server, syncId);

			var session = RunDirection(manager, SyncDirection.PullDownThenPushUp);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 0, sessionEnded: true, clientHasNoChanges: true);
			AssertGone(client, syncId);
			AssertRow(server, syncId, deleted: true, line: "Gone");
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 0, serverChanges: 0, serverApplied: 0);
		});
	}

	[TestMethod]
	public void InitialServerTombstoneIsSkippedOnPushUp()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = AddAddress(server, "Gone").SyncId;
			MarkDeleted(server, syncId);

			var session = RunDirection(manager, SyncDirection.PushUp);

			AssertPhases(session, pulling: false, pushing: false);
			AreEqual(0, calls.Count, () => FormatCalls(calls));
			AssertGone(client, syncId);
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 0, serverChanges: 0, serverApplied: 0);
		});
	}

	[TestMethod]
	public void NewerClientEditLosesToServerTombstoneOnPullDown()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Seed(server, manager, calls);
			MarkDeleted(server, syncId);
			MarkLive(client, syncId, "Edited");

			var session = RunDirection(manager, SyncDirection.PullDown);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			AssertRow(client, syncId, deleted: true, line: "Home");
			AssertRow(server, syncId, deleted: true, line: "Home");
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void NewerClientEditLosesToServerTombstoneOnPullThenPush()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Seed(server, manager, calls);
			MarkDeleted(server, syncId);
			MarkLive(client, syncId, "Edited");

			var session = RunDirection(manager, SyncDirection.PullDownThenPushUp);

			AssertPhases(session, pulling: true, pushing: true);
			AreEqual(2, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: false, clientHasNoChanges: false);
			AssertEndOnly(calls[1]);
			AssertRow(client, syncId, deleted: true, line: "Home");
			AssertRow(server, syncId, deleted: true, line: "Home");
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void NewerClientEditRestoresTheServerOnPushUp()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Seed(server, manager, calls);
			MarkDeleted(server, syncId);
			MarkLive(client, syncId, "Edited");

			var session = RunDirection(manager, SyncDirection.PushUp);

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: 1, endSession: true);
			AssertRow(client, syncId, deleted: false, line: "Edited");
			AssertRow(server, syncId, deleted: false, line: "Edited");
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 1);
		});
	}

	[TestMethod]
	public void NewerClientTombstoneDeletesAnOlderServerUndeleteOnPullThenPush()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = DeletedOnBoth(client, server, manager, calls);
			MarkLive(server, syncId, "Back");
			MarkDeleted(client, syncId);

			var session = RunDirection(manager, SyncDirection.PullDownThenPushUp);

			AssertPhases(session, pulling: true, pushing: true);
			AreEqual(2, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: false, clientHasNoChanges: false);
			AssertPush(calls[1], changeCount: 1, endSession: true);
			AssertRow(client, syncId, deleted: true, line: "Home");
			AssertRow(server, syncId, deleted: true, line: "Home");
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 1, serverApplied: 1);
		});
	}

	[TestMethod]
	public void NewerClientUndeleteLosesToOlderServerTombstoneOnPullDown()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Seed(server, manager, calls);
			MarkDeleted(server, syncId);
			MarkDeleted(client, syncId);
			MarkLive(client, syncId, "Back");

			var session = RunDirection(manager, SyncDirection.PullDown);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			AssertRow(client, syncId, deleted: true, line: "Home");
			AssertRow(server, syncId, deleted: true, line: "Home");
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void NewerClientUndeleteLosesToOlderServerTombstoneOnPullThenPush()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Seed(server, manager, calls);
			MarkDeleted(server, syncId);
			MarkDeleted(client, syncId);
			MarkLive(client, syncId, "Back");

			var session = RunDirection(manager, SyncDirection.PullDownThenPushUp);

			AssertPhases(session, pulling: true, pushing: true);
			AreEqual(2, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: false, clientHasNoChanges: false);
			AssertEndOnly(calls[1]);
			AssertRow(client, syncId, deleted: true, line: "Home");
			AssertRow(server, syncId, deleted: true, line: "Home");
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void NewerClientUndeleteRestoresTheServerOnPushUp()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Seed(server, manager, calls);
			MarkDeleted(server, syncId);
			MarkDeleted(client, syncId);
			MarkLive(client, syncId, "Back");

			var session = RunDirection(manager, SyncDirection.PushUp);

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: 1, endSession: true);
			AssertRow(client, syncId, deleted: false, line: "Back");
			AssertRow(server, syncId, deleted: false, line: "Back");
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 1);
		});
	}

	[TestMethod]
	public void NewerServerEditDropsTheClientTombstoneOnPullDown()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Seed(server, manager, calls);
			MarkDeleted(client, syncId);
			MarkLive(server, syncId, "ServerEdit");

			var session = RunDirection(manager, SyncDirection.PullDown);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			AssertRow(client, syncId, deleted: false, line: "ServerEdit");
			AssertRow(server, syncId, deleted: false, line: "ServerEdit");
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void NewerServerEditDropsTheClientTombstoneOnPullThenPush()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Seed(server, manager, calls);
			MarkDeleted(client, syncId);
			MarkLive(server, syncId, "ServerEdit");

			var session = RunDirection(manager, SyncDirection.PullDownThenPushUp);

			AssertPhases(session, pulling: true, pushing: true);
			AreEqual(2, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: false, clientHasNoChanges: false);
			AssertEndOnly(calls[1]);
			AssertRow(client, syncId, deleted: false, line: "ServerEdit");
			AssertRow(server, syncId, deleted: false, line: "ServerEdit");
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void NewerServerUndeleteDropsTheClientTombstoneOnPullThenPush()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = DeletedOnBoth(client, server, manager, calls);
			MarkDeleted(client, syncId);
			MarkLive(server, syncId, "Back");

			var session = RunDirection(manager, SyncDirection.PullDownThenPushUp);

			AssertPhases(session, pulling: true, pushing: true);
			AreEqual(2, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: false, clientHasNoChanges: false);
			AssertEndOnly(calls[1]);
			AssertRow(client, syncId, deleted: false, line: "Back");
			AssertRow(server, syncId, deleted: false, line: "Back");
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void OlderClientTombstoneDeletesANewerPeerOnPushUp()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Seed(server, manager, calls);
			MarkDeleted(client, syncId);
			MarkLive(server, syncId, "Newer");

			var session = RunDirection(manager, SyncDirection.PushUp);

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: 1, endSession: true);
			AssertRow(client, syncId, deleted: true, line: "Home");
			AssertRow(server, syncId, deleted: true, line: "Home");
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 1);
		}, asHub: false);
	}

	[TestMethod]
	public void OlderClientTombstoneDoesNotDeleteANewerHubOnPushUp()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Seed(server, manager, calls);
			MarkDeleted(client, syncId);
			MarkLive(server, syncId, "Newer");

			var session = RunDirection(manager, SyncDirection.PushUp);

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: 1, endSession: true);
			AssertRow(client, syncId, deleted: true, line: "Home");
			AssertRow(server, syncId, deleted: false, line: "Newer");
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 0);
		});
	}

	[TestMethod]
	public void PagedClientTombstonesPushUp()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var first = Seed(server, manager, calls, "One");
			var second = AddAddress(server, "Two").SyncId;
			SyncOk(manager);
			calls.Clear();
			MarkDeleted(client, first);
			MarkDeleted(client, second);

			var session = RunDirection(manager, SyncDirection.PushUp, settings => settings.ItemsPerSyncRequest = 1);

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(3, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: 1, endSession: false);
			AssertPush(calls[1], changeCount: 1, endSession: false);
			AssertEndOnly(calls[2]);
			AssertRow(server, first, deleted: true, line: "One");
			AssertRow(server, second, deleted: true, line: "Two");
			AssertStatistics(session, manager, clientChanges: 2, clientApplied: 0, serverChanges: 0, serverApplied: 2);
		});
	}

	[TestMethod]
	public void PagedServerTombstonesPullDown()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var first = Seed(server, manager, calls, "One");
			var second = AddAddress(server, "Two").SyncId;
			SyncOk(manager);
			calls.Clear();
			MarkDeleted(server, first);
			MarkDeleted(server, second);

			var session = RunDirection(manager, SyncDirection.PullDown, settings => settings.ItemsPerSyncRequest = 1);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(2, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: false, clientHasNoChanges: true);
			AssertPull(calls[1], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			AssertRow(client, first, deleted: true, line: "One");
			AssertRow(client, second, deleted: true, line: "Two");
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 2, serverChanges: 2, serverApplied: 0);
		});
	}

	[TestMethod]
	public void PeerPermanentDeleteOfAnUnknownRowWritesNothing()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = AddAddress(client, "Only").SyncId;
			MarkDeleted(client, syncId);

			var session = RunDirection(manager, SyncDirection.PushUp, settings =>
			{
				settings.PermanentDeletions = true;
				settings.AddFilter<AddressEntity>(skipDeletedItemsOnInitialSync: false);
			});

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: 1, endSession: true);
			IsTrue(session.Settings.PermanentDeletions);
			AssertGone(server, syncId);
			AssertRow(client, syncId, deleted: true, line: "Only");
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 0);
		}, asHub: false);
	}

	[TestMethod]
	public void PeerPermanentDeleteRemovesTheClientRowOnPullDown()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Seed(server, manager, calls);
			MarkDeleted(server, syncId);

			var session = RunDirection(manager, SyncDirection.PullDown, settings => settings.PermanentDeletions = true);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			IsTrue(session.Settings.PermanentDeletions);
			AssertGone(client, syncId);
			AssertRow(server, syncId, deleted: true, line: "Home");
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		}, asHub: false);
	}

	[TestMethod]
	public void PeerPermanentDeleteRemovesTheServerRowOnPushUp()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Seed(server, manager, calls);
			MarkDeleted(client, syncId);

			var session = RunDirection(manager, SyncDirection.PushUp, settings => settings.PermanentDeletions = true);

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: 1, endSession: true);
			IsTrue(session.Settings.PermanentDeletions);
			AssertRow(client, syncId, deleted: true, line: "Home");
			AssertGone(server, syncId);
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 1);
		}, asHub: false);
	}

	[TestMethod]
	public void ServerTombstonePullDownDeletesTheClient()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Seed(server, manager, calls);
			MarkDeleted(server, syncId);

			var session = RunDirection(manager, SyncDirection.PullDown);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			AssertRow(client, syncId, deleted: true, line: "Home");
			AssertRow(server, syncId, deleted: true, line: "Home");
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void ServerTombstonePullThenPushDeletesTheClient()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Seed(server, manager, calls);
			MarkDeleted(server, syncId);

			var session = RunDirection(manager, SyncDirection.PullDownThenPushUp);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			AssertRow(client, syncId, deleted: true, line: "Home");
			AssertRow(server, syncId, deleted: true, line: "Home");
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void ServerTombstonePushUpLeavesTheClientLive()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = Seed(server, manager, calls);
			MarkDeleted(server, syncId);

			var session = RunDirection(manager, SyncDirection.PushUp);

			AssertPhases(session, pulling: false, pushing: false);
			AreEqual(0, calls.Count, () => FormatCalls(calls));
			AssertRow(client, syncId, deleted: false, line: "Home");
			AssertRow(server, syncId, deleted: true, line: "Home");
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 0, serverChanges: 0, serverApplied: 0);
		});
	}

	[TestMethod]
	public void ServerUndeletePullDownRestoresTheClient()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = DeletedOnBoth(client, server, manager, calls);
			MarkLive(server, syncId, "Back");

			var session = RunDirection(manager, SyncDirection.PullDown);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			AssertRow(client, syncId, deleted: false, line: "Back");
			AssertRow(server, syncId, deleted: false, line: "Back");
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void ServerUndeletePullThenPushRestoresTheClient()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = DeletedOnBoth(client, server, manager, calls);
			MarkLive(server, syncId, "Back");

			var session = RunDirection(manager, SyncDirection.PullDownThenPushUp);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			AssertRow(client, syncId, deleted: false, line: "Back");
			AssertRow(server, syncId, deleted: false, line: "Back");
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void ServerUndeletePushUpLeavesTheClientDeleted()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var syncId = DeletedOnBoth(client, server, manager, calls);
			MarkLive(server, syncId, "Back");

			var session = RunDirection(manager, SyncDirection.PushUp);

			AssertPhases(session, pulling: false, pushing: false);
			AreEqual(0, calls.Count, () => FormatCalls(calls));
			AssertRow(client, syncId, deleted: true, line: "Home");
			AssertRow(server, syncId, deleted: false, line: "Back");
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 0, serverChanges: 0, serverApplied: 0);
		});
	}

	private static void AssertGone(ISampleDatabase database, Guid syncId)
	{
		IsNull(database.Addresses.Read(syncId));
	}

	private static void AssertRow(ISampleDatabase database, Guid syncId, bool deleted, string line)
	{
		var stored = (AddressEntity) database.Addresses.Read(syncId);
		IsNotNull(stored);
		AreEqual(deleted, stored.IsDeleted);
		AreEqual(line, stored.Line1);
	}

	private static void SkipDeletedOff(SyncSettings settings)
	{
		settings.AddFilter<AddressEntity>(skipDeletedItemsOnInitialSync: false);
	}

	private Guid DeletedOnBoth(ISampleDatabase client, ISampleDatabase server, SampleSyncManager manager, List<ServerCall> calls)
	{
		var syncId = Seed(server, manager, calls);
		MarkDeleted(server, syncId);
		SyncOk(manager);
		calls.Clear();
		return syncId;
	}

	private void MarkDeleted(ISampleDatabase database, Guid syncId, string line = null)
	{
		var stored = (AddressEntity) database.Addresses.Read(syncId);
		stored.IsDeleted = true;
		if (line != null)
		{
			stored.Line1 = line;
		}

		stored.ModifiedOn = UtcNow;
		Save(database, stored);
	}

	private void MarkLive(ISampleDatabase database, Guid syncId, string line)
	{
		var stored = (AddressEntity) database.Addresses.Read(syncId);
		stored.IsDeleted = false;
		stored.Line1 = line;
		stored.ModifiedOn = UtcNow;
		Save(database, stored);
	}

	private Guid Seed(ISampleDatabase server, SampleSyncManager manager, List<ServerCall> calls, string line = "Home")
	{
		var syncId = AddAddress(server, line).SyncId;
		SyncOk(manager);
		calls.Clear();
		return syncId;
	}

	private void Stamp(
		SampleSqlDatabaseProvider clientProvider,
		SampleSqlDatabaseProvider serverProvider,
		ISampleDatabase client,
		ISampleDatabase server,
		Guid syncId,
		bool clientDeleted,
		bool serverDeleted,
		string clientLine,
		string serverLine)
	{
		clientProvider.Settings.MaintainCreatedOn = false;
		clientProvider.Settings.MaintainModifiedOn = false;
		serverProvider.Settings.MaintainCreatedOn = false;
		serverProvider.Settings.MaintainModifiedOn = false;
		var when = UtcNow;
		var clientRow = (AddressEntity) client.Addresses.Read(syncId);
		clientRow.IsDeleted = clientDeleted;
		clientRow.Line1 = clientLine;
		clientRow.ModifiedOn = when;
		Save(client, clientRow);
		var serverRow = (AddressEntity) server.Addresses.Read(syncId);
		serverRow.IsDeleted = serverDeleted;
		serverRow.Line1 = serverLine;
		serverRow.ModifiedOn = when;
		Save(server, serverRow);
	}

	private SyncSession RunDirection(SampleSyncManager manager, SyncDirection direction, Action<SyncSettings> update = null)
	{
		return SyncOk(manager, settings =>
		{
			settings.SyncDirection = direction;
			update?.Invoke(settings);
		});
	}

	#endregion
}
