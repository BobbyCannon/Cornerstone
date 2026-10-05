#region References

using System;
using System.Linq;
using Cornerstone.Sample.Models;
using Cornerstone.Sample.Storage;
using Cornerstone.Sample.Sync;
using Cornerstone.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

/// <summary>
/// A second sync reads only rows whose CreatedOn or ModifiedOn is in [LastSyncedOn, session start).
/// A hub restamps ModifiedOn when it applies a push, so that row is inside the next pull window once.
/// </summary>
[TestClass]
[DoNotParallelize]
public class SyncWindowTests : SyncRecordingTest
{
	#region Methods

	[TestMethod]
	public void FailedSyncDoesNotMoveLastSyncedOn()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			AddAddress(client, "Home");
			var first = RunDirection(manager, SyncDirection.PullDownThenPushUp);
			calls.Clear();
			var synced = Stamps(manager);
			AreEqual(synced.Client, first.Settings.LastSyncedOnClient);
			AreEqual(synced.Server, first.Settings.LastSyncedOnServer);

			IncrementTime(minutes: 1);
			var rejected = AddAddress(client, "Rejected");
			var failed = RunRaw(manager, SyncDirection.PushUp, settings =>
			{
				settings.AddFilter<AddressEntity>(incomingFilter: x => false);
			});

			IsFalse(failed.SyncSuccessful);
			AreEqual(SyncIssueType.SyncEntityFiltered, failed.SyncIssues[0].IssueType);
			IsNull(server.Addresses.Read(rejected.SyncId));
			AreEqual(synced.Client, Stamps(manager).Client);
			AreEqual(synced.Server, Stamps(manager).Server);
			AreEqual(failed.StartedOn, manager.GetSyncSettings(SampleSyncClient.SyncAll).LastSyncAttemptedOn);
			AssertStatistics(failed, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 0);

			calls.Clear();
			var retry = RunDirection(manager, SyncDirection.PushUp);
			IsNotNull(server.Addresses.Read(rejected.SyncId));
			IsTrue(Stamps(manager).Client > synced.Client);
			AreEqual(rejected.SyncId, calls[0].ChangeIds[0]);
			AssertStatistics(retry, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 1);
		});
	}

	[TestMethod]
	public void SecondPullDownReturnsTheHubRestampOnce()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var address = AddAddress(client, "Home");
			RunDirection(manager, SyncDirection.PushUp);
			calls.Clear();
			var first = Stamps(manager);
			IncrementTime(minutes: 1);

			var second = RunDirection(manager, SyncDirection.PullDown);

			AssertPhases(second, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			AreEqual(address.SyncId, calls[0].ResultChangeIds[0]);
			AreEqual("Home", ((AddressEntity) client.Addresses.Read(address.SyncId)).Line1);
			AreEqual("Home", ((AddressEntity) server.Addresses.Read(address.SyncId)).Line1);
			AssertStatistics(second, manager, clientChanges: 0, clientApplied: 1, serverChanges: 1, serverApplied: 0);
			IsTrue(Stamps(manager).Server > first.Server);

			calls.Clear();
			IncrementTime(minutes: 1);
			var third = RunDirection(manager, SyncDirection.PullDown);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 0, sessionEnded: true, clientHasNoChanges: true);
			AssertStatistics(third, manager, clientChanges: 0, clientApplied: 0, serverChanges: 0, serverApplied: 0);
		});
	}

	[TestMethod]
	public void SecondPullDownSendsOnlyTheEditedServerRow()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var keep = AddAddress(server, "Keep");
			var edit = AddAddress(server, "Edit");
			RunDirection(manager, SyncDirection.PullDown);
			calls.Clear();
			Rename(server, edit.SyncId, "Edited");

			var second = RunDirection(manager, SyncDirection.PullDown);

			AssertPhases(second, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			AreEqual(edit.SyncId, calls[0].ResultChangeIds[0]);
			AreEqual("Keep", ((AddressEntity) client.Addresses.Read(keep.SyncId)).Line1);
			AreEqual("Edited", ((AddressEntity) client.Addresses.Read(edit.SyncId)).Line1);
			AssertStatistics(second, manager, clientChanges: 0, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void SecondPullDownSkipsUnchangedServerRows()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var address = AddAddress(server, "Home");
			var first = RunDirection(manager, SyncDirection.PullDown);
			calls.Clear();
			var synced = Stamps(manager);
			AreEqual(synced.Client, first.Settings.LastSyncedOnClient);
			IncrementTime(minutes: 1);

			var second = RunDirection(manager, SyncDirection.PullDown);

			AssertPhases(second, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 0, sessionEnded: true, clientHasNoChanges: true);
			AreEqual("Home", ((AddressEntity) client.Addresses.Read(address.SyncId)).Line1);
			IsTrue(Stamps(manager).Client > synced.Client);
			IsTrue(Stamps(manager).Server > synced.Server);
			AssertStatistics(second, manager, clientChanges: 0, clientApplied: 0, serverChanges: 0, serverApplied: 0);
		});
	}

	[TestMethod]
	public void SecondPullThenPushAppliesTheHubRestampAndDoesNotPushIt()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var address = AddAddress(client, "Home");
			RunDirection(manager, SyncDirection.PullDownThenPushUp);
			calls.Clear();
			IncrementTime(minutes: 1);

			var second = RunDirection(manager, SyncDirection.PullDownThenPushUp);

			AssertPhases(second, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			AreEqual(address.SyncId, calls[0].ResultChangeIds[0]);
			AreEqual("Home", ((AddressEntity) server.Addresses.Read(address.SyncId)).Line1);
			AssertStatistics(second, manager, clientChanges: 0, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void SecondPullThenPushSendsOnlyTheClientEdit()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var keep = AddAddress(client, "Keep");
			var edit = AddAddress(client, "Edit");
			RunDirection(manager, SyncDirection.PullDownThenPushUp);
			calls.Clear();
			IncrementTime(minutes: 1);
			Rename(client, edit.SyncId, "Edited");

			var second = RunDirection(manager, SyncDirection.PullDownThenPushUp);

			AssertPhases(second, pulling: true, pushing: true);
			AreEqual(2, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 2, sessionEnded: false, clientHasNoChanges: false);
			AssertPush(calls[1], changeCount: 1, endSession: true);
			AreEqual(edit.SyncId, calls[1].ChangeIds[0]);
			AreEqual("Keep", ((AddressEntity) server.Addresses.Read(keep.SyncId)).Line1);
			AreEqual("Edited", ((AddressEntity) server.Addresses.Read(edit.SyncId)).Line1);
			AreEqual("Edited", ((AddressEntity) client.Addresses.Read(edit.SyncId)).Line1);
			AssertStatistics(second, manager, clientChanges: 2, clientApplied: 1, serverChanges: 2, serverApplied: 1);
		});
	}

	[TestMethod]
	public void SecondPullThenPushWithNoClockAdvanceIsEmpty()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var address = AddAddress(client, "Home");
			var first = RunDirection(manager, SyncDirection.PullDownThenPushUp);
			calls.Clear();
			var synced = Stamps(manager);
			IsTrue(synced.Client > DateTime.MinValue);
			AreEqual(synced.Client, synced.Server);
			AreEqual(synced.Client, first.Settings.LastSyncedOnClient);

			var second = RunDirection(manager, SyncDirection.PullDownThenPushUp);

			AssertPhases(second, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 0, sessionEnded: true, clientHasNoChanges: true);
			AreEqual(synced.Client, Stamps(manager).Client);
			AreEqual(synced.Server, Stamps(manager).Server);
			AreEqual(1, ReadAll<AddressEntity>(server.Addresses).Count);
			AreEqual("Home", ((AddressEntity) server.Addresses.Read(address.SyncId)).Line1);
			AssertStatistics(second, manager, clientChanges: 0, clientApplied: 0, serverChanges: 0, serverApplied: 0);
		});
	}

	[TestMethod]
	public void SecondPushUpSendsOnlyTheEditedRow()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var keep = AddAddress(client, "Keep");
			var edit = AddAddress(client, "Edit");
			RunDirection(manager, SyncDirection.PushUp);
			calls.Clear();
			Rename(client, edit.SyncId, "Edited");

			var second = RunDirection(manager, SyncDirection.PushUp);

			AssertPhases(second, pulling: false, pushing: true);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: 1, endSession: true);
			AreEqual(edit.SyncId, calls[0].ChangeIds[0]);
			AreEqual("Keep", ((AddressEntity) server.Addresses.Read(keep.SyncId)).Line1);
			AreEqual("Edited", ((AddressEntity) server.Addresses.Read(edit.SyncId)).Line1);
			AssertStatistics(second, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 1);
		});
	}

	[TestMethod]
	public void SecondPushUpSkipsUnchangedClientRows()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var address = AddAddress(client, "Home");
			RunDirection(manager, SyncDirection.PushUp);
			calls.Clear();
			var synced = Stamps(manager);
			IncrementTime(minutes: 1);

			var second = RunDirection(manager, SyncDirection.PushUp);

			AssertPhases(second, pulling: false, pushing: false);
			AreEqual(0, calls.Count, () => FormatCalls(calls));
			AreEqual("Home", ((AddressEntity) server.Addresses.Read(address.SyncId)).Line1);
			IsTrue(Stamps(manager).Client > synced.Client);
			AssertStatistics(second, manager, clientChanges: 0, clientApplied: 0, serverChanges: 0, serverApplied: 0);
		});
	}

	private (DateTime Client, DateTime Server) Stamps(SampleSyncManager manager)
	{
		var settings = manager.GetSyncSettings(SampleSyncClient.SyncAll);
		return (settings.LastSyncedOnClient, settings.LastSyncedOnServer);
	}

	private void Rename(ISampleDatabase database, Guid syncId, string line)
	{
		var stored = (AddressEntity) database.Addresses.Read(syncId);
		stored.Line1 = line;
		stored.ModifiedOn = UtcNow;
		Save(database, stored);
	}

	private SyncSession RunDirection(SampleSyncManager manager, SyncDirection direction, Action<SyncSettings> update = null)
	{
		var session = RunRaw(manager, direction, update);
		IsTrue(session.SyncSuccessful, () => string.Join("; ", session.SyncIssues.Select(x => $"{x.IssueType}:{x.Message}")));
		return session;
	}

	private SyncSession RunRaw(SampleSyncManager manager, SyncDirection direction, Action<SyncSettings> update = null)
	{
		return manager.Sync(SampleSyncClient.SyncAll, settings =>
		{
			settings.SyncDirection = direction;
			update?.Invoke(settings);
		}, TimeSpan.FromSeconds(30));
	}

	#endregion
}
