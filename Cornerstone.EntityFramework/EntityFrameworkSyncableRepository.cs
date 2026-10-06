#region References

using System;
using System.Collections.Generic;
using System.Linq;
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

	public Type RealType => typeof(T);

	public string TypeName => _entityName ??= RealType.ToAssemblyName();

	#endregion

	#region Methods

	public void Add(ISyncEntity entity)
	{
		base.Add((T) entity);
	}

	public void Discard(ISyncEntity entity)
	{
		if (entity is not T item)
		{
			return;
		}

		var entry = Database.Entry(item);
		switch (entry.State)
		{
			case EntityState.Modified:
			{
				entry.CurrentValues.SetValues(entry.OriginalValues);
				entry.State = EntityState.Unchanged;
				break;
			}
			case EntityState.Added:
			{
				entry.State = EntityState.Detached;
				break;
			}
			case EntityState.Deleted:
			{
				entry.State = EntityState.Unchanged;
				break;
			}
		}
	}

	public int GetChangeCount(DateTime since, DateTime until, SyncRepositoryFilter filter)
	{
		return GetChangesQuery(since, until, filter).Count();
	}

	public IEnumerable<ISyncEntity> GetChanges(DateTime since, DateTime until, int skip, int take, SyncRepositoryFilter filter)
	{
		var changes = GetChangesQuery(since, until, filter);
		IQueryable<T> query = filter is SyncRepositoryFilter<T> { OrderBys.Length: > 0 } typed
			? changes.Order(typed.OrderBys).ThenBy(x => x.ModifiedOn).ThenBy(x => x.Id)
			: changes.OrderBy(x => x.ModifiedOn).ThenBy(x => x.Id);
		if (skip > 0)
		{
			query = query.Skip(skip);
		}

		return query
			.Take(take)
			.ToList();
	}

	public ISyncEntity Read(Guid syncId)
	{
		return Set.FirstOrDefault(x => Equals(x.SyncId, syncId));
	}

	public ISyncEntity Read(ISyncEntity syncEntity, SyncRepositoryFilter filter)
	{
		if (syncEntity is not T entity)
		{
			throw new CornerstoneException(Babel.Tower[BabelKeys.SyncEntityIncorrectType]);
		}

		if (filter is SyncRepositoryFilter<T> { HasLookupFilter: true } srf)
		{
			var query = Set.Where(srf.LookupFilter.Invoke(entity));
			if (srf.ScopeFilter != null)
			{
				query = query.Where(srf.ScopeFilter);
			}

			return query.FirstOrDefault();
		}

		var syncId = syncEntity.SyncId;
		return Set.FirstOrDefault(x => Equals(x.SyncId, syncId));
	}

	public IDictionary<Guid, object> ReadAllKeys()
	{
		return Set.AsNoTracking()
			.ToDictionary(x => x.SyncId, x => (object) x.Id);
	}

	public ISyncEntity ReadByPrimaryId(T2 primaryId)
	{
		return Set.FirstOrDefault(x => x.Id.Equals(primaryId));
	}

	public ISyncEntity ReadByPrimaryId(object primaryId)
	{
		return ReadByPrimaryId((T2) primaryId);
	}

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
		if (filter is SyncRepositoryFilter<T> srf)
		{
			if (srf.ScopeFilter != null)
			{
				query = query.Where(srf.ScopeFilter);
			}

			if (srf.OutgoingFilter != null)
			{
				query = query.Where(srf.OutgoingFilter);
			}
		}

		// If we have never synced (since == DateTime.MinValue) and the filter says
		// skip deleted items on initial sync, omit soft-deleted rows. After the first
		// sync those tombstones still go out so clients can delete locally.
		if ((since == DateTime.MinValue) && (filter?.SkipDeletedItemsOnInitialSync == true))
		{
			query = query.Where(x => !x.IsDeleted);
		}

		return query;
	}

	#endregion
}