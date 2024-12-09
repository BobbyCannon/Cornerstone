#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Storage;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents a Cornerstone database.
/// </summary>
public abstract class SyncableDatabase : Database, ISyncableDatabase
{
	#region Fields

	private readonly ConcurrentDictionary<string, ISyncableRepository> _syncableRepositories;

	#endregion

	#region Constructors

	/// <inheritdoc />
	protected SyncableDatabase(DatabaseSettings settings, DatabaseKeyCache keyCache) : base(settings)
	{
		_syncableRepositories = new ConcurrentDictionary<string, ISyncableRepository>();

		KeyCache = keyCache;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public DatabaseKeyCache KeyCache { get; set; }

	public abstract string[] SyncOrder { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public IEnumerable<ISyncableRepository> GetSyncableRepositories(SyncSettings settings)
	{
		//
		// NOTE: If you change this then update Cornerstone.EntityFramework.EntityFrameworkDatabase
		//

		if (_syncableRepositories.Count <= 0)
		{
			// Refresh the syncable repositories
			DetectSyncableRepositories(settings);
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

	/// <inheritdoc />
	public ISyncableRepository<T, T2> GetSyncableRepository<T, T2>() where T : SyncEntity<T2>
	{
		return GetSyncableEntityRepository<T, T2>();
	}

	/// <inheritdoc />
	public ISyncableRepository GetSyncableRepository(Type syncEntityType)
	{
		var r = Repositories.FirstOrDefault(x => x.Key == syncEntityType.ToAssemblyName());
		if (r.Key == null)
		{
			return null;
		}

		return r.Value as ISyncableRepository;
	}

	private SyncableRepository<T, T2> CreateSyncableRepository<T, T2>() where T : SyncEntity<T2>
	{
		var repository = new SyncableRepository<T, T2>(this);
		repository.AddingEntity += RepositoryAddingEntity;
		repository.DeletingEntity += RepositoryDeletingEntity;
		repository.SavedChanges += RepositorySavedChanges;
		repository.UpdateEntityRelationships += RepositoryUpdateEntityRelationships;
		repository.ValidateEntity += RepositoryValidateEntity;
		return repository;
	}

	private void DetectSyncableRepositories(SyncSettings settings)
	{
		var syncableRepositories = Repositories
			.Where(x => x.Value is ISyncableRepository)
			.ToList();

		_syncableRepositories.Clear();

		var order = SyncOrder.ToList();
		var repositories = order.Count <= 0
			? syncableRepositories
				.Select(x => x.Value)
				.Cast<ISyncableRepository>()
				.Where(x => !settings.ShouldExcludeRepository(x.TypeName))
				.ToList()
			: syncableRepositories
				.Where(x => order.Contains(x.Key))
				.OrderBy(x => order.IndexOf(x.Key))
				.Select(x => x.Value as ISyncableRepository)
				.Where(x => x != null)
				.ToList();

		foreach (var i in repositories)
		{
			_syncableRepositories.TryAdd(i.TypeName, i);
		}
	}

	private SyncableRepository<T, T2> GetSyncableEntityRepository<T, T2>() where T : SyncEntity<T2>
	{
		var type = typeof(T);
		var key = type.ToAssemblyName();

		if (Repositories.TryGetValue(key, out var foundRepository))
		{
			return (SyncableRepository<T, T2>) foundRepository;
		}

		var repository = CreateSyncableRepository<T, T2>();
		Repositories.Add(key, repository);
		return repository;
	}

	#endregion
}

/// <summary>
/// The interfaces for a Cornerstone syncable database.
/// </summary>
public interface ISyncableDatabase : IDatabase
{
	#region Properties

	/// <summary>
	/// An optional key manager for caching entity IDs (primary and sync).
	/// </summary>
	DatabaseKeyCache KeyCache { get; }

	/// <summary>
	/// The order to sync entities.
	/// </summary>
	string[] SyncOrder { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Gets a list of syncable repositories.
	/// </summary>
	/// <returns> The list of syncable repositories. </returns>
	IEnumerable<ISyncableRepository> GetSyncableRepositories(SyncSettings settings);

	/// <summary>
	/// Gets a syncable repository of the requested entity.
	/// </summary>
	/// <typeparam name="T"> The type of the entity to get a repository for. </typeparam>
	/// <typeparam name="T2"> The type of the entity key. </typeparam>
	/// <returns> The repository of entities requested. </returns>
	ISyncableRepository<T, T2> GetSyncableRepository<T, T2>() where T : SyncEntity<T2>;

	/// <summary>
	/// Gets a syncable repository of the requested entity.
	/// </summary>
	/// <returns> The repository of entities requested. </returns>
	ISyncableRepository GetSyncableRepository(Type syncEntityType);

	#endregion
}