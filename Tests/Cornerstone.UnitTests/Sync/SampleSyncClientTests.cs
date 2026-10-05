#region References

using System;
using System.Linq;
using Cornerstone.Profiling;
using Cornerstone.Sample.Models;
using Cornerstone.Sample.Sync;
using Cornerstone.Sync;
using Cornerstone.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
[DoNotParallelize]
public class SampleSyncClientTests : SyncScenarioTest
{
	#region Methods

	[TestMethod]
	public void ApplyChangesPushesAccount()
	{
		WithEachPair((clientDb, serverDb, _) =>
		{
			var clientProvider = new SampleSyncClientProvider("Client", ScenarioClientProvider, this);
			var serverProvider = new SampleSyncClientProvider("Server", ScenarioServerProvider, this);
			var client = (SyncClientForDatabase) clientProvider.GetSyncClient(new SyncStatistics(), new Profiler("Client"));
			var server = (SyncClientForDatabase) serverProvider.GetSyncClient(new SyncStatistics(), new Profiler("Server"));

			clientDb.Accounts.Add(CreateAccount());
			AreEqual(1, clientDb.SaveChanges());

			var sessionId = Guid.NewGuid();
			var settings = new SyncSettings
			{
				IncludeIssueDetails = true,
				SyncType = SampleSyncClient.SyncAll
			};
			client.BeginSync(sessionId, settings);
			var changes = client.GetChanges(sessionId, new SyncRequest
			{
				Since = DateTime.MinValue,
				Until = UtcNow.AddMinutes(1),
				Take = 100
			});
			AreEqual(1, changes.Collection.Count);

			var result = server.Sync(new SyncOperation
			{
				SessionId = sessionId,
				Settings = settings,
				Changes = new ServiceRequest<SyncObject>(changes.Collection)
			});
			AreEqual(0, result.AppliedIssues.Collection.Count, () => string.Join("; ", result.AppliedIssues.Collection.Select(x => x.Message)));

			DetachScenarioDatabases();
			var stored = serverDb.Accounts.Read(changes.Collection[0].SyncId) as AccountEntity;
			IsNotNull(stored);
			AreEqual("John", stored.Name);
		});
	}

	private AccountEntity CreateAccount()
	{
		return new AccountEntity
		{
			CreatedOn = UtcNow,
			EmailAddress = "john@domain.com",
			Name = "John",
			ModifiedOn = UtcNow,
			Roles = ",,",
			SyncId = Guid.NewGuid()
		};
	}

	#endregion
}