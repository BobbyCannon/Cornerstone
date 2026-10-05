#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Cornerstone.Sample.Models;
using Cornerstone.Sample.Sync;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Cornerstone.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Sync;

[TestClass]
[DoNotParallelize]
public class SyncKeyCacheTests : SyncScenarioTest
{
	#region Methods

	[TestMethod]
	public void CacheHitReadsByPrimaryKey()
	{
		ApplyCachedAddress(asServer: false, lookup: false, (counts, stored) =>
		{
			AreEqual(0, counts.ReadBySyncId);
			IsTrue(counts.ReadByPrimaryId >= 1);
			AreEqual("Updated", stored.Line1);
		});
	}

	[TestMethod]
	public void CacheStartsEmptyUntilEntityIsAddedToCache()
	{
		WithEachProvider((_, database) =>
		{
			var cache = database.KeyCache;
			IsNotNull(cache);

			var address = AddAddress(database, "Home");
			if (cache.GetEntityId(typeof(AddressEntity), address.SyncId) == null)
			{
				cache.AddEntityId(typeof(AddressEntity), address.SyncId, address.Id);
			}
			AreEqual(address.Id, (long) cache.GetEntityId(typeof(AddressEntity), address.SyncId));
			cache.RemoveEntityId(typeof(AddressEntity), address.SyncId);
			IsNull(cache.GetEntityId(typeof(AddressEntity), address.SyncId));
			cache.AddEntityId(typeof(AddressEntity), address.SyncId, address.Id);
			AreEqual(address.Id, (long) cache.GetEntityId(typeof(AddressEntity), address.SyncId));
			cache.RemoveEntityId(typeof(AddressEntity), address.SyncId);
			IsNull(cache.GetEntityId(typeof(AddressEntity), address.SyncId));
		}, enableKeyCache: true);
	}

	[TestMethod]
	public void InvalidCacheDoesNotPreventSync()
	{
		WithEachPair((client, server, manager) =>
		{
			var address = AddAddress(server, "Home");
			IsNotNull(client.KeyCache);
			client.KeyCache.AddEntityId(typeof(AddressEntity), address.SyncId, 999L);

			RunSync(manager);

			AreEqual(1, ReadAll<AddressEntity>(client.Addresses).Count);
			AreEqual("Home", ReadAll<AddressEntity>(client.Addresses)[0].Line1);
		}, enableKeyCache: true);
	}

	[TestMethod]
	public void LookupFilterSkipsTheKeyCache()
	{
		ApplyCachedAddress(asServer: false, lookup: true, (counts, stored) =>
		{
			AreEqual(0, counts.ReadByPrimaryId);
			AreEqual(0, counts.ReadBySyncId);
			IsTrue(counts.ReadByLookup >= 1);
			AreEqual("Home", stored.Line1);
		});
	}

	[TestMethod]
	public void ServerClientSkipsTheKeyCache()
	{
		ApplyCachedAddress(asServer: true, lookup: false, (counts, stored) =>
		{
			AreEqual(0, counts.ReadByPrimaryId);
			IsTrue(counts.ReadBySyncId >= 1);
			AreEqual("Updated", stored.Line1);
		});
	}

	private void ApplyCachedAddress(bool asServer, bool lookup, Action<ReadCounts, AddressEntity> assert)
	{
		WithEachProvider((provider, database) =>
		{
			var address = AddAddress(database, "Home");
			IsNotNull(provider.KeyCache);
			provider.KeyCache.AddEntityId(typeof(AddressEntity), address.SyncId, address.Id);

			var counts = new ReadCounts();
			var wrapped = new CountingDatabaseProvider(provider, counts);
			SyncClient client = asServer
				? new SampleServerSyncClient("Server", wrapped, this, new SyncStatistics(), new Profiler("Server"))
				: new SampleSyncClient("Client", wrapped, this, new SyncStatistics(), new Profiler("Client"));
			var settings = new SyncSettings();
			settings.IncludeIssueDetails = true;
			if (lookup)
			{
				settings.AddFilter<AddressEntity>(lookupFilter: incoming => row => row.Line1 == incoming.Line1);
			}

			var sessionId = Guid.NewGuid();
			client.BeginSync(sessionId, settings);
			IncrementTime(minutes: 1);
			var incoming = SyncObject.ToSyncObject(new Address
			{
				City = "Moved",
				CreatedOn = address.CreatedOn,
				Line1 = lookup ? "Home" : "Updated",
				ModifiedOn = UtcNow,
				Postal = "29640",
				State = "SC",
				SyncId = address.SyncId
			});
			var result = client.ApplyChanges(sessionId, new ServiceRequest<SyncObject>(incoming));
			AreEqual(0, result.Collection.Count, () => string.Join("; ", result.Collection.Select(x => $"{x.IssueType}:{x.Message}")));

			DetachTrackedEntities(database);
			var stored = (AddressEntity) database.Addresses.Read(address.SyncId);
			AreEqual("Moved", stored.City);
			assert(counts, stored);
		}, enableKeyCache: true);
	}

	#endregion

	#region Classes

	private sealed class CountingDatabase : ISyncableDatabase
	{
		#region Fields

		private readonly ReadCounts _counts;
		private readonly ISyncableDatabase _inner;

		#endregion

		#region Constructors

		public CountingDatabase(ISyncableDatabase inner, ReadCounts counts)
		{
			_inner = inner;
			_counts = counts;
		}

		#endregion

		#region Properties

		public DatabaseSettings DatabaseSettings => _inner.DatabaseSettings;

		public IDateTimeProvider DateTimeProvider => _inner.DateTimeProvider;

		public bool IsDisposed => _inner.IsDisposed;

		public DatabaseKeyCache KeyCache => _inner.KeyCache;

		public Profiler Profiler
		{
			get => _inner.Profiler;
			set => _inner.Profiler = value;
		}

		public (string entity, string syncObject)[] SyncOrder => _inner.SyncOrder;

		#endregion

		#region Methods

		public int DiscardChanges()
		{
			return _inner.DiscardChanges();
		}

		public void Dispose()
		{
			_inner.Dispose();
		}

		public DatabaseType GetDatabaseType()
		{
			return _inner.GetDatabaseType();
		}

		public Assembly GetMappingAssembly()
		{
			return _inner.GetMappingAssembly();
		}

		public IRepository<T, T2> GetReadOnlyRepository<T, T2>() where T : Entity<T2>
		{
			return _inner.GetReadOnlyRepository<T, T2>();
		}

		public IRepository<T, T2> GetRepository<T, T2>() where T : Entity<T2>
		{
			return _inner.GetRepository<T, T2>();
		}

		public IEnumerable<ISyncableRepository> GetSyncableRepositories()
		{
			var repositories = new List<ISyncableRepository>();
			foreach (var repository in _inner.GetSyncableRepositories())
			{
				repositories.Add(new CountingRepository(repository, _counts));
			}

			return repositories;
		}

		public ISyncableRepository<T, T2> GetSyncableRepository<T, T2>() where T : SyncEntity<T2>
		{
			return _inner.GetSyncableRepository<T, T2>();
		}

		public ISyncableRepository GetSyncableRepository(Type type)
		{
			var repository = _inner.GetSyncableRepository(type);
			if (repository == null)
			{
				return null;
			}

			return new CountingRepository(repository, _counts);
		}

		public bool IsDatabaseMigrated()
		{
			return _inner.IsDatabaseMigrated();
		}

		public void Migrate()
		{
			_inner.Migrate();
		}

		public T Remove<T, T2>(T item) where T : Entity<T2>
		{
			return _inner.Remove<T, T2>(item);
		}

		public int SaveChanges()
		{
			return _inner.SaveChanges();
		}

		public void UpdateDateTimeProvider(IDateTimeProvider dateTimeProvider)
		{
			_inner.UpdateDateTimeProvider(dateTimeProvider);
		}

		#endregion

		#region Events

		public event EventHandler<CollectionChangeTracker> ChangesSaved
		{
			add => _inner.ChangesSaved += value;
			remove => _inner.ChangesSaved -= value;
		}

		public event EventHandler Disposed
		{
			add => _inner.Disposed += value;
			remove => _inner.Disposed -= value;
		}

		#endregion
	}

	private sealed class CountingDatabaseProvider : ISyncableDatabaseProvider
	{
		#region Fields

		private readonly ReadCounts _counts;
		private readonly ISyncableDatabaseProvider _inner;

		#endregion

		#region Constructors

		public CountingDatabaseProvider(ISyncableDatabaseProvider inner, ReadCounts counts)
		{
			_inner = inner;
			_counts = counts;
		}

		#endregion

		#region Properties

		public DatabaseKeyCache KeyCache => _inner.KeyCache;

		public DatabaseSettings Settings
		{
			get => _inner.Settings;
			set => _inner.Settings = value;
		}

		#endregion

		#region Methods

		public IDatabase GetDatabase()
		{
			return GetSyncableDatabase();
		}

		public IDatabase GetDatabase(DatabaseSettings settings, DatabaseKeyCache keyCache)
		{
			return GetSyncableDatabase(settings, keyCache);
		}

		public ISyncableDatabase GetSyncableDatabase()
		{
			return new CountingDatabase(_inner.GetSyncableDatabase(), _counts);
		}

		public ISyncableDatabase GetSyncableDatabase(DatabaseSettings settings, DatabaseKeyCache keyCache)
		{
			return new CountingDatabase(_inner.GetSyncableDatabase(settings, keyCache), _counts);
		}

		#endregion
	}

	private sealed class CountingRepository : ISyncableRepository
	{
		#region Fields

		private readonly ReadCounts _counts;
		private readonly ISyncableRepository _inner;

		#endregion

		#region Constructors

		public CountingRepository(ISyncableRepository inner, ReadCounts counts)
		{
			_inner = inner;
			_counts = counts;
		}

		#endregion

		#region Properties

		public Type RealType => _inner.RealType;

		public string TypeName => _inner.TypeName;

		#endregion

		#region Methods

		public void Add(ISyncEntity entity)
		{
			_inner.Add(entity);
		}

		public void Discard(ISyncEntity entity)
		{
			_inner.Discard(entity);
		}

		public int GetChangeCount(DateTime since, DateTime until, SyncRepositoryFilter filter)
		{
			return _inner.GetChangeCount(since, until, filter);
		}

		public IEnumerable<ISyncEntity> GetChanges(DateTime since, DateTime until, int skip, int take, SyncRepositoryFilter filter)
		{
			return _inner.GetChanges(since, until, skip, take, filter);
		}

		public ISyncEntity Read(Guid syncId)
		{
			CountAddress(() => _counts.ReadBySyncId++);
			return _inner.Read(syncId);
		}

		public ISyncEntity Read(ISyncEntity entity, SyncRepositoryFilter filter)
		{
			CountAddress(() => _counts.ReadByLookup++);
			return _inner.Read(entity, filter);
		}

		public IDictionary<Guid, object> ReadAllKeys()
		{
			return _inner.ReadAllKeys();
		}

		public ISyncEntity ReadByPrimaryId(object primaryId)
		{
			CountAddress(() => _counts.ReadByPrimaryId++);
			return _inner.ReadByPrimaryId(primaryId);
		}

		public void Remove(ISyncEntity entity)
		{
			_inner.Remove(entity);
		}

		private void CountAddress(Action count)
		{
			if (_inner.RealType == typeof(AddressEntity))
			{
				count();
			}
		}

		#endregion
	}

	private sealed class ReadCounts
	{
		#region Properties

		public int ReadByLookup { get; set; }

		public int ReadByPrimaryId { get; set; }

		public int ReadBySyncId { get; set; }

		#endregion
	}

	#endregion
}
