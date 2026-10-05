#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using Cornerstone.Extensions;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Storage.Sql;

/// <summary>
/// SQL-backed sync repository. Writes are deferred until <see cref="SavePending" />
/// (typically <see cref="SqlSyncableDatabase.SaveChanges" />).
/// </summary>
public class SqlSyncableRepository<T, TKey> : ISqlSyncableRepository
	where T : SyncEntity<TKey>, new()
{
	#region Fields

	private readonly SqlSyncableDatabase _database;
	private readonly SqlRepository<T> _inner;
	private readonly List<T> _pendingDeletes;
	private readonly HashSet<T> _pendingUpserts;
	private string _typeName;

	#endregion

	#region Constructors

	public SqlSyncableRepository(SqlSyncableDatabase database)
	{
		_database = database;
		_inner = database.GetRepository<T>();
		_pendingDeletes = [];
		_pendingUpserts = [];
		if (!_database.IsDatabaseMigrated())
		{
			_inner.EnsureTableCreated();
		}
	}

	#endregion

	#region Properties

	public Type RealType => typeof(T);

	public string TypeName => _typeName ??= RealType.ToAssemblyName();

	#endregion

	#region Methods

	public void Add(ISyncEntity entity)
	{
		var item = RequireEntity(entity);
		_pendingDeletes.Remove(item);
		TrackUpsert(item);
		Attach(item);
	}

	public void Discard(ISyncEntity entity)
	{
		if (entity is not T item)
		{
			return;
		}

		_pendingUpserts.Remove(item);
		_pendingDeletes.Remove(item);
	}

	public void DiscardPending()
	{
		_pendingDeletes.Clear();
		_pendingUpserts.Clear();
	}

	public int GetChangeCount(DateTime since, DateTime until, SyncRepositoryFilter filter)
	{
		return CreateChangesQuery(since, until, filter).Count();
	}

	public IEnumerable<ISyncEntity> GetChanges(DateTime since, DateTime until, int skip, int take, SyncRepositoryFilter filter)
	{
		var query = CreateChangesQuery(since, until, filter)
			.OrderBy(x => x.ModifiedOn)
			.ThenBy(x => x.Id);
		if (skip > 0)
		{
			query = query.Skip(skip);
		}

		query = query.Take(take);
		return query.Query().Cast<ISyncEntity>().ToList();
	}

	public ISyncEntity Read(Guid syncId)
	{
		return Attach(_inner.Where(x => x.SyncId == syncId).Query().FirstOrDefault());
	}

	public ISyncEntity Read(ISyncEntity syncEntity, SyncRepositoryFilter filter)
	{
		var entity = RequireEntity(syncEntity);
		if (filter is SyncRepositoryFilter<T> { HasLookupFilter: true } typedFilter)
		{
			var query = _inner.Where(typedFilter.LookupFilter.Invoke(entity));
			if (typedFilter.ScopeFilter != null)
			{
				query = query.Where(typedFilter.ScopeFilter);
			}

			return Attach(query.Query().FirstOrDefault());
		}

		return Read(syncEntity.SyncId);
	}

	public IDictionary<Guid, object> ReadAllKeys()
	{
		return _inner
			.Where(x => true)
			.Query()
			.ToDictionary(x => x.SyncId, x => (object) x.Id);
	}

	public ISyncEntity ReadByPrimaryId(object primaryId)
	{
		var id = (TKey) primaryId;
		var parameter = Expression.Parameter(typeof(T), "x");
		var idProperty = typeof(SyncEntity<TKey>).GetProperty(nameof(SyncEntity<TKey>.Id));
		var body = Expression.Equal(
			Expression.Property(parameter, idProperty),
			Expression.Constant(id, typeof(TKey))
		);
		var predicate = Expression.Lambda<Func<T, bool>>(body, parameter);
		return Attach(_inner.Where(predicate).Query().FirstOrDefault());
	}

	public void Remove(ISyncEntity entity)
	{
		var item = RequireEntity(entity);
		_pendingUpserts.Remove(item);
		if (!_pendingDeletes.Contains(item))
		{
			_pendingDeletes.Add(item);
		}
	}

	public int SavePending()
	{
		var written = 0;
		written += _inner.DeleteNow(_pendingDeletes);

		var upserts = new List<T>(_pendingUpserts.Count);
		var tombstones = new List<T>();
		foreach (var entity in _pendingUpserts)
		{
			if (_pendingDeletes.Contains(entity))
			{
				continue;
			}

			ApplyMaintenance(entity);
			if (entity.IsDeleted)
			{
				tombstones.Add(entity);
			}
			else
			{
				upserts.Add(entity);
			}
		}

		written += _inner.UpsertSync(tombstones, false);
		written += _inner.UpsertSync(upserts);

		var keyCache = _database.KeyCache;
		if (keyCache != null)
		{
			foreach (var entity in upserts.Concat(tombstones))
			{
				var id = entity.Id;
				if (!EqualityComparer<TKey>.Default.Equals(id, default))
				{
					keyCache.AddEntityId(typeof(T), entity.SyncId, id);
				}
			}
		}

		DiscardPending();
		return written;
	}

	private void ApplyMaintenance(T entity)
	{
		var settings = _database.DatabaseSettings;
		var now = _database.DateTimeProvider.UtcNow;
		var isNew = EqualityComparer<TKey>.Default.Equals(entity.Id, default);

		// DateTime.MinValue means CreatedOn was never set. Fill that even when
		// MaintainCreatedOn is off. Sync turns the flag off so a real sender time is kept.
		if (isNew && (entity.CreatedOn == default))
		{
			entity.CreatedOn = now;
		}

		if (settings.MaintainModifiedOn)
		{
			entity.ModifiedOn = now;
		}

		if (settings.MaintainSyncId && (entity.SyncId == Guid.Empty))
		{
			entity.SyncId = Guid.NewGuid();
		}
	}

	private T Attach(T entity)
	{
		if (entity == null)
		{
			return null;
		}

		if (entity is INotifyPropertyChanged notifier)
		{
			notifier.PropertyChanged -= OnEntityPropertyChanged;
			notifier.PropertyChanged += OnEntityPropertyChanged;
		}

		return entity;
	}

	private SqlQuery<T> CreateChangesQuery(DateTime since, DateTime until, SyncRepositoryFilter filter)
	{
		var query = new SqlQuery<T>(_database)
			.Where(x => ((x.CreatedOn >= since) && (x.CreatedOn < until))
				|| ((x.ModifiedOn >= since) && (x.ModifiedOn < until)));

		if (filter is SyncRepositoryFilter<T> typedFilter)
		{
			if (typedFilter.ScopeFilter != null)
			{
				query = query.Where(typedFilter.ScopeFilter);
			}

			if (typedFilter.OutgoingFilter != null)
			{
				query = query.Where(typedFilter.OutgoingFilter);
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

	private void OnEntityPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (sender is T item)
		{
			TrackUpsert(item);
		}
	}

	private static T RequireEntity(ISyncEntity entity)
	{
		if (entity is T item)
		{
			return item;
		}

		throw new ArgumentException("The sync entity is not the correct type.", nameof(entity));
	}

	private void TrackUpsert(T entity)
	{
		if (entity == null)
		{
			return;
		}

		_pendingUpserts.Add(entity);
	}

	#endregion
}

internal interface ISqlSyncableRepository : ISyncableRepository
{
	#region Methods

	void DiscardPending();

	int SavePending();

	#endregion
}