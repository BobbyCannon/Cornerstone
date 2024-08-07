﻿#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Exceptions;
using Cornerstone.Extensions;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Storage;

/// <summary>
/// Represents a collection of entities for a Cornerstone database.
/// </summary>
/// <typeparam name="T"> The type contained in the repository. </typeparam>
/// <typeparam name="T2"> The type of the entity key. </typeparam>
[Serializable]
internal class Repository<T, T2> : IDatabaseRepository, IRepository<T, T2> where T : Entity<T2>
{
	#region Fields

	internal SpeedyList<EntityState<T, T2>> Cache;
	private readonly CollectionChangeTracker _collectionChangeTracker;
	private T2 _currentKey;
	private IQueryable<T> _query;
	private readonly Type _type;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a repository for the provided database.
	/// </summary>
	/// <param name="database"> The database this repository is for. </param>
	public Repository(Database database)
	{
		_type = typeof(T);
		_currentKey = default;
		_collectionChangeTracker = new CollectionChangeTracker();

		Cache = [];
		Database = database;

		UpdateCacheQuery();
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public int Count => Cache.Count(x => x.State != EntityStateType.Added);

	/// <inheritdoc />
	public bool IsReadOnly => false;

	/// <summary>
	/// Will keep the repository items in cache for the life cycle of the repository.
	/// </summary>
	public bool NeverClearCache { get; set; }

	/// <summary>
	/// The database this repository is for.
	/// </summary>
	protected Database Database { get; }

	/// <inheritdoc />
	Type IQueryable.ElementType => _query.ElementType;

	/// <inheritdoc />
	Expression IQueryable.Expression => _query.Expression;

	/// <inheritdoc />
	IQueryProvider IQueryable.Provider => _query.Provider;

	#endregion

	#region Methods

	/// <inheritdoc />
	public void Add(T entity)
	{
		if (entity == null)
		{
			return;
		}

		if (Cache.Any(x => entity == x.Entity))
		{
			return;
		}

		var duplicateId = Cache.FirstOrDefault(x => !x.Entity.Id.Equals(default(T2)) && Equals(x.Entity.Id, entity.Id));
		if (duplicateId != null)
		{
			throw new InvalidOperationException($"The instance of entity type '{typeof(T).Name}' cannot be tracked because another instance with the same key value is already being tracked.");
		}

		Cache.Add(new EntityState<T, T2>(this, entity, CloneEntity(entity), EntityStateType.Added));
		OnUpdateEntityRelationships(entity);
	}

	/// <inheritdoc />
	public void AddOrUpdate(T entity)
	{
		if (!entity.IdIsSet())
		{
			Add(entity);
			return;
		}

		var foundItem = Cache.FirstOrDefault(x => Equals(x.Entity.Id, entity.Id));
		if (foundItem == null)
		{
			Cache.Add(new EntityState<T, T2>(this, entity, CloneEntity(entity), EntityStateType.Unmodified));
			OnUpdateEntityRelationships(entity);
			return;
		}

		foundItem.UpdateEntity(foundItem.Entity, entity);
	}

	/// <summary>
	/// Adds or updates an entity in the repository. The ID of the entity must be the default value to add and a value to
	/// update.
	/// </summary>
	/// <param name="entity"> The entity to be added. </param>
	public void AddOrUpdate(object entity)
	{
		if (entity is not T myEntity)
		{
			throw new ArgumentException("The entity is not the correct type.");
		}

		AddOrUpdate(myEntity);
	}

	/// <inheritdoc />
	public void AssignKey(IEntity entity, List<IEntity> processed)
	{
		if (processed?.Contains(entity) == true)
		{
			return;
		}

		if (entity is not Entity<T2> item)
		{
			throw new ArgumentException("Entity is not for this repository.");
		}

		if (!item.IdIsSet())
		{
			var id = item.NewId(ref _currentKey);
			if (!Equals(id, default(T2)))
			{
				item.Id = id;
			}
		}

		if (entity is not ISyncEntity syncableEntity)
		{
			return;
		}

		var maintainedEntity = Database.Options.UnmaintainedEntities.All(x => x != entity.GetType());
		var maintainSyncId = maintainedEntity && Database.Options.MaintainSyncId;

		if (maintainSyncId && (syncableEntity.SyncId == Guid.Empty))
		{
			syncableEntity.SyncId = Guid.NewGuid();
		}

		if ((syncableEntity.SyncId != Guid.Empty) && (Database is ISyncableDatabase syncableDatabase))
		{
			syncableDatabase.KeyCache?.AddEntityId(typeof(T), syncableEntity.SyncId, item.Id);
		}
	}

	/// <inheritdoc />
	public void AssignKeys(List<IEntity> processed)
	{
		foreach (var entityState in Cache)
		{
			AssignKey(entityState.Entity, processed);
		}
	}

	/// <inheritdoc />
	public void Clear()
	{
		throw new NotImplementedException();
	}

	/// <summary>
	/// Check to see if the repository contains this entity.
	/// </summary>
	/// <param name="entity"> The entity to test for. </param>
	/// <returns> True if the entity exist or false it otherwise. </returns>
	public bool Contains(T entity)
	{
		return Cache.Any(x => entity == x.Entity) || _query.Any(x => Equals(x.Id, entity.Id));
	}

	/// <inheritdoc />
	public void CopyTo(T[] array, int arrayIndex)
	{
		var items = _query.ToArray();
		Array.Copy(items, 0, array, arrayIndex, items.Length);
	}

	/// <inheritdoc />
	public int DiscardChanges()
	{
		var response = Cache.Count;
		ResetCache();
		return response;
	}

	/// <inheritdoc />
	public void Dispose()
	{
		ResetCache();
	}

	/// <inheritdoc />
	public ReadOnlySet<string> GetChangedProperties()
	{
		return ReadOnlySet<string>.Empty;
	}

	/// <inheritdoc />
	public void ApplyChangesTo(object destination)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc />
	public IEnumerator<T> GetEnumerator()
	{
		return _query.GetEnumerator();
	}

	/// <summary>
	/// Returns a raw queryable.
	/// </summary>
	/// <param name="filter"> </param>
	/// <returns> </returns>
	public IQueryable<T> GetRawQueryable(Func<T, bool> filter)
	{
		return Cache
			.Select(x => x.Entity)
			.Where(filter)
			.AsQueryable();
	}

	/// <inheritdoc />
	public bool HasChanges()
	{
		return HasChanges(IncludeExcludeOptions.Empty);
	}

	/// <inheritdoc />
	public bool HasChanges(IncludeExcludeOptions options)
	{
		return GetChanges().Any();
	}

	/// <inheritdoc />
	public bool HasDependentRelationship(object[] value, object id)
	{
		var foreignKeyFunction = (Func<T, object>) value[4];
		return this.Any(x => id.Equals(foreignKeyFunction.Invoke(x)));
	}

	public IIncludableQueryable<T, object> Include(Expression<Func<T, object>> include)
	{
		return new IncludableQueryable<T, object>(_query);
	}

	/// <inheritdoc />
	public IIncludableQueryable<T, T3> Include<T3>(Expression<Func<T, T3>> include)
	{
		return new IncludableQueryable<T, T3>(_query);
	}

	/// <inheritdoc />
	public IIncludableQueryable<T, object> Including(params Expression<Func<T, object>>[] includes)
	{
		return new IncludableQueryable<T, object>(_query);
	}

	public IIncludableQueryable<T, T3> Including<T3>(params Expression<Func<T, T3>>[] includes)
	{
		return new IncludableQueryable<T, T3>(_query);
	}

	/// <summary>
	/// Insert an entity to the repository before the provided entity. The ID of the entity must be the default value.
	/// </summary>
	/// <param name="entity"> The entity to be added. </param>
	/// <param name="targetEntity"> The entity to locate insert point. </param>
	public void InsertBefore(T entity, T targetEntity)
	{
		if (Cache.Any(x => entity == x.Entity))
		{
			return;
		}

		var state = Cache.FirstOrDefault(x => x.Entity == targetEntity);
		var indexOf = Cache.IndexOf(state);

		if (indexOf < 0)
		{
			throw new ArgumentException("Could not find the target entity", nameof(targetEntity));
		}

		Cache.Insert(indexOf, new EntityState<T, T2>(this, entity, CloneEntity(entity), EntityStateType.Added));
	}

	/// <inheritdoc />
	public object Read(object id)
	{
		return Read((T2) id);
	}

	/// <summary>
	/// Get entity by ID.
	/// </summary>
	/// <param name="id"> </param>
	/// <returns> The entity or null. </returns>
	public T Read(T2 id)
	{
		var state = Cache.FirstOrDefault(x => Equals(x.Entity.Id, id));
		return state?.Entity;
	}

	/// <inheritdoc />
	public bool Remove(T2 id)
	{
		var state = Cache.FirstOrDefault(x => Equals(x.Entity.Id, id));

		if (state == null)
		{
			var instance = Activator.CreateInstance<T>();
			instance.Id = id;
			state = new EntityState<T, T2>(this, instance, CloneEntity(instance), EntityStateType.Removed);
			Cache.Add(state);
		}

		state.State = EntityStateType.Removed;

		OnDeletingEntity(state.Entity);

		return true;
	}

	/// <inheritdoc />
	public bool Remove(T entity)
	{
		if (entity == null)
		{
			return false;
		}

		return Remove(entity.Id);
	}

	/// <inheritdoc />
	public int Remove(Expression<Func<T, bool>> filter)
	{
		return Cache
			.Select(x => x.Entity)
			.Where(filter.Compile())
			.Select(Remove)
			.Count(x => x);
	}

	/// <inheritdoc />
	public void RemoveDependent(object[] value, object id)
	{
		var foreignKeyFunction = (Func<T, object>) value[4];
		Remove(x => id.Equals(foreignKeyFunction.Invoke(x)));
	}

	/// <inheritdoc />
	public void ResetHasChanges()
	{
		// not supported for repositories
	}

	/// <inheritdoc />
	public int SaveChanges()
	{
		_collectionChangeTracker.Reset();

		var changeCount = GetChanges().Count();
		if (changeCount == 0)
		{
			return 0;
		}

		var now = TimeService.CurrentTime.UtcNow;

		foreach (var entry in Cache.ToList())
		{
			var entity = entry.Entity;
			var createdEntity = entity as ICreatedEntity;
			var modifiableEntity = entity as IModifiableEntity;
			var syncableEntity = entity as ISyncEntity;
			var maintainedEntity = Database.Options.UnmaintainedEntities.All(x => x != entry.Entity.GetType());
			var maintainCreatedOnDate = maintainedEntity && Database.Options.MaintainCreatedOn;
			var maintainModifiedOnDate = maintainedEntity && Database.Options.MaintainModifiedOn;
			var maintainSyncId = maintainedEntity && Database.Options.MaintainSyncId;

			switch (entry.State)
			{
				case EntityStateType.Added:
				{
					_collectionChangeTracker.AddAddedEntity(entity);

					if ((createdEntity != null) && maintainCreatedOnDate)
					{
						createdEntity.CreatedOn = now;
					}

					if ((modifiableEntity != null) && maintainModifiedOnDate)
					{
						modifiableEntity.ModifiedOn = now;
					}

					if ((syncableEntity != null) && maintainSyncId && (syncableEntity.SyncId == Guid.Empty))
					{
						syncableEntity.SyncId = Guid.NewGuid();
					}

					entity.EntityAdded(now);
					entity.EntityAddedDeletedOrModified(now);

					Database.EntityAdded(entity);
					break;
				}
				case EntityStateType.Modified:
				{
					if (!entity.CanBeModified())
					{
						entry.Reset();
						changeCount--;
						continue;
					}

					_collectionChangeTracker.AddModifiedEntity(entity);

					// If Cornerstone is maintaining the CreatedOn date then we will not allow modifications outside Cornerstone
					if ((createdEntity != null) && maintainCreatedOnDate)
					{
						if (entry.OldEntity is ICreatedEntity oldCreatedEntity
							&& (oldCreatedEntity.CreatedOn != createdEntity.CreatedOn))
						{
							// Do not allow created on to change for entities.
							createdEntity.CreatedOn = oldCreatedEntity.CreatedOn;
						}
					}

					// If Cornerstone is maintaining the ModifiedOn then we will set it to 'now'
					if ((modifiableEntity != null) && maintainModifiedOnDate)
					{
						// Update modified to now for new entities.
						modifiableEntity.ModifiedOn = now;
					}

					if ((syncableEntity != null) && maintainSyncId && entry.OldEntity is ISyncEntity oldSyncableEntity)
					{
						// Do not allow sync ID to change for entities.
						syncableEntity.SetEntitySyncId(oldSyncableEntity.GetEntitySyncId());
					}

					entity.EntityModified(now);
					entity.EntityAddedDeletedOrModified(now);

					Database.EntityModified(entity);
					break;
				}
				case EntityStateType.Removed:
				{
					if ((syncableEntity != null) && !Database.Options.PermanentSyncEntityDeletions)
					{
						syncableEntity.IsDeleted = true;
						syncableEntity.ModifiedOn = now;
						entry.State = EntityStateType.Modified;

						_collectionChangeTracker.AddModifiedEntity(entity);

						entity.EntityModified(now);
						entity.EntityAddedDeletedOrModified(now);

						Database.EntityModified(entity);
					}
					else
					{
						RemoveFromCache(entry);

						_collectionChangeTracker.AddRemovedEntity(entity);

						entity.EntityDeleted(now);
						entity.EntityAddedDeletedOrModified(now);

						Database.EntityDeleted(entity);
					}
					break;
				}
			}

			entry.SaveChanges();
		}

		OnSavedChanges(_collectionChangeTracker);

		return changeCount;
	}

	/// <inheritdoc />
	public void SetDependentToNull(object[] value, object id)
	{
		var entityExpression = (LambdaExpression) value[1];
		var foreignKeyExpression = (LambdaExpression) value[3];
		var foreignKeyFunction = (Func<T, object>) value[4];

		var values = this.Where(x => foreignKeyFunction.Invoke(x) == id);
		var entityName = entityExpression.GetExpressionName();
		var foreignKeyName = foreignKeyExpression.GetExpressionName();

		foreach (var v in values)
		{
			v.SetMemberValue(entityName, null);
			v.SetMemberValue(foreignKeyName, null);
		}
	}

	/// <inheritdoc />
	public void UpdateRelationships()
	{
		Cache.ToList().ForEach(x => OnUpdateEntityRelationships(x.Entity));
	}

	/// <inheritdoc />
	public void ValidateEntities()
	{
		Cache.Where(x => (x.State == EntityStateType.Added) || (x.State == EntityStateType.Modified))
			.ToList()
			.ForEach(x => OnValidateEntity(x.Entity));

		Cache.Where(x => x.State == EntityStateType.Added)
			.ToList()
			.ForEach(x => OnAddingEntity(x.Entity));

		Cache.Where(x => x.State == EntityStateType.Removed)
			.ToList()
			.ForEach(x => OnDeletingEntity(x.Entity));
	}

	/// <summary>
	/// Occurs when an entity is being deleted.
	/// </summary>
	/// <param name="obj"> The entity that was deleted. </param>
	protected virtual void OnAddingEntity(T obj)
	{
		var handler = AddingEntity;
		handler?.Invoke(obj);
	}

	/// <summary>
	/// Occurs when an entity is being deleted.
	/// </summary>
	/// <param name="obj"> The entity that was deleted. </param>
	protected virtual void OnDeletingEntity(T obj)
	{
		var handler = DeletingEntity;
		handler?.Invoke(obj);
	}

	/// <summary>
	/// Called when for when changes are saved. <see cref="SaveChanges" />
	/// </summary>
	protected virtual void OnSavedChanges(CollectionChangeTracker e)
	{
		SavedChanges?.Invoke(this, e);
	}

	/// <summary>
	/// Occurs when an entity relationships are updated.
	/// </summary>
	/// <param name="obj"> The entity that was updated. </param>
	protected virtual void OnUpdateEntityRelationships(T obj)
	{
		UpdateEntityRelationships?.Invoke(obj);
	}

	/// <summary>
	/// Occurs when an entity is validated.
	/// </summary>
	/// <param name="obj"> The entity that was validated. </param>
	protected virtual void OnValidateEntity(T obj)
	{
		var handler = ValidateEntity;
		handler?.Invoke(obj, this);
	}

	internal bool AnyNew(object entity, Func<T, bool> func)
	{
		return Cache.Any(x => !ReferenceEquals(x.Entity, entity) && func(x.OldEntity));
	}

	private T CloneEntity(T entity)
	{
		var constructorInfo = _type.GetConstructor(Type.EmptyTypes);
		if (constructorInfo == null)
		{
			throw new CornerstoneException("Failed to create new instance...");
		}

		var response = (T) constructorInfo.Invoke(null);
		var properties = EntityState.GetStateProperties(_type).ToList();

		foreach (var property in properties)
		{
			var value = property.GetValue(entity, null);
			property.SetValue(response, value, null);
		}

		var enumerableType = typeof(IEnumerable);
		var collectionRelationships = _type
			.GetCachedProperties()
			.Where(x => x.IsVirtual())
			.Where(x => enumerableType.IsAssignableFrom(x.PropertyType))
			.Where(x => x.PropertyType.IsGenericType)
			.ToList();

		foreach (var relationship in collectionRelationships)
		{
			var currentCollection = relationship.GetValue(entity, null);
			if (currentCollection == null)
			{
				continue;
			}

			var currentCollectionType = currentCollection.GetType();
			if (currentCollectionType.Name == typeof(RelationshipRepository<T, T2>).Name)
			{
				relationship.SetValue(response, currentCollection, null);
			}
		}

		return response;
	}

	private IEnumerable<EntityState<T, T2>> GetChanges()
	{
		// Make sure we are not missing anything...
		foreach (var item in Cache.Where(x => x.State == EntityStateType.Unmodified))
		{
			item.RefreshState();
		}

		return Cache
			.Where(x => x.State != EntityStateType.Unmodified)
			.ToList();
	}

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	private void RemoveFromCache(EntityState<T, T2> entityState)
	{
		entityState.ResetEvents();

		Cache?.Remove(entityState);

		var keyCache = (Database as ISyncableDatabase)?.KeyCache;
		if (keyCache == null)
		{
			return;
		}

		if (entityState.Entity is ISyncEntity syncEntity)
		{
			keyCache.RemoveEntityId(syncEntity.GetType(), syncEntity.GetEntitySyncId());
		}

		if (entityState.OldEntity is ISyncEntity oldSyncEntity)
		{
			keyCache.RemoveEntityId(oldSyncEntity.GetType(), oldSyncEntity.GetEntitySyncId());
		}
	}

	private void ResetCache()
	{
		Cache?
			.Where(x => x.State == EntityStateType.Added)
			.ToList()
			.ForEach(RemoveFromCache);

		Cache?
			.ToList()
			.ForEach(x => { x.Reset(); });
	}

	private void UpdateCacheQuery()
	{
		_query = Cache
			.Where(x => x.State != EntityStateType.Added)
			.Select(x => x.Entity)
			.AsQueryable();
	}

	#endregion

	#region Events

	/// <summary>
	/// Occurs when an entity is being added.
	/// </summary>
	internal event Action<T> AddingEntity;

	/// <summary>
	/// Occurs when an entity is being deleted.
	/// </summary>
	internal event Action<T> DeletingEntity;

	/// <summary>
	/// An event for when changes are saved. <see cref="SaveChanges" />
	/// </summary>
	internal event EventHandler<CollectionChangeTracker> SavedChanges;

	/// <summary>
	/// Occurs when an entity relationships are updated.
	/// </summary>
	internal event Action<T> UpdateEntityRelationships;

	/// <summary>
	/// Occurs when an entity is being validated.
	/// </summary>
	internal event Action<T, IRepository<T, T2>> ValidateEntity;

	#endregion
}