#region References

using System;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;
using Cornerstone.Compare;
using Cornerstone.Extensions;
using Cornerstone.Logging;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Client.Data;
using Sample.Server.Data;
using Sample.Shared;
using Sample.Shared.Storage.Server;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
public class SyncManagerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void SyncShouldCallPostUpdateEvenIfSyncDoesNotRun()
	{
		var startTime = new DateTime(2020, 04, 23, 01, 55, 21, DateTimeKind.Utc);
		var timeProvider = new DateTimeProvider(() => startTime += TimeSpan.FromSeconds(1));
		var logListener = GetLogListener();
		var manager = GetSyncManager(logListener: logListener, timeProvider: timeProvider);

		// Start a sync that will never complete, that must be cancelled
		var resultsForSync = manager.SyncAllAsync(() => false);
		manager.WaitForSyncToStart();

		var syncPostUpdateCalled = false;
		manager.SyncAccounts(null, null, _ => syncPostUpdateCalled = true);
		Assert.IsTrue(syncPostUpdateCalled, "The sync postUpdate was not called as expected");
		Assert.IsTrue(manager.SyncSession.SyncRunning, "The sync manager should still be running");

		manager.CancelSync();
		manager.WaitForSyncToComplete();

		Assert.IsFalse(manager.SyncSession.SyncRunning, "The sync manager should not be running because it was cancelled");
		Assert.IsTrue(resultsForSync.Result.SyncCancelled, "The sync manager should have been cancelled");

		var expected = new[]
		{
			new[]
			{
				$"2020-04-23T01:55:23.0000000Z - {manager.SyncSession.SessionId} Verbose : Sync All started",
				$"2020-04-23T01:55:24.0000000Z - {manager.SyncSession.SessionId} Verbose : Starting to All sync...",
				$"2020-04-23T01:55:26.0000000Z - {manager.SyncSession.SessionId} Verbose : Cancelling running Sync All...",
				$"2020-04-23T01:55:27.0000000Z - {manager.SyncSession.SessionId} Verbose : The sync session was cancelled.",
				$"2020-04-23T01:55:29.0000000Z - {manager.SyncSession.SessionId} Verbose : Syncing All for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:30.0000000Z - {manager.SyncSession.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:31.0000000Z - {manager.SyncSession.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:32.0000000Z - {manager.SyncSession.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:55:35.0000000Z - {manager.SyncSession.SessionId} Verbose : Sync All completed. 00:11.000"
			},
			new[]
			{
				$"2020-04-23T01:55:23.0000000Z - {manager.SyncSession.SessionId} Verbose : Sync All started",
				$"2020-04-23T01:55:24.0000000Z - {manager.SyncSession.SessionId} Verbose : Starting to All sync...",
				$"2020-04-23T01:55:25.0000000Z - {manager.SyncSession.SessionId} Verbose : Syncing All for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:27.0000000Z - {manager.SyncSession.SessionId} Verbose : Cancelling running Sync All...",
				$"2020-04-23T01:55:28.0000000Z - {manager.SyncSession.SessionId} Verbose : The sync session was cancelled.",
				$"2020-04-23T01:55:30.0000000Z - {manager.SyncSession.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:31.0000000Z - {manager.SyncSession.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:32.0000000Z - {manager.SyncSession.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:55:35.0000000Z - {manager.SyncSession.SessionId} Verbose : Sync All completed. 00:11.000"
			},
			new[]
			{
				$"2020-04-23T01:55:23.0000000Z - {manager.SyncSession.SessionId} Verbose : Sync All started",
				$"2020-04-23T01:55:25.0000000Z - {manager.SyncSession.SessionId} Verbose : Starting to All sync...",
				$"2020-04-23T01:55:26.0000000Z - {manager.SyncSession.SessionId} Verbose : Cancelling running Sync All...",
				$"2020-04-23T01:55:27.0000000Z - {manager.SyncSession.SessionId} Verbose : The sync session was cancelled.",
				$"2020-04-23T01:55:29.0000000Z - {manager.SyncSession.SessionId} Verbose : Syncing All for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:30.0000000Z - {manager.SyncSession.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:31.0000000Z - {manager.SyncSession.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:32.0000000Z - {manager.SyncSession.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:55:35.0000000Z - {manager.SyncSession.SessionId} Verbose : Sync All completed. 00:11.000"
			},
			new[]
			{
				$"2020-04-23T01:55:23.0000000Z - {manager.SyncSession.SessionId} Verbose : Sync All started",
				$"2020-04-23T01:55:24.0000000Z - {manager.SyncSession.SessionId} Verbose : Starting to All sync...",
				$"2020-04-23T01:55:26.0000000Z - {manager.SyncSession.SessionId} Verbose : Syncing All for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:27.0000000Z - {manager.SyncSession.SessionId} Verbose : Cancelling running Sync All...",
				$"2020-04-23T01:55:28.0000000Z - {manager.SyncSession.SessionId} Verbose : The sync session was cancelled.",
				$"2020-04-23T01:55:30.0000000Z - {manager.SyncSession.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:31.0000000Z - {manager.SyncSession.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:32.0000000Z - {manager.SyncSession.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:55:35.0000000Z - {manager.SyncSession.SessionId} Verbose : Sync All completed. 00:11.000"
			},
			new[]
			{
				$"2020-04-23T01:55:23.0000000Z - {manager.SyncSession.SessionId} Verbose : Sync All started",
				$"2020-04-23T01:55:24.0000000Z - {manager.SyncSession.SessionId} Verbose : Starting to All sync...",
				$"2020-04-23T01:55:26.0000000Z - {manager.SyncSession.SessionId} Verbose : Cancelling running Sync All...",
				$"2020-04-23T01:55:27.0000000Z - {manager.SyncSession.SessionId} Verbose : The sync session was cancelled.",
				$"2020-04-23T01:55:28.0000000Z - {manager.SyncSession.SessionId} Verbose : Syncing All for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:30.0000000Z - {manager.SyncSession.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:31.0000000Z - {manager.SyncSession.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:32.0000000Z - {manager.SyncSession.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:55:35.0000000Z - {manager.SyncSession.SessionId} Verbose : Sync All completed. 00:11.000"
			}
		};

		var actual = logListener
			.GetEvents(manager.SyncSession.SessionId)
			.Select(x => x.GetDetailedMessage(includeSessionId: true))
			.ToList();

		actual.ForEach(x => Console.WriteLine($"$\"{x.Replace(manager.SyncSession.SessionId.ToString(), "{manager.SyncSession.SessionId}")}\","));

		var result = expected.Any(x => Compare(x, actual).Result == CompareResult.AreEqual);
		Assert.IsTrue(result, "The log does not match one of the possible orders...");
		Assert.AreEqual(0, manager.SyncTimers["All"].Average.TotalMilliseconds);
		Assert.AreEqual(1, manager.SyncTimers["All"].CancelledSyncs);
		Assert.AreEqual(0, manager.SyncTimers["All"].SuccessfulSyncs);
		Assert.AreEqual(0, manager.SyncTimers["All"].FailedSyncs);
	}

	[TestMethod]
	public void SyncShouldNotLogIfNotVerbose()
	{
		var startTime = new DateTime(2020, 04, 23, 01, 55, 21, DateTimeKind.Utc);
		var timeProvider = new DateTimeProvider(() => startTime += TimeSpan.FromSeconds(1));
		var logListener = GetLogListener(EventLevel.Informational);
		var manager = GetSyncManager(logListener: logListener, timeProvider: timeProvider);

		manager.SyncAccounts();
		manager.WaitForSyncToComplete();

		var expected = Array.Empty<string>();
		var actual = logListener
			.GetEvents(manager.SyncSession.SessionId)
			.Select(x => x.GetDetailedMessage(includeSessionId: true))
			.ToList();

		AreEqual(expected, actual);
		Assert.AreEqual(0, manager.SyncTimers["Accounts"].CancelledSyncs);
		Assert.AreEqual(1, manager.SyncTimers["Accounts"].SuccessfulSyncs);
		Assert.AreEqual(0, manager.SyncTimers["Accounts"].FailedSyncs);
	}

	[TestMethod]
	public void SyncShouldNotSendItemsBackUp()
	{
		var serverDatabaseProvider = GetServerDatabaseProvider();
		using (var serverDatabase = serverDatabaseProvider.GetDatabase())
		{
			var bob = GetModel<AccountEntity>();
			serverDatabase.Accounts.Add(bob);
			serverDatabase.SaveChanges();
		}

		var manager = GetSyncManager(timeProvider: this, serverDatabaseProvider: serverDatabaseProvider);

		IncrementTime(ticks: 1);

		var result = manager.SyncAll();
		AreEqual(0, result.SyncIssues.Count, string.Join("\r\n", result.SyncIssues.Select(x => x.Message)));
		IsTrue(result.SyncSuccessful);

		AreEqual("1,0,1,0,0", result.StatisticsForClient.ToString());
		AreEqual("1,0,0,0,0", result.StatisticsForServer.ToString());
	}

	[TestMethod]
	public void SyncsShouldNotAverageIfCancelled()
	{
		var startTime = new DateTime(2020, 04, 23, 01, 55, 21, DateTimeKind.Utc);
		var timeProvider = new DateTimeProvider(() => startTime += TimeSpan.FromSeconds(1));
		var logListener = GetLogListener();
		var manager = GetSyncManager(logListener: logListener, timeProvider: timeProvider);

		Assert.AreEqual(0, manager.SyncTimers["All"].Elapsed.Ticks);
		Assert.AreEqual(0, manager.SyncTimers["All"].Samples);

		// Start a sync that will until all sync have been started
		// Then we will let the first sync complete.
		var allSyncsStarted = false;
		var result1 = manager.SyncAccountsAsync(() => allSyncsStarted);
		manager.WaitForSyncToStart();
		var result2 = manager.SyncAllAsync();
		allSyncsStarted = true;
		manager.WaitForSyncToComplete();

		Assert.AreEqual(0, manager.SyncTimers["All"].Elapsed.Ticks);
		Assert.AreEqual(0, manager.SyncTimers["All"].Samples);
		Assert.AreEqual(0, manager.SyncTimers["All"].CancelledSyncs);
		Assert.AreEqual(0, manager.SyncTimers["All"].SuccessfulSyncs);
		Assert.AreEqual(0, manager.SyncTimers["All"].FailedSyncs);

		var actualResult1 = result1.AwaitResults();
		var actualResult2 = result2.AwaitResults();

		Assert.IsTrue(actualResult1.SyncSuccessful, "Sync should have been successful");
		Assert.AreEqual(SyncSessionState.CouldNotStart, actualResult2.State, "Sync should not have started");
		Assert.AreEqual(0, manager.SyncTimers["All"].Average.TotalMilliseconds);
		Assert.AreEqual(0, manager.SyncTimers["All"].CancelledSyncs);
		Assert.AreEqual(0, manager.SyncTimers["All"].SuccessfulSyncs);
		Assert.AreEqual(0, manager.SyncTimers["All"].FailedSyncs);
	}

	[TestMethod]
	public void SyncsShouldNotWaitForEachOther()
	{
		var startTime = new DateTime(2020, 04, 23, 01, 55, 21, DateTimeKind.Utc);
		var timeProvider = new DateTimeProvider(() => startTime += TimeSpan.FromSeconds(1));
		var logListener = GetLogListener();
		var manager = GetSyncManager(logListener: logListener, timeProvider: timeProvider);

		// Start a sync that will run for a long while
		var allSyncsStarted = false;
		var result1 = manager.SyncAllAsync(() => allSyncsStarted);
		manager.WaitForSyncToStart();
		var result2 = manager.SyncAccountsAsync();
		var result3 = manager.SyncAddressesAsync();
		allSyncsStarted = true;
		manager.WaitForSyncToComplete();
		var actualResult1 = result1.AwaitResults();
		var actualResult2 = result2.AwaitResults();
		var actualResult3 = result3.AwaitResults();

		var expected = new[]
		{
			new[]
			{
				$"2020-04-23T01:55:23.0000000Z - {result1.Result.SessionId} Verbose : Sync All started",
				$"2020-04-23T01:55:24.0000000Z - {result1.Result.SessionId} Verbose : Starting to All sync...",
				$"2020-04-23T01:55:25.0000000Z - {result1.Result.SessionId} Verbose : Syncing All for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:26.0000000Z - {result2.Result.SessionId} Verbose : Sync All is already running so Sync Accounts not started.",
				$"2020-04-23T01:55:27.0000000Z - {result3.Result.SessionId} Verbose : Sync All is already running so Sync Addresses not started.",
				$"2020-04-23T01:55:28.0000000Z - {result1.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:29.0000000Z - {result1.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:30.0000000Z - {result1.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:31.0000000Z - {result1.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:32.0000000Z - {result1.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:55:35.0000000Z - {result1.Result.SessionId} Verbose : Sync All completed. 00:11.000"
			},
			new[]
			{
				$"2020-04-23T01:55:23.0000000Z - {result1.Result.SessionId} Verbose : Sync All started",
				$"2020-04-23T01:55:24.0000000Z - {result1.Result.SessionId} Verbose : Starting to All sync...",
				$"2020-04-23T01:55:25.0000000Z - {result1.Result.SessionId} Verbose : Syncing All for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:26.0000000Z - {result2.Result.SessionId} Verbose : Sync All is already running so Sync Accounts not started.",
				$"2020-04-23T01:55:27.0000000Z - {result1.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:28.0000000Z - {result1.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:29.0000000Z - {result1.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:30.0000000Z - {result1.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:31.0000000Z - {result3.Result.SessionId} Verbose : Sync All is already running so Sync Addresses not started.",
				$"2020-04-23T01:55:32.0000000Z - {result1.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:55:35.0000000Z - {result1.Result.SessionId} Verbose : Sync All completed. 00:11.000"
			}
		};

		var actual = logListener.Events
			.Select(x => x.GetDetailedMessage(includeSessionId: true))
			.ToList();

		actual.ForEach(x =>
		{
			var message = x.Replace(result1.Result.SessionId.ToString(), "{result1.Result.SessionId}")
				.Replace(result2.Result.SessionId.ToString(), "{result2.Result.SessionId}")
				.Replace(result3.Result.SessionId.ToString(), "{result3.Result.SessionId}");

			Console.WriteLine($"$\"{message}\",");
		});

		var result = expected.Any(x => Compare(x, actual).Result == CompareResult.AreEqual);
		Assert.IsTrue(result, "The log does not match one of the possible orders...");
		Assert.IsTrue(actualResult1.SyncSuccessful, "Sync should have been successful");
		Assert.AreEqual(SyncSessionState.CouldNotStart, actualResult2.State, "Sync should not have started");
		Assert.AreEqual(SyncSessionState.CouldNotStart, actualResult3.State, "Sync should not have started");
		Assert.AreEqual(11000, manager.SyncTimers["All"].Average.TotalMilliseconds);
		Assert.AreEqual(1, manager.SyncTimers["All"].SuccessfulSyncs);
	}

	[TestMethod]
	public void SyncsShouldWaitForEachOther()
	{
		var startTime = new DateTime(2020, 04, 23, 01, 55, 21, DateTimeKind.Utc);
		var timeProvider = new DateTimeProvider(() => startTime += TimeSpan.FromSeconds(1));
		var logListener = GetLogListener();
		var manager = GetSyncManager(logListener: logListener, timeProvider: timeProvider);

		// Start a sync that will run for a long while
		var allSyncsStarted = false;
		var result1 = manager.SyncAllAsync(() => allSyncsStarted);
		manager.WaitForSyncToStart();
		var result2 = manager.SyncAccountsAsync(() => result1.IsCompleted, TimeSpan.FromMilliseconds(250));
		var result3 = manager.SyncAddressesAsync(() => result2.IsCompleted, TimeSpan.FromMilliseconds(500));
		allSyncsStarted = true;

		var waitResult = Task.WaitAll([result1, result2, result3], WaitTimeout);
		IsTrue(waitResult, $"Failed to complete syncs. {string.Join("\r\n", logListener.Events.Select(x => x.GetDetailedMessage()))}");

		var expected = new[]
		{
			new[]
			{
				$"2020-04-23T01:55:23.0000000Z - {result1.Result.SessionId} Verbose : Sync All started",
				$"2020-04-23T01:55:24.0000000Z - {result1.Result.SessionId} Verbose : Starting to All sync...",
				$"2020-04-23T01:55:25.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts waiting for Sync All to complete...",
				$"2020-04-23T01:55:26.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses waiting for Sync All to complete...",
				$"2020-04-23T01:55:27.0000000Z - {result1.Result.SessionId} Verbose : Syncing All for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:28.0000000Z - {result1.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:29.0000000Z - {result1.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:30.0000000Z - {result1.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:31.0000000Z - {result1.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:32.0000000Z - {result1.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:55:35.0000000Z - {result1.Result.SessionId} Verbose : Sync All completed. 00:11.000",
				$"2020-04-23T01:55:38.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts started",
				$"2020-04-23T01:55:39.0000000Z - {result2.Result.SessionId} Verbose : Starting to Accounts sync...",
				$"2020-04-23T01:55:40.0000000Z - {result2.Result.SessionId} Verbose : Syncing Accounts for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:41.0000000Z - {result2.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:42.0000000Z - {result2.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:43.0000000Z - {result2.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:44.0000000Z - {result2.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:45.0000000Z - {result2.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:55:48.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts completed. 00:09.000",
				$"2020-04-23T01:55:51.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses started",
				$"2020-04-23T01:55:52.0000000Z - {result3.Result.SessionId} Verbose : Starting to Addresses sync...",
				$"2020-04-23T01:55:53.0000000Z - {result3.Result.SessionId} Verbose : Syncing Addresses for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:54.0000000Z - {result3.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:55.0000000Z - {result3.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:56.0000000Z - {result3.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:57.0000000Z - {result3.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:58.0000000Z - {result3.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:56:01.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses completed. 00:09.000"
			},
			new[]
			{
				$"2020-04-23T01:55:23.0000000Z - {result1.Result.SessionId} Verbose : Sync All started",
				$"2020-04-23T01:55:24.0000000Z - {result1.Result.SessionId} Verbose : Starting to All sync...",
				$"2020-04-23T01:55:25.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts waiting for Sync All to complete...",
				$"2020-04-23T01:55:26.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses waiting for Sync All to complete...",
				$"2020-04-23T01:55:27.0000000Z - {result1.Result.SessionId} Verbose : Syncing All for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:28.0000000Z - {result1.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:29.0000000Z - {result1.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:30.0000000Z - {result1.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:31.0000000Z - {result1.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:32.0000000Z - {result1.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:55:35.0000000Z - {result1.Result.SessionId} Verbose : Sync All completed. 00:11.000",
				$"2020-04-23T01:55:38.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts started",
				$"2020-04-23T01:55:39.0000000Z - {result2.Result.SessionId} Verbose : Starting to Accounts sync...",
				$"2020-04-23T01:55:40.0000000Z - {result2.Result.SessionId} Verbose : Syncing Accounts for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:41.0000000Z - {result2.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:42.0000000Z - {result2.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:43.0000000Z - {result2.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:44.0000000Z - {result2.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:45.0000000Z - {result2.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:55:48.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts completed. 00:09.000",
				$"2020-04-23T01:55:51.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses started",
				$"2020-04-23T01:55:52.0000000Z - {result3.Result.SessionId} Verbose : Starting to Addresses sync...",
				$"2020-04-23T01:55:53.0000000Z - {result3.Result.SessionId} Verbose : Syncing Addresses for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:54.0000000Z - {result3.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:55.0000000Z - {result3.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:56.0000000Z - {result3.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:57.0000000Z - {result3.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:58.0000000Z - {result3.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:56:01.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses completed. 00:09.000"
			},
			new[]
			{
				$"2020-04-23T01:55:23.0000000Z - {result1.Result.SessionId} Verbose : Sync All started",
				$"2020-04-23T01:55:24.0000000Z - {result1.Result.SessionId} Verbose : Starting to All sync...",
				$"2020-04-23T01:55:25.0000000Z - {result1.Result.SessionId} Verbose : Syncing All for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:26.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts waiting for Sync All to complete...",
				$"2020-04-23T01:55:27.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses waiting for Sync All to complete...",
				$"2020-04-23T01:55:28.0000000Z - {result1.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:29.0000000Z - {result1.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:30.0000000Z - {result1.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:31.0000000Z - {result1.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:32.0000000Z - {result1.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:55:35.0000000Z - {result1.Result.SessionId} Verbose : Sync All completed. 00:11.000",
				$"2020-04-23T01:55:38.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts started",
				$"2020-04-23T01:55:39.0000000Z - {result2.Result.SessionId} Verbose : Starting to Accounts sync...",
				$"2020-04-23T01:55:40.0000000Z - {result2.Result.SessionId} Verbose : Syncing Accounts for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:41.0000000Z - {result2.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:42.0000000Z - {result2.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:43.0000000Z - {result2.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:44.0000000Z - {result2.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:45.0000000Z - {result2.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:55:48.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts completed. 00:09.000",
				$"2020-04-23T01:55:51.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses started",
				$"2020-04-23T01:55:52.0000000Z - {result3.Result.SessionId} Verbose : Starting to Addresses sync...",
				$"2020-04-23T01:55:53.0000000Z - {result3.Result.SessionId} Verbose : Syncing Addresses for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:54.0000000Z - {result3.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:55.0000000Z - {result3.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:56.0000000Z - {result3.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:57.0000000Z - {result3.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:58.0000000Z - {result3.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:56:01.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses completed. 00:09.000"
			},
			new[]
			{
				$"2020-04-23T01:55:23.0000000Z - {result1.Result.SessionId} Verbose : Sync All started",
				$"2020-04-23T01:55:24.0000000Z - {result1.Result.SessionId} Verbose : Starting to All sync...",
				$"2020-04-23T01:55:25.0000000Z - {result1.Result.SessionId} Verbose : Syncing All for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:26.0000000Z - {result1.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:26.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts waiting for Sync All to complete...",
				$"2020-04-23T01:55:27.0000000Z - {result1.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:28.0000000Z - {result1.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:29.0000000Z - {result1.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:30.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses waiting for Sync All to complete...",
				$"2020-04-23T01:55:31.0000000Z - {result1.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:55:34.0000000Z - {result1.Result.SessionId} Verbose : Sync All completed. 00:10.000",
				$"2020-04-23T01:55:37.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts started",
				$"2020-04-23T01:55:38.0000000Z - {result2.Result.SessionId} Verbose : Starting to Accounts sync...",
				$"2020-04-23T01:55:39.0000000Z - {result2.Result.SessionId} Verbose : Syncing Accounts for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:40.0000000Z - {result2.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:41.0000000Z - {result2.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:42.0000000Z - {result2.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:43.0000000Z - {result2.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:44.0000000Z - {result2.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:55:47.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts completed. 00:09.000",
				$"2020-04-23T01:55:50.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses started",
				$"2020-04-23T01:55:51.0000000Z - {result3.Result.SessionId} Verbose : Starting to Addresses sync...",
				$"2020-04-23T01:55:52.0000000Z - {result3.Result.SessionId} Verbose : Syncing Addresses for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:53.0000000Z - {result3.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:54.0000000Z - {result3.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:55.0000000Z - {result3.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:56.0000000Z - {result3.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:57.0000000Z - {result3.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:56:00.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses completed. 00:09.000"
			},
			new[]
			{
				$"2020-04-23T01:55:23.0000000Z - {result1.Result.SessionId} Verbose : Sync All started",
				$"2020-04-23T01:55:24.0000000Z - {result1.Result.SessionId} Verbose : Starting to All sync...",
				$"2020-04-23T01:55:25.0000000Z - {result1.Result.SessionId} Verbose : Syncing All for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:26.0000000Z - {result1.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:28.0000000Z - {result1.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:27.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts waiting for Sync All to complete...",
				$"2020-04-23T01:55:29.0000000Z - {result1.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:30.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses waiting for Sync All to complete...",
				$"2020-04-23T01:55:31.0000000Z - {result1.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:32.0000000Z - {result1.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:55:35.0000000Z - {result1.Result.SessionId} Verbose : Sync All completed. 00:11.000",
				$"2020-04-23T01:55:38.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts started",
				$"2020-04-23T01:55:39.0000000Z - {result2.Result.SessionId} Verbose : Starting to Accounts sync...",
				$"2020-04-23T01:55:40.0000000Z - {result2.Result.SessionId} Verbose : Syncing Accounts for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:41.0000000Z - {result2.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:42.0000000Z - {result2.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:43.0000000Z - {result2.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:44.0000000Z - {result2.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:45.0000000Z - {result2.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:55:48.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts completed. 00:09.000",
				$"2020-04-23T01:55:51.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses started",
				$"2020-04-23T01:55:52.0000000Z - {result3.Result.SessionId} Verbose : Starting to Addresses sync...",
				$"2020-04-23T01:55:53.0000000Z - {result3.Result.SessionId} Verbose : Syncing Addresses for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:54.0000000Z - {result3.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:55.0000000Z - {result3.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:56.0000000Z - {result3.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:57.0000000Z - {result3.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:58.0000000Z - {result3.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:56:01.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses completed. 00:09.000"
			},
			new[]
			{
				$"2020-04-23T01:55:23.0000000Z - {result1.Result.SessionId} Verbose : Sync All started",
				$"2020-04-23T01:55:24.0000000Z - {result1.Result.SessionId} Verbose : Starting to All sync...",
				$"2020-04-23T01:55:25.0000000Z - {result1.Result.SessionId} Verbose : Syncing All for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:26.0000000Z - {result1.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:27.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts waiting for Sync All to complete...",
				$"2020-04-23T01:55:28.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses waiting for Sync All to complete...",
				$"2020-04-23T01:55:29.0000000Z - {result1.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:30.0000000Z - {result1.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:31.0000000Z - {result1.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:32.0000000Z - {result1.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:55:35.0000000Z - {result1.Result.SessionId} Verbose : Sync All completed. 00:11.000",
				$"2020-04-23T01:55:38.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts started",
				$"2020-04-23T01:55:39.0000000Z - {result2.Result.SessionId} Verbose : Starting to Accounts sync...",
				$"2020-04-23T01:55:40.0000000Z - {result2.Result.SessionId} Verbose : Syncing Accounts for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:41.0000000Z - {result2.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:42.0000000Z - {result2.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:43.0000000Z - {result2.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:44.0000000Z - {result2.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:45.0000000Z - {result2.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:55:48.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts completed. 00:09.000",
				$"2020-04-23T01:55:51.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses started",
				$"2020-04-23T01:55:52.0000000Z - {result3.Result.SessionId} Verbose : Starting to Addresses sync...",
				$"2020-04-23T01:55:53.0000000Z - {result3.Result.SessionId} Verbose : Syncing Addresses for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:54.0000000Z - {result3.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:55.0000000Z - {result3.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:56.0000000Z - {result3.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:57.0000000Z - {result3.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:58.0000000Z - {result3.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:56:01.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses completed. 00:09.000"
			},
			new[]
			{
				$"2020-04-23T01:55:23.0000000Z - {result1.Result.SessionId} Verbose : Sync All started",
				$"2020-04-23T01:55:24.0000000Z - {result1.Result.SessionId} Verbose : Starting to All sync...",
				$"2020-04-23T01:55:25.0000000Z - {result1.Result.SessionId} Verbose : Syncing All for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:26.0000000Z - {result1.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:27.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts waiting for Sync All to complete...",
				$"2020-04-23T01:55:28.0000000Z - {result1.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:29.0000000Z - {result1.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:30.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses waiting for Sync All to complete...",
				$"2020-04-23T01:55:31.0000000Z - {result1.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:32.0000000Z - {result1.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:55:35.0000000Z - {result1.Result.SessionId} Verbose : Sync All completed. 00:11.000",
				$"2020-04-23T01:55:38.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts started",
				$"2020-04-23T01:55:39.0000000Z - {result2.Result.SessionId} Verbose : Starting to Accounts sync...",
				$"2020-04-23T01:55:40.0000000Z - {result2.Result.SessionId} Verbose : Syncing Accounts for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:41.0000000Z - {result2.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:42.0000000Z - {result2.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:43.0000000Z - {result2.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:44.0000000Z - {result2.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:45.0000000Z - {result2.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:55:48.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts completed. 00:09.000",
				$"2020-04-23T01:55:51.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses started",
				$"2020-04-23T01:55:52.0000000Z - {result3.Result.SessionId} Verbose : Starting to Addresses sync...",
				$"2020-04-23T01:55:53.0000000Z - {result3.Result.SessionId} Verbose : Syncing Addresses for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:54.0000000Z - {result3.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:55.0000000Z - {result3.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:56.0000000Z - {result3.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:57.0000000Z - {result3.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:58.0000000Z - {result3.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:56:01.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses completed. 00:09.000"
			},
			new[]
			{
				$"2020-04-23T01:55:23.0000000Z - {result1.Result.SessionId} Verbose : Sync All started",
				$"2020-04-23T01:55:24.0000000Z - {result1.Result.SessionId} Verbose : Starting to All sync...",
				$"2020-04-23T01:55:25.0000000Z - {result1.Result.SessionId} Verbose : Syncing All for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:26.0000000Z - {result1.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:27.0000000Z - {result1.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:28.0000000Z - {result1.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:29.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts waiting for Sync All to complete...",
				$"2020-04-23T01:55:30.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses waiting for Sync All to complete...",
				$"2020-04-23T01:55:31.0000000Z - {result1.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:32.0000000Z - {result1.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:55:35.0000000Z - {result1.Result.SessionId} Verbose : Sync All completed. 00:11.000",
				$"2020-04-23T01:55:38.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts started",
				$"2020-04-23T01:55:39.0000000Z - {result2.Result.SessionId} Verbose : Starting to Accounts sync...",
				$"2020-04-23T01:55:40.0000000Z - {result2.Result.SessionId} Verbose : Syncing Accounts for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:41.0000000Z - {result2.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:42.0000000Z - {result2.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:43.0000000Z - {result2.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:44.0000000Z - {result2.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:45.0000000Z - {result2.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:55:48.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts completed. 00:09.000",
				$"2020-04-23T01:55:51.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses started",
				$"2020-04-23T01:55:52.0000000Z - {result3.Result.SessionId} Verbose : Starting to Addresses sync...",
				$"2020-04-23T01:55:53.0000000Z - {result3.Result.SessionId} Verbose : Syncing Addresses for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:54.0000000Z - {result3.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:55.0000000Z - {result3.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:56.0000000Z - {result3.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:57.0000000Z - {result3.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:58.0000000Z - {result3.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:56:01.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses completed. 00:09.000"
			},
			new[]
			{
				$"2020-04-23T01:55:23.0000000Z - {result1.Result.SessionId} Verbose : Sync All started",
				$"2020-04-23T01:55:24.0000000Z - {result1.Result.SessionId} Verbose : Starting to All sync...",
				$"2020-04-23T01:55:25.0000000Z - {result1.Result.SessionId} Verbose : Syncing All for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:27.0000000Z - {result1.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:26.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts waiting for Sync All to complete...",
				$"2020-04-23T01:55:28.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses waiting for Sync All to complete...",
				$"2020-04-23T01:55:29.0000000Z - {result1.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:30.0000000Z - {result1.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:31.0000000Z - {result1.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:32.0000000Z - {result1.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:55:35.0000000Z - {result1.Result.SessionId} Verbose : Sync All completed. 00:11.000",
				$"2020-04-23T01:55:38.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts started",
				$"2020-04-23T01:55:39.0000000Z - {result2.Result.SessionId} Verbose : Starting to Accounts sync...",
				$"2020-04-23T01:55:40.0000000Z - {result2.Result.SessionId} Verbose : Syncing Accounts for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:41.0000000Z - {result2.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:42.0000000Z - {result2.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:43.0000000Z - {result2.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:44.0000000Z - {result2.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:45.0000000Z - {result2.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:55:48.0000000Z - {result2.Result.SessionId} Verbose : Sync Accounts completed. 00:09.000",
				$"2020-04-23T01:55:51.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses started",
				$"2020-04-23T01:55:52.0000000Z - {result3.Result.SessionId} Verbose : Starting to Addresses sync...",
				$"2020-04-23T01:55:53.0000000Z - {result3.Result.SessionId} Verbose : Syncing Addresses for 1/1/0001 12:00:00 AM, 1/1/0001 12:00:00 AM",
				$"2020-04-23T01:55:54.0000000Z - {result3.Result.SessionId} Verbose : Starting the sync session.",
				$"2020-04-23T01:55:55.0000000Z - {result3.Result.SessionId} Verbose : The sync session has started.",
				$"2020-04-23T01:55:56.0000000Z - {result3.Result.SessionId} Verbose : Starting to pull from server to client.",
				$"2020-04-23T01:55:57.0000000Z - {result3.Result.SessionId} Verbose : Starting to push to server from client.",
				$"2020-04-23T01:55:58.0000000Z - {result3.Result.SessionId} Verbose : Starting to end the session.",
				$"2020-04-23T01:56:01.0000000Z - {result3.Result.SessionId} Verbose : Sync Addresses completed. 00:09.000"
			}
		};

		var actual = logListener.Events
			.Select(x => x.GetDetailedMessage(includeSessionId: true))
			.ToList();

		actual.ForEach(x =>
		{
			var message = x.Replace(result1.Result.SessionId.ToString(), "{result1.Result.SessionId}")
				.Replace(result2.Result.SessionId.ToString(), "{result2.Result.SessionId}")
				.Replace(result3.Result.SessionId.ToString(), "{result3.Result.SessionId}");

			Console.WriteLine($"$\"{message}\",");
		});

		var result = expected.Any(x => Compare(x, actual).Result == CompareResult.AreEqual);
		Assert.IsTrue(result, "The log does not match one of the possible orders...");
	}

	private IServerSyncClientProvider GetServerSyncClientProvider(ISyncableDatabaseProvider serverDatabaseProvider, IDateTimeProvider timeProvider)
	{
		return new SampleServerSyncClientProvider(serverDatabaseProvider, timeProvider);
	}

	private ISyncClientProvider GetSyncClientProvider(ISyncableDatabaseProvider clientDatabaseProvider, IDateTimeProvider timeProvider)
	{
		return new SampleSyncClientProvider(clientDatabaseProvider, timeProvider);
	}

	private SampleSyncManager GetSyncManager(ISyncableDatabaseProvider clientDatabaseProvider = null, ISyncableDatabaseProvider serverDatabaseProvider = null,
		IRuntimeInformation runtimeInformation = null, LogListener logListener = null, IDateTimeProvider timeProvider = null, IDispatcher dispatcher = null)
	{
		clientDatabaseProvider ??= GetClientDatabaseProvider();
		runtimeInformation ??= GetRuntimeInformation();
		serverDatabaseProvider ??= GetServerDatabaseProvider();
		var clientSyncClientProvider = GetSyncClientProvider(clientDatabaseProvider, timeProvider);
		var serverSyncClientProvider = GetServerSyncClientProvider(serverDatabaseProvider, timeProvider);
		var manager = new SampleSyncManager(clientSyncClientProvider, serverSyncClientProvider,
			runtimeInformation, timeProvider ?? this, dispatcher ?? GetDispatcher());

		return manager;
	}

	#endregion
}