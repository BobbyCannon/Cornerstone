#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cornerstone.Extensions;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Cornerstone.EntityFramework;

/// <summary>
/// Represents an Entity Framework Cornerstone database.
/// </summary>
public abstract class EntityFrameworkSyncableDatabase : EntityFrameworkDatabase, ISyncableDatabase
{
	#region Fields

	private readonly ConcurrentDictionary<string, ISyncableRepository> _syncableRepositories;

	#endregion

	#region Constructors

	/// <summary>
	/// Default constructor needed for Add-Migration
	/// </summary>
	protected EntityFrameworkSyncableDatabase()
	{
	}

	/// <summary>
	/// Initializes an instance of the database.
	/// </summary>
	/// <param name="startup"> The startup options for this database. </param>
	/// <param name="settings"> The settings for this database. </param>
	/// <param name="keyCache"> An optional key manager for caching entity IDs (primary and sync). </param>
	protected EntityFrameworkSyncableDatabase(DbContextOptions startup, DatabaseSettings settings, DatabaseKeyCache keyCache)
		: base(startup, settings)
	{
		_syncableRepositories = new ConcurrentDictionary<string, ISyncableRepository>();

		KeyCache = keyCache;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public DatabaseKeyCache KeyCache { get; }

	/// <inheritdoc />
	public abstract string[] SyncOrder { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public IEnumerable<ISyncableRepository> GetSyncableRepositories()
	{
		//
		// NOTE: If you change this then update Cornerstone.SyncableDatabase
		//

		if (_syncableRepositories.Count <= 0)
		{
			// Refresh the syncable repositories
			DetectSyncableRepositories();
		}

		if (SyncOrder.Length <= 0)
		{
			return _syncableRepositories
				.Values
				.OrderBy(x => x.TypeName)
				.ToList();
		}

		var order = SyncOrder.Reverse().ToList();
		var ordered = _syncableRepositories
			.OrderBy(x => x.Key == order[0]);

		var response = order
			.Skip(1)
			.Aggregate(ordered, (current, key) => current.ThenBy(x => x.Key == key))
			.Select(x => x.Value)
			.ToList();

		return response;
	}

	/// <summary>
	/// Gets a syncable repository of the requested entity.
	/// </summary>
	/// <returns> The repository for the sync entity. </returns>
	public ISyncableRepository<T, T2> GetSyncableRepository<T, T2>() where T : SyncEntity<T2>
	{
		return new EntityFrameworkSyncableRepository<T, T2>(this, Set<T>());
	}

	/// <inheritdoc />
	public ISyncableRepository GetSyncableRepository(Type syncEntityType)
	{
		var assemblyName = syncEntityType.ToAssemblyName();

		if (_syncableRepositories.TryGetValue(assemblyName, out var repository))
		{
			return repository;
		}

		var idType = syncEntityType.GetCachedProperties(BindingFlags.Public | BindingFlags.Instance).First(x => x.Name == "Id").PropertyType;
		var methods = GetType().GetCachedMethods(BindingFlags.Public | BindingFlags.Instance);
		var setMethod = methods.First(x => (x.Name == "Set") && x.IsGenericMethodDefinition);
		var method = setMethod.MakeGenericMethod(syncEntityType);
		var entitySet = method.Invoke(this, null);
		var repositoryType = typeof(EntityFrameworkSyncableRepository<,>).MakeGenericType(syncEntityType, idType);
		repository = repositoryType.CreateInstance(this, entitySet) as ISyncableRepository;

		_syncableRepositories.AddOrUpdate(syncEntityType.ToAssemblyName(), repository, (_, _) => repository);

		return repository;
	}

	/// <inheritdoc />
	protected override void OnChangesSaved(CollectionChangeTracker e)
	{
		KeyCache?.UpdateCache(e);
		base.OnChangesSaved(e);
	}

	/// <summary>
	/// Reads all repositories and puts all the syncable ones in an internal list.
	/// </summary>
	private void DetectSyncableRepositories()
	{
		var type = GetType();
		var syncEntityType = typeof(ISyncEntity);
		var cachedProperties = type.GetCachedProperties();
		var properties = cachedProperties
			.Where(x =>
				(x.PropertyType.Name == typeof(IRepository<,>).Name)
				|| (x.PropertyType.Name == typeof(ISyncableRepository<,>).Name)
			)
			.ToList();

		_syncableRepositories.Clear();

		for (var i = 0; i < properties.Count; i++)
		{
			var property = properties[i];
			var genericType = property.PropertyType.GetCachedGenericArguments().First();
			
			if (!syncEntityType.IsAssignableFrom(genericType))
			{
				continue;
			}

			GetSyncableRepository(genericType);
		}
	}

	#endregion
}