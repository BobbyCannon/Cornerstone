#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Generators;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Cornerstone.Testing;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Shared.Storage;
using Sample.Shared.Storage.Server;

#endregion

namespace Cornerstone.IntegrationTests.Sync;

[TestClass]
public class SyncManagerTests : CornerstoneUnitTest
{
	#region Fields

	private IEnumerable<SyncScenario> _combinations;

	#endregion

	#region Methods

	[TestMethod]
	public void PullDownShouldNotPushClientContent()
	{
		foreach (var manager in GetScenarios())
		{
			manager.AddSaveAndCleanupForClient<IServerDatabase, AddressEntity>(GetServerModel<AddressEntity>());

			var result = manager.Process("All", x => x.SyncDirection = SyncDirection.PullDown);
			var issues = result.SyncIssues;
			AreEqual(0, issues.Count, string.Join(",", issues.Select(x => x.Message)));

			using var clientDatabase = (IServerDatabase) manager.ClientSyncClientProvider.GetSyncableDatabase();
			using var serverDatabase = (IServerDatabase) manager.ServerSyncClientProvider.GetSyncableDatabase();

			var addresses1 = clientDatabase.Addresses.OrderBy(x => x.Id).ToList();
			var addresses2 = serverDatabase.Addresses.OrderBy(x => x.Id).ToList();
			AreEqual(1, addresses1.Count);
			AreEqual(0, addresses2.Count);
		}
	}

	[TestMethod]
	public void PullDownShouldSuccessButShouldNotPushClientContent()
	{
		foreach (var manager in GetScenarios())
		{
			manager.AddSaveAndCleanupForClient<IServerDatabase, AddressEntity>(GetServerModel<AddressEntity>(x => x.Line1 = "Hello World"));
			manager.AddSaveAndCleanupForServer<IServerDatabase, AddressEntity>(GetServerModel<AddressEntity>(x => x.Line1 = "Foo Bar"));

			IncrementTime(seconds: 1);

			var result = manager.Process("All", x => x.SyncDirection = SyncDirection.PullDown);
			var issues = result.SyncIssues;
			AreEqual(0, issues.Count, string.Join(",", issues.Select(x => x.Message)));

			using var clientDatabase = (IServerDatabase) manager.ClientSyncClientProvider.GetSyncableDatabase();
			using var serverDatabase = (IServerDatabase) manager.ServerSyncClientProvider.GetSyncableDatabase();

			var addresses1 = clientDatabase.Addresses.OrderBy(x => x.Id).ToList();
			var addresses2 = serverDatabase.Addresses.OrderBy(x => x.Id).ToList();
			AreEqual(2, addresses1.Count);
			AreEqual(1, addresses2.Count);

			AreNotEqual(addresses1[0].Line1, addresses2[0].Line1);
			AreEqual(addresses1[1].Line1, addresses2[0].Line1);
		}
	}

	private IEnumerable<SyncManager> GetScenarios()
	{
		_combinations ??= CodeGenerator.GetAllCodeCombinations<SyncScenario>(x => (x.Client != DatabaseType.Unknown) && (x.Server != DatabaseType.Unknown));

		foreach (var combination in _combinations)
		{
			$"{combination.Client} Client <> {combination.Server} Server".Dump();

			yield return GetSyncManager(combination.Client, combination.Server);
		}
	}

	private SyncManager GetSyncManager(DatabaseType clientType, DatabaseType serverType)
	{
		var client = GetServerSyncClientProvider("Client", clientType);
		var server = GetServerSyncClientProvider("Server", serverType, clientType == serverType);
		var manager = new SyncManager(client, server, GetInstance<IRuntimeInformation>(), this, GetInstance<IDispatcher>(), "All");
		return manager;
	}

	#endregion

	#region Structures

	private struct SyncScenario
	{
		#region Properties

		public DatabaseType Client { get; set; }

		public DatabaseType Server { get; set; }

		#endregion
	}

	#endregion
}