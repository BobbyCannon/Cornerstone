#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Cornerstone.Reflection;
using Cornerstone.Storage;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Cornerstone.EntityFramework;

/// <summary>
/// Represents a collection of entities for a Cornerstone database.
/// </summary>
/// <typeparam name="T"> The entity type this collection is for. </typeparam>
/// <typeparam name="T2"> The type of the entity key. </typeparam>
public class EntityFrameworkRepository<T, T2> : EntityFrameworkRepository<T>, IRepository<T, T2>
	where T : Entity<T2>
{
	#region Constructors

	/// <summary>
	/// Initializes a repository.
	/// </summary>
	/// <param name="database"> The database where this repository resides. </param>
	/// <param name="set"> The database set this repository is for. </param>
	public EntityFrameworkRepository(EntityFrameworkDatabase database, DbSet<T> set)
		: base(database, set)
	{
	}

	#endregion

	#region Methods

	/// <summary>
	/// Adds or updates an entity in the repository. The ID of the entity must be the default value to add and a value to
	/// update.
	/// </summary>
	/// <param name="entity"> The entity to be added. </param>
	public override void AddOrUpdate(T entity)
	{
		if (Set.Any(x => x.Id.Equals(entity.Id)))
		{
			Set.Update(entity);
			return;
		}

		Set.Add(entity);
	}

	public override bool Contains(T item)
	{
		if (item == null)
		{
			return false;
		}
		var id = item.Id;
		return Set.Any(x => Equals(x.Id, id));
	}

	public bool Remove(T2 id)
	{
		var entity = Set.Local.FirstOrDefault(x => Equals(x.Id, id));
		if (entity == null)
		{
			entity = Activator.CreateInstance<T>();
			entity.Id = id;
			Set.Attach(entity);
		}

		Set.Remove(entity);
		return true;
	}

	#endregion
}

/// <summary>
/// Represents a collection of entities for a Cornerstone database.
/// </summary>
/// <typeparam name="T"> The entity type this collection is for. </typeparam>
public abstract class EntityFrameworkRepository<T> : IRepository<T> where T : Entity
{
	#region Fields

	/// <summary>
	/// The set of the entities.
	/// </summary>
	protected readonly DbSet<T> Set;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a repository.
	/// </summary>
	/// <param name="database"> The database where this repository resides. </param>
	/// <param name="set"> The database set this repository is for. </param>
	protected EntityFrameworkRepository(EntityFrameworkDatabase database, DbSet<T> set)
	{
		Database = database;
		Set = set;
	}

	#endregion

	#region Properties

	public int Count => Set.Count();

	/// <summary>
	/// The database where this repository resides.
	/// </summary>
	public EntityFrameworkDatabase Database { get; }

	/// <summary>
	/// Gets the type of the element(s) that are returned when the expression tree associated with this instance of
	/// <see cref="T:System.Linq.IQueryable" /> is executed.
	/// </summary>
	/// <returns>
	/// A <see cref="T:System.Type" /> that represents the type of the element(s) that are returned when the expression tree
	/// associated with this object is executed.
	/// </returns>
	public Type ElementType => ((IQueryable<T>) Set).ElementType;

	/// <summary>
	/// Gets the expression tree that is associated with the instance of <see cref="T:System.Linq.IQueryable" />.
	/// </summary>
	/// <returns>
	/// The <see cref="T:System.Linq.Expressions.Expression" /> that is associated with this instance of
	/// <see cref="T:System.Linq.IQueryable" />.
	/// </returns>
	public Expression Expression => ((IQueryable<T>) Set).Expression;

	public bool IsReadOnly => false;

	/// <summary>
	/// Gets the query provider that is associated with this data source.
	/// </summary>
	/// <returns>
	/// The <see cref="T:System.Linq.IQueryProvider" /> that is associated with this data source.
	/// </returns>
	public IQueryProvider Provider => ((IQueryable<T>) Set).Provider;

	#endregion

	#region Methods

	/// <summary>
	/// Add an entity to the repository. The ID of the entity must be the default value.
	/// </summary>
	/// <param name="entity"> The entity to be added. </param>
	public void Add(T entity)
	{
		Set.Add(entity);
	}

	public abstract void AddOrUpdate(T entity);

	public void Clear()
	{
		throw new NotSupportedException();
	}

	public abstract bool Contains(T item);

	public void CopyTo(T[] array, int arrayIndex)
	{
		var items = Set.ToArray();
		Array.Copy(items, 0, array, arrayIndex, items.Length);
	}

	public IEnumerator<T> GetEnumerator()
	{
		return ((IQueryable<T>) Set).GetEnumerator();
	}

	public IIncludableQueryable<T, T3> Include<T3>(Expression<Func<T, T3>> include)
	{
		return Including(include);
	}

	public IIncludableQueryable<T, object> Including(params Expression<Func<T, object>>[] includes)
	{
		return Including<object>(includes);
	}

	/// <summary>
	/// Configures the query to include multiple related entities in the results.
	/// </summary>
	/// <param name="includes"> The related entities to include. </param>
	/// <returns> The results of the query including the related entities. </returns>
	[UnconditionalSuppressMessage("Trimming", "IL2055:MakeGenericType")]
	[UnconditionalSuppressMessage("Trimming", "IL2072:Activator.CreateInstance")]
	public IIncludableQueryable<T, T3> Including<T3>(params Expression<Func<T, T3>>[] includes)
	{
		var result = includes.Aggregate(Set.AsQueryable(), (current, include) => current.Include(include));
		if (result is Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<T, T3> aiq)
		{
			return new EntityIncludableQueryable<T, T3>(aiq);
		}

		// Try to find the internal includable queryable, not good, but it is what we have to do...
		var includableQueryType = (Type) typeof(EntityFrameworkQueryableExtensions)
			.GetMembers(BindingFlags.Instance | BindingFlags.NonPublic)
			.FirstOrDefault(x => x.Name == "IncludableQueryable`2");

		// Check to ensure we found the type
		if (includableQueryType == null)
		{
			throw new CornerstoneException("Critical: Need to look into IncludableQueryable");
		}

		// Create an instance of the includable queryable so that we can pass it to ThenInclude
		var includableQueryTypeGeneric = includableQueryType.MakeGenericType(typeof(T), typeof(T3));
		var instance = Activator.CreateInstance(includableQueryTypeGeneric, result);
		return new EntityIncludableQueryable<T, T3>((Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<T, T3>) instance);
	}

	public bool Remove(T entity)
	{
		Set.Remove(entity);
		return true;
	}

	public int Remove(Expression<Func<T, bool>> filter)
	{
		var count = Set.Count(filter);
		Set.RemoveRange(Set.Where(filter));
		return count;
	}

	protected void UpdateRelationships(T entity)
	{
		var baseType = typeof(IEntity);
		var entityType = entity.GetRealType();
		var entityTypeInfo = SourceReflector.GetRequiredSourceType(entityType);
		var entityProperties = entityTypeInfo.GetProperties();
		var entityRelationships = entityProperties.Where(x => x.IsVirtual)
			.Where(x => baseType.IsAssignableFrom(x.PropertyInfo.PropertyType))
			.ToList();

		foreach (var entityRelationship in entityRelationships)
		{
			if (!(entityRelationship.GetValue(entity) is IEntity otherEntity))
			{
				continue;
			}

			var otherEntityProperties = SourceReflector.GetRequiredSourceType(otherEntity.GetRealType()).GetProperties();
			var otherEntityIdProperty = otherEntityProperties.FirstOrDefault(x => x.Name == "Id");
			var entityRelationshipIdProperty = entityProperties.FirstOrDefault(x => x.Name == (entityRelationship.Name + "Id"));

			if ((otherEntityIdProperty != null) && (entityRelationshipIdProperty != null))
			{
				var entityId = entityRelationshipIdProperty.GetValue(entity);
				var otherId = otherEntityIdProperty.GetValue(otherEntity);

				if (!Equals(entityId, otherId))
				{
					// resets entityId to entity.Id if it does not match
					entityRelationshipIdProperty.SetValue(entity, otherId);
				}
			}

			var otherEntitySyncIdProperty = otherEntityProperties.FirstOrDefault(x => x.Name == "SyncId");
			var entityRelationshipSyncIdProperty = entityProperties.FirstOrDefault(x => x.Name == (entityRelationship.Name + "SyncId"));

			if ((otherEntitySyncIdProperty != null) && (entityRelationshipSyncIdProperty != null))
			{
				var entitySyncId = entityRelationshipSyncIdProperty.GetValue(entity);
				var otherSyncId = otherEntitySyncIdProperty?.GetValue(otherEntity);

				if (!Equals(entitySyncId, otherSyncId))
				{
					// resets entityId to entity.SyncId if it does not match
					entityRelationshipSyncIdProperty.SetValue(entity, otherSyncId);
				}
			}
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion
}