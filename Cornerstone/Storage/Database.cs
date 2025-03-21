﻿#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Cornerstone.Extensions;
using Cornerstone.Runtime;
using Cornerstone.Storage.Configuration;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Storage;

/// <summary>
/// Represents a Cornerstone database.
/// </summary>
public abstract class Database : IDatabase
{
	#region Fields

	private readonly CollectionChangeTracker _collectionChangeTracker;
	private Assembly _mappingAssembly;
	private int _saveChangeCount;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes an instance of the database class.
	/// </summary>
	/// <param name="dateTimeProvider"> The provider for date and time. </param>
	/// <param name="settings"> The options for this database. </param>
	protected Database(IDateTimeProvider dateTimeProvider, DatabaseSettings settings)
	{
		_collectionChangeTracker = new CollectionChangeTracker();

		DateTimeProvider = dateTimeProvider;
		IndexConfigurations = new Dictionary<string, IndexConfiguration>();
		OneToManyRelationships = new Dictionary<string, object[]>();
		DatabaseSettings = settings?.DeepClone() ?? new DatabaseSettings();
		EntityIndexConfigurations = new Dictionary<Type, List<IndexConfiguration>>();
		EntityPropertyConfigurations = new Dictionary<Type, List<IPropertyConfiguration>>();
		PropertyConfigurations = new Dictionary<string, IPropertyConfiguration>();
		Repositories = new Dictionary<string, IDatabaseRepository>();
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public DatabaseSettings DatabaseSettings { get; }

	/// <inheritdoc />
	public IDateTimeProvider DateTimeProvider { get; private set; }

	/// <inheritdoc />
	public bool IsDisposed { get; private set; }

	internal bool HasBeenConfiguredViaMapping { get; set; }

	internal Dictionary<string, IDatabaseRepository> Repositories { get; }

	private Dictionary<Type, List<IndexConfiguration>> EntityIndexConfigurations { get; }

	private IDictionary<Type, List<IPropertyConfiguration>> EntityPropertyConfigurations { get; }

	private IDictionary<string, IndexConfiguration> IndexConfigurations { get; }

	private Dictionary<string, object[]> OneToManyRelationships { get; }

	private IDictionary<string, IPropertyConfiguration> PropertyConfigurations { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Discard all changes made in this context to the underlying database.
	/// </summary>
	public int DiscardChanges()
	{
		return Repositories.Values.Sum(x => x.DiscardChanges());
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		OnDisposed();
		GC.SuppressFinalize(this);
		IsDisposed = true;
	}

	/// <inheritdoc />
	public DatabaseType GetDatabaseType()
	{
		return DatabaseType.Memory;
	}

	/// <summary>
	/// Gets the assembly that contains the entity mappings. Base implementation defaults to the implemented types assembly.
	/// </summary>
	public virtual Assembly GetMappingAssembly()
	{
		return _mappingAssembly ??= GetType().Assembly;
	}

	/// <summary>
	/// Gets a read only repository for the provided type.
	/// </summary>
	/// <typeparam name="T"> The type of the item in the repository. </typeparam>
	/// <typeparam name="T2"> The type of the entity key. </typeparam>
	/// <returns> The repository for the type. </returns>
	public IRepository<T, T2> GetReadOnlyRepository<T, T2>() where T : Entity<T2>
	{
		return GetRepository<T, T2>();
	}

	/// <summary>
	/// Gets a repository for the provided type.
	/// </summary>
	/// <typeparam name="T"> The type of the item in the repository. </typeparam>
	/// <returns> The repository for the type. </returns>
	public IRepository<T> GetRepository<T>() where T : Entity
	{
		var entityType = typeof(T);
		var entityKey = entityType.BaseType?.GetGenericArguments().FirstOrDefault();
		var genericMethod = typeof(Database)
			.GetCachedGenericMethod(nameof(GetRepository), [entityType, entityKey],
				[], ReflectionExtensions.DefaultPublicFlags
			);
		return (IRepository<T>)genericMethod.Invoke(this, []);
	}

	/// <summary>
	/// Gets a repository for the provided type.
	/// </summary>
	/// <typeparam name="T"> The type of the item in the repository. </typeparam>
	/// <typeparam name="T2"> The type of the entity key. </typeparam>
	/// <returns> The repository for the type. </returns>
	public IRepository<T, T2> GetRepository<T, T2>() where T : Entity<T2>
	{
		var type = typeof(T);
		var key = type.ToAssemblyName();

		if (Repositories.TryGetValue(key, out var foundRepository))
		{
			return (Repository<T, T2>) foundRepository;
		}

		var repository = CreateRepository<T, T2>();
		Repositories.Add(key, repository);
		return repository;
	}

	/// <summary>
	/// Create a configuration that represents an Index.
	/// </summary>
	/// <param name="entityType"> The type of the entity. </param>
	/// <param name="name"> The name of the index. </param>
	/// <returns> The index configuration. </returns>
	public IndexConfiguration HasIndex(Type entityType, string name)
	{
		var indexName = $"{name}";

		if (IndexConfigurations.TryGetValue(indexName, out var index))
		{
			return index;
		}

		var response = new IndexConfiguration(indexName);
		IndexConfigurations.Add(indexName, response);

		List<IndexConfiguration> entityConfigurations;

		if (EntityIndexConfigurations.TryGetValue(entityType, out var configuration))
		{
			entityConfigurations = configuration;
		}
		else
		{
			entityConfigurations = [];
			EntityIndexConfigurations.Add(entityType, entityConfigurations);
		}

		entityConfigurations.Add(response);

		return response;
	}

	/// <summary>
	/// Creates a configuration that represent a required one-to-many relationship.
	/// </summary>
	/// <param name="required"> The value to determine if this property is required. </param>
	/// <param name="entity"> The entity to relate to. </param>
	/// <param name="collectionKey"> The collection on the entity that relates back to this entity. </param>
	/// <param name="foreignKey"> The ID for the entity to relate to. </param>
	/// <typeparam name="T1"> The entity that host the relationship. </typeparam>
	/// <typeparam name="T2"> The type of the entity key of the host. </typeparam>
	/// <typeparam name="T3"> The entity to build a relationship to. </typeparam>
	/// <typeparam name="T4"> The type of the entity key to build the relationship to. </typeparam>
	public PropertyConfiguration<T1, T2> HasRequired<T1, T2, T3, T4>(
		bool required,
		Expression<Func<T1, T3>> entity,
		Expression<Func<T1, object>> foreignKey,
		Expression<Func<T3, ICollection<T1>>> collectionKey = null)
		where T1 : Entity<T2>
		where T3 : Entity<T4>
	{
		var property = Property<T1, T2>(foreignKey).IsRequired(required);

		if (collectionKey != null)
		{
			var key = $"{typeof(T3).Name}-{collectionKey.GetExpressionName()}";
			var repositoryFactory = GetRepository<T1, T2>();
			OneToManyRelationships.AddIfMissing(key,
				() => [repositoryFactory, entity, entity.Compile(), foreignKey, foreignKey.Compile(), property]
			);
		}

		return property;
	}

	/// <inheritdoc />
	public void Migrate()
	{
	}

	/// <summary>
	/// Creates a configuration for an entity property.
	/// </summary>
	/// <param name="expression"> The expression for the property. </param>
	/// <typeparam name="T"> The entity for the configuration. </typeparam>
	/// <typeparam name="T2"> The type of the entity key. </typeparam>
	/// <returns> The configuration for the entity property. </returns>
	public PropertyConfiguration<T, T2> Property<T, T2>(Expression<Func<T, object>> expression) where T : Entity<T2>
	{
		var typeofT = typeof(T);
		var name = $"{typeofT.ToAssemblyName()}.{expression.GetExpressionName()}";

		if (PropertyConfigurations.TryGetValue(name, out var configuration))
		{
			return (PropertyConfiguration<T, T2>) configuration;
		}

		var response = new PropertyConfiguration<T, T2>(expression);
		PropertyConfigurations.AddIfMissing(name, () => response);

		List<IPropertyConfiguration> entityConfigurations;

		if (EntityPropertyConfigurations.TryGetValue(typeofT, out var propertyConfiguration))
		{
			entityConfigurations = propertyConfiguration;
		}
		else
		{
			entityConfigurations = [];
			EntityPropertyConfigurations.Add(typeofT, entityConfigurations);
		}

		entityConfigurations.Add(response);

		return response;
	}

	/// <inheritdoc />
	public T Remove<T, T2>(T item) where T : Entity<T2>
	{
		GetRepository<T, T2>().Remove(item);
		return item;
	}

	/// <summary>
	/// Save the data to the data store.
	/// </summary>
	/// <returns> The number of items saved. </returns>
	public virtual int SaveChanges()
	{
		if (_saveChangeCount++ > 2)
		{
			throw new OverflowException("Database save changes stuck in a processing loop.");
		}

		try
		{
			var first = _saveChangeCount == 1;
			if (first)
			{
				_collectionChangeTracker.Reset();
			}

			Repositories.Values.ForEach(x => x.UpdateRelationships());
			Repositories.Values.ForEach(x => x.AssignKeys());
			Repositories.Values.ForEach(x => x.ValidateEntities());
			Repositories.Values.ForEach(x => x.UpdateRelationships());

			var response = Repositories.Values.Sum(x => x.SaveChanges());

			if (Repositories.Any(x => x.Value.HasChanges()))
			{
				response += SaveChanges();
			}

			if (first)
			{
				OnChangesSaved(_collectionChangeTracker);
			}

			// It's possible that values were added during OnSavedChanges.
			if (Repositories.Any(x => x.Value.HasChanges()))
			{
				// Consider this loop done?
				_saveChangeCount = 0;
				response += SaveChanges();
			}

			return response;
		}
		finally
		{
			_saveChangeCount = 0;
		}
	}

	/// <summary>
	/// Save the data to the data store.
	/// </summary>
	/// <returns> The number of items saved. </returns>
	public virtual Task<int> SaveChangesAsync()
	{
		return Task.Run(SaveChanges);
	}

	/// <inheritdoc />
	public void UpdateDateTimeProvider(IDateTimeProvider dateTimeProvider)
	{
		DateTimeProvider = dateTimeProvider;
	}

	/// <summary>
	/// Called when an entity is added. Note: this is before saving.
	/// See <see cref="ChangesSaved" /> for after save state.
	/// </summary>
	/// <param name="entity"> The entity added. </param>
	protected internal virtual void EntityAdded(IEntity entity)
	{
	}

	/// <summary>
	/// Called when an entity is deleted. Note: this is before saving.
	/// See <see cref="ChangesSaved" /> for after save state.
	/// </summary>
	/// <param name="entity"> The entity deleted. </param>
	protected internal virtual void EntityDeleted(IEntity entity)
	{
	}

	/// <summary>
	/// Called when an entity is modified. Note: this is before saving.
	/// See <see cref="ChangesSaved" /> for after save state.
	/// </summary>
	/// <param name="entity"> The entity modified. </param>
	protected internal virtual void EntityModified(IEntity entity)
	{
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> Should be true if managed resources should be disposed. </param>
	protected virtual void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}

		Repositories.Values.ForEach(repository => repository.Dispose());
	}

	/// <summary>
	/// Called when for when changes are saved. <see cref="ChangesSaved" />
	/// </summary>
	protected virtual void OnChangesSaved(CollectionChangeTracker e)
	{
		ChangesSaved?.Invoke(this, e);
	}

	/// <summary>
	/// An invocator for the event when the database has been disposed.
	/// </summary>
	protected virtual void OnDisposed()
	{
		Disposed?.Invoke(this, EventArgs.Empty);
	}

	internal void UpdateDependentIds(IEntity entity, List<IEntity> processed)
	{
		if (processed.Contains(entity))
		{
			return;
		}

		var entityType = entity.GetType();
		var properties = entityType.GetCachedProperties();

		processed.Add(entity);

		UpdateDependentIds(entity, properties, processed);
		UpdateDependentCollectionIds(entity, properties, processed);
	}

	private static void AssignNewValue<T1, T2>(T1 obj, Expression<Func<T1, T2>> expression, T2 value)
	{
		var valueParameterExpression = Expression.Parameter(typeof(T2));
		var targetExpression = (expression.Body as UnaryExpression)?.Operand ?? expression.Body;

		var assign = Expression.Lambda<Action<T1, T2>>
		(
			Expression.Assign(targetExpression, Expression.Convert(valueParameterExpression, targetExpression.Type)),
			expression.Parameters.Single(), valueParameterExpression
		);

		assign.Compile().Invoke(obj, value);
	}

	private IRelationshipRepository BuildRelationship(Type entityType, Type collectionType, IEntity entity, IEnumerable collection, string key)
	{
		var collectionTypeProperties = collectionType.GetCachedProperties();
		var collectionKey = collectionTypeProperties.First(x => x.Name == "Id").PropertyType;
		var entityProperties = entityType.GetCachedProperties();
		var entityKey = entityProperties.First(x => x.Name == "Id").PropertyType;
		var genericMethod = typeof(Database)
			.GetCachedGenericMethod(nameof(BuildRelationship),
				[entityType, entityKey, collectionType, collectionKey],
				[entityType, typeof(IEnumerable), typeof(string)],
				ReflectionExtensions.DefaultPrivateFlags
			);
		return (IRelationshipRepository) genericMethod.Invoke(this, [entity, collection, key]);
	}

	/// <summary>
	/// Builds relationship repository for the entity provided.
	/// </summary>
	/// <typeparam name="T1"> The type of the entity with the relationship. </typeparam>
	/// <typeparam name="T1K"> The type of the key for the entity. </typeparam>
	/// <typeparam name="T2"> The type of the related collection. </typeparam>
	/// <typeparam name="T2K"> The type of the key for the collection. </typeparam>
	/// <param name="entity"> The entity to process. </param>
	/// <param name="collection"> The entities to add or update to the repository. </param>
	/// <param name="key"> The key of the relationship </param>
	/// <returns> The repository for the relationship. </returns>
	// ReSharper disable once UnusedMember.Local
	private RelationshipRepository<T2, T2K> BuildRelationship<T1, T1K, T2, T2K>(T1 entity, IEnumerable collection, string key)
		where T1 : Entity<T1K>
		where T2 : Entity<T2K>
	{
		if (!OneToManyRelationships.ContainsKey(key) || (entity == null))
		{
			return null;
		}

		var value = OneToManyRelationships[key];
		var repository = (IRepository<T2, T2K>) value[0];
		var entityExpression = (Expression<Func<T2, T1>>) value[1];
		var entityFunction = (Func<T2, T1>) value[2];
		var foreignKeyExpression = (Expression<Func<T2, object>>) value[3];
		var foreignKeyFunction = (Func<T2, object>) value[4];

		var response = new RelationshipRepository<T2, T2K>(key, (Repository<T2, T2K>) repository, x =>
		{
			var invokedKey = foreignKeyFunction.Invoke(x);
			if (!Equals(invokedKey, default(T2K)) && (invokedKey?.Equals(entity.Id) == true))
			{
				return true;
			}

			var invokedEntity = entityFunction.Invoke(x);
			return invokedEntity == entity;
		}, x =>
		{
			if (entity.IdIsSet())
			{
				// Should I use SetMemberValue instead? which is faster?
				AssignNewValue(x, foreignKeyExpression, entity.Id);
			}
			else
			{
				// Should I use SetMemberValue instead? which is faster?
				AssignNewValue(x, entityExpression, entity);
			}
		}, x =>
		{
			var invokedEntity = entityFunction.Invoke(x);
			if (invokedEntity == null)
			{
				return;
			}

			var invokedKey = foreignKeyFunction.Invoke(x);
			if ((invokedKey != null) && !invokedKey.Equals(invokedEntity.Id))
			{
				invokedEntity.Id = (T1K) invokedKey;
			}
		});

		collection.ForEach(x => response.AddOrUpdate((T2) x));
		return response;
	}

	private IDatabaseRepository CreateRepository(Type entityType, Type entityKey)
	{
		var genericMethod = typeof(Database)
			.GetCachedGenericMethod(nameof(CreateRepository), [entityType, entityKey],
				[], ReflectionExtensions.DefaultPrivateFlags
			);
		return (IDatabaseRepository) genericMethod.Invoke(this, []);
	}

	private Repository<T, T2> CreateRepository<T, T2>() where T : Entity<T2>
	{
		var repository = new Repository<T, T2>(this);
		repository.AddingEntity += RepositoryAddingEntity;
		repository.DeletingEntity += RepositoryDeletingEntity;
		repository.SavedChanges += RepositorySavedChanges;
		repository.UpdateEntityRelationships += RepositoryUpdateEntityRelationships;
		repository.ValidateEntity += RepositoryValidateEntity;
		return repository;
	}

	private void UpdateDependentCollectionIds(IEntity entity, ICollection<PropertyInfo> properties, List<IEntity> processed)
	{
		var enumerableType = typeof(IEnumerable);
		var collectionRelationships = properties
			.Where(x => x.IsVirtual())
			.Where(x => enumerableType.IsAssignableFrom(x.PropertyType))
			.Where(x => x.PropertyType.IsGenericType)
			.ToList();

		foreach (var relationship in collectionRelationships)
		{
			if (relationship.GetValue(entity, null) is not IEnumerable<IEntity> currentCollection)
			{
				continue;
			}

			var collectionType = relationship.PropertyType.GetGenericArguments()[0];
			if (!Repositories.ContainsKey(collectionType.ToAssemblyName()))
			{
				continue;
			}

			var repository = Repositories[collectionType.ToAssemblyName()];
			foreach (var item in currentCollection)
			{
				repository.AssignKey(item, processed);
			}
		}
	}

	private void UpdateDependentIds(IEntity entity, ICollection<PropertyInfo> properties, List<IEntity> processed)
	{
		var entityRelationships = properties
			.Where(x => x.IsVirtual())
			.ToList();

		foreach (var entityRelationship in entityRelationships)
		{
			if (entityRelationship.GetValue(entity, null) is not IEntity expectedEntity)
			{
				continue;
			}

			if (processed.Contains(expectedEntity))
			{
				continue;
			}

			var collectionType = expectedEntity.GetType();
			if (!Repositories.ContainsKey(collectionType.ToAssemblyName()))
			{
				continue;
			}

			var repository = Repositories[collectionType.ToAssemblyName()];
			repository.AssignKey(expectedEntity, processed);
		}
	}

	private void UpdateEntityChildRelationships(IEntity item, IEntity entity)
	{
		var itemType = item.GetType();
		var itemProperties = itemType.GetCachedProperties();
		var entityType = entity.GetType();
		var entityProperties = entityType.GetCachedProperties();

		var entityRelationship = itemProperties.FirstOrDefault(x => x.Name == entityType.Name);
		entityRelationship?.SetValue(item, entity, null);

		var entityRelationshipId = itemProperties.FirstOrDefault(x => x.Name == (entityType.Name + "Id"));
		var entityKeyId = entityProperties.FirstOrDefault(x => x.Name == nameof(Entity<int>.Id));
		entityRelationshipId?.SetValue(item, entityKeyId?.GetValue(entity), null);
	}

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private void UpdateEntityCollectionRelationships<T, T2>(EntityState<T, T2> entityState)
		where T : Entity<T2>
	{
		var enumerableType = typeof(IEnumerable);
		var entityType = typeof(T);
		var entity = entityState.Entity;
		var collectionRelationships = entityType
			.GetCachedVirtualProperties()
			.Where(x => enumerableType.IsAssignableFrom(x.PropertyType))
			.Where(x => x.PropertyType.IsGenericType)
			.ToList();

		foreach (var relationship in collectionRelationships)
		{
			// Check to see if we have a repository for the generic type.
			var collectionType = relationship.PropertyType.GetCachedGenericArguments()[0];
			if (!Repositories.ContainsKey(collectionType.ToAssemblyName()))
			{
				continue;
			}

			// Converts the relationship to a relationship (filtered) repository.
			var currentCollection = (IEnumerable<IEntity>) relationship.GetValue(entity, null);

			if (currentCollection is IRelationshipRepository or null)
			{
				// We are already a relationship repository so just update the relationships
				continue;
			}

			// See if the entity has a relationship filter.
			var key1 = $"{entityType.Name}-{collectionType.Name}";
			var key2 = $"{entityType.Name}-{relationship.Name}";

			var relationshipFilter = BuildRelationship(entityType, collectionType, entity, currentCollection, key1)
				?? BuildRelationship(entityType, collectionType, entity, currentCollection, key2);

			// Check to see if the custom memory context has a filter method.
			if (relationshipFilter == null)
			{
				// No filter so there's nothing to do.
				continue;
			}

			// Update relationship collection to the new filtered collection.
			relationship.SetValue(entity, relationshipFilter, null);
		}
	}

	private void UpdateEntityDirectRelationships<T, T2>(EntityState<T, T2> entityState)
		where T : Entity<T2>
	{
		var baseType = typeof(IEntity);
		var currentEntity = entityState.Entity;
		var oldEntity = entityState.OldEntity;
		var entityType = typeof(T);
		var entityProperties = entityType.GetCachedProperties();
		var entityRelationships = entityType
			.GetCachedVirtualProperties()
			.Where(x => baseType.IsAssignableFrom(x.PropertyType))
			.ToList();

		foreach (var entityRelationship in entityRelationships)
		{
			var currentRelationshipEntity = entityRelationship.GetValue(currentEntity, null) as IEntity;
			var oldRelationshipEntity = entityRelationship.GetValue(oldEntity, null) as IEntity;

			var entityRelationshipIdProperty = entityProperties.FirstOrDefault(x => x.Name == (entityRelationship.Name + "Id"));

			if ((currentRelationshipEntity == null)
				&& (entityRelationshipIdProperty != null))
			{
				var otherEntityId = entityRelationshipIdProperty.GetValue(currentEntity, null);
				var defaultValue = entityRelationshipIdProperty.PropertyType.CreateInstance();

				if (!Equals(otherEntityId, defaultValue)
					&& Repositories.ContainsKey(entityRelationship.PropertyType.ToAssemblyName()))
				{
					var repository = Repositories[entityRelationship.PropertyType.ToAssemblyName()];
					currentRelationshipEntity = (IEntity) repository.Read(otherEntityId);
					entityRelationship.SetValue(currentEntity, currentRelationshipEntity, null);
				}
			}
			else if ((currentRelationshipEntity != null)
					&& (entityRelationshipIdProperty != null))
			{
				var repository = Repositories[entityRelationship.PropertyType.ToAssemblyName()];
				var repositoryType = repository.GetType();

				// Check to see if this is a new child entity.
				if (!currentRelationshipEntity.IdIsSet())
				{
					if (currentRelationshipEntity.GetType() == entityType)
					{
						repositoryType.GetCachedMethod("InsertBefore").Invoke(repository, [currentRelationshipEntity, currentEntity]);
					}
					else
					{
						// Still adding 400ms per 10000 items, why?
						repositoryType.GetCachedMethod("Add", currentRelationshipEntity.GetType()).Invoke(repository, [currentRelationshipEntity]);
					}
				}
				else
				{
					// Check to see if the entity already exists.
					var exists = (bool) repositoryType.GetCachedMethod("Contains").Invoke(repository, [currentRelationshipEntity]);
					if (!exists)
					{
						repositoryType.GetCachedMethod("Add", currentRelationshipEntity.GetType()).Invoke(repository, [currentRelationshipEntity]);
					}
				}

				var otherEntityProperties = currentRelationshipEntity.GetType().GetCachedProperties();
				var otherEntityIdProperty = otherEntityProperties.FirstOrDefault(x => x.Name == "Id");
				var entityId = entityRelationshipIdProperty.GetValue(currentEntity, null);
				var otherId = otherEntityIdProperty?.GetValue(currentRelationshipEntity);

				if (!Equals(entityId, otherId))
				{
					// resets entityId to entity.Id if it does not match
					entityRelationshipIdProperty.SetValue(currentEntity, otherId, null);
				}

				var entityRelationshipSyncIdProperty = entityProperties.FirstOrDefault(x => x.Name == (entityRelationship.Name + "SyncId"));

				if (entityRelationship.GetValue(currentEntity, null) is ISyncEntity syncEntity
					&& (entityRelationshipSyncIdProperty != null))
				{
					var otherEntitySyncId = (Guid?) entityRelationshipSyncIdProperty.GetValue(currentEntity, null);
					var syncEntitySyncId = syncEntity.GetEntitySyncId();
					if (otherEntitySyncId != syncEntitySyncId)
					{
						// resets entitySyncId to entity.SyncId if it does not match
						entityRelationshipSyncIdProperty.SetValue(currentEntity, syncEntitySyncId, null);
					}
				}
			}
		}
	}

	private protected void RepositoryAddingEntity<T2>(Entity<T2> entity)
	{
		// todo: add relationship check...
	}

	private protected void RepositoryDeletingEntity<T2>(Entity<T2> entity)
	{
		var key = $"{entity.GetRealType().Name}-";

		foreach (var relationship in OneToManyRelationships.Where(x => x.Key.StartsWith(key)))
		{
			var repository = (IDatabaseRepository) relationship.Value[0];
			var configuration = (IPropertyConfiguration) relationship.Value[5];

			switch (configuration.DeleteBehavior)
			{
				case RelationshipDeleteBehavior.Cascade:
				{
					repository.RemoveDependent(relationship.Value, entity.Id);
					continue;
				}
				case RelationshipDeleteBehavior.SetNull:
				{
					repository.SetDependentToNull(relationship.Value, entity.Id);
					continue;
				}
			}

			if (!repository.HasDependentRelationship(relationship.Value, entity.Id))
			{
				continue;
			}

			var message = $"The association between entity types '{entity.GetRealType().Name}' and '{configuration.TypeName}' has been severed but the relationship is either marked as 'Required' or is implicitly required because the foreign key is not nullable.";
			throw new InvalidOperationException(message);
		}
	}

	private protected void RepositorySavedChanges(object sender, CollectionChangeTracker tracker)
	{
		_collectionChangeTracker.Update(tracker);
	}

	private protected void RepositoryUpdateEntityRelationships<T, T2>(EntityState<T, T2> entityState) where T : Entity<T2>
	{
		UpdateEntityDirectRelationships(entityState);
		UpdateEntityCollectionRelationships(entityState);
	}

	private protected void RepositoryValidateEntity<T, T2>(T entity, IRepository<T, T2> repository) where T : Entity<T2>
	{
		if (DatabaseSettings.DisableEntityValidations)
		{
			return;
		}

		var typeofT = typeof(T);

		if (EntityPropertyConfigurations.TryGetValue(typeofT, out var propertyConfiguration))
		{
			foreach (var configuration in propertyConfiguration)
			{
				if (configuration is PropertyConfiguration<T, T2> validation)
				{
					validation.Validate(entity);
				}
			}
		}

		if (EntityIndexConfigurations.TryGetValue(typeofT, out var indexConfiguration))
		{
			foreach (var configuration in indexConfiguration)
			{
				configuration.Validate(entity, repository);
			}
		}
	}

	#endregion

	#region Events

	/// <summary>
	/// An event for when changes are saved. <see cref="SaveChanges" />
	/// </summary>
	public event EventHandler<CollectionChangeTracker> ChangesSaved;

	/// <inheritdoc />
	public event EventHandler Disposed;

	#endregion
}

/// <summary>
/// The interfaces for a Cornerstone database.
/// </summary>
public interface IDatabase : IDisposable
{
	#region Properties

	/// <summary>
	/// Gets the options for this database.
	/// </summary>
	DatabaseSettings DatabaseSettings { get; }

	/// <summary>
	/// The date and time provider.
	/// </summary>
	public IDateTimeProvider DateTimeProvider { get; }

	/// <summary>
	/// Gets a value indicating whether the database has been disposed of.
	/// </summary>
	bool IsDisposed { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Discard all changes made in this context to the underlying database.
	/// </summary>
	int DiscardChanges();

	/// <summary>
	/// Return the type of the database.
	/// </summary>
	/// <returns> The type of the database. </returns>
	DatabaseType GetDatabaseType();

	/// <summary>
	/// Gets the assembly that contains the entity mappings. Base implementation defaults to the implemented types assembly.
	/// </summary>
	Assembly GetMappingAssembly();

	/// <summary>
	/// Gets a read only repository of the requested entity.
	/// </summary>
	/// <typeparam name="T"> The type of the entity to get a repository for. </typeparam>
	/// <typeparam name="T2"> The type of the entity key. </typeparam>
	/// <returns> The repository of entities requested. </returns>
	IRepository<T, T2> GetReadOnlyRepository<T, T2>() where T : Entity<T2>;

	/// <summary>
	/// Gets a repository of the requested entity.
	/// </summary>
	/// <typeparam name="T"> The type of the entity to get a repository for. </typeparam>
	/// <returns> The repository of entities requested. </returns>
	IRepository<T> GetRepository<T>() where T : Entity;

	/// <summary>
	/// Gets a repository of the requested entity.
	/// </summary>
	/// <typeparam name="T"> The type of the entity to get a repository for. </typeparam>
	/// <typeparam name="T2"> The type of the entity key. </typeparam>
	/// <returns> The repository of entities requested. </returns>
	IRepository<T, T2> GetRepository<T, T2>() where T : Entity<T2>;

	/// <summary>
	/// Migrate the database.
	/// </summary>
	void Migrate();

	/// <summary>
	/// Removes an entity from the database
	/// </summary>
	/// <typeparam name="T"> The type of the entity to get a repository for. </typeparam>
	/// <typeparam name="T2"> The type of the entity key. </typeparam>
	/// <param name="item"> The item to be removed. </param>
	/// <returns> The entity that was removed. </returns>
	T Remove<T, T2>(T item) where T : Entity<T2>;

	/// <summary>
	/// Saves all changes made in this context to the underlying database.
	/// </summary>
	/// <returns>
	/// The number of objects written to the underlying database.
	/// </returns>
	/// <exception cref="T:System.InvalidOperationException"> Thrown if the context has been disposed. </exception>
	int SaveChanges();

	/// <summary>
	/// Update the date time provider.
	/// </summary>
	/// <param name="dateTimeProvider"> The new date time provider. </param>
	void UpdateDateTimeProvider(IDateTimeProvider dateTimeProvider);

	#endregion

	#region Events

	/// <summary>
	/// An event for when changes are saved. <see cref="SaveChanges" />
	/// </summary>
	public event EventHandler<CollectionChangeTracker> ChangesSaved;

	/// <summary>
	/// An event for when the database has been disposed.
	/// </summary>
	public event EventHandler Disposed;

	#endregion
}