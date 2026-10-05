#region References

using System;
using System.Linq;
using Cornerstone.Collections;
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
public class SyncEngineScenarioTests : SyncScenarioTest
{
	#region Methods

	[TestMethod]
	public void AddItemToClient()
	{
		WithEachPair((client, server, manager) =>
		{
			AddAddress(client, "Blah");
			RunSync(manager);

			AreEqual(1, ReadAll<AddressEntity>(client.Addresses).Count);
			AreEqual(1, ReadAll<AddressEntity>(server.Addresses).Count);
			AreEqual("Blah", ReadAll<AddressEntity>(server.Addresses)[0].Line1);
		});
	}

	[TestMethod]
	public void AddItemToClientAndServer()
	{
		WithEachPair((client, server, manager) =>
		{
			AddAddress(client, "Foo");
			AddAddress(server, "Bar");
			RunSync(manager);

			var clientAddresses = ReadAll<AddressEntity>(client.Addresses);
			var serverAddresses = ReadAll<AddressEntity>(server.Addresses);
			AreEqual(2, clientAddresses.Count);
			AreEqual(2, serverAddresses.Count);
			CollectionAssert.AreEquivalent(
				clientAddresses.Select(x => x.Line1).ToList(),
				serverAddresses.Select(x => x.Line1).ToList()
			);
		});
	}

	[TestMethod]
	public void AddItemToClientAndServerForceRelationshipUpdate()
	{
		WithEachPair((client, server, manager) =>
		{
			var clientAddress = AddAddress(client, "Foo");
			AddAccount(client, "Foo", clientAddress);
			var serverAddress = AddAddress(server, "Bar");
			AddAccount(server, "Bar", serverAddress);

			RunSync(manager);

			var clientAccounts = ReadAll<AccountEntity>(client.Accounts);
			var serverAccounts = ReadAll<AccountEntity>(server.Accounts);
			AreEqual(2, clientAccounts.Count);
			AreEqual(2, serverAccounts.Count);

			foreach (var account in serverAccounts)
			{
				IsTrue(account.AddressSyncId.HasValue);
				var address = server.Addresses.Read(account.AddressSyncId.Value) as AddressEntity;
				IsNotNull(address);
			}
		});
	}

	[TestMethod]
	public void AddItemToClientThroughWebLoopback()
	{
		WithEachWebPair((client, server, manager) =>
		{
			AddAddress(client, "Blah");
			RunSync(manager);

			AreEqual(1, ReadAll<AddressEntity>(client.Addresses).Count);
			AreEqual(1, ReadAll<AddressEntity>(server.Addresses).Count);
			AreEqual("Blah", ReadAll<AddressEntity>(server.Addresses)[0].Line1);
		});
	}

	[TestMethod]
	public void AddItemToServer()
	{
		WithEachPair((client, server, manager) =>
		{
			AddAddress(server, "Blah");
			RunSync(manager);

			AreEqual(1, ReadAll<AddressEntity>(client.Addresses).Count);
			AreEqual(1, ReadAll<AddressEntity>(server.Addresses).Count);
			AreEqual("Blah", ReadAll<AddressEntity>(client.Addresses)[0].Line1);
		});
	}

	[TestMethod]
	public void AddItemWithRelationshipToClientAndServer()
	{
		WithEachPair((client, server, manager) =>
		{
			var fooAddress = AddAddress(client, "Foo");
			AddAccount(client, "Foo", fooAddress);
			var barAddress = AddAddress(server, "Bar");
			AddAccount(server, "Bar", barAddress);

			RunSync(manager);

			AreEqual(2, ReadAll<AddressEntity>(client.Addresses).Count);
			AreEqual(2, ReadAll<AddressEntity>(server.Addresses).Count);
			AreEqual(2, ReadAll<AccountEntity>(client.Accounts).Count);
			AreEqual(2, ReadAll<AccountEntity>(server.Accounts).Count);

			var serverFoo = ReadAll<AccountEntity>(server.Accounts).Single(x => x.Name == "Foo");
			var fooOnServer = server.Addresses.Read(serverFoo.AddressSyncId.Value) as AddressEntity;
			IsNotNull(fooOnServer);
			AreEqual("Foo", fooOnServer.Line1);
			AreEqual(fooOnServer.Id, serverFoo.AddressId);
		});
	}

	[TestMethod]
	public void AddItemWithSameSyncIdOnBothSides()
	{
		WithEachPair((client, server, manager) =>
		{
			var syncId = Guid.NewGuid();
			AddAddress(client, "Client", syncId: syncId);
			AddAddress(server, "Server", syncId: syncId);
			RunSync(manager);

			AreEqual(1, ReadAll<AddressEntity>(client.Addresses).Count);
			AreEqual(1, ReadAll<AddressEntity>(server.Addresses).Count);
			AreEqual("Server", ((AddressEntity) client.Addresses.Read(syncId)).Line1);
			AreEqual("Server", ((AddressEntity) server.Addresses.Read(syncId)).Line1);
		});
	}

	[TestMethod]
	public void AddressSqlTypesRoundTrip()
	{
		WithEachPair((client, server, manager) =>
		{
			var customer = AddCustomer(client, "Typed");
			var photo = new byte[] { 1, 2, 3, 4 };
			var entity = new AddressEntity
			{
				AccuracyMeters = 1.5f,
				City = "Greenville",
				CreatedOn = UtcNow,
				CustomerId = customer.Id,
				CustomerSyncId = customer.SyncId,
				ExternalId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
				Floor = 3,
				IsPrimary = true,
				Latitude = 34.85,
				Line1 = "Typed Lane",
				Longitude = -82.39,
				ModifiedOn = UtcNow,
				Photo = photo,
				Postal = "29601",
				State = "SC",
				SyncId = Guid.NewGuid(),
				TaxRate = 7.25m,
				UnitNumber = 12
			};
			client.Addresses.Add(entity);
			Save(client);

			RunSync(manager);

			var serverAddress = (AddressEntity) server.Addresses.Read(entity.SyncId);
			IsNotNull(serverAddress);
			AreEqual(1.5f, serverAddress.AccuracyMeters);
			AreEqual(3, serverAddress.Floor);
			IsTrue(serverAddress.IsPrimary);
			AreEqual(34.85, serverAddress.Latitude);
			AreEqual(-82.39, serverAddress.Longitude);
			AreEqual(7.25m, serverAddress.TaxRate);
			AreEqual((short) 12, serverAddress.UnitNumber);
			AreEqual(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), serverAddress.ExternalId);
			CollectionAssert.AreEqual(photo, serverAddress.Photo);
		});
	}

	[TestMethod]
	public void CreatedOnUnchangedWhenLine1Updates()
	{
		WithEachPair((client, server, manager) =>
		{
			var address = AddAddress(client, "Old");
			var createdOn = address.CreatedOn;
			RunSync(manager);

			var stored = (AddressEntity) client.Addresses.Read(address.SyncId);
			stored.Line1 = "New";
			stored.ModifiedOn = UtcNow;
			Save(client, stored);

			RunSync(manager);

			var serverAddress = (AddressEntity) server.Addresses.Read(address.SyncId);
			var clientAddress = (AddressEntity) client.Addresses.Read(address.SyncId);
			AreEqual("New", serverAddress.Line1);
			AreEqual(createdOn, serverAddress.CreatedOn);
			AreEqual(createdOn, clientAddress.CreatedOn);
		});
	}

	[TestMethod]
	public void DeleteBookmarkOnClient()
	{
		WithEachPair((client, server, manager) =>
		{
			var keep = AddBookmark(client, "Keep");
			var drop = AddBookmark(client, "Drop");
			RunSync(manager);

			var stored = (BookmarkEntity) client.Bookmarks.Read(drop.SyncId);
			stored.IsDeleted = true;
			stored.ModifiedOn = UtcNow;
			Save(client, stored);

			RunSync(manager);

			AreEqual(1, ReadAll<BookmarkEntity>(client.Bookmarks).Count(x => !x.IsDeleted));
			AreEqual(1, ReadAll<BookmarkEntity>(client.Bookmarks).Count(x => x.IsDeleted));
			AreEqual(1, ReadAll<BookmarkEntity>(server.Bookmarks).Count(x => !x.IsDeleted));
			AreEqual(1, ReadAll<BookmarkEntity>(server.Bookmarks).Count(x => x.IsDeleted));
			IsTrue(((BookmarkEntity) server.Bookmarks.Read(drop.SyncId)).IsDeleted);
			IsFalse(((BookmarkEntity) server.Bookmarks.Read(keep.SyncId)).IsDeleted);
		});
	}

	[TestMethod]
	public void DeleteBookmarkOnServer()
	{
		WithEachPair((client, server, manager) =>
		{
			var keep = AddBookmark(server, "Keep");
			var drop = AddBookmark(server, "Drop");
			RunSync(manager);

			var stored = (BookmarkEntity) server.Bookmarks.Read(drop.SyncId);
			stored.IsDeleted = true;
			stored.ModifiedOn = UtcNow;
			Save(server, stored);

			RunSync(manager);

			AreEqual(1, ReadAll<BookmarkEntity>(client.Bookmarks).Count(x => !x.IsDeleted));
			AreEqual(1, ReadAll<BookmarkEntity>(client.Bookmarks).Count(x => x.IsDeleted));
			IsTrue(((BookmarkEntity) client.Bookmarks.Read(drop.SyncId)).IsDeleted);
			IsFalse(((BookmarkEntity) client.Bookmarks.Read(keep.SyncId)).IsDeleted);
		});
	}

	[TestMethod]
	public void DeleteItemOnClient()
	{
		WithEachPair((client, server, manager) =>
		{
			AddAddress(server, "Keep");
			var deleted = AddAddress(server, "Drop");
			RunSync(manager);

			var clientDeleted = client.Addresses.Read(deleted.SyncId) as AddressEntity;
			IsNotNull(clientDeleted);
			clientDeleted.IsDeleted = true;
			clientDeleted.ModifiedOn = UtcNow;
			Save(client, clientDeleted);

			RunSync(manager);

			AreEqual(1, ReadAll<AddressEntity>(client.Addresses).Count(x => !x.IsDeleted));
			AreEqual(1, ReadAll<AddressEntity>(client.Addresses).Count(x => x.IsDeleted));
			AreEqual(1, ReadAll<AddressEntity>(server.Addresses).Count(x => !x.IsDeleted));
			AreEqual(1, ReadAll<AddressEntity>(server.Addresses).Count(x => x.IsDeleted));
		});
	}

	[TestMethod]
	public void DeleteItemOnClientThroughWebLoopback()
	{
		WithEachWebPair((client, server, manager) =>
		{
			var address = AddAddress(client, "Home");
			RunSync(manager);

			var clientAddress = client.Addresses.Read(address.SyncId) as AddressEntity;
			IsNotNull(clientAddress);
			clientAddress.IsDeleted = true;
			clientAddress.ModifiedOn = UtcNow;
			Save(client, clientAddress);

			RunSync(manager);

			IsTrue(((AddressEntity) server.Addresses.Read(address.SyncId)).IsDeleted);
			IsTrue(((AddressEntity) client.Addresses.Read(address.SyncId)).IsDeleted);
		});
	}

	[TestMethod]
	public void DeleteItemOnServer()
	{
		WithEachPair((client, server, manager) =>
		{
			AddAddress(server, "Keep");
			var deleted = AddAddress(server, "Drop");
			RunSync(manager);

			var serverDeleted = server.Addresses.Read(deleted.SyncId) as AddressEntity;
			IsNotNull(serverDeleted);
			serverDeleted.IsDeleted = true;
			serverDeleted.ModifiedOn = UtcNow;
			Save(server, serverDeleted);

			RunSync(manager);

			AreEqual(1, ReadAll<AddressEntity>(client.Addresses).Count(x => !x.IsDeleted));
			AreEqual(1, ReadAll<AddressEntity>(client.Addresses).Count(x => x.IsDeleted));
		});
	}

	[TestMethod]
	public void DeleteParentBookmarkLeavesChildren()
	{
		WithEachPair((client, server, manager) =>
		{
			var parent = AddBookmark(client, "Folder", true);
			RunSync(manager);

			var child = AddBookmark(client, "Page", parentSyncId: parent.SyncId);
			RunSync(manager);

			var serverChildBefore = (BookmarkEntity) server.Bookmarks.Read(child.SyncId);
			var serverParentBefore = (BookmarkEntity) server.Bookmarks.Read(parent.SyncId);
			IsNotNull(serverChildBefore);
			IsNotNull(serverParentBefore);
			AreEqual(parent.SyncId, serverChildBefore.ParentSyncId);
			AreEqual(serverParentBefore.Id, serverChildBefore.ParentId);

			var storedParent = (BookmarkEntity) client.Bookmarks.Read(parent.SyncId);
			storedParent.IsDeleted = true;
			storedParent.ModifiedOn = UtcNow;
			Save(client, storedParent);

			RunSync(manager);

			var serverParent = (BookmarkEntity) server.Bookmarks.Read(parent.SyncId);
			var serverChild = (BookmarkEntity) server.Bookmarks.Read(child.SyncId);
			IsNotNull(serverParent);
			IsNotNull(serverChild);
			IsTrue(serverParent.IsDeleted);
			IsFalse(serverChild.IsDeleted);
			AreEqual(parent.SyncId, serverChild.ParentSyncId);
			AreEqual(serverParent.Id, serverChild.ParentId);
			IsFalse(((BookmarkEntity) client.Bookmarks.Read(child.SyncId)).IsDeleted);
		});
	}

	[TestMethod]
	public void ExistingParentResolvesBookmarkParentIdFromCache()
	{
		WithEachPair((client, server, manager) =>
		{
			var parent = AddBookmark(server, "Folder", true);
			RunSync(manager);

			var child = AddBookmark(client, "Page", parentSyncId: parent.SyncId);
			RunSync(manager);

			var serverChild = (BookmarkEntity) server.Bookmarks.Read(child.SyncId);
			var serverParent = (BookmarkEntity) server.Bookmarks.Read(parent.SyncId);
			IsNotNull(serverChild);
			IsNotNull(serverParent);
			AreEqual(parent.SyncId, serverChild.ParentSyncId);
			AreEqual(serverParent.Id, serverChild.ParentId);
		});
	}

	[TestMethod]
	public void GetChangesSkipKeepsParentBeforeChildAcrossPages()
	{
		WithEachProvider((provider, database) =>
		{
			var child = AddBookmark(database, "Old Page");
			var parent = AddBookmark(database, "New Folder", true);
			var storedChild = (BookmarkEntity) database.Bookmarks.Read(child.SyncId);
			storedChild.ParentSyncId = parent.SyncId;
			storedChild.ModifiedOn = UtcNow;
			Save(database, storedChild);

			var client = new SampleSyncClient("Client", provider, this, new SyncStatistics(), new Profiler("Client"));
			var sessionId = Guid.NewGuid();
			var settings = new SyncSettings { IncludeIssueDetails = true };
			settings.AddFilter<AddressEntity>();
			settings.AddFilter<AccountEntity>();
			settings.AddFilter(
				orderBy: [
					new OrderBy<BookmarkEntity>(x => x.ParentSyncId),
					new OrderBy<BookmarkEntity>(x => x.Order)
				]
			);
			client.BeginSync(sessionId, settings);

			var first = client.GetChanges(sessionId, new SyncRequest
			{
				Since = DateTime.MinValue,
				Until = UtcNow.AddMinutes(1),
				Take = 1
			});
			AreEqual(1, first.Collection.Count);
			AreEqual(parent.SyncId, first.Collection[0].SyncId);

			var second = client.GetChanges(sessionId, new SyncRequest
			{
				Since = DateTime.MinValue,
				Until = UtcNow.AddMinutes(1),
				Skip = first.Collection.Count,
				Take = 1
			});
			AreEqual(1, second.Collection.Count);
			AreEqual(child.SyncId, second.Collection[0].SyncId);
		});
	}

	[TestMethod]
	public void HierarchyOrderByPutsNewParentBeforeOldChild()
	{
		WithEachProvider((provider, database) =>
		{
			var child = AddBookmark(database, "Old Page");
			var parent = AddBookmark(database, "New Folder", true);
			var storedChild = (BookmarkEntity) database.Bookmarks.Read(child.SyncId);
			storedChild.ParentSyncId = parent.SyncId;
			storedChild.ModifiedOn = UtcNow;
			Save(database, storedChild);

			var client = new SampleSyncClient("Client", provider, this, new SyncStatistics(), new Profiler("Client"));
			var sessionId = Guid.NewGuid();
			var settings = new SyncSettings { IncludeIssueDetails = true };
			settings.AddFilter<AddressEntity>();
			settings.AddFilter<AccountEntity>();
			settings.AddFilter(
				orderBy: [
					new OrderBy<BookmarkEntity>(x => x.ParentSyncId),
					new OrderBy<BookmarkEntity>(x => x.Order)
				]
			);
			client.BeginSync(sessionId, settings);

			var changes = client.GetChanges(sessionId, new SyncRequest
			{
				Since = DateTime.MinValue,
				Until = UtcNow.AddMinutes(1),
				Take = 100
			});
			AreEqual(2, changes.Collection.Count);
			AreEqual(parent.SyncId, changes.Collection[0].SyncId);
			AreEqual(child.SyncId, changes.Collection[1].SyncId);
		});
	}

	[TestMethod]
	public void HubAuthenticatedAccountIgnoresClientTenantSettings()
	{
		WithEachWebPair((client, server, manager) =>
		{
			var customer1 = AddCustomer(server, "Customer 1", Customer1SyncId);
			var customer2 = AddCustomer(server, "Customer 2", Customer2SyncId);
			var address1 = AddAddress(server, "One Main", customer: customer1);
			AddAddress(server, "Two Main", customer: customer2);
			ServerAuthenticatedAccount = AddAccount(server, "Cust1", address1, customer1);

			RunSync(manager, settings => settings.Values["CustomerSyncId"] = Customer2SyncId.ToString());

			AreEqual(1, ReadAll<CustomerEntity>(client.Customers).Count);
			AreEqual(Customer1SyncId, ReadAll<CustomerEntity>(client.Customers)[0].SyncId);
			AreEqual(1, ReadAll<AddressEntity>(client.Addresses).Count);
			AreEqual("One Main", ReadAll<AddressEntity>(client.Addresses)[0].Line1);
			AreEqual(2, ReadAll<AddressEntity>(server.Addresses).Count);
		});
	}

	[TestMethod]
	public void HubCapsItemsPerSyncRequest()
	{
		WithEachWebPair((client, server, manager) =>
		{
			AddAddress(client, "One");
			AddAddress(client, "Two");
			AddAddress(client, "Three");

			var session = RunSync(manager, settings => settings.ItemsPerSyncRequest = 50000);

			AreEqual(10000, session.Settings.ItemsPerSyncRequest);
			AreEqual(3, ReadAll<AddressEntity>(server.Addresses).Count);
		});
	}

	[TestMethod]
	public void HubRejectsNewAccountRewrittenToTenantWithOtherCustomerAddress()
	{
		WithEachPair((_, server, _) =>
		{
			var customer1 = AddCustomer(server, "Customer 1", Customer1SyncId);
			var customer2 = AddCustomer(server, "Customer 2", Customer2SyncId);
			var address1 = AddAddress(server, "One Main", customer: customer1);
			var address2 = AddAddress(server, "Two Main", customer: customer2);
			var account = AddAccount(server, "Cust1", address1, customer1);

			var serverSync = new SampleServerSyncClient(
				"Server",
				ScenarioServerProvider,
				this,
				new SyncStatistics(),
				new Profiler("Server")
			)
			{
				AuthenticatedAccount = account
			};

			var payload = new Account
			{
				AddressSyncId = address2.SyncId,
				CreatedOn = UtcNow.AddMinutes(-1),
				CustomerSyncId = Customer1SyncId,
				EmailAddress = "intruder@domain.com",
				ModifiedOn = UtcNow,
				Name = "Intruder",
				Roles = ",,",
				SyncId = Guid.NewGuid()
			};
			var result = serverSync.Sync(new SyncOperation
			{
				SessionId = Guid.NewGuid(),
				Settings = new SyncSettings { IncludeIssueDetails = true },
				Changes = new ServiceRequest<SyncObject>(SyncObject.ToSyncObject(payload))
			});
			DetachScenarioDatabases();

			AreEqual(1, result.AppliedIssues.Collection.Count);
			AreEqual(SyncIssueType.RelationshipConstraint, result.AppliedIssues.Collection[0].IssueType);
			IsNull(server.Accounts.Read(payload.SyncId));
			AreEqual(1, ReadAll<AccountEntity>(server.Accounts).Count);
		});
	}

	[TestMethod]
	public void HubRejectsPayloadRewrittenToMatchTenant()
	{
		WithEachPair((_, server, _) =>
		{
			var customer1 = AddCustomer(server, "Customer 1", Customer1SyncId);
			var customer2 = AddCustomer(server, "Customer 2", Customer2SyncId);
			var address1 = AddAddress(server, "One Main", customer: customer1);
			var address2 = AddAddress(server, "Two Main", customer: customer2);
			var account = AddAccount(server, "Cust1", address1, customer1);

			var serverSync = new SampleServerSyncClient(
				"Server",
				ScenarioServerProvider,
				this,
				new SyncStatistics(),
				new Profiler("Server")
			)
			{
				AuthenticatedAccount = account
			};

			var payload = new Address
			{
				City = "City",
				CreatedOn = UtcNow.AddMinutes(-1),
				CustomerSyncId = Customer1SyncId,
				Line1 = "Stolen",
				ModifiedOn = UtcNow,
				Postal = "29640",
				State = "SC",
				SyncId = address2.SyncId
			};
			var result = serverSync.Sync(new SyncOperation
			{
				SessionId = Guid.NewGuid(),
				Settings = new SyncSettings { IncludeIssueDetails = true },
				Changes = new ServiceRequest<SyncObject>(SyncObject.ToSyncObject(payload))
			});
			DetachScenarioDatabases();

			AreEqual(1, result.AppliedIssues.Collection.Count);
			AreEqual(SyncIssueType.SyncEntityFiltered, result.AppliedIssues.Collection[0].IssueType);
			var remaining = (AddressEntity) server.Addresses.Read(address2.SyncId);
			IsNotNull(remaining);
			AreEqual("Two Main", remaining.Line1);
			AreEqual(Customer2SyncId, remaining.CustomerSyncId);
		});
	}

	[TestMethod]
	public void HubRejectsSpokeAddressForOtherCustomer()
	{
		WithEachWebPair((client, server, manager) =>
		{
			var customer1 = AddCustomer(server, "Customer 1", Customer1SyncId);
			var customer2 = AddCustomer(server, "Customer 2", Customer2SyncId);
			var address1 = AddAddress(server, "One Main", customer: customer1);
			ServerAuthenticatedAccount = AddAccount(server, "Cust1", address1, customer1);
			RunSync(manager);

			var clientOther = AddCustomer(client, "Customer 2", Customer2SyncId);
			AddAddress(client, "Other Tenant Street", customer: clientOther);
			var session = manager.Sync(SampleSyncClient.SyncAll, null, TimeSpan.FromSeconds(30));
			DetachScenarioDatabases();
			IncrementTime(seconds: 1);
			IsTrue(session.SyncCompleted);

			IsNull(ReadAll<AddressEntity>(server.Addresses).FirstOrDefault(x => x.Line1 == "Other Tenant Street"));
			AreEqual(1, ReadAll<AddressEntity>(server.Addresses).Count(x => !x.IsDeleted && (x.Line1 == "One Main")));
		});
	}

	[TestMethod]
	public void HubStripsIncludeIssueDetails()
	{
		WithEachWebPair((client, server, manager) =>
		{
			AddAddress(client, "Home");
			var session = RunSync(manager, settings => settings.IncludeIssueDetails = true);
			IsFalse(session.Settings.IncludeIssueDetails);
			AreEqual(1, ReadAll<AddressEntity>(server.Addresses).Count);
		});
	}

	[TestMethod]
	public void LastWriteWinsWhenBothSidesReparentBookmark()
	{
		WithEachPair((client, server, manager) =>
		{
			var firstParent = AddBookmark(client, "First Folder", true);
			var secondParent = AddBookmark(client, "Second Folder", true);
			var child = AddBookmark(client, "Page", parentSyncId: firstParent.SyncId);
			RunSync(manager);

			var clientChild = (BookmarkEntity) client.Bookmarks.Read(child.SyncId);
			clientChild.ParentSyncId = firstParent.SyncId;
			clientChild.ModifiedOn = UtcNow;
			Save(client, clientChild);

			var serverChild = (BookmarkEntity) server.Bookmarks.Read(child.SyncId);
			serverChild.ParentSyncId = secondParent.SyncId;
			serverChild.ModifiedOn = UtcNow;
			Save(server, serverChild);

			RunSync(manager);

			AreEqual(secondParent.SyncId, ((BookmarkEntity) client.Bookmarks.Read(child.SyncId)).ParentSyncId);
			AreEqual(secondParent.SyncId, ((BookmarkEntity) server.Bookmarks.Read(child.SyncId)).ParentSyncId);
			var clientSecond = (BookmarkEntity) client.Bookmarks.Read(secondParent.SyncId);
			AreEqual(clientSecond.Id, ((BookmarkEntity) client.Bookmarks.Read(child.SyncId)).ParentId);
		});
	}

	[TestMethod]
	public void LastWriteWinsWhenClientReparentsAfterServer()
	{
		WithEachPair((client, server, manager) =>
		{
			var firstParent = AddBookmark(client, "First Folder", true);
			var secondParent = AddBookmark(client, "Second Folder", true);
			var child = AddBookmark(client, "Page", parentSyncId: firstParent.SyncId);
			RunSync(manager);

			var serverChild = (BookmarkEntity) server.Bookmarks.Read(child.SyncId);
			serverChild.ParentSyncId = firstParent.SyncId;
			serverChild.ModifiedOn = UtcNow;
			Save(server, serverChild);

			var clientChild = (BookmarkEntity) client.Bookmarks.Read(child.SyncId);
			clientChild.ParentSyncId = secondParent.SyncId;
			clientChild.ModifiedOn = UtcNow;
			Save(client, clientChild);

			RunSync(manager);

			AreEqual(secondParent.SyncId, ((BookmarkEntity) client.Bookmarks.Read(child.SyncId)).ParentSyncId);
			AreEqual(secondParent.SyncId, ((BookmarkEntity) server.Bookmarks.Read(child.SyncId)).ParentSyncId);
		});
	}

	[TestMethod]
	public void LaterDeleteWinsOverEarlierUpdate()
	{
		WithEachPair((client, server, manager) =>
		{
			var address = AddAddress(server, "123 Elm Street");
			RunSync(manager);

			var clientAddress = client.Addresses.Read(address.SyncId) as AddressEntity;
			IsNotNull(clientAddress);
			clientAddress.Line1 = "123 Client Street";
			clientAddress.ModifiedOn = UtcNow;
			Save(client, clientAddress);

			var serverAddress = server.Addresses.Read(address.SyncId) as AddressEntity;
			IsNotNull(serverAddress);
			serverAddress.IsDeleted = true;
			serverAddress.ModifiedOn = UtcNow;
			Save(server, serverAddress);

			RunSync(manager);

			IsTrue(((AddressEntity) client.Addresses.Read(address.SyncId)).IsDeleted);
			IsTrue(((AddressEntity) server.Addresses.Read(address.SyncId)).IsDeleted);
		});
	}

	[TestMethod]
	public void LookupFilterDoesNotMergeSameEmailAcrossCustomers()
	{
		WithEachPair((client, server, manager) =>
		{
			var customer1 = AddCustomer(server, "Customer 1", Customer1SyncId);
			var customer2 = AddCustomer(server, "Customer 2", Customer2SyncId);
			AddAccount(server, "Shared", customer: customer1);
			var clientCustomer2 = AddCustomer(client, "Customer 2", Customer2SyncId);
			AddAccount(client, "Shared", customer: clientCustomer2);

			RunSync(manager, settings =>
			{
				settings.SyncDirection = SyncDirection.PushUp;
				settings.AddFilter<CustomerEntity>();
				settings.AddFilter<AddressEntity>();
				settings.AddFilter<AccountEntity>(lookupFilter: incoming => x =>
					(x.EmailAddress == incoming.EmailAddress) && (x.CustomerSyncId == incoming.CustomerSyncId));
			});

			AreEqual(2, ReadAll<AccountEntity>(server.Accounts).Count);
			AreEqual(1, ReadAll<AccountEntity>(server.Accounts).Count(x => x.CustomerSyncId == Customer1SyncId));
			AreEqual(1, ReadAll<AccountEntity>(server.Accounts).Count(x => x.CustomerSyncId == Customer2SyncId));
		});
	}

	[TestMethod]
	public void LookupFilterMatchesOnEmailNotSyncId()
	{
		WithEachPair((client, server, manager) =>
		{
			var address = AddAddress(client, "Home");
			var account = AddAccount(client, "John", address);
			var serverAddress = AddAddress(server, "Home");
			var serverCustomer = EnsureCustomer(server, serverAddress.CustomerSyncId);
			var serverAccount = new AccountEntity
			{
				AddressId = serverAddress.Id,
				AddressSyncId = serverAddress.SyncId,
				CreatedOn = UtcNow,
				CustomerId = serverCustomer.Id,
				CustomerSyncId = serverCustomer.SyncId,
				EmailAddress = account.EmailAddress,
				ModifiedOn = UtcNow,
				Name = "ServerJohn",
				Roles = ",,",
				SyncId = Guid.NewGuid()
			};
			server.Accounts.Add(serverAccount);
			Save(server);

			RunSync(manager, settings =>
			{
				settings.SyncDirection = SyncDirection.PushUp;
				settings.AddFilter<AddressEntity>();
				settings.AddFilter<AccountEntity>(lookupFilter: incoming => x => x.EmailAddress == incoming.EmailAddress);
			});

			AreEqual(1, ReadAll<AccountEntity>(server.Accounts).Count);
			AreEqual(1, ReadAll<AccountEntity>(client.Accounts).Count);
		});
	}

	[TestMethod]
	public void MissingParentBookmarkLeavesParentIdNull()
	{
		WithEachPair((client, server, manager) =>
		{
			var missingParent = Guid.NewGuid();
			var child = AddBookmark(client, "Page", parentSyncId: missingParent);
			RunSync(manager);

			var serverChild = (BookmarkEntity) server.Bookmarks.Read(child.SyncId);
			var clientChild = (BookmarkEntity) client.Bookmarks.Read(child.SyncId);
			IsNotNull(serverChild);
			IsNotNull(clientChild);
			AreEqual(missingParent, serverChild.ParentSyncId);
			AreEqual(missingParent, clientChild.ParentSyncId);
			IsNull(serverChild.ParentId);
			IsNull(clientChild.ParentId);
		});
	}

	[TestMethod]
	public void NewAccountCannotBindOtherCustomerAddress()
	{
		WithEachPair((client, server, manager) =>
		{
			var customer1 = AddCustomer(server, "Customer 1", Customer1SyncId);
			var customer2 = AddCustomer(server, "Customer 2", Customer2SyncId);
			AddAddress(server, "One Main", customer: customer1);
			var address2 = AddAddress(server, "Two Main", customer: customer2);
			RunSync(manager);

			var clientCustomer1 = (CustomerEntity) client.Customers.Read(Customer1SyncId);
			var clientAddress2 = (AddressEntity) client.Addresses.Read(address2.SyncId);
			IsNotNull(clientCustomer1);
			IsNotNull(clientAddress2);
			var incoming = AddAccount(client, "Intruder", clientAddress2, clientCustomer1);

			var session = manager.Sync(SampleSyncClient.SyncAll, null, TimeSpan.FromSeconds(30));
			DetachScenarioDatabases();
			IncrementTime(seconds: 1);

			IsTrue(session.SyncIssues.Count > 0);
			IsNull(server.Accounts.Read(incoming.SyncId));
		});
	}

	[TestMethod]
	public void NewBookmarkCannotBindOtherCustomerParent()
	{
		WithEachPair((client, server, manager) =>
		{
			var customer1 = AddCustomer(server, "Customer 1", Customer1SyncId);
			var customer2 = AddCustomer(server, "Customer 2", Customer2SyncId);
			var parent = AddBookmark(server, "Folder", true, customer: customer2);
			RunSync(manager);

			var clientCustomer1 = (CustomerEntity) client.Customers.Read(Customer1SyncId);
			IsNotNull(clientCustomer1);
			var incoming = AddBookmark(
				client,
				"Page",
				parentSyncId: parent.SyncId,
				customer: clientCustomer1
			);

			var session = manager.Sync(SampleSyncClient.SyncAll, null, TimeSpan.FromSeconds(30));
			DetachScenarioDatabases();
			IncrementTime(seconds: 1);

			IsTrue(session.SyncIssues.Count > 0);
			IsNull(server.Bookmarks.Read(incoming.SyncId));
		});
	}

	[TestMethod]
	public void NewlyCreatedItemModifiedAfterSyncStartShouldStillSync()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Elm");
			var client = new SampleSyncClient("Server", provider, this, new SyncStatistics(), new Profiler("Server"));
			var sessionId = Guid.NewGuid();
			var settings = new SyncSettings { IncludeIssueDetails = true };
			settings.AddFilter<AddressEntity>();
			settings.AddFilter<AccountEntity>();
			var session = client.BeginSync(sessionId, settings);

			IncrementTime(seconds: 1);
			var stored = ReadAll<AddressEntity>(database.Addresses)[0];
			stored.City = "Test";
			stored.ModifiedOn = UtcNow;
			Save(database, stored);

			var changes = client.GetChanges(sessionId, new SyncRequest
			{
				Since = DateTime.MinValue,
				Until = session.StartedOn,
				Take = 100
			});
			AreEqual(1, changes.Collection.Count);
			AreEqual(stored.SyncId, changes.Collection[0].SyncId);
		});
	}

	[TestMethod]
	public void OldChildOfNewParentSyncsWhenOrderedByParentSyncId()
	{
		WithEachPair((client, server, manager) =>
		{
			var child = AddBookmark(client, "Old Page");
			var parent = AddBookmark(client, "New Folder", true);
			var storedChild = (BookmarkEntity) client.Bookmarks.Read(child.SyncId);
			storedChild.ParentSyncId = parent.SyncId;
			storedChild.ModifiedOn = UtcNow;
			Save(client, storedChild);

			RunSync(manager);

			AreEqual(2, ReadAll<BookmarkEntity>(server.Bookmarks).Count);
			var serverChild = (BookmarkEntity) server.Bookmarks.Read(child.SyncId);
			var serverParent = (BookmarkEntity) server.Bookmarks.Read(parent.SyncId);
			IsNotNull(serverChild);
			IsNotNull(serverParent);
			AreEqual(parent.SyncId, serverChild.ParentSyncId);
		});
	}

	[TestMethod]
	public void OlderClientDoesNotOverwriteNewerServer()
	{
		WithEachPair((client, server, manager) =>
		{
			var address = AddAddress(server, "Original");
			RunSync(manager);

			var clientAddress = client.Addresses.Read(address.SyncId) as AddressEntity;
			IsNotNull(clientAddress);
			clientAddress.Line1 = "OlderOnClient";
			clientAddress.ModifiedOn = UtcNow;
			Save(client, clientAddress);

			var serverAddress = server.Addresses.Read(address.SyncId) as AddressEntity;
			IsNotNull(serverAddress);
			serverAddress.Line1 = "NewerOnServer";
			serverAddress.ModifiedOn = UtcNow;
			Save(server, serverAddress);

			RunSync(manager);

			AreEqual("NewerOnServer", ((AddressEntity) client.Addresses.Read(address.SyncId)).Line1);
			AreEqual("NewerOnServer", ((AddressEntity) server.Addresses.Read(address.SyncId)).Line1);
		});
	}

	[TestMethod]
	public void OlderClientTombstoneDoesNotOverwriteNewerHubOnPush()
	{
		WithEachPair((client, server, manager) =>
		{
			var address = AddAddress(server, "123 Elm Street");
			RunSync(manager);

			var clientAddress = client.Addresses.Read(address.SyncId) as AddressEntity;
			IsNotNull(clientAddress);
			clientAddress.IsDeleted = true;
			clientAddress.ModifiedOn = UtcNow;
			Save(client, clientAddress);

			IncrementTime(minutes: 1);

			var serverAddress = server.Addresses.Read(address.SyncId) as AddressEntity;
			IsNotNull(serverAddress);
			serverAddress.Line1 = "Spoke A";
			serverAddress.ModifiedOn = UtcNow;
			Save(server, serverAddress);

			RunSync(manager, settings => settings.SyncDirection = SyncDirection.PushUp);

			var clientStored = (AddressEntity) client.Addresses.Read(address.SyncId);
			var serverStored = (AddressEntity) server.Addresses.Read(address.SyncId);
			IsTrue(clientStored.IsDeleted);
			IsFalse(serverStored.IsDeleted);
			AreEqual("Spoke A", serverStored.Line1);
		});
	}

	[TestMethod]
	public void OlderTombstoneStillDeletesWhenLocalIsNewer()
	{
		WithEachPair((client, server, manager) =>
		{
			var address = AddAddress(server, "123 Elm Street");
			RunSync(manager);

			var serverAddress = server.Addresses.Read(address.SyncId) as AddressEntity;
			IsNotNull(serverAddress);
			serverAddress.IsDeleted = true;
			serverAddress.ModifiedOn = UtcNow;
			Save(server, serverAddress);

			IncrementTime(minutes: 1);

			var clientAddress = client.Addresses.Read(address.SyncId) as AddressEntity;
			IsNotNull(clientAddress);
			clientAddress.Line1 = "123 Client Street";
			clientAddress.ModifiedOn = UtcNow;
			Save(client, clientAddress);

			RunSync(manager);

			var clientStored = (AddressEntity) client.Addresses.Read(address.SyncId);
			var serverStored = (AddressEntity) server.Addresses.Read(address.SyncId);
			IsTrue(clientStored.IsDeleted);
			IsTrue(serverStored.IsDeleted);
		});
	}

	[TestMethod]
	public void PagedPullSyncsAllItems()
	{
		WithEachPair((client, server, manager) =>
		{
			AddAddress(server, "One");
			AddAddress(server, "Two");
			AddAddress(server, "Three");

			RunSync(manager, settings => settings.ItemsPerSyncRequest = 1);

			var clientLines = ReadAll<AddressEntity>(client.Addresses).Select(x => x.Line1).ToList();
			AreEqual(3, clientLines.Count);
			CollectionAssert.AreEquivalent(new[] { "One", "Two", "Three" }, clientLines);
		});
	}

	[TestMethod]
	public void PagedPushSyncsAllItems()
	{
		WithEachPair((client, server, manager) =>
		{
			AddAddress(client, "One");
			AddAddress(client, "Two");
			AddAddress(client, "Three");

			RunSync(manager, settings => settings.ItemsPerSyncRequest = 1);

			var clientLines = ReadAll<AddressEntity>(client.Addresses).Select(x => x.Line1).ToList();
			var serverLines = ReadAll<AddressEntity>(server.Addresses).Select(x => x.Line1).ToList();
			AreEqual(3, clientLines.Count);
			AreEqual(3, serverLines.Count);
			CollectionAssert.AreEquivalent(clientLines, serverLines);
			CollectionAssert.AreEquivalent(new[] { "One", "Two", "Three" }, serverLines);
		});
	}

	[TestMethod]
	public void PermanentDeleteIsSoftOnWebLoopbackServer()
	{
		WithEachWebPair((client, server, manager) =>
		{
			var address = AddAddress(client, "Home");
			RunSync(manager);

			var local = client.Addresses.Read(address.SyncId) as AddressEntity;
			IsNotNull(local);
			local.IsDeleted = true;
			local.ModifiedOn = UtcNow;
			Save(client, local);

			RunSync(manager, settings => settings.PermanentDeletions = true);

			var serverAddress = server.Addresses.Read(address.SyncId) as AddressEntity;
			IsNotNull(serverAddress);
			IsTrue(serverAddress.IsDeleted);
		});
	}

	[TestMethod]
	public void PermanentDeleteRemovesRowOnPeerPair()
	{
		WithEachPair((client, server, _) =>
		{
			var manager = new SampleSyncManager(
				new SampleSyncClientProvider("Client", ScenarioClientProvider, this),
				new SampleSyncClientProvider("Peer", ScenarioServerProvider, this),
				new SyncSession(this),
				RuntimeInformation,
				this,
				Dispatcher
			);
			manager.GetSyncSettings(SampleSyncClient.SyncAll).IncludeIssueDetails = true;

			var address = AddAddress(client, "Home");
			RunSync(manager);

			var local = client.Addresses.Read(address.SyncId) as AddressEntity;
			IsNotNull(local);
			local.IsDeleted = true;
			local.ModifiedOn = UtcNow;
			Save(client, local);

			RunSync(manager, settings => settings.PermanentDeletions = true);

			var stored = server.Addresses.Read(address.SyncId) as AddressEntity;
			if (ScenarioPairName.Contains("-Ef"))
			{
				IsTrue((stored == null) || stored.IsDeleted);
			}
			else
			{
				IsNull(stored);
			}
		});
	}

	[TestMethod]
	public void PermanentDeleteStillSoftDeletesOnSqlPair()
	{
		WithEachPair((client, server, manager) =>
		{
			var address = AddAddress(client, "Home");
			RunSync(manager);

			var local = client.Addresses.Read(address.SyncId) as AddressEntity;
			IsNotNull(local);
			local.IsDeleted = true;
			local.ModifiedOn = UtcNow;
			Save(client, local);

			RunSync(manager, settings => settings.PermanentDeletions = true);

			var serverAddress = server.Addresses.Read(address.SyncId) as AddressEntity;
			IsNotNull(serverAddress);
			IsTrue(serverAddress.IsDeleted);
			var clientAddress = client.Addresses.Read(address.SyncId) as AddressEntity;
			IsNotNull(clientAddress);
			IsTrue(clientAddress.IsDeleted);
		});
	}

	[TestMethod]
	public void PullDownShouldNotPushClientContent()
	{
		WithEachPair((client, server, manager) =>
		{
			AddAddress(client, "ClientOnly");
			RunSync(manager, settings => settings.SyncDirection = SyncDirection.PullDown);

			AreEqual(1, ReadAll<AddressEntity>(client.Addresses).Count);
			AreEqual(0, ReadAll<AddressEntity>(server.Addresses).Count);
		});
	}

	[TestMethod]
	public void PullDownShouldPullServerAndNotPushClient()
	{
		WithEachPair((client, server, manager) =>
		{
			AddAddress(client, "Hello World");
			AddAddress(server, "Foo Bar");
			RunSync(manager, settings => settings.SyncDirection = SyncDirection.PullDown);

			var clientAddresses = ReadAll<AddressEntity>(client.Addresses);
			var serverAddresses = ReadAll<AddressEntity>(server.Addresses);
			AreEqual(2, clientAddresses.Count);
			AreEqual(1, serverAddresses.Count);
			IsTrue(clientAddresses.Any(x => x.Line1 == "Foo Bar"));
			IsFalse(serverAddresses.Any(x => x.Line1 == "Hello World"));
		});
	}

	[TestMethod]
	public void PushUpShouldNotPullServerContent()
	{
		WithEachPair((client, server, manager) =>
		{
			AddAddress(server, "ServerOnly");
			RunSync(manager, settings => settings.SyncDirection = SyncDirection.PushUp);

			AreEqual(0, ReadAll<AddressEntity>(client.Addresses).Count);
			AreEqual(1, ReadAll<AddressEntity>(server.Addresses).Count);
		});
	}

	[TestMethod]
	public void PushUpShouldPushClientAndNotPullServer()
	{
		WithEachPair((client, server, manager) =>
		{
			AddAddress(client, "Hello World");
			AddAddress(server, "Foo Bar");
			RunSync(manager, settings => settings.SyncDirection = SyncDirection.PushUp);

			var clientAddresses = ReadAll<AddressEntity>(client.Addresses);
			var serverAddresses = ReadAll<AddressEntity>(server.Addresses);
			AreEqual(1, clientAddresses.Count);
			AreEqual(2, serverAddresses.Count);
			IsTrue(serverAddresses.Any(x => x.Line1 == "Hello World"));
			IsFalse(clientAddresses.Any(x => x.Line1 == "Foo Bar"));
		});
	}

	[TestMethod]
	public void ReparentExistingBookmarkOnClient()
	{
		WithEachPair((client, server, manager) =>
		{
			var oldParent = AddBookmark(client, "Old Folder", true);
			var newParent = AddBookmark(client, "New Folder", true);
			var child = AddBookmark(client, "Page", parentSyncId: oldParent.SyncId);
			RunSync(manager);

			var storedChild = (BookmarkEntity) client.Bookmarks.Read(child.SyncId);
			storedChild.ParentSyncId = newParent.SyncId;
			storedChild.ModifiedOn = UtcNow;
			Save(client, storedChild);

			RunSync(manager);

			var serverChild = (BookmarkEntity) server.Bookmarks.Read(child.SyncId);
			var serverNewParent = (BookmarkEntity) server.Bookmarks.Read(newParent.SyncId);
			IsNotNull(serverChild);
			IsNotNull(serverNewParent);
			AreEqual(newParent.SyncId, serverChild.ParentSyncId);
			AreEqual(serverNewParent.Id, serverChild.ParentId);
			AreEqual(newParent.SyncId, ((BookmarkEntity) client.Bookmarks.Read(child.SyncId)).ParentSyncId);
		});
	}

	[TestMethod]
	public void ReparentExistingBookmarkOnServer()
	{
		WithEachPair((client, server, manager) =>
		{
			var oldParent = AddBookmark(server, "Old Folder", true);
			var newParent = AddBookmark(server, "New Folder", true);
			var child = AddBookmark(server, "Page", parentSyncId: oldParent.SyncId);
			RunSync(manager);

			var storedChild = (BookmarkEntity) server.Bookmarks.Read(child.SyncId);
			storedChild.ParentSyncId = newParent.SyncId;
			storedChild.ModifiedOn = UtcNow;
			Save(server, storedChild);

			RunSync(manager);

			var clientChild = (BookmarkEntity) client.Bookmarks.Read(child.SyncId);
			var clientNewParent = (BookmarkEntity) client.Bookmarks.Read(newParent.SyncId);
			IsNotNull(clientChild);
			IsNotNull(clientNewParent);
			AreEqual(newParent.SyncId, clientChild.ParentSyncId);
			AreEqual(clientNewParent.Id, clientChild.ParentId);
		});
	}

	[TestMethod]
	public void SameSyncIdKeepsLaterClientContent()
	{
		WithEachPair((client, server, manager) =>
		{
			var syncId = Guid.NewGuid();
			AddAddress(server, "Server", syncId: syncId);
			AddAddress(client, "Client", syncId: syncId);
			RunSync(manager);

			AreEqual(1, ReadAll<AddressEntity>(client.Addresses).Count);
			AreEqual(1, ReadAll<AddressEntity>(server.Addresses).Count);
			AreEqual("Client", ((AddressEntity) client.Addresses.Read(syncId)).Line1);
			AreEqual("Client", ((AddressEntity) server.Addresses.Read(syncId)).Line1);
		});
	}

	[TestMethod]
	public void ServerLimitRepositoryToAddressOnly()
	{
		WithEachPair((client, server, manager) =>
		{
			var address = AddAddress(client, "Home");
			AddAccount(client, "John", address);

			RunSync(manager, settings =>
			{
				settings.AddFilter<AddressEntity>();
				settings.AddFilter<AccountEntity>(outgoingFilter: x => x.Name == "__none__");
			});

			AreEqual(1, ReadAll<AddressEntity>(server.Addresses).Count);
			AreEqual(0, ReadAll<AccountEntity>(server.Accounts).Count);
		});
	}

	[TestMethod]
	public void ServerShouldNotPushUnmodifiedEntitiesOnSecondSync()
	{
		WithEachPair((client, server, manager) =>
		{
			AddAddress(server, "Blah");
			RunSync(manager);
			var second = RunSync(manager);
			AreEqual(0, second.StatisticsForServer.Changes);
			AreEqual(0, second.StatisticsForClient.AppliedChanges);
		});
	}

	[TestMethod]
	public void SoftDeletedEntitiesShouldNotSyncOnInitial()
	{
		WithEachPair((client, server, manager) =>
		{
			var address = AddAddress(server, "Blah");
			var stored = (AddressEntity) server.Addresses.Read(address.SyncId);
			stored.IsDeleted = true;
			stored.ModifiedOn = UtcNow;
			Save(server, stored);

			RunSync(manager, settings =>
			{
				settings.AddFilter<AddressEntity>(skipDeletedItemsOnInitialSync: true);
				settings.AddFilter<AccountEntity>();
			});

			AreEqual(0, ReadAll<AddressEntity>(client.Addresses).Count);
			AreEqual(1, ReadAll<AddressEntity>(server.Addresses).Count);
		});
	}

	[TestMethod]
	public void SoftDeletedEntitiesShouldSyncOnInitial()
	{
		WithEachPair((client, server, manager) =>
		{
			var address = AddAddress(server, "Blah");
			var stored = (AddressEntity) server.Addresses.Read(address.SyncId);
			stored.IsDeleted = true;
			stored.ModifiedOn = UtcNow;
			Save(server, stored);

			RunSync(manager, settings => { settings.AddFilter<AddressEntity>(skipDeletedItemsOnInitialSync: false); });

			AreEqual(1, ReadAll<AddressEntity>(client.Addresses).Count);
			IsTrue(ReadAll<AddressEntity>(client.Addresses)[0].IsDeleted);
		});
	}

	[TestMethod]
	public void SoftDeletedServerRowUnknownToClientIsCreatedAsTombstone()
	{
		WithEachPair((client, server, manager) =>
		{
			var address = AddAddress(server, "Never Seen");
			var stored = (AddressEntity) server.Addresses.Read(address.SyncId);
			stored.IsDeleted = true;
			stored.ModifiedOn = UtcNow;
			Save(server, stored);

			AreEqual(0, ReadAll<AddressEntity>(client.Addresses).Count);

			RunSync(manager, settings => { settings.AddFilter<AddressEntity>(skipDeletedItemsOnInitialSync: false); });

			var clientAddress = client.Addresses.Read(address.SyncId) as AddressEntity;
			IsNotNull(clientAddress);
			IsTrue(clientAddress.IsDeleted);
			AreEqual("Never Seen", clientAddress.Line1);
		});
	}

	[TestMethod]
	public void SyncShouldNotSendItemsBackUp()
	{
		WithEachPair((client, server, manager) =>
		{
			AddAddress(server, "Blah");
			var session = RunSync(manager);
			AreEqual(0, session.StatisticsForServer.AppliedChanges);
			IsTrue(session.StatisticsForServer.Changes > 0);
			IsTrue(session.StatisticsForClient.AppliedChanges > 0);
		});
	}

	[TestMethod]
	public void TenantAccountDoesNotBindOtherCustomerAddress()
	{
		WithEachPair((client, server, manager) =>
		{
			var customer1 = AddCustomer(server, "Customer 1", Customer1SyncId);
			var customer2 = AddCustomer(server, "Customer 2", Customer2SyncId);
			var address1 = AddAddress(server, "One Main", customer: customer1);
			var address2 = AddAddress(server, "Two Main", customer: customer2);
			AddAccount(server, "Cust1", address1, customer1);
			RunSync(manager);

			var clientAccount = ReadAll<AccountEntity>(client.Accounts).Single(x => x.Name == "Cust1");
			clientAccount.AddressSyncId = address2.SyncId;
			clientAccount.ModifiedOn = UtcNow;
			Save(client, clientAccount);

			var session = manager.Sync(SampleSyncClient.SyncAll, null, TimeSpan.FromSeconds(30));
			DetachScenarioDatabases();
			IncrementTime(seconds: 1);

			var serverAccount = ReadAll<AccountEntity>(server.Accounts).Single(x => x.Name == "Cust1");
			AreEqual(address1.Id, serverAccount.AddressId);
			AreEqual(address1.SyncId, serverAccount.AddressSyncId);
			AreNotEqual(address2.Id, serverAccount.AddressId);
			IsTrue(session.SyncIssues.Count > 0);
		});
	}

	[TestMethod]
	public void TenantFilterDoesNotPullOtherCustomer()
	{
		WithEachPair((client, server, manager) =>
		{
			var customer1 = AddCustomer(server, "Customer 1", Customer1SyncId);
			var customer2 = AddCustomer(server, "Customer 2", Customer2SyncId);
			var address1 = AddAddress(server, "One Main", customer: customer1);
			AddAddress(server, "Two Main", customer: customer2);
			ServerAuthenticatedAccount = AddAccount(server, "Cust1", address1, customer1);

			RunSync(manager, settings => settings.Values["CustomerSyncId"] = Customer2SyncId.ToString());

			AreEqual(1, ReadAll<CustomerEntity>(client.Customers).Count);
			AreEqual(Customer1SyncId, ReadAll<CustomerEntity>(client.Customers)[0].SyncId);
			AreEqual(1, ReadAll<AddressEntity>(client.Addresses).Count);
			AreEqual("One Main", ReadAll<AddressEntity>(client.Addresses)[0].Line1);
			AreEqual(2, ReadAll<AddressEntity>(server.Addresses).Count);
		});
	}

	[TestMethod]
	public void ThreeWaySyncShouldWork()
	{
		WithEachHomogeneousTrio((serverProvider, serverDb, client1Provider, client1Db, client2Provider, client2Db) =>
		{
			AddAddress(serverDb, "123 Elm Street", syncId: Guid.Parse("00000000-0000-0000-0000-000000000001"));

			var manager1 = NewPeerManager(client1Provider, serverProvider);
			var manager2 = NewPeerManager(client2Provider, serverProvider);
			RunSync(manager1);
			DetachTrackedEntities(client1Db);
			DetachTrackedEntities(client2Db);
			DetachTrackedEntities(serverDb);
			RunSync(manager2);
			DetachTrackedEntities(client1Db);
			DetachTrackedEntities(client2Db);
			DetachTrackedEntities(serverDb);

			AreEqual(1, ReadAll<AddressEntity>(client1Db.Addresses).Count);
			AreEqual(1, ReadAll<AddressEntity>(client2Db.Addresses).Count);

			AddAddress(client2Db, "123 Main Street", syncId: Guid.Parse("00000000-0000-0000-0000-000000000002"));
			IncrementTime(seconds: 1);
			RunSync(manager2);
			DetachTrackedEntities(client1Db);
			DetachTrackedEntities(client2Db);
			DetachTrackedEntities(serverDb);
			IncrementTime(seconds: 1);
			RunSync(manager1);
			DetachTrackedEntities(client1Db);
			DetachTrackedEntities(client2Db);
			DetachTrackedEntities(serverDb);

			AreEqual(2, ReadAll<AddressEntity>(client1Db.Addresses).Count);
			AreEqual(2, ReadAll<AddressEntity>(client2Db.Addresses).Count);
			AreEqual(2, ReadAll<AddressEntity>(serverDb.Addresses).Count);
		});
	}

	[TestMethod]
	public void UndeleteItemOnClient()
	{
		WithEachPair((client, server, manager) =>
		{
			var address = AddAddress(server, "123 Elm Street");
			RunSync(manager);

			var clientAddress = client.Addresses.Read(address.SyncId) as AddressEntity;
			IsNotNull(clientAddress);
			clientAddress.IsDeleted = true;
			clientAddress.ModifiedOn = UtcNow;
			Save(client, clientAddress);
			RunSync(manager);

			IsTrue(((AddressEntity) server.Addresses.Read(address.SyncId)).IsDeleted);

			clientAddress = client.Addresses.Read(address.SyncId) as AddressEntity;
			IsNotNull(clientAddress);
			clientAddress.IsDeleted = false;
			clientAddress.Line1 = "Back";
			clientAddress.ModifiedOn = UtcNow;
			Save(client, clientAddress);
			RunSync(manager, settings => settings.SyncDirection = SyncDirection.PushUp);

			var serverStored = (AddressEntity) server.Addresses.Read(address.SyncId);
			var clientStored = (AddressEntity) client.Addresses.Read(address.SyncId);
			IsFalse(serverStored.IsDeleted);
			IsFalse(clientStored.IsDeleted);
			AreEqual("Back", serverStored.Line1);
			AreEqual("Back", clientStored.Line1);
		});
	}

	[TestMethod]
	public void UnknownIncomingTypeDoesNotBlockGoodAddress()
	{
		WithEachPair((client, server, _) =>
		{
			AddAddress(client, "Good");

			var clientSync = new SampleSyncClient("Client", ScenarioClientProvider, this, new SyncStatistics(), new Profiler("Client"));
			var serverSync = new SampleSyncClient("Server", ScenarioServerProvider, this, new SyncStatistics(), new Profiler("Server"));
			var sessionId = Guid.NewGuid();
			var settings = new SyncSettings { IncludeIssueDetails = true };
			settings.AddFilter<AddressEntity>();
			settings.AddFilter<AccountEntity>();
			clientSync.BeginSync(sessionId, settings);

			var changes = clientSync.GetChanges(sessionId, new SyncRequest
			{
				Since = DateTime.MinValue,
				Until = UtcNow.AddMinutes(1),
				Take = 100
			});
			IsTrue(changes.Collection.Count >= 1);

			changes.Collection.Insert(0, new SyncObject
			{
				SyncId = Guid.NewGuid(),
				TypeName = "Unknown.Type, Unknown",
				Status = SyncObjectStatus.Added,
				ModifiedOn = UtcNow,
				Data = [1, 2, 3]
			});

			var result = serverSync.Sync(new SyncOperation
			{
				SessionId = sessionId,
				Settings = settings,
				Changes = new ServiceRequest<SyncObject>(changes.Collection)
			});
			DetachScenarioDatabases();

			AreEqual("Good", ReadAll<AddressEntity>(server.Addresses).Single(x => x.Line1 == "Good").Line1);
			IsTrue(result.AppliedIssues.Collection.Any(x => x.TypeName == "Unknown.Type, Unknown"));
		});
	}

	[TestMethod]
	public void UnparentExistingBookmarkOnClient()
	{
		WithEachPair((client, server, manager) =>
		{
			var parent = AddBookmark(client, "Folder", true);
			var child = AddBookmark(client, "Page", parentSyncId: parent.SyncId);
			RunSync(manager);

			var storedChild = (BookmarkEntity) client.Bookmarks.Read(child.SyncId);
			storedChild.ParentSyncId = null;
			storedChild.ModifiedOn = UtcNow;
			Save(client, storedChild);

			RunSync(manager);

			var serverChild = (BookmarkEntity) server.Bookmarks.Read(child.SyncId);
			IsNotNull(serverChild);
			IsNull(serverChild.ParentSyncId);
			IsNull(serverChild.ParentId);
			IsNull(((BookmarkEntity) client.Bookmarks.Read(child.SyncId)).ParentSyncId);
		});
	}

	[TestMethod]
	public void UpdateAccountAddressOnClient()
	{
		WithEachPair((client, server, manager) =>
		{
			var first = AddAddress(client, "First");
			var second = AddAddress(client, "Second");
			var account = AddAccount(client, "John", first);
			RunSync(manager);

			var stored = (AccountEntity) client.Accounts.Read(account.SyncId);
			stored.AddressSyncId = second.SyncId;
			stored.ModifiedOn = UtcNow;
			Save(client, stored);

			RunSync(manager);

			var serverAccount = (AccountEntity) server.Accounts.Read(account.SyncId);
			var serverSecond = (AddressEntity) server.Addresses.Read(second.SyncId);
			IsNotNull(serverAccount);
			IsNotNull(serverSecond);
			AreEqual(second.SyncId, serverAccount.AddressSyncId);
			AreEqual(serverSecond.Id, serverAccount.AddressId);
		});
	}

	[TestMethod]
	public void UpdateBookmarkNameAndOrderOnClient()
	{
		WithEachPair((client, server, manager) =>
		{
			var bookmark = AddBookmark(client, "Old", order: 1);
			RunSync(manager);

			var stored = (BookmarkEntity) client.Bookmarks.Read(bookmark.SyncId);
			stored.Name = "New";
			stored.Order = 5;
			stored.ModifiedOn = UtcNow;
			Save(client, stored);

			RunSync(manager);

			var serverBookmark = (BookmarkEntity) server.Bookmarks.Read(bookmark.SyncId);
			AreEqual("New", serverBookmark.Name);
			AreEqual(5, serverBookmark.Order);
			AreEqual("New", ((BookmarkEntity) client.Bookmarks.Read(bookmark.SyncId)).Name);
		});
	}

	[TestMethod]
	public void UpdateItemOnClient()
	{
		WithEachPair((client, server, manager) =>
		{
			var address = AddAddress(server, "123 Elm Street");
			RunSync(manager);

			var clientAddress = client.Addresses.Read(address.SyncId) as AddressEntity;
			IsNotNull(clientAddress);
			clientAddress.Line1 = "123 Client Street";
			clientAddress.City = "ClientCity";
			clientAddress.ModifiedOn = UtcNow;
			Save(client, clientAddress);

			RunSync(manager);

			AreEqual("123 Client Street", ((AddressEntity) server.Addresses.Read(address.SyncId)).Line1);
			AreEqual("ClientCity", ((AddressEntity) server.Addresses.Read(address.SyncId)).City);
			AreEqual("123 Client Street", ((AddressEntity) client.Addresses.Read(address.SyncId)).Line1);
		});
	}

	[TestMethod]
	public void UpdateItemOnClientThenServer()
	{
		WithEachPair((client, server, manager) =>
		{
			var address = AddAddress(server, "123 Elm Street");
			RunSync(manager);

			var serverAddress = server.Addresses.Read(address.SyncId) as AddressEntity;
			IsNotNull(serverAddress);
			serverAddress.Line1 = "Foo Server";
			serverAddress.City = "ServerCity";
			serverAddress.ModifiedOn = UtcNow;
			Save(server, serverAddress);

			RunSync(manager);

			AreEqual("Foo Server", ((AddressEntity) client.Addresses.Read(address.SyncId)).Line1);
			AreEqual("ServerCity", ((AddressEntity) client.Addresses.Read(address.SyncId)).City);
			AreEqual("Foo Server", ((AddressEntity) server.Addresses.Read(address.SyncId)).Line1);
		});
	}

	[TestMethod]
	public void UpdateItemOnClientThroughWebLoopback()
	{
		WithEachWebPair((client, server, manager) =>
		{
			var address = AddAddress(client, "123 Elm Street");
			RunSync(manager);

			var clientAddress = client.Addresses.Read(address.SyncId) as AddressEntity;
			IsNotNull(clientAddress);
			clientAddress.Line1 = "123 Client Street";
			clientAddress.ModifiedOn = UtcNow;
			Save(client, clientAddress);

			RunSync(manager);

			AreEqual("123 Client Street", ((AddressEntity) server.Addresses.Read(address.SyncId)).Line1);
			AreEqual("123 Client Street", ((AddressEntity) client.Addresses.Read(address.SyncId)).Line1);
		});
	}

	[TestMethod]
	public void UpdateItemOnServerThenClient()
	{
		WithEachPair((client, server, manager) =>
		{
			var address = AddAddress(server, "123 Elm Street");
			RunSync(manager);

			var serverAddress = server.Addresses.Read(address.SyncId) as AddressEntity;
			IsNotNull(serverAddress);
			serverAddress.Line1 = "123 Server Street";
			serverAddress.ModifiedOn = UtcNow;
			Save(server, serverAddress);

			var clientAddress = client.Addresses.Read(address.SyncId) as AddressEntity;
			IsNotNull(clientAddress);
			clientAddress.Line1 = "123 Client Street";
			clientAddress.ModifiedOn = UtcNow;
			Save(client, clientAddress);

			RunSync(manager);

			AreEqual("123 Client Street", ((AddressEntity) client.Addresses.Read(address.SyncId)).Line1);
			AreEqual("123 Client Street", ((AddressEntity) server.Addresses.Read(address.SyncId)).Line1);
		});
	}

	[TestMethod]
	public void ValidAddressStillAppliesWhenAccountRejected()
	{
		WithEachPair((client, server, manager) =>
		{
			var customer1 = AddCustomer(server, "Customer 1", Customer1SyncId);
			var customer2 = AddCustomer(server, "Customer 2", Customer2SyncId);
			var address1 = AddAddress(server, "One Main", customer: customer1);
			var address2 = AddAddress(server, "Two Main", customer: customer2);
			AddAccount(server, "Cust1", address1, customer1);
			RunSync(manager);

			AddAddress(client, "Good New Street", customer: ReadAll<CustomerEntity>(client.Customers).Single(x => x.SyncId == Customer1SyncId));
			var clientAccount = ReadAll<AccountEntity>(client.Accounts).Single(x => x.Name == "Cust1");
			clientAccount.AddressSyncId = address2.SyncId;
			clientAccount.Name = "Hacked";
			clientAccount.Roles = ",Admin,";
			clientAccount.ModifiedOn = UtcNow;
			Save(client, clientAccount);

			var session = manager.Sync(SampleSyncClient.SyncAll, null, TimeSpan.FromSeconds(30));
			DetachScenarioDatabases();
			IncrementTime(seconds: 1);
			IsTrue(session.SyncCompleted);

			IsNotNull(server.Addresses.Read(ReadAll<AddressEntity>(client.Addresses).Single(x => x.Line1 == "Good New Street").SyncId));
			AreEqual("Good New Street", ReadAll<AddressEntity>(server.Addresses).Single(x => x.Line1 == "Good New Street").Line1);

			var serverAccount = ReadAll<AccountEntity>(server.Accounts).Single(x => x.Name == "Cust1");
			AreEqual("Cust1", serverAccount.Name);
			AreEqual(",,", serverAccount.Roles);
			AreEqual(address1.Id, serverAccount.AddressId);
			AreEqual(address1.SyncId, serverAccount.AddressSyncId);
			AreNotEqual(address2.Id, serverAccount.AddressId);
		});
	}

	#endregion
}