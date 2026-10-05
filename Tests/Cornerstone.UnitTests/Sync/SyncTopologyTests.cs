#region References

using System.Linq;
using Cornerstone.Sample.Models;
using Cornerstone.Sample.Storage;
using Cornerstone.Sample.Sync;
using Cornerstone.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
[DoNotParallelize]
public class SyncTopologyTests : SyncScenarioTest
{
	#region Methods

	[TestMethod]
	public void HubAddReachesBothSpokes()
	{
		WithEachHomogeneousTrio((hubProvider, hub, spoke1Provider, spoke1, spoke2Provider, spoke2) =>
		{
			var spoke1Manager = NewHubSpokeManager(spoke1Provider, hubProvider);
			var spoke2Manager = NewHubSpokeManager(spoke2Provider, hubProvider);

			AddAddress(hub, "Hub Street");
			RunSync(spoke1Manager);
			Refresh(hub, spoke1, spoke2);
			RunSync(spoke2Manager);
			Refresh(hub, spoke1, spoke2);

			AreEqual(1, ReadAll<AddressEntity>(spoke1.Addresses).Count);
			AreEqual(1, ReadAll<AddressEntity>(spoke2.Addresses).Count);
			AreEqual("Hub Street", ReadAll<AddressEntity>(spoke1.Addresses)[0].Line1);
			AreEqual("Hub Street", ReadAll<AddressEntity>(spoke2.Addresses)[0].Line1);
		});
	}

	[TestMethod]
	public void HubSpokeReparentReachesOtherSpoke()
	{
		WithEachHomogeneousTrio((hubProvider, hub, spoke1Provider, spoke1, spoke2Provider, spoke2) =>
		{
			var spoke1Manager = NewHubSpokeManager(spoke1Provider, hubProvider);
			var spoke2Manager = NewHubSpokeManager(spoke2Provider, hubProvider);

			var oldParent = AddBookmark(spoke1, "Old", true);
			var newParent = AddBookmark(spoke1, "New", true);
			var child = AddBookmark(spoke1, "Page", parentSyncId: oldParent.SyncId);
			RunSync(spoke1Manager);
			Refresh(hub, spoke1, spoke2);
			RunSync(spoke2Manager);
			Refresh(hub, spoke1, spoke2);

			var stored = (BookmarkEntity) spoke1.Bookmarks.Read(child.SyncId);
			stored.ParentSyncId = newParent.SyncId;
			stored.ModifiedOn = UtcNow;
			Save(spoke1, stored);

			RunSync(spoke1Manager);
			Refresh(hub, spoke1, spoke2);
			RunSync(spoke2Manager);
			Refresh(hub, spoke1, spoke2);

			AreEqual(newParent.SyncId, ((BookmarkEntity) hub.Bookmarks.Read(child.SyncId)).ParentSyncId);
			AreEqual(newParent.SyncId, ((BookmarkEntity) spoke2.Bookmarks.Read(child.SyncId)).ParentSyncId);
		});
	}

	[TestMethod]
	public void HubSpokeSecondSyncAppliesNoChanges()
	{
		WithEachHomogeneousTrio((hubProvider, hub, spoke1Provider, spoke1, spoke2Provider, spoke2) =>
		{
			var spoke1Manager = NewHubSpokeManager(spoke1Provider, hubProvider);
			var spoke2Manager = NewHubSpokeManager(spoke2Provider, hubProvider);

			AddAddress(spoke1, "Settled");
			RunSync(spoke1Manager);
			Refresh(hub, spoke1, spoke2);
			RunSync(spoke2Manager);
			Refresh(hub, spoke1, spoke2);

			var second = RunSync(spoke2Manager);
			Refresh(hub, spoke1, spoke2);
			AreEqual(1, ReadAll<AddressEntity>(hub.Addresses).Count);
			AreEqual(1, ReadAll<AddressEntity>(spoke1.Addresses).Count);
			AreEqual(1, ReadAll<AddressEntity>(spoke2.Addresses).Count);
			AreEqual("Settled", ReadAll<AddressEntity>(hub.Addresses)[0].Line1);
			AreEqual("Settled", ReadAll<AddressEntity>(spoke2.Addresses)[0].Line1);
			AreEqual(0, second.StatisticsForClient.Changes);
		});
	}

	[TestMethod]
	public void HubSpokeSoftDeleteReachesOtherSpoke()
	{
		WithEachHomogeneousTrio((hubProvider, hub, spoke1Provider, spoke1, spoke2Provider, spoke2) =>
		{
			var spoke1Manager = NewHubSpokeManager(spoke1Provider, hubProvider);
			var spoke2Manager = NewHubSpokeManager(spoke2Provider, hubProvider);

			var address = AddAddress(spoke1, "Drop");
			RunSync(spoke1Manager);
			Refresh(hub, spoke1, spoke2);
			RunSync(spoke2Manager);
			Refresh(hub, spoke1, spoke2);

			var stored = (AddressEntity) spoke1.Addresses.Read(address.SyncId);
			stored.IsDeleted = true;
			stored.ModifiedOn = UtcNow;
			Save(spoke1, stored);

			RunSync(spoke1Manager);
			Refresh(hub, spoke1, spoke2);
			RunSync(spoke2Manager);
			Refresh(hub, spoke1, spoke2);

			IsTrue(((AddressEntity) hub.Addresses.Read(address.SyncId)).IsDeleted);
			IsTrue(((AddressEntity) spoke2.Addresses.Read(address.SyncId)).IsDeleted);
		});
	}

	[TestMethod]
	public void HubSpokeUndeleteReachesOtherSpoke()
	{
		WithEachHomogeneousTrio((hubProvider, hub, spoke1Provider, spoke1, spoke2Provider, spoke2) =>
		{
			var spoke1Manager = NewHubSpokeManager(spoke1Provider, hubProvider);
			var spoke2Manager = NewHubSpokeManager(spoke2Provider, hubProvider);

			var address = AddAddress(spoke1, "Gone");
			RunSync(spoke1Manager);
			Refresh(hub, spoke1, spoke2);
			RunSync(spoke2Manager);
			Refresh(hub, spoke1, spoke2);

			var stored = (AddressEntity) spoke1.Addresses.Read(address.SyncId);
			stored.IsDeleted = true;
			stored.ModifiedOn = UtcNow;
			Save(spoke1, stored);
			RunSync(spoke1Manager);
			Refresh(hub, spoke1, spoke2);
			RunSync(spoke2Manager);
			Refresh(hub, spoke1, spoke2);
			IsTrue(((AddressEntity) spoke2.Addresses.Read(address.SyncId)).IsDeleted);

			stored = (AddressEntity) spoke1.Addresses.Read(address.SyncId);
			stored.IsDeleted = false;
			stored.Line1 = "Back";
			stored.ModifiedOn = UtcNow;
			Save(spoke1, stored);
			RunSync(spoke1Manager, settings => settings.SyncDirection = SyncDirection.PushUp);
			Refresh(hub, spoke1, spoke2);
			RunSync(spoke2Manager);
			Refresh(hub, spoke1, spoke2);

			IsFalse(((AddressEntity) hub.Addresses.Read(address.SyncId)).IsDeleted);
			IsFalse(((AddressEntity) spoke2.Addresses.Read(address.SyncId)).IsDeleted);
			AreEqual("Back", ((AddressEntity) spoke2.Addresses.Read(address.SyncId)).Line1);
		});
	}

	[TestMethod]
	public void HubUpdateReachesBothSpokes()
	{
		WithEachHomogeneousTrio((hubProvider, hub, spoke1Provider, spoke1, spoke2Provider, spoke2) =>
		{
			var spoke1Manager = NewHubSpokeManager(spoke1Provider, hubProvider);
			var spoke2Manager = NewHubSpokeManager(spoke2Provider, hubProvider);

			var address = AddAddress(hub, "Old");
			RunSync(spoke1Manager);
			Refresh(hub, spoke1, spoke2);
			RunSync(spoke2Manager);
			Refresh(hub, spoke1, spoke2);

			var stored = (AddressEntity) hub.Addresses.Read(address.SyncId);
			stored.Line1 = "New";
			stored.ModifiedOn = UtcNow;
			Save(hub, stored);

			RunSync(spoke1Manager);
			Refresh(hub, spoke1, spoke2);
			RunSync(spoke2Manager);
			Refresh(hub, spoke1, spoke2);

			AreEqual("New", ((AddressEntity) spoke1.Addresses.Read(address.SyncId)).Line1);
			AreEqual("New", ((AddressEntity) spoke2.Addresses.Read(address.SyncId)).Line1);
		});
	}

	[TestMethod]
	public void LaterSpokeUpdateWinsOnHubThenOtherSpoke()
	{
		WithEachHomogeneousTrio((hubProvider, hub, spoke1Provider, spoke1, spoke2Provider, spoke2) =>
		{
			var spoke1Manager = NewHubSpokeManager(spoke1Provider, hubProvider);
			var spoke2Manager = NewHubSpokeManager(spoke2Provider, hubProvider);

			var address = AddAddress(hub, "Seed");
			RunSync(spoke1Manager);
			Refresh(hub, spoke1, spoke2);
			RunSync(spoke2Manager);
			Refresh(hub, spoke1, spoke2);

			var spoke1Address = (AddressEntity) spoke1.Addresses.Read(address.SyncId);
			spoke1Address.Line1 = "From Spoke 1";
			spoke1Address.ModifiedOn = UtcNow;
			Save(spoke1, spoke1Address);
			RunSync(spoke1Manager);
			Refresh(hub, spoke1, spoke2);

			var spoke2Address = (AddressEntity) spoke2.Addresses.Read(address.SyncId);
			spoke2Address.Line1 = "From Spoke 2";
			spoke2Address.ModifiedOn = UtcNow;
			Save(spoke2, spoke2Address);
			RunSync(spoke2Manager);
			Refresh(hub, spoke1, spoke2);
			RunSync(spoke1Manager);
			Refresh(hub, spoke1, spoke2);

			AreEqual("From Spoke 2", ((AddressEntity) hub.Addresses.Read(address.SyncId)).Line1);
			AreEqual("From Spoke 2", ((AddressEntity) spoke1.Addresses.Read(address.SyncId)).Line1);
			AreEqual("From Spoke 2", ((AddressEntity) spoke2.Addresses.Read(address.SyncId)).Line1);
		});
	}

	[TestMethod]
	public void MeshFlipServerSlotLaterPeerWins()
	{
		WithEachHomogeneousTrio((aProvider, a, bProvider, b, _, _) =>
		{
			var aToB = NewPeerManager(aProvider, bProvider);
			var bToA = NewPeerManager(bProvider, aProvider);

			var address = AddAddress(a, "Seed");
			RunSync(aToB);
			Refresh(a, b);

			var aAddress = (AddressEntity) a.Addresses.Read(address.SyncId);
			aAddress.Line1 = "A as client";
			aAddress.ModifiedOn = UtcNow;
			Save(a, aAddress);

			var bAddress = (AddressEntity) b.Addresses.Read(address.SyncId);
			bAddress.Line1 = "B as server first";
			bAddress.ModifiedOn = UtcNow;
			Save(b, bAddress);

			RunSync(aToB);
			Refresh(a, b);
			AreEqual("B as server first", ((AddressEntity) a.Addresses.Read(address.SyncId)).Line1);
			AreEqual("B as server first", ((AddressEntity) b.Addresses.Read(address.SyncId)).Line1);

			bAddress = (AddressEntity) b.Addresses.Read(address.SyncId);
			bAddress.Line1 = "B as client";
			bAddress.ModifiedOn = UtcNow;
			Save(b, bAddress);

			aAddress = (AddressEntity) a.Addresses.Read(address.SyncId);
			aAddress.Line1 = "A as server";
			aAddress.ModifiedOn = UtcNow;
			Save(a, aAddress);

			RunSync(bToA);
			Refresh(a, b);
			AreEqual("A as server", ((AddressEntity) a.Addresses.Read(address.SyncId)).Line1);
			AreEqual("A as server", ((AddressEntity) b.Addresses.Read(address.SyncId)).Line1);
		});
	}

	[TestMethod]
	public void MeshSoftDeleteReachesThirdPeer()
	{
		WithEachHomogeneousTrio((aProvider, a, bProvider, b, cProvider, c) =>
		{
			var aToB = NewPeerManager(aProvider, bProvider);
			var cToB = NewPeerManager(cProvider, bProvider);

			var address = AddAddress(a, "Drop");
			RunSync(aToB);
			Refresh(a, b, c);
			RunSync(cToB);
			Refresh(a, b, c);

			var stored = (AddressEntity) a.Addresses.Read(address.SyncId);
			stored.IsDeleted = true;
			stored.ModifiedOn = UtcNow;
			Save(a, stored);

			RunSync(aToB);
			Refresh(a, b, c);
			RunSync(cToB);
			Refresh(a, b, c);

			IsTrue(((AddressEntity) b.Addresses.Read(address.SyncId)).IsDeleted);
			IsTrue(((AddressEntity) c.Addresses.Read(address.SyncId)).IsDeleted);
		});
	}

	[TestMethod]
	public void MeshUnionViaAbThenBcThenCa()
	{
		WithEachHomogeneousTrio((aProvider, a, bProvider, b, cProvider, c) =>
		{
			var aToB = NewPeerManager(aProvider, bProvider);
			var cToB = NewPeerManager(cProvider, bProvider);
			var cToA = NewPeerManager(cProvider, aProvider);

			AddAddress(a, "From A");
			RunSync(aToB);
			Refresh(a, b, c);

			AddAddress(c, "From C");
			RunSync(cToB);
			Refresh(a, b, c);

			RunSync(cToA);
			Refresh(a, b, c);

			AreEqual(2, ReadAll<AddressEntity>(a.Addresses).Count);
			AreEqual(2, ReadAll<AddressEntity>(b.Addresses).Count);
			AreEqual(2, ReadAll<AddressEntity>(c.Addresses).Count);
			CollectionAssert.AreEquivalent(
				new[] { "From A", "From C" },
				ReadAll<AddressEntity>(a.Addresses).Select(x => x.Line1).ToList()
			);
			CollectionAssert.AreEquivalent(
				new[] { "From A", "From C" },
				ReadAll<AddressEntity>(b.Addresses).Select(x => x.Line1).ToList()
			);
			CollectionAssert.AreEquivalent(
				new[] { "From A", "From C" },
				ReadAll<AddressEntity>(c.Addresses).Select(x => x.Line1).ToList()
			);
		});
	}

	[TestMethod]
	public void SpokeAddReachesHubAndOtherSpoke()
	{
		WithEachHomogeneousTrio((hubProvider, hub, spoke1Provider, spoke1, spoke2Provider, spoke2) =>
		{
			var spoke1Manager = NewHubSpokeManager(spoke1Provider, hubProvider);
			var spoke2Manager = NewHubSpokeManager(spoke2Provider, hubProvider);

			AddAddress(spoke1, "Spoke One");
			RunSync(spoke1Manager);
			Refresh(hub, spoke1, spoke2);
			RunSync(spoke2Manager);
			Refresh(hub, spoke1, spoke2);

			AreEqual(1, ReadAll<AddressEntity>(hub.Addresses).Count);
			AreEqual(1, ReadAll<AddressEntity>(spoke2.Addresses).Count);
			AreEqual("Spoke One", ReadAll<AddressEntity>(hub.Addresses)[0].Line1);
			AreEqual("Spoke One", ReadAll<AddressEntity>(spoke2.Addresses)[0].Line1);
		});
	}

	[TestMethod]
	public void TwoIslandsMasterHubUpdateReachesAllSpokes()
	{
		WithTwoIslands((master, slave) =>
		{
			var slaveHubToMaster = NewHubSpokeManager(slave.Hub.Provider, master.Hub.Provider);

			var address = AddAddress(master.Hub.Database, "Old");
			SyncSpokesToHub(master);
			Refresh(master, slave);
			RunSync(slaveHubToMaster);
			Refresh(master, slave);
			SyncSpokesToHub(slave);
			Refresh(master, slave);

			var stored = (AddressEntity) master.Hub.Database.Addresses.Read(address.SyncId);
			stored.Line1 = "Master Hub New";
			stored.ModifiedOn = UtcNow;
			Save(master.Hub.Database, stored);

			SyncSpokesToHub(master);
			Refresh(master, slave);
			RunSync(slaveHubToMaster);
			Refresh(master, slave);
			SyncSpokesToHub(slave);
			Refresh(master, slave);

			AssertLineOnAll("Master Hub New", master, slave);
		});
	}

	[TestMethod]
	public void TwoIslandsMasterLaterWriteWinsOverSlaveSpoke()
	{
		WithTwoIslands((master, slave) =>
		{
			var slaveHubToMaster = NewHubSpokeManager(slave.Hub.Provider, master.Hub.Provider);

			var address = AddAddress(master.Spoke1.Database, "Seed");
			SyncSpokesToHub(master);
			Refresh(master, slave);
			RunSync(slaveHubToMaster);
			Refresh(master, slave);
			SyncSpokesToHub(slave);
			Refresh(master, slave);

			var slaveAddress = (AddressEntity) slave.Spoke1.Database.Addresses.Read(address.SyncId);
			slaveAddress.Line1 = "Slave";
			slaveAddress.ModifiedOn = UtcNow;
			Save(slave.Spoke1.Database, slaveAddress);
			SyncSpokesToHub(slave);
			Refresh(master, slave);
			RunSync(slaveHubToMaster);
			Refresh(master, slave);

			var masterAddress = (AddressEntity) master.Spoke1.Database.Addresses.Read(address.SyncId);
			masterAddress.Line1 = "Master";
			masterAddress.ModifiedOn = UtcNow;
			Save(master.Spoke1.Database, masterAddress);
			SyncSpokesToHub(master);
			Refresh(master, slave);
			RunSync(slaveHubToMaster);
			Refresh(master, slave);
			SyncSpokesToHub(slave);
			Refresh(master, slave);

			AssertLineOnAll("Master", master, slave);
		});
	}

	[TestMethod]
	public void TwoIslandsMasterSpokeAddReachesSlaveIsland()
	{
		WithTwoIslands((master, slave) =>
		{
			var slaveHubToMaster = NewHubSpokeManager(slave.Hub.Provider, master.Hub.Provider);

			AddAddress(master.Spoke1.Database, "From Master Spoke");
			SyncSpokesToHub(master);
			Refresh(master, slave);
			RunSync(slaveHubToMaster);
			Refresh(master, slave);
			SyncSpokesToHub(slave);
			Refresh(master, slave);

			AssertLineOnAll("From Master Spoke", master, slave);
		});
	}

	[TestMethod]
	public void TwoIslandsReparentCrossesHubs()
	{
		WithTwoIslands((master, slave) =>
		{
			var slaveHubToMaster = NewHubSpokeManager(slave.Hub.Provider, master.Hub.Provider);

			var oldParent = AddBookmark(master.Spoke1.Database, "Old", true);
			var newParent = AddBookmark(master.Spoke1.Database, "New", true);
			var child = AddBookmark(master.Spoke1.Database, "Page", parentSyncId: oldParent.SyncId);
			SyncSpokesToHub(master);
			Refresh(master, slave);
			RunSync(slaveHubToMaster);
			Refresh(master, slave);
			SyncSpokesToHub(slave);
			Refresh(master, slave);

			var stored = (BookmarkEntity) master.Spoke1.Database.Bookmarks.Read(child.SyncId);
			stored.ParentSyncId = newParent.SyncId;
			stored.ModifiedOn = UtcNow;
			Save(master.Spoke1.Database, stored);

			SyncSpokesToHub(master);
			Refresh(master, slave);
			RunSync(slaveHubToMaster);
			Refresh(master, slave);
			SyncSpokesToHub(slave);
			Refresh(master, slave);

			AreEqual(newParent.SyncId, ((BookmarkEntity) master.Hub.Database.Bookmarks.Read(child.SyncId)).ParentSyncId);
			AreEqual(newParent.SyncId, ((BookmarkEntity) slave.Hub.Database.Bookmarks.Read(child.SyncId)).ParentSyncId);
			AreEqual(newParent.SyncId, ((BookmarkEntity) slave.Spoke1.Database.Bookmarks.Read(child.SyncId)).ParentSyncId);
			AreEqual(newParent.SyncId, ((BookmarkEntity) slave.Spoke2.Database.Bookmarks.Read(child.SyncId)).ParentSyncId);
		});
	}

	[TestMethod]
	public void TwoIslandsSecondSyncAppliesNoChanges()
	{
		WithTwoIslands((master, slave) =>
		{
			var slaveHubToMaster = NewHubSpokeManager(slave.Hub.Provider, master.Hub.Provider);

			AddAddress(master.Spoke1.Database, "Settled");
			SyncSpokesToHub(master);
			Refresh(master, slave);
			RunSync(slaveHubToMaster);
			Refresh(master, slave);
			SyncSpokesToHub(slave);
			Refresh(master, slave);

			AssertLineOnAll("Settled", master, slave);

			var second = RunSync(slaveHubToMaster);
			Refresh(master, slave);
			AreEqual(0, second.StatisticsForServer.AppliedChanges);
			AreEqual(0, second.StatisticsForClient.Changes);
			AssertLineOnAll("Settled", master, slave);
		});
	}

	[TestMethod]
	public void TwoIslandsSlaveSpokeAddReachesMasterIsland()
	{
		WithTwoIslands((master, slave) =>
		{
			var slaveHubToMaster = NewHubSpokeManager(slave.Hub.Provider, master.Hub.Provider);

			AddAddress(slave.Spoke1.Database, "From Slave Spoke");
			SyncSpokesToHub(slave);
			Refresh(master, slave);
			RunSync(slaveHubToMaster);
			Refresh(master, slave);
			SyncSpokesToHub(master);
			Refresh(master, slave);

			AssertLineOnAll("From Slave Spoke", master, slave);
		});
	}

	[TestMethod]
	public void TwoIslandsSoftDeleteCrossesHubs()
	{
		WithTwoIslands((master, slave) =>
		{
			var slaveHubToMaster = NewHubSpokeManager(slave.Hub.Provider, master.Hub.Provider);

			var address = AddAddress(master.Spoke1.Database, "Drop");
			SyncSpokesToHub(master);
			Refresh(master, slave);
			RunSync(slaveHubToMaster);
			Refresh(master, slave);
			SyncSpokesToHub(slave);
			Refresh(master, slave);

			var stored = (AddressEntity) master.Spoke1.Database.Addresses.Read(address.SyncId);
			stored.IsDeleted = true;
			stored.ModifiedOn = UtcNow;
			Save(master.Spoke1.Database, stored);

			SyncSpokesToHub(master);
			Refresh(master, slave);
			RunSyncIncludingDeleted(slaveHubToMaster);
			Refresh(master, slave);
			SyncSpokesToHub(slave);
			Refresh(master, slave);

			IsTrue(((AddressEntity) master.Hub.Database.Addresses.Read(address.SyncId)).IsDeleted);
			IsTrue(((AddressEntity) slave.Hub.Database.Addresses.Read(address.SyncId)).IsDeleted);
			IsTrue(((AddressEntity) slave.Spoke1.Database.Addresses.Read(address.SyncId)).IsDeleted);
			IsTrue(((AddressEntity) slave.Spoke2.Database.Addresses.Read(address.SyncId)).IsDeleted);
		});
	}

	private void AssertLineOnAll(string line1, SyncIsland master, SyncIsland slave)
	{
		AreEqual(line1, ReadLine(master.Hub.Database, line1));
		AreEqual(line1, ReadLine(master.Spoke1.Database, line1));
		AreEqual(line1, ReadLine(master.Spoke2.Database, line1));
		AreEqual(line1, ReadLine(slave.Hub.Database, line1));
		AreEqual(line1, ReadLine(slave.Spoke1.Database, line1));
		AreEqual(line1, ReadLine(slave.Spoke2.Database, line1));
	}

	private static string ReadLine(ISampleDatabase database, string expected)
	{
		var matches = ReadAll<AddressEntity>(database.Addresses).Where(x => x.Line1 == expected).ToList();
		return matches.Count == 1 ? matches[0].Line1 : $"count={matches.Count}";
	}

	private void RunSyncIncludingDeleted(SampleSyncManager manager)
	{
		RunSync(manager, settings =>
		{
			settings.AddFilter<AddressEntity>(skipDeletedItemsOnInitialSync: false);
			settings.AddFilter<AccountEntity>(skipDeletedItemsOnInitialSync: false);
			settings.AddFilter<CustomerEntity>(skipDeletedItemsOnInitialSync: false);
			settings.AddFilter<BookmarkEntity>(skipDeletedItemsOnInitialSync: false);
			settings.AddFilter<SettingEntity>(skipDeletedItemsOnInitialSync: false);
		});
	}

	private void SyncSpokesToHub(SyncIsland island)
	{
		RunSyncIncludingDeleted(NewHubSpokeManager(island.Spoke1.Provider, island.Hub.Provider));
		Refresh(island.Hub.Database, island.Spoke1.Database, island.Spoke2.Database);
		RunSyncIncludingDeleted(NewHubSpokeManager(island.Spoke2.Provider, island.Hub.Provider));
		Refresh(island.Hub.Database, island.Spoke1.Database, island.Spoke2.Database);
	}

	#endregion
}