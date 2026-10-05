#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cornerstone.Sample.Models;
using Cornerstone.Sample.Storage;
using Cornerstone.Sample.Sync;
using Cornerstone.Storage;
using Cornerstone.Storage.Sql;
using Cornerstone.Sync;
using Cornerstone.UnitTests.Storage.Sql;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EntityState = Microsoft.EntityFrameworkCore.EntityState;

#endregion

namespace Cornerstone.UnitTests.Sync;

public abstract class SyncScenarioTest : CornerstoneUnitTest
{
	#region Fields

	protected static readonly Guid Customer1SyncId = Guid.Parse("11111111-1111-1111-1111-111111111111");
	protected static readonly Guid Customer2SyncId = Guid.Parse("22222222-2222-2222-2222-222222222222");

	private readonly List<string> _efDatabasePaths;
	private readonly SqlServerTestPool _sqlServerPool;

	#endregion

	#region Constructors

	protected SyncScenarioTest()
	{
		_efDatabasePaths = [];
		_sqlServerPool = new SqlServerTestPool();
	}

	#endregion

	#region Properties

	protected ISampleDatabase ScenarioClient { get; set; }

	protected ISyncableDatabaseProvider ScenarioClientProvider { get; set; }

	protected string ScenarioPairName { get; set; }

	protected ISampleDatabase ScenarioServer { get; set; }

	protected ISyncableDatabaseProvider ScenarioServerProvider { get; set; }

	protected AccountEntity ServerAuthenticatedAccount { get; set; }

	#endregion

	#region Methods

	[TestCleanup]
	public override void TestCleanup()
	{
		_sqlServerPool.ResetCheckedOut();
		base.TestCleanup();
	}

	protected AccountEntity AddAccount(ISampleDatabase database, string name, AddressEntity address = null, CustomerEntity customer = null)
	{
		if ((customer == null) && address?.CustomerSyncId is { } addressCustomer && (addressCustomer != Guid.Empty))
		{
			customer = EnsureCustomer(database, addressCustomer);
		}

		var entity = new AccountEntity
		{
			AddressId = address?.Id,
			AddressSyncId = address?.SyncId,
			CreatedOn = UtcNow,
			CustomerId = customer?.Id,
			CustomerSyncId = customer?.SyncId ?? address?.CustomerSyncId,
			EmailAddress = $"{name.Replace(" ", string.Empty).ToLowerInvariant()}@domain.com",
			ModifiedOn = UtcNow,
			Name = name,
			Roles = ",,",
			SyncId = Guid.NewGuid()
		};
		database.Accounts.Add(entity);
		Save(database);
		return entity;
	}

	protected AddressEntity AddAddress(ISampleDatabase database, string line1, string state = "SC", Guid? syncId = null, CustomerEntity customer = null)
	{
		var entity = new AddressEntity
		{
			City = "City",
			CreatedOn = UtcNow,
			CustomerId = customer?.Id,
			CustomerSyncId = customer?.SyncId,
			ExternalId = Guid.NewGuid(),
			Line1 = line1,
			ModifiedOn = UtcNow,
			Postal = "29640",
			State = state,
			SyncId = syncId ?? Guid.NewGuid()
		};
		database.Addresses.Add(entity);
		Save(database);
		return entity;
	}

	protected BookmarkEntity AddBookmark(ISampleDatabase database, string name, bool isParent = false, Guid? parentSyncId = null, int order = 0, Guid? syncId = null, CustomerEntity customer = null)
	{
		var entity = new BookmarkEntity
		{
			CreatedOn = UtcNow,
			CustomerId = customer?.Id,
			CustomerSyncId = customer?.SyncId,
			IsParent = isParent,
			ModifiedOn = UtcNow,
			Name = name,
			Order = order,
			ParentSyncId = parentSyncId,
			SyncId = syncId ?? Guid.NewGuid()
		};
		database.Bookmarks.Add(entity);
		Save(database);
		return entity;
	}

	protected CustomerEntity AddCustomer(ISampleDatabase database, string name, Guid? syncId = null)
	{
		var entity = new CustomerEntity
		{
			CreatedOn = UtcNow,
			ModifiedOn = UtcNow,
			Name = name,
			SyncId = syncId ?? Guid.NewGuid()
		};
		database.Customers.Add(entity);
		Save(database);
		return entity;
	}

	protected SettingEntity AddSetting(ISampleDatabase database, string name, string value, Guid? syncId = null)
	{
		var entity = new SettingEntity
		{
			CreatedOn = UtcNow,
			ModifiedOn = UtcNow,
			Name = name,
			SyncId = syncId ?? Guid.NewGuid(),
			Value = value
		};
		database.Settings.Add(entity);
		Save(database);
		return entity;
	}

	protected void DetachScenarioDatabases()
	{
		DetachTrackedEntities(ScenarioClient);
		DetachTrackedEntities(ScenarioServer);
	}

	/// <summary>
	/// Drop the EF identity map so a later Read hits the database after another context saved.
	/// SQL repositories always query; this is a no-op there.
	/// </summary>
	protected static void DetachTrackedEntities(ISampleDatabase database)
	{
		if (database is not DbContext context)
		{
			return;
		}

		foreach (var entry in context.ChangeTracker.Entries().ToList())
		{
			entry.State = EntityState.Detached;
		}
	}

	protected CustomerEntity EnsureCustomer(ISampleDatabase database, Guid? syncId = null, string name = "Default")
	{
		if (syncId is { } id && (id != Guid.Empty))
		{
			if (database.Customers.Read(id) is CustomerEntity existingBySyncId)
			{
				return existingBySyncId;
			}

			return AddCustomer(database, name, id);
		}

		var existing = ReadAll<CustomerEntity>(database.Customers);
		if (existing.Count > 0)
		{
			return existing[0];
		}

		return AddCustomer(database, name);
	}

	protected SampleSyncManager NewHubSpokeManager(ISyncableDatabaseProvider spokeProvider, ISyncableDatabaseProvider hubProvider)
	{
		var manager = new SampleSyncManager(
			new SampleSyncClientProvider("Client", spokeProvider, this),
			new SampleWebServerSyncClientProvider(hubProvider, this, () => ServerAuthenticatedAccount),
			new SyncSession(this),
			RuntimeInformation,
			this,
			Dispatcher
		);
		manager.GetSyncSettings(SampleSyncClient.SyncAll).IncludeIssueDetails = true;
		return manager;
	}

	protected SampleSyncManager NewPeerManager(ISyncableDatabaseProvider clientProvider, ISyncableDatabaseProvider serverProvider)
	{
		var manager = new SampleSyncManager(
			new SampleSyncClientProvider("Client", clientProvider, this),
			new SampleSyncClientProvider("Server", serverProvider, this, () => ServerAuthenticatedAccount),
			new SyncSession(this),
			RuntimeInformation,
			this,
			Dispatcher
		);
		manager.GetSyncSettings(SampleSyncClient.SyncAll).IncludeIssueDetails = true;
		return manager;
	}

	protected static List<T> ReadAll<T>(ISyncableRepository repository)
		where T : class, ISyncEntity
	{
		return repository.ReadAllKeys()
			.Keys
			.Select(id => repository.Read(id) as T)
			.Where(x => x != null)
			.OrderBy(x => x.SyncId)
			.ToList();
	}

	protected static void Refresh(params ISampleDatabase[] databases)
	{
		foreach (var database in databases)
		{
			DetachTrackedEntities(database);
		}
	}

	protected static void Refresh(SyncIsland master, SyncIsland slave)
	{
		Refresh(
			master.Hub.Database,
			master.Spoke1.Database,
			master.Spoke2.Database,
			slave.Hub.Database,
			slave.Spoke1.Database,
			slave.Spoke2.Database
		);
	}

	protected SyncSession RunSync(SampleSyncManager manager, Action<SyncSettings> updateSettings = null)
	{
		var session = manager.Sync(SampleSyncClient.SyncAll, updateSettings, TimeSpan.FromSeconds(30));
		DetachScenarioDatabases();
		IsTrue(session.SyncSuccessful,
			() => $"state={session.State} issues={FormatIssues(session)} pair={ScenarioPairName}");
		IncrementTime(seconds: 1);
		return session;
	}

	protected void Save(ISampleDatabase database)
	{
		IncrementTime(seconds: 1);
		database.SaveChanges();
		IncrementTime(seconds: 1);
	}

	protected void Save(ISampleDatabase database, AddressEntity entity)
	{
		Save(database);
	}

	protected void Save(ISampleDatabase database, AccountEntity entity)
	{
		Save(database);
	}

	protected void Save(ISampleDatabase database, BookmarkEntity entity)
	{
		Save(database);
	}

	protected void Save(ISampleDatabase database, CustomerEntity entity)
	{
		Save(database);
	}

	protected void WithEachHomogeneousTrio(
		Action<ISyncableDatabaseProvider, ISampleDatabase, ISyncableDatabaseProvider, ISampleDatabase, ISyncableDatabaseProvider, ISampleDatabase> test)
	{
		foreach (var backend in GetScenarioBackends())
		{
			RunHomogeneousTrio(backend.Name, backend.Create, test);
		}
	}

	protected void WithEachPair(Action<ISampleDatabase, ISampleDatabase, SampleSyncManager> test, bool enableKeyCache = false)
	{
		RunEachPair(test, false, enableKeyCache);
	}

	protected void WithEachProvider(Action<ISyncableDatabaseProvider, ISampleDatabase> test, bool enableKeyCache = false)
	{
		foreach (var backend in GetScenarioBackends(enableKeyCache))
		{
			RunSingleProvider(backend.Name, backend.Create(), test);
		}
	}

	protected void WithEachWebPair(Action<ISampleDatabase, ISampleDatabase, SampleSyncManager> test)
	{
		RunEachPair(test, true);
	}

	protected void WithTwoIslands(Action<SyncIsland, SyncIsland> test)
	{
		foreach (var backend in GetScenarioBackends())
		{
			RunTwoIslands(backend.Name, backend.Create, test);
		}
	}

	private void CleanupScenarioStores()
	{
		SqliteConnection.ClearAllPools();
		foreach (var path in _efDatabasePaths)
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		_efDatabasePaths.Clear();
		_sqlServerPool.ResetCheckedOut();
	}

	private static string FormatIssues(SyncSession session)
	{
		return string.Join("; ", session.SyncIssues.Select(x => $"{x.IssueType}:{x.Message}"));
	}

	private List<ScenarioBackend> GetScenarioBackends(bool enableKeyCache = false)
	{
		DatabaseKeyCache CreateKeyCache()
		{
			return enableKeyCache ? new DatabaseKeyCache() : null;
		}

		var backends = new List<ScenarioBackend>
		{
			new("Sqlite", () => NewSqlMapperProvider(SqlProvider.Sqlite, CreateKeyCache())),
			new("EfSqlite", () => NewEfProvider(SqlProvider.Sqlite, CreateKeyCache()))
		};

		if (IncludeSqlServerDatabase)
		{
			_sqlServerPool.RequireAvailable();
			backends.Add(new ScenarioBackend("Sql", () => NewSqlMapperProvider(SqlProvider.SqlServer, CreateKeyCache())));
			backends.Add(new ScenarioBackend("EfSql", () => NewEfProvider(SqlProvider.SqlServer, CreateKeyCache())));
		}

		return backends;
	}

	private SampleEntityFrameworkDatabaseProvider NewEfProvider(SqlProvider provider, DatabaseKeyCache keyCache = null)
	{
		if (provider == SqlProvider.SqlServer)
		{
			return new SampleEntityFrameworkDatabaseProvider(
				_sqlServerPool.Checkout(true),
				SqlProvider.SqlServer,
				this,
				keyCache
			);
		}

		var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
		_efDatabasePaths.Add(path);
		return new SampleEntityFrameworkDatabaseProvider(
			$"Data Source={path}",
			SqlProvider.Sqlite,
			this,
			keyCache
		);
	}

	private SampleSqlDatabaseProvider NewSqlMapperProvider(SqlProvider provider, DatabaseKeyCache keyCache = null)
	{
		if (provider == SqlProvider.SqlServer)
		{
			return new SampleSqlDatabaseProvider(
				_sqlServerPool.Checkout(false),
				SqlProvider.SqlServer,
				this,
				keyCache
			);
		}

		return new SampleSqlDatabaseProvider(
			$"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared;",
			SqlProvider.Sqlite,
			this,
			keyCache
		);
	}

	private SyncIsland OpenIsland(Func<ISyncableDatabaseProvider> createProvider)
	{
		return new SyncIsland(
			OpenNode(createProvider),
			OpenNode(createProvider),
			OpenNode(createProvider)
		);
	}

	private SyncNode OpenNode(Func<ISyncableDatabaseProvider> createProvider)
	{
		var provider = createProvider();
		var database = (ISampleDatabase) provider.GetSyncableDatabase();
		database.Migrate();
		return new SyncNode(provider, database);
	}

	private void RunEachPair(Action<ISampleDatabase, ISampleDatabase, SampleSyncManager> test, bool useWebPair, bool enableKeyCache = false)
	{
		var backends = GetScenarioBackends(enableKeyCache);
		foreach (var client in backends)
		{
			foreach (var server in backends)
			{
				var pairName = $"{client.Name}Client-{server.Name}Server";
				if (useWebPair)
				{
					pairName += "-Web";
				}

				RunProviderPair(pairName, client.Create(), server.Create(), test, useWebPair);
			}
		}
	}

	private void RunHomogeneousTrio(
		string name,
		Func<ISyncableDatabaseProvider> createProvider,
		Action<ISyncableDatabaseProvider, ISampleDatabase, ISyncableDatabaseProvider, ISampleDatabase, ISyncableDatabaseProvider, ISampleDatabase> test)
	{
		try
		{
			var serverProvider = createProvider();
			var client1Provider = createProvider();
			var client2Provider = createProvider();
			using var server = (ISampleDatabase) serverProvider.GetSyncableDatabase();
			using var client1 = (ISampleDatabase) client1Provider.GetSyncableDatabase();
			using var client2 = (ISampleDatabase) client2Provider.GetSyncableDatabase();
			server.Migrate();
			client1.Migrate();
			client2.Migrate();
			ScenarioPairName = name;
			test(serverProvider, server, client1Provider, client1, client2Provider, client2);
		}
		catch (Exception ex)
		{
			throw new Exception($"{name}: {ex.Message}", ex);
		}
		finally
		{
			ScenarioPairName = null;
			CleanupScenarioStores();
		}
	}

	private void RunPair(
		string pairName,
		ISyncableDatabaseProvider clientProvider,
		ISyncableDatabaseProvider serverProvider,
		Action<ISampleDatabase, ISampleDatabase, SampleSyncManager> test,
		bool useWebPair = false)
	{
		using var clientDatabase = (ISampleDatabase) clientProvider.GetSyncableDatabase();
		using var serverDatabase = (ISampleDatabase) serverProvider.GetSyncableDatabase();
		clientDatabase.Migrate();
		serverDatabase.Migrate();

		ServerAuthenticatedAccount = null;
		ScenarioPairName = pairName;
		ScenarioClient = clientDatabase;
		ScenarioServer = serverDatabase;
		ScenarioClientProvider = clientProvider;
		ScenarioServerProvider = serverProvider;

		ISyncClientProvider serverSyncClientProvider = useWebPair
			? new SampleWebServerSyncClientProvider(serverProvider, this, () => ServerAuthenticatedAccount)
			: new SampleSyncClientProvider("Server", serverProvider, this, () => ServerAuthenticatedAccount);

		var manager = new SampleSyncManager(
			new SampleSyncClientProvider("Client", clientProvider, this),
			serverSyncClientProvider,
			new SyncSession(this),
			RuntimeInformation,
			this,
			Dispatcher
		);
		manager.GetSyncSettings(SampleSyncClient.SyncAll).IncludeIssueDetails = true;
		try
		{
			test(clientDatabase, serverDatabase, manager);
		}
		catch (Exception ex)
		{
			throw new Exception($"{pairName}: {ex.Message}", ex);
		}
		finally
		{
			ScenarioClient = null;
			ScenarioServer = null;
			ScenarioClientProvider = null;
			ScenarioServerProvider = null;
			ScenarioPairName = null;
		}
	}

	private void RunProviderPair(
		string pairName,
		ISyncableDatabaseProvider clientProvider,
		ISyncableDatabaseProvider serverProvider,
		Action<ISampleDatabase, ISampleDatabase, SampleSyncManager> test,
		bool useWebPair = false)
	{
		try
		{
			RunPair(pairName, clientProvider, serverProvider, test, useWebPair);
		}
		finally
		{
			CleanupScenarioStores();
		}
	}

	private void RunSingleProvider(string name, ISyncableDatabaseProvider provider, Action<ISyncableDatabaseProvider, ISampleDatabase> test)
	{
		try
		{
			using var database = (ISampleDatabase) provider.GetSyncableDatabase();
			database.Migrate();
			ScenarioPairName = name;
			ScenarioClient = database;
			ScenarioClientProvider = provider;
			test(provider, database);
		}
		catch (Exception ex)
		{
			throw new Exception($"{name}: {ex.Message}", ex);
		}
		finally
		{
			ScenarioClient = null;
			ScenarioClientProvider = null;
			ScenarioPairName = null;
			CleanupScenarioStores();
		}
	}

	private void RunTwoIslands(
		string name,
		Func<ISyncableDatabaseProvider> createProvider,
		Action<SyncIsland, SyncIsland> test)
	{
		try
		{
			var master = OpenIsland(createProvider);
			var slave = OpenIsland(createProvider);
			using (master.Hub.Database)
			using (master.Spoke1.Database)
			using (master.Spoke2.Database)
			using (slave.Hub.Database)
			using (slave.Spoke1.Database)
			using (slave.Spoke2.Database)
			{
				ScenarioPairName = name;
				test(master, slave);
			}
		}
		catch (Exception ex)
		{
			throw new Exception($"{name}: {ex.Message}", ex);
		}
		finally
		{
			ScenarioPairName = null;
			CleanupScenarioStores();
		}
	}

	#endregion

	#region Classes

	protected sealed class SyncIsland
	{
		#region Constructors

		public SyncIsland(SyncNode hub, SyncNode spoke1, SyncNode spoke2)
		{
			Hub = hub;
			Spoke1 = spoke1;
			Spoke2 = spoke2;
		}

		#endregion

		#region Properties

		public SyncNode Hub { get; }

		public SyncNode Spoke1 { get; }

		public SyncNode Spoke2 { get; }

		#endregion
	}

	protected sealed class SyncNode
	{
		#region Constructors

		public SyncNode(ISyncableDatabaseProvider provider, ISampleDatabase database)
		{
			Provider = provider;
			Database = database;
		}

		#endregion

		#region Properties

		public ISampleDatabase Database { get; }

		public ISyncableDatabaseProvider Provider { get; }

		#endregion
	}

	private sealed class ScenarioBackend
	{
		#region Constructors

		public ScenarioBackend(string name, Func<ISyncableDatabaseProvider> create)
		{
			Name = name;
			Create = create;
		}

		#endregion

		#region Properties

		public Func<ISyncableDatabaseProvider> Create { get; }

		public string Name { get; }

		#endregion
	}

	#endregion
}