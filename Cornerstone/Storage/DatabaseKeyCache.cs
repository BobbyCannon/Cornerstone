#region References

using System;
using System.Collections.Concurrent;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Storage;

/// <summary>
/// Cache for managing database keys. This allows for caching of entities primary and secondary keys (Id, SyncId).
/// </summary>
public class DatabaseKeyCache
{
	#region Fields

	private readonly ConcurrentDictionary<Type, MemoryCache<string, object>> _cachedEntityId;
	private readonly TimeSpan _cacheTimeout;

	#endregion

	#region Constructors

	/// <summary>
	/// Instantiate an instance of the database key cache.
	/// </summary>
	public DatabaseKeyCache() : this(TimeSpan.FromMinutes(15))
	{
	}

	/// <summary>
	/// Instantiate an instance of the database key cache.
	/// </summary>
	/// <param name="cacheTimeout"> The timeout for removing an item from the cache. </param>
	public DatabaseKeyCache(TimeSpan cacheTimeout)
	{
		_cachedEntityId = new ConcurrentDictionary<Type, MemoryCache<string, object>>();
		_cacheTimeout = cacheTimeout;

		SyncEntitiesToCache = [];
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets the list of entities to cache the keys (ID, Sync ID). If the collection is empty
	/// then cache all sync entities.
	/// </summary>
	public Type[] SyncEntitiesToCache { get; private set; }

	#endregion

	#region Methods

	/// <summary>
	/// Cache an entity ID for the sync entity.
	/// </summary>
	/// <param name="entity"> The entity to be cached. </param>
	public void AddEntity(ISyncEntity entity)
	{
		AddEntityId(entity.GetRealType(), entity.GetEntitySyncId(), entity.GetEntityId());
	}

	/// <summary>
	/// Cache an entity ID for the entity Sync ID.
	/// </summary>
	/// <param name="type"> The type of the entity. </param>
	/// <param name="syncId"> The sync ID of the entity. Will be converted to a string using "ToString". </param>
	/// <param name="id"> The ID of the entity. </param>
	public void AddEntityId(Type type, object syncId, object id)
	{
		if (id == null)
		{
			return;
		}

		if ((SyncEntitiesToCache.Length > 0) && !SyncEntitiesToCache.Contains(type))
		{
			// We are filtering what sync entities to cache and this entity is not in the list
			return;
		}

		var cache = _cachedEntityId.GetOrAdd(type, _ => new MemoryCache<string, object>(_cacheTimeout));
		cache.AddOrUpdate(syncId.ToString(), id);
	}

	/// <summary>
	/// Cleanup the cache by removing old entries and empty collections.
	/// </summary>
	public void Cleanup()
	{
		foreach (var entry in _cachedEntityId)
		{
			entry.Value.Cleanup();
		}
	}

	/// <summary>
	/// Clear all caches from the manager
	/// </summary>
	public void Clear()
	{
		_cachedEntityId.ForEach(x => x.Value.Clear());
		_cachedEntityId.Clear();
	}

	/// <summary>
	/// Get the entity ID for the sync entity.
	/// </summary>
	/// <param name="entity"> The type of the entity. </param>
	/// <returns> The ID of the entity. </returns>
	public object GetEntityId<T>(T entity) where T : ISyncEntity
	{
		return GetEntityId(entity.GetRealType(), entity.GetEntitySyncId());
	}

	/// <summary>
	/// Get the entity ID for the sync ID.
	/// </summary>
	/// <param name="type"> The type of the entity. </param>
	/// <param name="syncId"> The sync ID of the entity. </param>
	/// <returns> The ID of the entity. </returns>
	public object GetEntityId(Type type, object syncId)
	{
		var cache = _cachedEntityId.GetOrAdd(type, _ => new MemoryCache<string, object>(_cacheTimeout));
		return cache.TryGet(syncId.ToString(), out var cachedItem) ? cachedItem.Value : null;
	}

	/// <summary>
	/// Returns true if they keys have been loaded.
	/// </summary>
	public bool HasKeysBeenLoadedIntoCache(Type type)
	{
		return _cachedEntityId.ContainsKey(type);
	}

	/// <summary>
	/// Initializes the default key cache.
	/// </summary>
	/// <param name="syncEntitiesToCache"> An optional set of specific entity types to cache. </param>
	public void Initialize(params Type[] syncEntitiesToCache)
	{
		SyncEntitiesToCache = syncEntitiesToCache;

		// Reset the cache because we may have added / removed support for new entities
		_cachedEntityId.Clear();
	}

	/// <summary>
	/// Initializes the default key cache and load the keys.
	/// </summary>
	/// <param name="provider"> The syncable database provider. </param>
	/// <param name="syncEntitiesToCache"> An optional set of specific entity types to cache. </param>
	public void InitializeAndLoad(ISyncableDatabaseProvider provider, params string[] syncEntitiesToCache)
	{
		var syncTypes = syncEntitiesToCache.Select(Type.GetType).ToArray();
		InitializeAndLoad(provider, syncTypes);
	}

	/// <summary>
	/// Initializes the default key cache and load the keys.
	/// </summary>
	/// <param name="provider"> The syncable database provider. </param>
	/// <param name="syncEntitiesToCache"> An optional set of specific entity types to cache. </param>
	public void InitializeAndLoad(ISyncableDatabaseProvider provider, params Type[] syncEntitiesToCache)
	{
		using var database = provider.GetSyncableDatabase();
		Initialize(syncEntitiesToCache);
		LoadKeysIntoCache(database);
	}

	/// <summary>
	/// Initializes the default key cache and load the keys.
	/// </summary>
	/// <param name="database"> The syncable database. </param>
	/// <param name="syncEntitiesToCache"> An optional set of specific entity types to cache. </param>
	public void InitializeAndLoad(ISyncableDatabase database, params Type[] syncEntitiesToCache)
	{
		Initialize(syncEntitiesToCache);
		LoadKeysIntoCache(database);
	}

	/// <summary>
	/// Loads all keys for the sync entities to be cached.
	/// </summary>
	/// <param name="database"> The database to use for loading. </param>
	public void LoadKeysIntoCache(ISyncableDatabase database)
	{
		LoadKeysIntoCache(database, SyncEntitiesToCache);
	}

	/// <summary>
	/// Allows for caching of individual sync types.
	/// </summary>
	/// <param name="database"> The database to use for loading. </param>
	/// <param name="types"> The types to be loaded. </param>
	public void LoadKeysIntoCache(ISyncableDatabase database, params Type[] types)
	{
		var repositories = database.GetSyncableRepositories().ToList();

		foreach (var type in types)
		{
			var repository = repositories.FirstOrDefault(x => x.RealType == type);
			if (repository == null)
			{
				continue;
			}

			if ((SyncEntitiesToCache.Length > 0) && !SyncEntitiesToCache.Contains(type))
			{
				// We are filtering what sync entities to cache and this entity is not in the list
				continue;
			}

			var keys = repository.ReadAllKeys();
			var cache = CreateCache(type);
			keys.ForEach(x => AddEntityId(cache, x.Key, x.Value));
		}
	}

	/// <summary>
	/// Remove an entity ID for the entity Sync ID.
	/// </summary>
	/// <param name="entity"> The entity to be un-cached. </param>
	public void RemoveEntity(ISyncEntity entity)
	{
		if (entity == null)
		{
			return;
		}

		RemoveEntityId(entity.GetRealType(), entity.GetEntitySyncId());
	}

	/// <summary>
	/// Remove an entity ID for the entity Sync ID.
	/// </summary>
	/// <param name="type"> The type of the entity. </param>
	/// <param name="syncId"> The sync ID of the entity. </param>
	public void RemoveEntityId(Type type, object syncId)
	{
		if (!_cachedEntityId.TryGetValue(type, out var cache))
		{
			return;
		}

		cache.Remove(syncId.ToString());
	}

	/// <summary>
	/// Does the key cache support the following type.
	/// </summary>
	/// <param name="type"> The type to test for. </param>
	/// <returns> True if the type is support or false if otherwise. </returns>
	public bool SupportsType(Type type)
	{
		return (SyncEntitiesToCache.Length <= 0)
			|| SyncEntitiesToCache.Contains(type);
	}

	/// <summary>
	/// Update the cache with the provided tracker changes.
	/// </summary>
	/// <param name="tracker"> The tracker with the changes. </param>
	public void UpdateCache(CollectionChangeTracker tracker)
	{
		if (tracker == null)
		{
			return;
		}

		foreach (var item in tracker.Added)
		{
			if (item is ISyncEntity syncEntity)
			{
				AddEntity(syncEntity);
			}
		}

		foreach (var item in tracker.Modified)
		{
			if (item is ISyncEntity syncEntity)
			{
				AddEntity(syncEntity);
			}
		}

		foreach (var item in tracker.Removed)
		{
			if (item is ISyncEntity syncEntity)
			{
				RemoveEntity(syncEntity);
			}
		}
	}

	/// <summary>
	/// Cache an entity ID for the entity Sync ID.
	/// </summary>
	/// <param name="cache"> The cache to add the keys to. </param>
	/// <param name="syncId"> The sync ID of the entity. </param>
	/// <param name="id"> The ID of the entity. </param>
	private void AddEntityId(MemoryCache<string, object> cache, object syncId, object id)
	{
		if (id == null)
		{
			return;
		}

		cache.AddOrUpdate(syncId.ToString(), id);
	}

	private MemoryCache<string, object> CreateCache(Type type)
	{
		return _cachedEntityId.GetOrAdd(type, _ => new MemoryCache<string, object>(_cacheTimeout));
	}

	#endregion
}