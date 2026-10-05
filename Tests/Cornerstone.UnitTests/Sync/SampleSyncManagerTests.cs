#region References

using System;
using System.Linq;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sample.Models;
using Cornerstone.Sample.Storage;
using Cornerstone.Sample.Sync;
using Cornerstone.Storage.Sql;
using Cornerstone.Sync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
[DoNotParallelize]
public class SampleSyncManagerTests : SyncScenarioTest
{
	#region Methods

	[TestMethod]
	public void SyncAccount()
	{
		WithEachPair((client, server, manager) =>
		{
			client.Accounts.Add(CreateAccount());
			AreEqual(1, client.SaveChanges());
			SetTime(UtcNow.AddMinutes(1));

			var session = RunSync(manager);
			IsTrue(session.SyncSuccessful || manager.SyncSession.SyncSuccessful, () =>
				$"result={session.State} manager={manager.SyncSession.State} issues={string.Join("; ", session.SyncIssues.Concat(manager.SyncSession.SyncIssues).Select(x => $"{x.IssueType}:{x.Message}"))}");

			var stored = server.Accounts.Read(client.Accounts.ReadAllKeys().Keys.Single()) as AccountEntity;
			IsNotNull(stored);
			AreEqual("John", stored.Name);
		});
	}

	[TestMethod]
	public void EmptyFiltersDoNotSync()
	{
		var clientProvider = NewMemoryProvider();
		var serverProvider = NewMemoryProvider();
		using var clientDatabase = (ISampleDatabase) clientProvider.GetSyncableDatabase();
		using var serverDatabase = (ISampleDatabase) serverProvider.GetSyncableDatabase();
		clientDatabase.Migrate();
		serverDatabase.Migrate();

		var manager = NewEmptyFilterManager(clientProvider, serverProvider);
		AddAddress(clientDatabase, "Stay");

		var session = manager.Sync(SampleSyncClient.SyncAll, null, TimeSpan.FromSeconds(30));

		IsFalse(session.SyncSuccessful);
		IsTrue(session.SyncIssues.Any(x => x.IssueType == SyncIssueType.RepositoryFiltered));
		AreEqual(0, ReadAll<AddressEntity>(serverDatabase.Addresses).Count);
		AreEqual(DateTime.MinValue, manager.GetSyncSettings(SampleSyncClient.SyncAll).LastSyncedOnClient);
		AreEqual(DateTime.MinValue, manager.GetSyncSettings(SampleSyncClient.SyncAll).LastSyncedOnServer);
	}

	[TestMethod]
	public void RegisteredFiltersStillSync()
	{
		var clientProvider = NewMemoryProvider();
		var serverProvider = NewMemoryProvider();
		using var clientDatabase = (ISampleDatabase) clientProvider.GetSyncableDatabase();
		using var serverDatabase = (ISampleDatabase) serverProvider.GetSyncableDatabase();
		clientDatabase.Migrate();
		serverDatabase.Migrate();

		var manager = new SampleSyncManager(
			new SampleSyncClientProvider("Client", clientProvider, this),
			new SampleSyncClientProvider("Server", serverProvider, this),
			new SyncSession(this),
			RuntimeInformation,
			this,
			Dispatcher
		);
		AddAddress(clientDatabase, "Home");

		var session = manager.Sync(SampleSyncClient.SyncAll, null, TimeSpan.FromSeconds(30));

		IsTrue(session.SyncSuccessful, () => string.Join("; ", session.SyncIssues.Select(x => $"{x.IssueType}:{x.Message}")));
		IsTrue(ReadAll<AddressEntity>(serverDatabase.Addresses).Any(x => x.Line1 == "Home"));
		IsTrue(manager.GetSyncSettings(SampleSyncClient.SyncAll).LastSyncedOnClient > DateTime.MinValue);
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

	private SampleSyncManager NewEmptyFilterManager(ISyncableDatabaseProvider clientProvider, ISyncableDatabaseProvider serverProvider)
	{
		return new SampleSyncManager(
			new EmptyFilterProvider("Client", clientProvider, this),
			new EmptyFilterProvider("Server", serverProvider, this),
			new SyncSession(this),
			RuntimeInformation,
			this,
			Dispatcher
		);
	}

	private SampleSqlDatabaseProvider NewMemoryProvider()
	{
		return new SampleSqlDatabaseProvider(
			$"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared;",
			SqlProvider.Sqlite,
			this
		);
	}

	#endregion

	#region Classes

	private sealed class EmptyFilterProvider : ISyncClientProvider
	{
		#region Fields

		private readonly ISyncableDatabaseProvider _databaseProvider;
		private readonly IDateTimeProvider _dateTimeProvider;
		private readonly string _name;

		#endregion

		#region Constructors

		public EmptyFilterProvider(string name, ISyncableDatabaseProvider databaseProvider, IDateTimeProvider dateTimeProvider)
		{
			_name = name;
			_databaseProvider = databaseProvider;
			_dateTimeProvider = dateTimeProvider;
		}

		#endregion

		#region Methods

		public SyncClient GetSyncClient(SyncStatistics syncStatistics, Profiler syncClientProfiler)
		{
			return new EmptyFilterSyncClient(_name, _databaseProvider, _dateTimeProvider, syncStatistics, syncClientProfiler);
		}

		public ISyncableDatabase GetSyncableDatabase()
		{
			return _databaseProvider.GetSyncableDatabase();
		}

		#endregion
	}

	private sealed class EmptyFilterSyncClient : SampleSyncClient
	{
		#region Constructors

		public EmptyFilterSyncClient(
			string name,
			ISyncableDatabaseProvider databaseProvider,
			IDateTimeProvider dateTimeProvider,
			SyncStatistics statistics,
			Profiler profiler
		) : base(name, databaseProvider, dateTimeProvider, statistics, profiler)
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