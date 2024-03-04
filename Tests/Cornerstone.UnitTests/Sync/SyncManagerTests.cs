#region References

using System;
using System.Linq;
using Cornerstone.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Shared;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
public class SyncManagerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void SyncShouldNotSendItemsBackUp()
	{
		var clientDatabaseProvider = GetClientDatabaseProvider();
		var runtimeInformation = GetRuntimeInformation();
		var serverDatabaseProvider = GetClientDatabaseProvider();
		var serverClient = new SyncClient("Server (local)", serverDatabaseProvider)
		{
			Options = { EnablePrimaryKeyCache = true, IsServerClient = true }
		};
		var serverSyncClientProvider = new SyncClientProvider(() => serverClient);
		var logListener = GetLogListener();

		var bob = GetClientAccount("Bob", Guid.NewGuid());

		using (var serverDatabase = serverDatabaseProvider.GetDatabase())
		{
			serverDatabase.Accounts.Add(bob);
			serverDatabase.SaveChanges();
		}

		var manager = new SharedSyncManager(clientDatabaseProvider, runtimeInformation, serverSyncClientProvider, logListener, null, "All");
		serverClient.IncomingConverter = manager.GetIncomingConverter();
		serverClient.OutgoingConverter = manager.GetOutgoingConverter();

		var result = manager.SyncAll();
		AreEqual(0, result.SyncIssues.Count, string.Join("\r\n", result.SyncIssues.Select(x => x.Message)));
		IsTrue(result.SyncSuccessful);

		AreEqual("1,0,1,0,0", result.StatisticsForClient.ToString());
		AreEqual("1,0,0,0,0", result.StatisticsForServer.ToString());
	}

	#endregion
}