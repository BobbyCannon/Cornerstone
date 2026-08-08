#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Reflection;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Cornerstone.EntityFramework;

/// <summary>
/// Represents an Entity Framework Cornerstone database.
/// </summary>
[SourceReflection]
public abstract class EntityFrameworkSyncableDatabase : EntityFrameworkDatabase, ISyncableDatabase
{
	#region Fields

	private readonly ConcurrentDictionary<string, ISyncableRepository> _syncableRepositories;

	#endregion

	#region Constructors

	/// <summary>
	/// Default constructor needed for Add-MigrationStatus
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

	public DatabaseKeyCache KeyCache { get; }

	public abstract (string entity, string syncObject)[] SyncOrder { get; }

	#endregion

	#region Methods

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

		var rank = SyncOrder
			.Select((key, index) => new { key, index })
			.ToDictionary(x => x.key.entity, x => x.index);

		var response = _syncableRepositories
			.OrderBy(kvp => rank.TryGetValue(kvp.Key, out var r) ? r : int.MaxValue)
			.ThenBy(kvp => kvp.Key)
			.Select(kvp => kvp.Value)
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

	public ISyncableRepository GetSyncableRepository(Type syncEntityType)
	{
		var assemblyName = syncEntityType.ToAssemblyName();

		if (_syncableRepositories.TryGetValue(assemblyName, out var repository))
		{
			return repository;
		}

		var syncEntityTypeInfo = SourceReflector.GetRequiredSourceType(syncEntityType);
		var idType = syncEntityTypeInfo.GetProperties().First(x => x.Name == "Id").PropertyInfo.PropertyType;

		var thisTypeInfo = SourceReflector.GetRequiredSourceType(typeof(DbContext));
		var methods = thisTypeInfo.GetMethods();
		var setMethod = methods.First(x => (x.Name == "Set") && x.MethodInfo?.IsGenericMethodDefinition == true);
		var method = setMethod.MethodInfo.MakeGenericMethod(syncEntityType);
		var entitySet = method.Invoke(this, null);
		var repositoryType = typeof(EntityFrameworkSyncableRepository<,>).MakeGenericType(syncEntityType, idType);
		repository = Activator.CreateInstance(repositoryType, this, entitySet) as ISyncableRepository;

		_syncableRepositories.AddOrUpdate(syncEntityType.ToAssemblyName(), repository, (_, _) => repository);

		return repository;
	}

	/// <summary>
	/// Reads all repositories and puts all the syncable ones in an internal list.
	/// </summary>
	private void DetectSyncableRepositories()
	{
		var sourceType = SourceReflector.GetRequiredSourceType(GetType());
		var syncEntityType = typeof(ISyncEntity);
		var cachedProperties = sourceType.GetProperties();
		var properties = cachedProperties
			.Where(x =>
				(x.PropertyInfo.PropertyType.Name == typeof(IRepository<,>).Name)
				|| (x.PropertyInfo.PropertyType.Name == typeof(ISyncableRepository<,>).Name)
			)
			.ToList();

		_syncableRepositories.Clear();

		for (var i = 0; i < properties.Count; i++)
		{
			var property = properties[i];
			var genericType = property.PropertyInfo.PropertyType.GetGenericArguments().First();

			if (!syncEntityType.IsAssignableFrom(genericType))
			{
				continue;
			}

			GetSyncableRepository(genericType);
		}
	}

	#endregion
}