#region References

using System;
using System.Linq;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sample.Models;
using Cornerstone.Sample.Sync;
using Cornerstone.Sync;
using Cornerstone.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
[DoNotParallelize]
public class SyncClientFilterTests : SyncScenarioTest
{
	#region Methods

	[TestMethod]
	public void BeginSyncThrowsWhenSessionAlreadyInProgress()
	{
		WithEachProvider((provider, _) =>
		{
			var client = NewClient("Client", provider);
			var sessionId = Guid.NewGuid();
			client.BeginSync(sessionId, NewSettings());
			ExpectedException<InvalidOperationException>(() => client.BeginSync(Guid.NewGuid(), NewSettings()));
		});
	}

	[TestMethod]
	public void EmptyFiltersExcludeOutgoingChanges()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home");

			var client = new NoDefaultFilterSyncClient("Client", provider, this, new SyncStatistics(), new Profiler("Client"));
			var sessionId = Guid.NewGuid();
			client.BeginSync(sessionId, new SyncSettings { IncludeIssueDetails = true });

			var changes = client.GetChanges(sessionId, NewRequest());
			AreEqual(0, changes.Collection.Count);
		});
	}

	[TestMethod]
	public void ExistingDeleteCopiesPayloadFields()
	{
		WithEachProvider((provider, database) =>
		{
			var stored = AddAddress(database, "Home", "SC");
			var client = NewClient("Server", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.AddFilter<AddressEntity>();
			settings.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, settings);

			IncrementTime(seconds: 1);
			var result = client.ApplyChanges(sessionId, new ServiceRequest<SyncObject>(SyncObject.ToSyncObject(NewAddressPayload(stored.SyncId, "Hacked", "GA", true))));

			DetachTrackedEntities(database);
			AreEqual(0, result.Collection.Count);
			var remaining = (AddressEntity) database.Addresses.Read(stored.SyncId);
			IsNotNull(remaining);
			IsTrue(remaining.IsDeleted);
			AreEqual("Hacked", remaining.Line1);
			AreEqual("GA", remaining.State);
		});
	}

	[TestMethod]
	public void GetChangesSinceEqualsUntilReturnsChangesFromSinceToNow()
	{
		WithEachProvider((provider, database) =>
		{
			var now = UtcNow;
			AddAddress(database, "Home");

			var client = NewClient("Client", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.AddFilter<AddressEntity>();
			settings.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, settings);

			var changes = client.GetChanges(sessionId, new SyncRequest
			{
				Since = now,
				Until = now,
				Take = 100
			});
			AreEqual(1, changes.Collection.Count);
		});
	}

	[TestMethod]
	public void GetChangesSkipPagesAcrossItems()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "One");
			AddAddress(database, "Two");
			AddAddress(database, "Three");

			var client = NewClient("Client", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.AddFilter<AddressEntity>();
			settings.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, settings);

			var first = client.GetChanges(sessionId, new SyncRequest
			{
				Since = DateTime.MinValue,
				Until = UtcNow.AddMinutes(1),
				Take = 2
			});
			AreEqual(2, first.Collection.Count);
			AreEqual(3, first.TotalCount);
			IsTrue(first.HasMore);
			AreEqual(0, first.Skipped);

			var second = client.GetChanges(sessionId, new SyncRequest
			{
				Since = DateTime.MinValue,
				Until = UtcNow.AddMinutes(1),
				Skip = first.Collection.Count,
				Take = 2
			});
			AreEqual(1, second.Collection.Count);
			IsFalse(first.Collection.Any(x => x.SyncId == second.Collection[0].SyncId));
		});
	}

	[TestMethod]
	public void GetCorrectionsReturnsEmptyForKnownIssue()
	{
		WithEachProvider((provider, database) =>
		{
			var address = AddAddress(database, "Home");

			var client = NewClient("Client", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.AddFilter<AddressEntity>();
			settings.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, settings);

			var changes = client.GetChanges(sessionId, NewRequest());
			AreEqual(1, changes.Collection.Count);

			var corrections = client.GetCorrections(sessionId, new ServiceRequest<SyncIssue>(new SyncIssue
			{
				Id = address.SyncId,
				TypeName = changes.Collection[0].TypeName,
				IssueType = SyncIssueType.RelationshipConstraint
			}));
			AreEqual(0, corrections.Collection.Count);
		});
	}

	[TestMethod]
	public void GetCorrectionsSkipsEmptyIdAndUnknownType()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home");

			var client = NewClient("Client", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.AddFilter<AddressEntity>();
			settings.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, settings);

			var corrections = client.GetCorrections(sessionId, new ServiceRequest<SyncIssue>(new SyncIssue { Id = Guid.Empty, TypeName = "Cornerstone.Sample.Models.Address" }, new SyncIssue { Id = Guid.NewGuid(), TypeName = "Unknown.Type, Unknown" }));
			AreEqual(0, corrections.Collection.Count);
		});
	}

	[TestMethod]
	public void IncomingFilterAllowsAddWhenRelatedRowMatches()
	{
		WithEachProvider((provider, database) =>
		{
			var address = AddAddress(database, "Home", "SC");
			var client = NewClient("Server", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.AddFilter<AddressEntity>(incomingFilter: x => x.State == "SC");
			settings.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, settings);

			var payload = NewAccountPayload(Guid.NewGuid(), "John", address.SyncId);
			var result = client.ApplyChanges(sessionId, new ServiceRequest<SyncObject>(SyncObject.ToSyncObject(payload)));

			DetachTrackedEntities(database);
			AreEqual(0, result.Collection.Count, () => string.Join("; ", result.Collection.Select(x => x.Message)));
			var stored = (AccountEntity) database.Accounts.Read(payload.SyncId);
			IsNotNull(stored);
			AreEqual("John", stored.Name);
			AreEqual(address.SyncId, stored.AddressSyncId);
			AreEqual(address.Id, stored.AddressId);
		});
	}

	[TestMethod]
	public void IncomingFilterAllowsUpdateWhenStoredRowMatches()
	{
		WithEachProvider((provider, database) =>
		{
			var stored = AddAddress(database, "Home", "SC");
			var client = NewClient("Server", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.AddFilter<AddressEntity>(incomingFilter: x => x.State == "SC");
			settings.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, settings);

			var result = client.ApplyChanges(sessionId, new ServiceRequest<SyncObject>(SyncObject.ToSyncObject(NewAddressPayload(stored.SyncId, "Updated Home", "SC"))));

			DetachTrackedEntities(database);
			AreEqual(0, result.Collection.Count);
			var remaining = (AddressEntity) database.Addresses.Read(stored.SyncId);
			IsNotNull(remaining);
			AreEqual("Updated Home", remaining.Line1);
			AreEqual("SC", remaining.State);
		});
	}

	[TestMethod]
	public void IncomingFilterDoesNotRejectWrongEntityType()
	{
		var filter = new SyncRepositoryFilter<AddressEntity>(incomingFilter: x => x.State == "SC");
		IsFalse(filter.ShouldFilterIncomingEntity(new AccountEntity { Name = "John" }));
		IsTrue(filter.ShouldFilterIncomingEntity(new AddressEntity { State = "GA" }));
		IsFalse(filter.ShouldFilterIncomingEntity(new AddressEntity { State = "SC" }));
	}

	[TestMethod]
	public void IncomingFilterRejectsAddWhenRelatedRowDoesNotMatch()
	{
		WithEachProvider((provider, database) =>
		{
			var address = AddAddress(database, "Work", "GA");
			var client = NewClient("Server", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.AddFilter<AddressEntity>(incomingFilter: x => x.State == "SC");
			settings.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, settings);

			var payload = NewAccountPayload(Guid.NewGuid(), "Intruder", address.SyncId);
			var syncObject = SyncObject.ToSyncObject(payload);
			var result = client.ApplyChanges(sessionId, new ServiceRequest<SyncObject>(syncObject));

			DetachTrackedEntities(database);
			AreEqual(1, result.Collection.Count);
			AreEqual(SyncIssueType.RelationshipConstraint, result.Collection[0].IssueType);
			AreEqual(payload.SyncId, result.Collection[0].Id);
			AreEqual(syncObject.TypeName, result.Collection[0].TypeName);
			IsNull(database.Accounts.Read(payload.SyncId));
		});
	}

	[TestMethod]
	public void IncomingFilterRejectsDeleteWhenStoredRowDoesNotMatch()
	{
		WithEachProvider((provider, database) =>
		{
			var stored = AddAddress(database, "Work", "GA");
			var client = NewClient("Server", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.AddFilter<AddressEntity>(incomingFilter: x => x.State == "SC");
			settings.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, settings);

			var result = client.ApplyChanges(sessionId, new ServiceRequest<SyncObject>(SyncObject.ToSyncObject(NewAddressPayload(stored.SyncId, "Work", "SC", true))));

			DetachTrackedEntities(database);
			AreEqual(1, result.Collection.Count);
			AreEqual(SyncIssueType.SyncEntityFiltered, result.Collection[0].IssueType);
			var remaining = (AddressEntity) database.Addresses.Read(stored.SyncId);
			IsNotNull(remaining);
			IsFalse(remaining.IsDeleted);
			AreEqual("GA", remaining.State);
		});
	}

	[TestMethod]
	public void IncomingFilterRejectsEntitiesThatDoNotMatch()
	{
		WithEachPair((clientDatabase, serverDatabase, _) =>
		{
			AddAddress(clientDatabase, "Home", "SC");
			AddAddress(clientDatabase, "Work", "GA");

			var client = NewClient("Client", ScenarioClientProvider);
			var server = NewClient("Server", ScenarioServerProvider);
			var sessionId = Guid.NewGuid();
			var outgoing = NewSettings();
			outgoing.AddFilter<AddressEntity>();
			outgoing.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, outgoing);

			var changes = client.GetChanges(sessionId, NewRequest());
			AreEqual(2, changes.Collection.Count);

			var incoming = NewSettings();
			incoming.AddFilter<AddressEntity>(incomingFilter: x => x.State == "SC");
			incoming.AddFilter<AccountEntity>();
			var result = server.Sync(new SyncOperation
			{
				SessionId = sessionId,
				Settings = incoming,
				Changes = new ServiceRequest<SyncObject>(changes.Collection)
			});

			DetachScenarioDatabases();
			AreEqual(1, result.AppliedIssues.Collection.Count);
			AreEqual(SyncIssueType.SyncEntityFiltered, result.AppliedIssues.Collection[0].IssueType);
			AreEqual(1, ReadAll<AddressEntity>(serverDatabase.Addresses).Count);
			AreEqual("SC", ReadAll<AddressEntity>(serverDatabase.Addresses)[0].State);
		});
	}

	[TestMethod]
	public void IncomingFilterRejectsRelatedRowsForABatchOfAccounts()
	{
		WithEachProvider((provider, database) =>
		{
			var address = AddAddress(database, "Work", "GA");
			var client = NewClient("Server", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.AddFilter<AddressEntity>(incomingFilter: x => x.State == "SC");
			settings.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, settings);

			var payloads = Enumerable.Range(0, 8)
				.Select(i => NewAccountPayload(Guid.NewGuid(), $"Intruder{i}", address.SyncId))
				.ToList();
			var result = client.ApplyChanges(sessionId, new ServiceRequest<SyncObject>(
				payloads.Select(SyncObject.ToSyncObject).ToList()
			));

			DetachTrackedEntities(database);
			AreEqual(8, result.Collection.Count);
			foreach (var issue in result.Collection)
			{
				AreEqual(SyncIssueType.RelationshipConstraint, issue.IssueType);
			}

			AreEqual(0, ReadAll<AccountEntity>(database.Accounts).Count);
		});
	}

	[TestMethod]
	public void IncomingFilterRejectsUpdateWhenRelatedRowDoesNotMatch()
	{
		WithEachProvider((provider, database) =>
		{
			var inScope = AddAddress(database, "Home", "SC");
			var outOfScope = AddAddress(database, "Work", "GA");
			var stored = AddAccount(database, "Cust1", inScope);
			var client = NewClient("Server", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.AddFilter<AddressEntity>(incomingFilter: x => x.State == "SC");
			settings.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, settings);

			var result = client.ApplyChanges(sessionId, new ServiceRequest<SyncObject>(SyncObject.ToSyncObject(NewAccountPayload(stored.SyncId, "Hacked", outOfScope.SyncId))));

			DetachTrackedEntities(database);
			AreEqual(1, result.Collection.Count);
			AreEqual(SyncIssueType.RelationshipConstraint, result.Collection[0].IssueType);
			var remaining = (AccountEntity) database.Accounts.Read(stored.SyncId);
			IsNotNull(remaining);
			AreEqual("Cust1", remaining.Name);
			AreEqual(inScope.SyncId, remaining.AddressSyncId);
		});
	}

	[TestMethod]
	public void IncomingFilterRejectsUpdateWhenStoredRowDoesNotMatch()
	{
		WithEachProvider((provider, database) =>
		{
			var stored = AddAddress(database, "Work", "GA");
			var client = NewClient("Server", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.AddFilter<AddressEntity>(incomingFilter: x => x.State == "SC");
			settings.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, settings);

			var result = client.ApplyChanges(sessionId, new ServiceRequest<SyncObject>(SyncObject.ToSyncObject(NewAddressPayload(stored.SyncId, "Stolen", "SC"))));

			DetachTrackedEntities(database);
			AreEqual(1, result.Collection.Count);
			AreEqual(SyncIssueType.SyncEntityFiltered, result.Collection[0].IssueType);
			var remaining = (AddressEntity) database.Addresses.Read(stored.SyncId);
			IsNotNull(remaining);
			AreEqual("Work", remaining.Line1);
			AreEqual("GA", remaining.State);
		});
	}

	[TestMethod]
	public void IncomingOnlyFilterDoesNotLimitGetChanges()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home", "SC");
			AddAddress(database, "Work", "GA");

			var client = NewClient("Client", provider);
			var sessionId = Guid.NewGuid();
			var settings = new SyncSettings { IncludeIssueDetails = true };
			settings.AddFilter<AddressEntity>(incomingFilter: x => x.State == "SC");
			settings.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, settings);

			var changes = client.GetChanges(sessionId, NewRequest());
			AreEqual(2, changes.Collection.Count);
		});
	}

	[TestMethod]
	public void IncomingFalseRejectsApplyWhileGetChangesStillSends()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home", "SC");

			var client = NewClient("Server", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.AddFilter<AddressEntity>(incomingFilter: x => false);
			settings.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, settings);

			var changes = client.GetChanges(sessionId, NewRequest());
			AreEqual(1, changes.Collection.Count);

			var result = client.ApplyChanges(sessionId, new ServiceRequest<SyncObject>(SyncObject.ToSyncObject(NewAddressPayload(Guid.NewGuid(), "Catalog", "SC"))));
			DetachTrackedEntities(database);
			AreEqual(1, result.Collection.Count);
			AreEqual(SyncIssueType.SyncEntityFiltered, result.Collection[0].IssueType);
			AreEqual(1, ReadAll<AddressEntity>(database.Addresses).Count);
		});
	}

	[TestMethod]
	public void LookupMissWithInScopeSyncIdUpdatesExisting()
	{
		WithEachProvider((provider, database) =>
		{
			var address = AddAddress(database, "Home", "SC");
			var stored = AddAccount(database, "Cust1", address);
			var client = NewClient("Server", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.AddFilter<AddressEntity>();
			settings.AddFilter<AccountEntity>(lookupFilter: incoming => x => x.EmailAddress == incoming.EmailAddress);
			client.BeginSync(sessionId, settings);

			IncrementTime(seconds: 1);
			var payload = NewAccountPayload(stored.SyncId, "Hacked", address.SyncId);
			var result = client.ApplyChanges(sessionId, new ServiceRequest<SyncObject>(SyncObject.ToSyncObject(payload)));

			DetachTrackedEntities(database);
			AreEqual(0, result.Collection.Count, () => string.Join("; ", result.Collection.Select(x => x.Message)));
			AreEqual(1, ReadAll<AccountEntity>(database.Accounts).Count);
			var remaining = (AccountEntity) database.Accounts.Read(stored.SyncId);
			IsNotNull(remaining);
			AreEqual("Hacked", remaining.Name);
			AreEqual("hacked@domain.com", remaining.EmailAddress);
		});
	}

	[TestMethod]
	public void LookupMissWithOutOfScopeSyncIdIsFilteredNotAdded()
	{
		WithEachProvider((provider, database) =>
		{
			var stored = AddAddress(database, "Work", "GA");
			var client = NewClient("Server", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.AddFilter<AddressEntity>(lookupFilter: incoming => x => (x.SyncId == incoming.SyncId) && (x.State == "SC"), incomingFilter: x => x.State == "SC");
			settings.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, settings);

			var result = client.ApplyChanges(sessionId, new ServiceRequest<SyncObject>(SyncObject.ToSyncObject(NewAddressPayload(stored.SyncId, "Stolen", "SC"))));

			DetachTrackedEntities(database);
			AreEqual(1, result.Collection.Count);
			AreEqual(SyncIssueType.SyncEntityFiltered, result.Collection[0].IssueType);
			AreEqual(1, ReadAll<AddressEntity>(database.Addresses).Count);
			var remaining = (AddressEntity) database.Addresses.Read(stored.SyncId);
			IsNotNull(remaining);
			AreEqual("Work", remaining.Line1);
			AreEqual("GA", remaining.State);
		});
	}

	[TestMethod]
	public void OutgoingFilterLimitsGetChanges()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home", "SC");
			AddAddress(database, "Work", "GA");

			var client = NewClient("Client", provider);
			var sessionId = Guid.NewGuid();
			var settings = new SyncSettings { IncludeIssueDetails = true };
			settings.AddFilter<AddressEntity>(outgoingFilter: x => x.State == "SC");
			settings.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, settings);

			var changes = client.GetChanges(sessionId, new SyncRequest
			{
				Since = DateTime.MinValue,
				Until = UtcNow.AddMinutes(1),
				Take = 100
			});
			AreEqual(1, changes.Collection.Count);
		});
	}

	[TestMethod]
	public void RepositoryNotInFiltersProducesRepositoryFilteredIssue()
	{
		WithEachPair((clientDatabase, serverDatabase, _) =>
		{
			AddAddress(clientDatabase, "Home");

			var client = NewClient("Client", ScenarioClientProvider);
			var server = new NoDefaultFilterSyncClient("Server", ScenarioServerProvider, this, new SyncStatistics(), new Profiler("Server"));
			var sessionId = Guid.NewGuid();
			var outgoing = NewSettings();
			outgoing.AddFilter<AddressEntity>();
			outgoing.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, outgoing);
			var changes = client.GetChanges(sessionId, NewRequest());
			AreEqual(1, changes.Collection.Count);

			var incoming = NewSettings();
			incoming.AddFilter<AccountEntity>();
			var result = server.Sync(new SyncOperation
			{
				SessionId = sessionId,
				Settings = incoming,
				Changes = new ServiceRequest<SyncObject>(changes.Collection)
			});

			DetachScenarioDatabases();
			AreEqual(1, result.AppliedIssues.Collection.Count);
			AreEqual(SyncIssueType.RepositoryFiltered, result.AppliedIssues.Collection[0].IssueType);
			AreEqual(0, ReadAll<AddressEntity>(serverDatabase.Addresses).Count);
		});
	}

	[TestMethod]
	public void ServerSanitizeKeepsDeletedRowsWhenClientAsksForPermanentDelete()
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
		});
	}

	[TestMethod]
	public void SoftDeletedEntitiesSyncAfterInitialWhenSkipDeletedIsSet()
	{
		WithEachProvider((provider, database) =>
		{
			var address = AddAddress(database, "Gone");
			var stored = (AddressEntity) database.Addresses.Read(address.SyncId);
			stored.IsDeleted = true;
			stored.ModifiedOn = UtcNow;
			Save(database, stored);

			var client = NewClient("Client", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.AddFilter<AddressEntity>(skipDeletedItemsOnInitialSync: true);
			settings.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, settings);

			var initial = client.GetChanges(sessionId, NewRequest());
			AreEqual(0, initial.Collection.Count);

			var later = client.GetChanges(sessionId, new SyncRequest
			{
				Since = UtcNow.AddMinutes(-5),
				Until = UtcNow.AddMinutes(1),
				Take = 100
			});
			AreEqual(1, later.Collection.Count);
			AreEqual(address.SyncId, later.Collection[0].SyncId);
		});
	}

	[TestMethod]
	public void ScopeFilterAndOutgoingBothRequiredForGetChanges()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home", "SC");
			var home = ReadAll<AddressEntity>(database.Addresses).First(x => x.Line1 == "Home");
			home.Postal = "12345";
			Save(database, home);
			AddAddress(database, "Home2", "SC");
			AddAddress(database, "Work", "GA");

			var client = NewClient("Client", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.AddFilter<AddressEntity>(scopeFilter: x => x.State == "SC", outgoingFilter: x => x.Postal == "12345");
			settings.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, settings);

			var changes = client.GetChanges(sessionId, NewRequest());
			AreEqual(1, changes.Collection.Count);
			AreEqual("Home", ((Address) changes.Collection[0].ToSyncModel()).Line1);
		});
	}

	[TestMethod]
	public void ScopeFilterAndsIntoLookup()
	{
		WithEachProvider((provider, database) =>
		{
			var stored = AddAddress(database, "Work", "GA");
			var client = NewClient("Server", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.AddFilter<AddressEntity>(scopeFilter: x => x.State == "SC", lookupFilter: incoming => x => x.Line1 == incoming.Line1);
			settings.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, settings);

			var result = client.ApplyChanges(sessionId, new ServiceRequest<SyncObject>(SyncObject.ToSyncObject(NewAddressPayload(stored.SyncId, "Work", "SC"))));

			DetachTrackedEntities(database);
			AreEqual(1, result.Collection.Count);
			AreEqual(SyncIssueType.SyncEntityFiltered, result.Collection[0].IssueType);
			var remaining = (AddressEntity) database.Addresses.Read(stored.SyncId);
			IsNotNull(remaining);
			AreEqual("Work", remaining.Line1);
			AreEqual("GA", remaining.State);
		});
	}

	[TestMethod]
	public void ScopeFilterLimitsGetChanges()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home", "SC");
			AddAddress(database, "Work", "GA");

			var client = NewClient("Client", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.AddFilter<AddressEntity>(scopeFilter: x => x.State == "SC");
			settings.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, settings);

			var changes = client.GetChanges(sessionId, NewRequest());
			AreEqual(1, changes.Collection.Count);
			AreEqual("Home", ((Address) changes.Collection[0].ToSyncModel()).Line1);
		});
	}

	[TestMethod]
	public void ScopeFilterOutgoingFalseIsWriteOnly()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home", "SC");

			var client = NewClient("Server", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.AddFilter<AddressEntity>(scopeFilter: x => x.State == "SC", outgoingFilter: x => false);
			settings.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, settings);

			var changes = client.GetChanges(sessionId, NewRequest());
			AreEqual(0, changes.Collection.Count);

			var payload = NewAddressPayload(Guid.NewGuid(), "In Only", "SC");
			var result = client.ApplyChanges(sessionId, new ServiceRequest<SyncObject>(SyncObject.ToSyncObject(payload)));
			DetachTrackedEntities(database);
			AreEqual(0, result.Collection.Count, () => string.Join("; ", result.Collection.Select(x => x.Message)));
			IsNotNull(database.Addresses.Read(payload.SyncId));

			var rejected = client.ApplyChanges(sessionId, new ServiceRequest<SyncObject>(SyncObject.ToSyncObject(NewAddressPayload(Guid.NewGuid(), "Out Of Scope", "GA"))));
			DetachTrackedEntities(database);
			AreEqual(1, rejected.Collection.Count);
			AreEqual(SyncIssueType.SyncEntityFiltered, rejected.Collection[0].IssueType);
		});
	}

	[TestMethod]
	public void ScopeFilterRejectsApplyWhenIncomingIsUnset()
	{
		WithEachProvider((provider, database) =>
		{
			var client = NewClient("Server", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.AddFilter<AddressEntity>(scopeFilter: x => x.State == "SC");
			settings.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, settings);

			var result = client.ApplyChanges(sessionId, new ServiceRequest<SyncObject>(SyncObject.ToSyncObject(NewAddressPayload(Guid.NewGuid(), "Work", "GA"))));

			DetachTrackedEntities(database);
			AreEqual(1, result.Collection.Count);
			AreEqual(SyncIssueType.SyncEntityFiltered, result.Collection[0].IssueType);
			AreEqual(0, ReadAll<AddressEntity>(database.Addresses).Count);
		});
	}

	[TestMethod]
	public void ScopeFilterRejectsRelatedRow()
	{
		WithEachProvider((provider, database) =>
		{
			var address = AddAddress(database, "Work", "GA");
			var client = NewClient("Server", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.AddFilter<AddressEntity>(scopeFilter: x => x.State == "SC");
			settings.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, settings);

			var payload = NewAccountPayload(Guid.NewGuid(), "Intruder", address.SyncId);
			var syncObject = SyncObject.ToSyncObject(payload);
			var result = client.ApplyChanges(sessionId, new ServiceRequest<SyncObject>(syncObject));

			DetachTrackedEntities(database);
			AreEqual(1, result.Collection.Count);
			AreEqual(SyncIssueType.RelationshipConstraint, result.Collection[0].IssueType);
			AreEqual(payload.SyncId, result.Collection[0].Id);
			AreEqual(syncObject.TypeName, result.Collection[0].TypeName);
			IsNull(database.Accounts.Read(payload.SyncId));
		});
	}

	[TestMethod]
	public void ScopeFilterRejectsStoredRowWhenPayloadRewritten()
	{
		WithEachProvider((provider, database) =>
		{
			var stored = AddAddress(database, "Work", "GA");
			var client = NewClient("Server", provider);
			var sessionId = Guid.NewGuid();
			var settings = NewSettings();
			settings.AddFilter<AddressEntity>(scopeFilter: x => x.State == "SC");
			settings.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, settings);

			var result = client.ApplyChanges(sessionId, new ServiceRequest<SyncObject>(SyncObject.ToSyncObject(NewAddressPayload(stored.SyncId, "Stolen", "SC"))));

			DetachTrackedEntities(database);
			AreEqual(1, result.Collection.Count);
			AreEqual(SyncIssueType.SyncEntityFiltered, result.Collection[0].IssueType);
			var remaining = (AddressEntity) database.Addresses.Read(stored.SyncId);
			IsNotNull(remaining);
			AreEqual("Work", remaining.Line1);
			AreEqual("GA", remaining.State);
		});
	}

	private Account NewAccountPayload(Guid syncId, string name, Guid addressSyncId)
	{
		var modifiedOn = UtcNow;
		return new Account
		{
			AddressSyncId = addressSyncId,
			CreatedOn = modifiedOn.AddMinutes(-1),
			EmailAddress = $"{name.Replace(" ", string.Empty).ToLowerInvariant()}@domain.com",
			ModifiedOn = modifiedOn,
			Name = name,
			Roles = ",,",
			SyncId = syncId
		};
	}

	private Address NewAddressPayload(Guid syncId, string line1, string state, bool isDeleted = false)
	{
		var modifiedOn = UtcNow;
		return new Address
		{
			City = "City",
			CreatedOn = modifiedOn.AddMinutes(-1),
			IsDeleted = isDeleted,
			Line1 = line1,
			ModifiedOn = modifiedOn,
			Postal = "29640",
			State = state,
			SyncId = syncId
		};
	}

	private SampleSyncClient NewClient(string name, ISyncableDatabaseProvider provider)
	{
		return new SampleSyncClient(name, provider, this, new SyncStatistics(), new Profiler(name));
	}

	private SyncRequest NewRequest()
	{
		return new SyncRequest
		{
			Since = DateTime.MinValue,
			Until = UtcNow.AddMinutes(1),
			Take = 100
		};
	}

	private static SyncSettings NewSettings()
	{
		return new SyncSettings { IncludeIssueDetails = true };
	}

	#endregion

	#region Classes

	private sealed class NoDefaultFilterSyncClient : SampleSyncClient
	{
		#region Constructors

		public NoDefaultFilterSyncClient(
			string name,
			ISyncableDatabaseProvider provider,
			IDateTimeProvider dateTimeProvider,
			SyncStatistics statistics,
			Profiler profiler
		) : base(name, provider, dateTimeProvider, statistics, profiler)
		{
		}

		#endregion

		#region Methods

		protected override void SetSyncSettings()
		{
		}

		#endregion
	}

	#endregion
}