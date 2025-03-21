#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Runtime;
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
	protected SyncableDatabase(IDateTimeProvider dateTimeProvider, DatabaseSettings settings, DatabaseKeyCache keyCache) : base(dateTimeProvider, settings)
	{
		_syncableRepositories = new ConcurrentDictionary<string, ISyncableRepository>();

		KeyCache = keyCache;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public DatabaseKeyCache KeyCache { get; set; }

	/// <inheritdoc />
	public abstract string[] SyncOrder { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public IEnumerable<ISyncableRepository> GetSyncableRepositories()
	{
		//
		// NOTE: If you change this then update Cornerstone.EntityFramework.EntityFrameworkDatabase
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

	/// <inheritdoc />
	protected override void OnChangesSaved(CollectionChangeTracker e)
	{
		KeyCache?.UpdateCache(e);
		base.OnChangesSaved(e);
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

	private void DetectSyncableRepositories()
	{
		_syncableRepositories.Clear();

		var repositories = Repositories
			.Where(x => x.Value is ISyncableRepository)
			.Select(x => x.Value)
			.Cast<ISyncableRepository>()
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
	/// The order in which to sync.
	/// </summary>
	string[] SyncOrder { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Gets a list of syncable repositories.
	/// </summary>
	/// <returns> The list of syncable repositories. </returns>
	IEnumerable<ISyncableRepository> GetSyncableRepositories();

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
	ISyncableRepository GetSyncableRepository(Type type);

	#endregion
}