#region References

using System;
using System.Linq;
using Cornerstone.Extensions;
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
public class SpeedyParityTests : SyncScenarioTest
{
	#region Methods

	[TestMethod]
	public void AddItemToClientAndServerWithSyncEntityWithCustomLookupFilter()
	{
		WithEachPair((client, server, manager) =>
		{
			AddSetting(client, "Foo", "Bar", Guid.Parse("3E89C239-9B29-4E47-B4CA-C0695450FC07"));
			AddSetting(server, "Foo", "Bar", Guid.Parse("5323C23E-6959-47EE-9EDD-6CC1185859DA"));

			RunSync(manager, settings =>
			{
				settings.SyncDirection = SyncDirection.PushUp;
				settings.AddFilter<SettingEntity>(lookupFilter: incoming => x => x.Name == incoming.Name);
			});

			AreEqual(1, ReadAll<SettingEntity>(client.Settings).Count);
			AreEqual(1, ReadAll<SettingEntity>(server.Settings).Count);
			AreNotEqual(
				ReadAll<SettingEntity>(client.Settings)[0].SyncId,
				ReadAll<SettingEntity>(server.Settings)[0].SyncId);
		});
	}

	[TestMethod]
	public void ClientTimeFasterThanServerTimeSyncStartShouldStillSync()
	{
		WithEachPair((clientDb, serverDb, manager) =>
		{
			SetTime(new DateTime(2019, 7, 10, 11, 59, 0, DateTimeKind.Utc));
			AddAddress(serverDb, "123 Elm Street");

			var client = NewClient("Client", ScenarioClientProvider);
			var server = NewClient("Server", ScenarioServerProvider);
			var settings = new SyncSettings { IncludeIssueDetails = true };
			SampleSyncFilters.ApplyAll(settings);
			var sessionId = Guid.NewGuid();

			SetTime(new DateTime(2019, 7, 10, 12, 1, 0, DateTimeKind.Utc));
			var serverStart = UtcNow;
			server.BeginSync(sessionId, settings);

			SetTime(new DateTime(2019, 7, 10, 12, 0, 50, DateTimeKind.Utc));
			var clientStart = UtcNow;
			client.BeginSync(sessionId, settings);

			SetTime(new DateTime(2019, 7, 10, 12, 1, 1, DateTimeKind.Utc));
			var pull = server.GetChanges(sessionId, new SyncRequest
			{
				Since = DateTime.MinValue,
				Until = serverStart,
				Take = 100
			});
			AreEqual(1, pull.TotalCount);
			AreEqual(1, pull.Collection.Count);

			SetTime(new DateTime(2019, 7, 10, 12, 0, 53, DateTimeKind.Utc));
			AddAddress(clientDb, "123 Main Street");

			SetTime(new DateTime(2019, 7, 10, 12, 0, 54, DateTimeKind.Utc));
			client.EndSync(sessionId);
			server.EndSync(sessionId);

			settings.LastSyncedOnClient = clientStart;
			settings.LastSyncedOnServer = serverStart;

			SetTime(new DateTime(2019, 7, 10, 12, 1, 5, DateTimeKind.Utc));
			RunSync(manager, s =>
			{
				s.LastSyncedOnClient = clientStart;
				s.LastSyncedOnServer = serverStart;
			});

			IsTrue(ReadAll<AddressEntity>(serverDb.Addresses).Any(x => x.Line1 == "123 Main Street"));
		});
	}

	[TestMethod]
	public void EntitiesShouldBeFilteredMoreByAndAlso()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home", "SC");
			var home = ReadAll<AddressEntity>(database.Addresses).First(x => x.Line1 == "Home");
			home.Postal = "12345";
			Save(database, home);
			AddAddress(database, "Home2", "SC");
			var home2 = ReadAll<AddressEntity>(database.Addresses).First(x => x.Line1 == "Home2");
			home2.Postal = "54321";
			Save(database, home2);
			AddAddress(database, "Work", "GA");

			var client = new NoDefaultFilterSyncClient("Client", provider, this, new SyncStatistics(), new Profiler("Client"));
			var sessionId = Guid.NewGuid();
			var settings = new SyncSettings { IncludeIssueDetails = true };
			settings.AddFilter<AddressEntity>(outgoingFilter: x => x.State == "SC");
			client.BeginSync(sessionId, settings);

			var request = NewRequest();
			var changes = client.GetChanges(sessionId, request);
			AreEqual(2, changes.Collection.Count);

			settings.ResetFilters();
			settings.AddFilter<AddressEntity>(outgoingFilter: x => (x.State == "SC") && (x.Postal == "12345"));
			changes = client.GetChanges(sessionId, request);
			AreEqual(1, changes.Collection.Count);
			var model = (Address) changes.Collection[0].ToSyncModel();
			AreEqual("Home", model.Line1);
			AreEqual("12345", model.Postal);
		});
	}

	[TestMethod]
	public void EntitiesShouldBeFilteredMoreByOr()
	{
		WithEachProvider((provider, database) =>
		{
			AddAddress(database, "Home", "SC");
			AddAddress(database, "Home2", "NC");
			AddAddress(database, "Work", "GA");

			var client = new NoDefaultFilterSyncClient("Client", provider, this, new SyncStatistics(), new Profiler("Client"));
			var sessionId = Guid.NewGuid();
			var settings = new SyncSettings { IncludeIssueDetails = true };
			settings.AddFilter<AddressEntity>(outgoingFilter: x => x.State == "SC");
			client.BeginSync(sessionId, settings);

			var request = NewRequest();
			var changes = client.GetChanges(sessionId, request);
			AreEqual(1, changes.Collection.Count);

			settings.ResetFilters();
			settings.AddFilter<AddressEntity>(outgoingFilter: x => (x.State == "SC") || (x.State == "GA"));
			changes = client.GetChanges(sessionId, request);
			AreEqual(2, changes.Collection.Count);
		});
	}

	[TestMethod]
	public void EntityOptionalRelationshipsShouldBeFiltered()
	{
		WithEachProvider((provider, database) =>
		{
			var home = AddAddress(database, "Home", "SC");
			AddAccount(database, "John Doe", home);
			AddAddress(database, "Work", "GA");

			var client = new NoDefaultFilterSyncClient("Client", provider, this, new SyncStatistics(), new Profiler("Client"));
			var sessionId = Guid.NewGuid();
			var settings = new SyncSettings { IncludeIssueDetails = true };
			settings.AddFilter<AddressEntity>(outgoingFilter: x => x.State == "SC");
			client.BeginSync(sessionId, settings);

			var changes = client.GetChanges(sessionId, NewRequest());
			AreEqual(1, changes.Collection.Count);
			AreEqual("Home", ((Address) changes.Collection[0].ToSyncModel()).Line1);
		});
	}

	[TestMethod]
	public void EntityRequiredRelationshipsShouldBeFiltered()
	{
		WithEachProvider((provider, database) =>
		{
			var home = AddAddress(database, "Home", "SC");
			AddAccount(database, "John Doe", home);

			var client = new NoDefaultFilterSyncClient("Client", provider, this, new SyncStatistics(), new Profiler("Client"));
			var sessionId = Guid.NewGuid();
			var settings = new SyncSettings { IncludeIssueDetails = true };
			settings.AddFilter<AccountEntity>();
			client.BeginSync(sessionId, settings);

			var changes = client.GetChanges(sessionId, NewRequest());
			AreEqual(1, changes.Collection.Count);
			AreEqual("John Doe", ((Account) changes.Collection[0].ToSyncModel()).Name);
		});
	}

	[TestMethod]
	public void LookupFilterExpressionShouldOverrideSyncId()
	{
		WithEachPair((client, server, manager) =>
		{
			AddSetting(client, "Setting1", "foo", Guid.Parse("00000000-0000-0000-0000-000000000001"));
			AddSetting(client, "Setting2", "bar", Guid.Parse("00000000-0000-0000-0000-000000000002"));
			IncrementTime(seconds: 1);
			AddSetting(server, "Setting1", "hello", Guid.Parse("00000000-0000-0000-0000-000000000003"));
			AddSetting(server, "Setting2", "world", Guid.Parse("00000000-0000-0000-0000-000000000004"));

			RunSync(manager, settings =>
			{
				settings.SyncDirection = SyncDirection.PushUp;
				settings.AddFilter<SettingEntity>(lookupFilter: incoming => x => x.Name == incoming.Name);
			});

			var clientSettings = ReadAll<SettingEntity>(client.Settings);
			var serverSettings = ReadAll<SettingEntity>(server.Settings);
			AreEqual(2, clientSettings.Count);
			AreEqual(2, serverSettings.Count);

			var clientOne = clientSettings.First(x => x.Name == "Setting1");
			AreEqual("foo", clientOne.Value);
			AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000001"), clientOne.SyncId);

			var serverOne = serverSettings.First(x => x.Name == "Setting1");
			AreEqual("hello", serverOne.Value);
			AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000003"), serverOne.SyncId);
		});
	}

	[TestMethod]
	public void OlderSpokeTombstoneDoesNotDeleteNewerHubRow()
	{
		WithEachProvider((provider, database) =>
		{
			var stored = AddAddress(database, "Home");
			var tombstoneOn = stored.ModifiedOn;
			var hub = (AddressEntity) database.Addresses.Read(stored.SyncId);
			IncrementTime(minutes: 1);
			hub.Line1 = "Spoke A";
			hub.ModifiedOn = UtcNow;
			Save(database, hub);

			var server = new SampleServerSyncClient("Server", provider, this, new SyncStatistics(), new Profiler("Server"));
			var sessionId = Guid.NewGuid();
			server.BeginSync(sessionId, new SyncSettings { IncludeIssueDetails = true });

			var payload = new Address
			{
				City = "City",
				CreatedOn = tombstoneOn.AddMinutes(-1),
				IsDeleted = true,
				Line1 = "Home",
				ModifiedOn = tombstoneOn,
				Postal = "29640",
				State = "SC",
				SyncId = stored.SyncId
			};

			var result = server.ApplyChanges(sessionId, new ServiceRequest<SyncObject>(SyncObject.ToSyncObject(payload)));

			DetachTrackedEntities(database);
			AreEqual(0, result.Collection.Count);
			var remaining = (AddressEntity) database.Addresses.Read(stored.SyncId);
			IsNotNull(remaining);
			IsFalse(remaining.IsDeleted);
			AreEqual("Spoke A", remaining.Line1);
		});
	}

	[TestMethod]
	public void OlderTombstoneAppliesWhenLocalModifiedOnIsNewer()
	{
		WithEachProvider((provider, database) =>
		{
			var stored = AddAddress(database, "Home");
			var tombstoneOn = stored.ModifiedOn;
			var local = (AddressEntity) database.Addresses.Read(stored.SyncId);
			IncrementTime(minutes: 1);
			local.Line1 = "Updated";
			local.ModifiedOn = UtcNow;
			Save(database, local);

			var client = NewClient("Client", provider);
			var sessionId = Guid.NewGuid();
			client.BeginSync(sessionId, new SyncSettings { IncludeIssueDetails = true });

			var payload = new Address
			{
				City = "City",
				CreatedOn = tombstoneOn.AddMinutes(-1),
				IsDeleted = true,
				Line1 = "Home",
				ModifiedOn = tombstoneOn,
				Postal = "29640",
				State = "SC",
				SyncId = stored.SyncId
			};

			var result = client.ApplyChanges(sessionId, new ServiceRequest<SyncObject>(SyncObject.ToSyncObject(payload)));

			DetachTrackedEntities(database);
			AreEqual(0, result.Collection.Count);
			var remaining = (AddressEntity) database.Addresses.Read(stored.SyncId);
			IsNotNull(remaining);
			IsTrue(remaining.IsDeleted);
		});
	}

	[TestMethod]
	public void ServerClientShouldNotAcceptFilteredCorrections()
	{
		WithEachProvider((provider, database) =>
		{
			var address = AddAddress(database, "Home");
			AddAccount(database, "John Doe", address);

			var server = NewClient("Server", provider);
			var sessionId = Guid.NewGuid();
			var settings = new SyncSettings { IncludeIssueDetails = true };
			settings.AddFilter<AddressEntity>(outgoingFilter: x => x.Id > 0);
			server.BeginSync(sessionId, settings);

			var issues = new ServiceRequest<SyncIssue>(new SyncIssue
			{
				Id = ReadAll<AccountEntity>(database.Accounts)[0].SyncId,
				IssueType = SyncIssueType.RelationshipConstraint,
				TypeName = typeof(Account).ToAssemblyName()
			});
			var corrections = server.GetCorrections(sessionId, issues);
			AreEqual(0, corrections.Collection.Count);
		});
	}

	[TestMethod]
	public void ServerLimitRepository()
	{
		WithEachPair((client, server, manager) =>
		{
			AddAddress(client, "Client Home");
			AddAccount(client, "Client User", ReadAll<AddressEntity>(client.Addresses).First(x => x.Line1 == "Client Home"));
			AddAddress(server, "Server Home");

			RunSync(manager, settings =>
			{
				settings.ResetFilters();
				settings.AddFilter<AddressEntity>();
				settings.AddFilter<AccountEntity>(outgoingFilter: x => x.Name == "__none__");
			});

			IsTrue(ReadAll<AddressEntity>(server.Addresses).Any(x => x.Line1 == "Client Home"));
			AreEqual(0, ReadAll<AccountEntity>(server.Accounts).Count);
		});
	}

	[TestMethod]
	public void UseItemOnClientAndServerDeletesIt()
	{
		WithEachPair((client, server, manager) =>
		{
			var foo = AddAddress(client, "Foo");
			AddAccount(client, "Foo Bar", foo);
			AddAddress(server, "Bar");
			RunSync(manager);

			var account = ReadAll<AccountEntity>(server.Accounts).First(x => x.Name == "Foo Bar");
			AreEqual("Foo", ((AddressEntity) server.Addresses.Read(account.AddressSyncId.GetValueOrDefault())).Line1);

			var clientAccount = (AccountEntity) client.Accounts.Read(account.SyncId);
			var bar = ReadAll<AddressEntity>(client.Addresses).First(x => x.Line1 == "Bar");
			clientAccount.AddressSyncId = bar.SyncId;
			clientAccount.AddressId = bar.Id;
			clientAccount.ModifiedOn = UtcNow;
			Save(client, clientAccount);

			var serverBar = ReadAll<AddressEntity>(server.Addresses).First(x => x.Line1 == "Bar");
			serverBar.IsDeleted = true;
			serverBar.ModifiedOn = UtcNow;
			Save(server, serverBar);

			RunSync(manager);

			var serverPerson = ReadAll<AccountEntity>(server.Accounts).First(x => x.Name == "Foo Bar");
			AreEqual(bar.SyncId, serverPerson.AddressSyncId);
			IsTrue(ReadAll<AddressEntity>(server.Addresses).Any(x => (x.SyncId == bar.SyncId) && !x.IsDeleted)
				|| ReadAll<AddressEntity>(server.Addresses).Any(x => x.SyncId == bar.SyncId));
		});
	}

	[TestMethod]
	public void UseItemOnServerAndClientDeletesIt()
	{
		WithEachPair((client, server, manager) =>
		{
			var foo = AddAddress(client, "Foo");
			AddAccount(client, "Foo Bar", foo);
			AddAddress(server, "Bar");
			RunSync(manager);

			var serverAccount = ReadAll<AccountEntity>(server.Accounts).First(x => x.Name == "Foo Bar");
			var serverBar = ReadAll<AddressEntity>(server.Addresses).First(x => x.Line1 == "Bar");
			serverAccount.AddressSyncId = serverBar.SyncId;
			serverAccount.AddressId = serverBar.Id;
			serverAccount.ModifiedOn = UtcNow;
			Save(server, serverAccount);

			var clientBar = ReadAll<AddressEntity>(client.Addresses).First(x => x.Line1 == "Bar");
			clientBar.IsDeleted = true;
			clientBar.ModifiedOn = UtcNow;
			Save(client, clientBar);

			RunSync(manager);

			AreEqual(ReadAll<AddressEntity>(server.Addresses).Count, ReadAll<AddressEntity>(client.Addresses).Count);
			AreEqual(ReadAll<AccountEntity>(server.Accounts).Count, ReadAll<AccountEntity>(client.Accounts).Count);
		});
	}

	private SampleSyncClient NewClient(string name, ISyncableDatabaseProvider provider)
	{
		return new SampleSyncClient(name, provider, this, new SyncStatistics(), new Profiler(name));
	}

	private static SyncRequest NewRequest()
	{
		return new SyncRequest
		{
			Since = DateTime.MinValue,
			Until = DateTime.MaxValue.Subtract(TimeSpan.FromDays(1)),
			Take = 100
		};
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