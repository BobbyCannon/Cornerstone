﻿#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Exceptions;
using Cornerstone.Extensions;
using Cornerstone.Storage;

#endregion

namespace Cornerstone.Sync;

/// <summary>
/// Represents a collection of entities for a Cornerstone database.
/// </summary>
/// <typeparam name="T"> The type contained in the repository. </typeparam>
/// <typeparam name="T2"> The type of the entity key. </typeparam>
[Serializable]
internal class SyncableRepository<T, T2> : Repository<T, T2>, ISyncableRepository<T, T2> where T : SyncEntity<T2>
{
	#region Constructors

	/// <summary>
	/// Initializes a syncable repository for the provided database.
	/// </summary>
	/// <param name="database"> The database this repository is for. </param>
	public SyncableRepository(Database database) : base(database)
	{
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public Type RealType => typeof(T);

	/// <inheritdoc />
	public string TypeName => RealType.ToAssemblyName();

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
		var query = GetChangesQuery(since, until, filter)
			.OrderBy(x => x.ModifiedOn)
			.ThenBy(x => x.Id)
			.AsQueryable();

		if (skip > 0)
		{
			query = query.Skip(skip);
		}

		var entities = query.Take(take).ToList();
		var objects = entities
			.Select(x => x.ToSyncObject())
			.Where(x => !x.Equals(SyncObjectExtensions.Empty))
			.ToList();

		return objects;
	}

	/// <inheritdoc />
	public IDictionary<Guid, object> ReadAllKeys()
	{
		return this.ToDictionary(x => x.SyncId, x => (object) x.Id);
	}

	/// <inheritdoc />
	public ISyncEntity ReadByPrimaryId(object primaryId)
	{
		return (ISyncEntity) Read(primaryId);
	}

	/// <inheritdoc />
	public ISyncEntity ReadByPrimaryId(T2 primaryId)
	{
		return Read(primaryId);
	}

	/// <inheritdoc />
	public void Remove(ISyncEntity entity)
	{
		base.Remove((T) entity);
	}

	private IQueryable<T> GetChangesQuery(DateTime since, DateTime until, SyncRepositoryFilter filter)
	{
		var query = this.Where(x => ((x.CreatedOn >= since) && (x.CreatedOn < until))
			|| ((x.ModifiedOn >= since) && (x.ModifiedOn < until)));

		// Disable merge because merged expression is very hard to read
		// ReSharper disable once MergeSequentialPatterns
		if (filter is SyncRepositoryFilter<T> srf && (srf.OutgoingExpression != null))
		{
			query = query.Where(srf.OutgoingFilter);
		}

		// If we have never synced, meaning we are syncing from DateTime.MinValue, and
		// the repository has a filter that say we should skip deleted item on initial sync.
		// The "SyncEntity.IsDeleted" is a soft-deleted flag that suggest an item is deleted,
		// but it still exists in the database. If an item is "soft deleted" we will normally
		// still sync the item to allow the clients (non-server) to have the opportunity to
		// hard delete the item on their end.
		if ((since == DateTime.MinValue) && (filter?.SkipDeletedItemsOnInitialSync == true))
		{
			// We can skip soft deleted items that will be hard deleted on clients anyway
			query = query.Where(x => !x.IsDeleted);
		}

		return query
			.OrderBy(x => x.ModifiedOn)
			.ThenBy(x => x.Id)
			.AsQueryable();
	}

	/// <inheritdoc />
	ISyncEntity ISyncableRepository.Read(Guid syncId)
	{
		return Read(syncId);
	}

	/// <inheritdoc />
	ISyncEntity ISyncableRepository.Read(ISyncEntity syncEntity, SyncRepositoryFilter filter)
	{
		if (syncEntity is not T entity)
		{
			throw new CornerstoneException("The sync entity is not the correct type.");
		}

		if (filter is SyncRepositoryFilter<T> { HasLookupFilter: true } srf)
		{
			return FirstOrDefault(srf.LookupFilter.Invoke(entity));
		}

		var syncId = syncEntity.GetEntitySyncId();
		return Read(syncId);
	}

	#endregion
}