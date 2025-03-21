#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Exceptions;
using Cornerstone.Extensions;
using Cornerstone.Sync;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Cornerstone.EntityFramework;

/// <summary>
/// Represents a syncable repository.
/// </summary>
/// <typeparam name="T"> The entity for the repository. </typeparam>
/// <typeparam name="T2"> The type of the sync entity. </typeparam>
public class EntityFrameworkSyncableRepository<T, T2>
	: EntityFrameworkRepository<T, T2>, ISyncableRepository<T, T2>
	where T : SyncEntity<T2>
{
	#region Fields

	private string _entityName;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a repository.
	/// </summary>
	/// <param name="database"> The database where this repository resides. </param>
	/// <param name="set"> The database set this repository is for. </param>
	public EntityFrameworkSyncableRepository(EntityFrameworkDatabase database, DbSet<T> set) : base(database, set)
	{
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public Type RealType => typeof(T);

	/// <inheritdoc />
	public string TypeName => _entityName ??= RealType.ToAssemblyName();

	#endregion

	#region Methods

	/// <inheritdoc />
	public void Add(ISyncEntity entity)
	{
		base.Add((T) entity);
	}

	/// <inheritdoc />
	public int GetChangeCount(DateTime since, DateTime until, SyncRepositoryFilter filter)
	{
		return GetChangesQuery(since, until, filter).Count();
	}

	/// <inheritdoc />
	public IEnumerable<SyncObject> GetChanges(DateTime since, DateTime until, int skip, int take, SyncRepositoryFilter filter)
	{
		var query = GetChangesQuery(since, until, filter);

		if (skip > 0)
		{
			query = query.Skip(skip);
		}

		var entities = query.Take(take).ToList();
		var objects = entities
			.Select(x => x.ToSyncObject())
			.ToList();

		return objects;
	}

	/// <inheritdoc />
	public ISyncEntity Read(Guid syncId)
	{
		return Set.FirstOrDefault(x => Equals(x.SyncId, syncId));
	}

	/// <inheritdoc />
	public ISyncEntity Read(ISyncEntity syncEntity, SyncRepositoryFilter filter)
	{
		if (!(syncEntity is T entity))
		{
			throw new CornerstoneException(Babel.Tower[BabelKeys.SyncEntityIncorrectType]);
		}

		if (filter is SyncRepositoryFilter<T> { HasLookupFilter: true } srf)
		{
			return Set.FirstOrDefault(srf.LookupFilter.Invoke(entity));
		}

		var syncId = syncEntity.GetEntitySyncId();
		return Set.FirstOrDefault(x => Equals(x.SyncId, syncId));
	}

	/// <inheritdoc />
	public IDictionary<Guid, object> ReadAllKeys()
	{
		return Set.AsNoTracking()
			.ToDictionary(x => x.SyncId, x => (object) x.Id);
	}

	/// <inheritdoc />
	public ISyncEntity ReadByPrimaryId(T2 primaryId)
	{
		return Set.FirstOrDefault(x => x.Id.Equals(primaryId));
	}

	/// <inheritdoc />
	public ISyncEntity ReadByPrimaryId(object primaryId)
	{
		return ReadByPrimaryId((T2) primaryId);
	}

	/// <inheritdoc />
	public void Remove(ISyncEntity entity)
	{
		base.Remove((T) entity);
	}

	private IQueryable<T> GetChangesQuery(DateTime since, DateTime until, SyncRepositoryFilter filter)
	{
		var query = Set
			.AsNoTracking()
			.Where(x => ((x.CreatedOn >= since) && (x.CreatedOn < until))
				|| ((x.ModifiedOn >= since) && (x.ModifiedOn < until)));

		// Disable merge because merged expression is very hard to read
		// ReSharper disable once MergeSequentialPatterns
		if (filter is SyncRepositoryFilter<T> srf && (srf.OutgoingExpression != null))
		{
			query = query.Where(srf.OutgoingFilter);
		}

		// If we have never synced, meaning we are syncing from DateTime.MinValue, and
		// the repository has a filter that say we should skip deleted item on initial sync.
		// The "SyncEntity.IsDeleted" is a soft deleted flag that suggest an item is deleted
		// but it still exist in the database. If an item is "soft deleted" we will normally
		// still sync the item to allow the clients (non-server) to have the opportunity to
		// hard delete the item on their end.
		if ((since == DateTime.MinValue) && (filter?.SkipDeletedItemsOnInitialSync == true))
		{
			// We can skip soft deleted items that we will hard deleted on clients anyways
			query = query.Where(x => !x.IsDeleted);
		}

		return query
			.OrderBy(x => x.ModifiedOn)
			.ThenBy(x => x.Id)
			.AsQueryable();
	}

	#endregion
}