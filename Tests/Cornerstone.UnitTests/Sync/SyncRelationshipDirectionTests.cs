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
/// Relationships, repository sync order, lookup keys, and scope filters for each direction.
/// GetChanges walks repositories in sync order. A type with orderBy uses that order, then ModifiedOn.
/// Bookmark order is ParentSyncId, then Order, so a parent precedes its children.
/// </summary>
[TestClass]
[DoNotParallelize]
public class SyncRelationshipDirectionTests : SyncRecordingTest
{
	#region Methods

	[TestMethod]
	public void AccountWithMissingAddressStillSyncsOnPushUp()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var missing = Guid.NewGuid();
			var account = AddAccount(client, "John");
			account.AddressSyncId = missing;
			account.ModifiedOn = UtcNow;
			Save(client, account);

			var session = RunDirection(manager, SyncDirection.PushUp);

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: 1, endSession: true);
			var stored = (AccountEntity) server.Accounts.Read(account.SyncId);
			IsNotNull(stored);
			AreEqual(missing, stored.AddressSyncId);
			IsNull(stored.AddressId);
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 1);
		});
	}

	[TestMethod]
	public void AddressSyncsBeforeANewerAccountOnPullDown()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			AddAddress(client, "Spacer");
			var address = AddAddress(server, "Home");
			var account = AddAccount(server, "John", address);
			Touch(server, address);

			var session = RunDirection(manager, SyncDirection.PullDown, settings => settings.ItemsPerSyncRequest = 1);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(2, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: false, clientHasNoChanges: true);
			AssertPull(calls[1], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			AreEqual(address.SyncId, calls[0].ResultChangeIds[0]);
			AreEqual(account.SyncId, calls[1].ResultChangeIds[0]);
			var clientAddress = (AddressEntity) client.Addresses.Read(address.SyncId);
			var clientAccount = (AccountEntity) client.Accounts.Read(account.SyncId);
			AreEqual(address.SyncId, clientAccount.AddressSyncId);
			AreEqual(clientAddress.Id, clientAccount.AddressId);
			AreNotEqual(address.Id, clientAccount.AddressId);
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 2, serverChanges: 2, serverApplied: 0);
		});
	}

	[TestMethod]
	public void AddressSyncsBeforeANewerAccountOnPushUp()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			AddAddress(server, "Spacer");
			var address = AddAddress(client, "Home");
			var account = AddAccount(client, "John", address);
			Touch(client, address);

			var session = RunDirection(manager, SyncDirection.PushUp, settings => settings.ItemsPerSyncRequest = 1);

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(3, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: 1, endSession: false);
			AssertPush(calls[1], changeCount: 1, endSession: false);
			AssertEndOnly(calls[2]);
			AreEqual(address.SyncId, calls[0].ChangeIds[0]);
			AreEqual(account.SyncId, calls[1].ChangeIds[0]);
			var serverAddress = (AddressEntity) server.Addresses.Read(address.SyncId);
			var serverAccount = (AccountEntity) server.Accounts.Read(account.SyncId);
			AreEqual(address.SyncId, serverAccount.AddressSyncId);
			AreEqual(serverAddress.Id, serverAccount.AddressId);
			AreNotEqual(address.Id, serverAccount.AddressId);
			AssertStatistics(session, manager, clientChanges: 2, clientApplied: 0, serverChanges: 0, serverApplied: 2);
		});
	}

	[TestMethod]
	public void OlderChildFollowsItsParentOnPullDown()
	{
		WithRecording((client, server, manager, calls, _, serverProvider) =>
		{
			var child = AddBookmark(server, "Page");
			var parent = AddBookmark(server, "Folder", true);
			LinkParent(serverProvider, server, child, parent, parent.ModifiedOn.AddSeconds(-1));

			var session = RunDirection(manager, SyncDirection.PullDown, settings => settings.ItemsPerSyncRequest = 1);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(2, calls.Count, () => FormatCalls(calls));
			AreEqual(parent.SyncId, calls[0].ResultChangeIds[0]);
			AreEqual(child.SyncId, calls[1].ResultChangeIds[0]);
			var clientParent = (BookmarkEntity) client.Bookmarks.Read(parent.SyncId);
			var stored = (BookmarkEntity) client.Bookmarks.Read(child.SyncId);
			AreEqual(parent.SyncId, stored.ParentSyncId);
			AreEqual(clientParent.Id, stored.ParentId);
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 2, serverChanges: 2, serverApplied: 0);
		});
	}

	[TestMethod]
	public void OlderChildFollowsItsParentOnPushUp()
	{
		WithRecording((client, server, manager, calls, clientProvider, _) =>
		{
			var child = AddBookmark(client, "Page");
			var parent = AddBookmark(client, "Folder", true);
			LinkParent(clientProvider, client, child, parent, parent.ModifiedOn.AddSeconds(-1));

			var session = RunDirection(manager, SyncDirection.PushUp, settings => settings.ItemsPerSyncRequest = 1);

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(3, calls.Count, () => FormatCalls(calls));
			AreEqual(parent.SyncId, calls[0].ChangeIds[0]);
			AreEqual(child.SyncId, calls[1].ChangeIds[0]);
			var serverParent = (BookmarkEntity) server.Bookmarks.Read(parent.SyncId);
			var stored = (BookmarkEntity) server.Bookmarks.Read(child.SyncId);
			AreEqual(parent.SyncId, stored.ParentSyncId);
			AreEqual(serverParent.Id, stored.ParentId);
			AssertStatistics(session, manager, clientChanges: 2, clientApplied: 0, serverChanges: 0, serverApplied: 2);
		});
	}

	[TestMethod]
	public void ParentBeforeChildBindsParentIdOnPullDown()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var parent = AddBookmark(server, "Folder", true);
			var child = AddBookmark(server, "Page", parentSyncId: parent.SyncId);

			var session = RunDirection(manager, SyncDirection.PullDown, settings => settings.ItemsPerSyncRequest = 1);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(2, calls.Count, () => FormatCalls(calls));
			AreEqual(parent.SyncId, calls[0].ResultChangeIds[0]);
			AreEqual(child.SyncId, calls[1].ResultChangeIds[0]);
			var clientParent = (BookmarkEntity) client.Bookmarks.Read(parent.SyncId);
			var clientChild = (BookmarkEntity) client.Bookmarks.Read(child.SyncId);
			AreEqual(parent.SyncId, clientChild.ParentSyncId);
			AreEqual(clientParent.Id, clientChild.ParentId);
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 2, serverChanges: 2, serverApplied: 0);
		});
	}

	[TestMethod]
	public void SamePageParentBindsParentId()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var parent = AddBookmark(client, "Folder", true);
			var child = AddBookmark(client, "Page", parentSyncId: parent.SyncId);

			var session = RunDirection(manager, SyncDirection.PullDownThenPushUp);

			AssertPhases(session, pulling: true, pushing: true);
			AreEqual(2, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 0, sessionEnded: false, clientHasNoChanges: false);
			AssertPush(calls[1], changeCount: 2, endSession: true);
			var serverParent = (BookmarkEntity) server.Bookmarks.Read(parent.SyncId);
			var serverChild = (BookmarkEntity) server.Bookmarks.Read(child.SyncId);
			AreEqual(parent.SyncId, serverChild.ParentSyncId);
			AreEqual(serverParent.Id, serverChild.ParentId);
			AssertStatistics(session, manager, clientChanges: 2, clientApplied: 0, serverChanges: 0, serverApplied: 2);
		});
	}

	[TestMethod]
	public void ParentBeforeChildBindsParentIdOnPushUp()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var parent = AddBookmark(client, "Folder", true);
			var child = AddBookmark(client, "Page", parentSyncId: parent.SyncId);

			var session = RunDirection(manager, SyncDirection.PushUp, settings => settings.ItemsPerSyncRequest = 1);

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(3, calls.Count, () => FormatCalls(calls));
			AreEqual(parent.SyncId, calls[0].ChangeIds[0]);
			AreEqual(child.SyncId, calls[1].ChangeIds[0]);
			var serverParent = (BookmarkEntity) server.Bookmarks.Read(parent.SyncId);
			var serverChild = (BookmarkEntity) server.Bookmarks.Read(child.SyncId);
			AreEqual(serverParent.Id, serverChild.ParentId);
			AssertStatistics(session, manager, clientChanges: 2, clientApplied: 0, serverChanges: 0, serverApplied: 2);
		});
	}

	[TestMethod]
	public void PullDownAccountBindsTheClientAddressId()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var address = AddAddress(server, "Home");
			var account = AddAccount(server, "John", address);

			var session = RunDirection(manager, SyncDirection.PullDown);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 2, sessionEnded: true, clientHasNoChanges: true);
			var clientAddress = (AddressEntity) client.Addresses.Read(address.SyncId);
			var clientAccount = (AccountEntity) client.Accounts.Read(account.SyncId);
			AreEqual(clientAddress.Id, clientAccount.AddressId);
			AreEqual(address.SyncId, clientAccount.AddressSyncId);
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 2, serverChanges: 2, serverApplied: 0);
		});
	}

	[TestMethod]
	public void PullDownLookupAddsASecondAccount()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var clientAddress = AddAddress(client, "Home");
			var clientAccount = AddAccount(client, "John", clientAddress);
			var serverAddress = AddAddress(server, "Home");
			var serverCustomer = EnsureCustomer(server, serverAddress.CustomerSyncId);
			var serverAccount = new AccountEntity
			{
				AddressId = serverAddress.Id,
				AddressSyncId = serverAddress.SyncId,
				CreatedOn = UtcNow,
				CustomerId = serverCustomer.Id,
				CustomerSyncId = serverCustomer.SyncId,
				EmailAddress = clientAccount.EmailAddress,
				ModifiedOn = UtcNow,
				Name = "ServerJohn",
				Roles = ",,",
				SyncId = Guid.NewGuid()
			};
			server.Accounts.Add(serverAccount);
			Save(server);

			var session = RunDirection(manager, SyncDirection.PullDown, LookupByEmail);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AreEqual(2, ReadAll<AccountEntity>(client.Accounts).Count);
			AreEqual("John", ((AccountEntity) client.Accounts.Read(clientAccount.SyncId)).Name);
			IsNotNull(client.Accounts.Read(serverAccount.SyncId));
			AreEqual(1, ReadAll<AccountEntity>(server.Accounts).Count);
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 3, serverChanges: 3, serverApplied: 0);
		});
	}

	[TestMethod]
	public void PullDownScopeKeepsOnlyMatchingAddresses()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			AddAddress(server, "ServerSc", "SC");
			AddAddress(server, "ServerGa", "GA");

			var session = RunDirection(manager, SyncDirection.PullDown, ScopeSouthCarolina);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			AreEqual(1, ReadAll<AddressEntity>(client.Addresses).Count);
			AreEqual("ServerSc", ReadAll<AddressEntity>(client.Addresses)[0].Line1);
			AreEqual(2, ReadAll<AddressEntity>(server.Addresses).Count);
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void PullDownWithoutLookupAddsASecondAccount()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			InsertAccount(client, "Client", "same@domain.com");
			var serverAccount = InsertAccount(server, "Server", "same@domain.com");

			var session = RunDirection(manager, SyncDirection.PullDown);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(2, ReadAll<AccountEntity>(client.Accounts).Count);
			AreEqual(1, ReadAll<AccountEntity>(server.Accounts).Count);
			IsNotNull(client.Accounts.Read(serverAccount.SyncId));
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 1, serverChanges: 1, serverApplied: 0);
			IsTrue(session.SyncSuccessful);
		});
	}

	[TestMethod]
	public void PullThenPushAccountBindsTheServerAddressId()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var address = AddAddress(client, "Home");
			var account = AddAccount(client, "John", address);

			var session = RunDirection(manager, SyncDirection.PullDownThenPushUp);

			AssertPhases(session, pulling: true, pushing: true);
			AreEqual(2, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 0, sessionEnded: false, clientHasNoChanges: false);
			AssertPush(calls[1], changeCount: 2, endSession: true);
			var serverAddress = (AddressEntity) server.Addresses.Read(address.SyncId);
			var serverAccount = (AccountEntity) server.Accounts.Read(account.SyncId);
			AreEqual(serverAddress.Id, serverAccount.AddressId);
			AssertStatistics(session, manager, clientChanges: 2, clientApplied: 0, serverChanges: 0, serverApplied: 2);
		});
	}

	[TestMethod]
	public void PullThenPushScopeFiltersBothDirections()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			AddAddress(client, "ClientGa", "GA");
			AddAddress(server, "ServerSc", "SC");
			AddAddress(server, "ServerGa", "GA");

			var session = RunDirection(manager, SyncDirection.PullDownThenPushUp, ScopeSouthCarolina);

			AssertPhases(session, pulling: true, pushing: false);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			AreEqual(2, ReadAll<AddressEntity>(client.Addresses).Count);
			IsNotNull(ReadAll<AddressEntity>(client.Addresses).Single(x => x.Line1 == "ClientGa"));
			IsNotNull(ReadAll<AddressEntity>(client.Addresses).Single(x => x.Line1 == "ServerSc"));
			AreEqual(2, ReadAll<AddressEntity>(server.Addresses).Count);
			IsNull(ReadAll<AddressEntity>(server.Addresses).FirstOrDefault(x => x.Line1 == "ClientGa"));
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 1, serverChanges: 1, serverApplied: 0);
		});
	}

	[TestMethod]
	public void PushUpAccountBindsTheServerAddressId()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			AddAddress(server, "Spacer");
			var address = AddAddress(client, "Home");
			var account = AddAccount(client, "John", address);

			var session = RunDirection(manager, SyncDirection.PushUp);

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: 2, endSession: true);
			var serverAddress = (AddressEntity) server.Addresses.Read(address.SyncId);
			var serverAccount = (AccountEntity) server.Accounts.Read(account.SyncId);
			AreEqual(serverAddress.Id, serverAccount.AddressId);
			AreNotEqual(address.Id, serverAccount.AddressId);
			AssertStatistics(session, manager, clientChanges: 2, clientApplied: 0, serverChanges: 0, serverApplied: 2);
		});
	}

	[TestMethod]
	public void PushUpAccountBoundToAnOutOfScopeAddressIsRejected()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var address = AddAddress(server, "ServerGa", "GA");
			RunDirection(manager, SyncDirection.PullDown);
			calls.Clear();
			var syncedOn = manager.GetSyncSettings(SampleSyncClient.SyncAll).LastSyncedOnClient;
			var clientAddress = (AddressEntity) client.Addresses.Read(address.SyncId);
			var account = AddAccount(client, "Intruder", clientAddress);

			var session = RunRaw(manager, SyncDirection.PushUp, ScopeSouthCarolina);

			IsFalse(session.SyncSuccessful);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: 1, endSession: true);
			AreEqual(SyncIssueType.RelationshipConstraint, session.SyncIssues[0].IssueType);
			IsNull(server.Accounts.Read(account.SyncId));
			AreEqual(syncedOn, manager.GetSyncSettings(SampleSyncClient.SyncAll).LastSyncedOnClient);
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 0);
		});
	}

	[TestMethod]
	public void PushUpLookupDoesNotMergeANewerClientAccount()
	{
		WithRecording((client, server, manager, calls, _, _) =>
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
			account.Name = "NewerJohn";
			account.ModifiedOn = UtcNow;
			Save(client, account);

			var session = RunDirection(manager, SyncDirection.PushUp, LookupByEmail);

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(2, ReadAll<AccountEntity>(server.Accounts).Count);
			AreEqual("ServerJohn", ((AccountEntity) server.Accounts.Read(serverAccount.SyncId)).Name);
			AreEqual("NewerJohn", ((AccountEntity) server.Accounts.Read(account.SyncId)).Name);
			AssertStatistics(session, manager, clientChanges: 2, clientApplied: 0, serverChanges: 0, serverApplied: 2);
		});
	}

	[TestMethod]
	public void PushUpLookupMergesTheSameEmail()
	{
		WithRecording((client, server, manager, calls, _, _) =>
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

			var session = RunDirection(manager, SyncDirection.PushUp, LookupByEmail);

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(1, ReadAll<AccountEntity>(server.Accounts).Count);
			AreEqual(1, ReadAll<AccountEntity>(client.Accounts).Count);
			AreEqual("ServerJohn", ((AccountEntity) server.Accounts.Read(serverAccount.SyncId)).Name);
			AssertStatistics(session, manager, clientChanges: 2, clientApplied: 0, serverChanges: 0, serverApplied: 1);
		});
	}

	[TestMethod]
	public void PushUpScopeHidesAnOutOfScopeAccountBeforeLookup()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var serverAccount = InsertAccount(server, "Keep", "same@domain.com");
			var clientAccount = InsertAccount(client, "Other", "same@domain.com");

			var session = RunDirection(manager, SyncDirection.PushUp, settings =>
			{
				LookupByEmail(settings);
				settings.AddFilter<AccountEntity>(
					scopeFilter: x => x.Name == "Keep",
					lookupFilter: incoming => x => x.EmailAddress == incoming.EmailAddress);
			});

			AssertPhases(session, pulling: false, pushing: false);
			AreEqual(0, calls.Count, () => FormatCalls(calls));
			AreEqual(1, ReadAll<AccountEntity>(server.Accounts).Count);
			AreEqual("Keep", ((AccountEntity) server.Accounts.Read(serverAccount.SyncId)).Name);
			AreEqual("Other", ((AccountEntity) client.Accounts.Read(clientAccount.SyncId)).Name);
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 0, serverChanges: 0, serverApplied: 0);
		});
	}

	[TestMethod]
	public void PushUpScopeSendsOnlyMatchingAddresses()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			AddAddress(client, "ClientSc", "SC");
			AddAddress(client, "ClientGa", "GA");

			var session = RunDirection(manager, SyncDirection.PushUp, ScopeSouthCarolina);

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPush(calls[0], changeCount: 1, endSession: true);
			AreEqual(1, ReadAll<AddressEntity>(server.Addresses).Count);
			AreEqual("ClientSc", ReadAll<AddressEntity>(server.Addresses)[0].Line1);
			AreEqual(2, ReadAll<AddressEntity>(client.Addresses).Count);
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 1);
		});
	}

	[TestMethod]
	public void PushUpWithoutLookupAddsASecondAccount()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var serverAccount = InsertAccount(server, "Server", "same@domain.com");
			var clientAccount = InsertAccount(client, "Client", "same@domain.com");

			var session = RunDirection(manager, SyncDirection.PushUp);

			AssertPhases(session, pulling: false, pushing: true);
			AreEqual(2, ReadAll<AccountEntity>(server.Accounts).Count);
			AreEqual(1, ReadAll<AccountEntity>(client.Accounts).Count);
			IsNotNull(server.Accounts.Read(clientAccount.SyncId));
			AssertStatistics(session, manager, clientChanges: 1, clientApplied: 0, serverChanges: 0, serverApplied: 1);
			IsTrue(session.SyncSuccessful);
		});
	}

	[TestMethod]
	public void ServerAccountBoundToAnOutOfScopeAddressIsRejectedOnPull()
	{
		WithRecording((client, server, manager, calls, _, _) =>
		{
			var address = AddAddress(server, "ServerGa", "GA");
			RunDirection(manager, SyncDirection.PullDown);
			calls.Clear();
			var syncedOn = manager.GetSyncSettings(SampleSyncClient.SyncAll).LastSyncedOnClient;
			var serverAddress = (AddressEntity) server.Addresses.Read(address.SyncId);
			var account = AddAccount(server, "Intruder", serverAddress);

			var session = RunRaw(manager, SyncDirection.PullDownThenPushUp, ScopeSouthCarolina);

			IsFalse(session.SyncSuccessful);
			AreEqual(1, calls.Count, () => FormatCalls(calls));
			AssertPull(calls[0], resultChanges: 1, sessionEnded: true, clientHasNoChanges: true);
			AreEqual(SyncIssueType.RelationshipConstraint, session.SyncIssues[0].IssueType);
			IsNull(client.Accounts.Read(account.SyncId));
			AreEqual(syncedOn, manager.GetSyncSettings(SampleSyncClient.SyncAll).LastSyncedOnClient);
			AssertStatistics(session, manager, clientChanges: 0, clientApplied: 0, serverChanges: 1, serverApplied: 0);
		});
	}

	private static void LookupByEmail(SyncSettings settings)
	{
		settings.AddFilter<AddressEntity>();
		settings.AddFilter<AccountEntity>(lookupFilter: incoming => x => x.EmailAddress == incoming.EmailAddress);
	}

	private static void ScopeSouthCarolina(SyncSettings settings)
	{
		settings.AddFilter<AddressEntity>(scopeFilter: x => x.State == "SC");
	}

	private AccountEntity InsertAccount(ISampleDatabase database, string name, string email, AddressEntity address = null)
	{
		var entity = new AccountEntity
		{
			AddressId = address?.Id,
			AddressSyncId = address?.SyncId,
			CreatedOn = UtcNow,
			EmailAddress = email,
			ModifiedOn = UtcNow,
			Name = name,
			Roles = ",,",
			SyncId = Guid.NewGuid()
		};
		database.Accounts.Add(entity);
		Save(database);
		return entity;
	}

	private void LinkParent(SampleSqlDatabaseProvider provider, ISampleDatabase database, BookmarkEntity child, BookmarkEntity parent, DateTime modifiedOn)
	{
		provider.Settings.MaintainModifiedOn = false;
		var stored = (BookmarkEntity) database.Bookmarks.Read(child.SyncId);
		stored.ParentSyncId = parent.SyncId;
		stored.ModifiedOn = modifiedOn;
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

	private void Touch(ISampleDatabase database, AddressEntity address)
	{
		var stored = (AddressEntity) database.Addresses.Read(address.SyncId);
		stored.ModifiedOn = UtcNow;
		Save(database, stored);
	}

	#endregion
}
