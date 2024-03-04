#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Cornerstone.Storage;

/// <summary>
/// Represents a collection of entities for a Cornerstone database.
/// </summary>
/// <typeparam name="T"> The type of the entity of the collection. </typeparam>
/// <typeparam name="T2"> The type of the entity key. </typeparam>
public interface IRepository<T, in T2> : ICollection<T>, IQueryable<T> where T : Entity<T2>
{
	#region Methods

	/// <summary>
	/// Adds or updates an entity in the repository. The ID of the entity must be the default value to add and a value to
	/// update.
	/// </summary>
	/// <param name="entity"> The entity to be added. </param>
	void AddOrUpdate(T entity);

	/// <summary>
	/// Configures the query to include related entities in the results.
	/// </summary>
	/// <param name="include"> The related entities to include. </param>
	/// <returns> The results of the query including the related entities. </returns>
	IIncludableQueryable<T, T3> Include<T3>(Expression<Func<T, T3>> include);

	/// <summary>
	/// Configures the query to include multiple related entities in the results.
	/// </summary>
	/// <param name="includes"> The related entities to include. </param>
	/// <returns> The results of the query including the related entities. </returns>
	IIncludableQueryable<T, object> Including(params Expression<Func<T, object>>[] includes);

	/// <summary>
	/// Configures the query to include multiple related entities in the results.
	/// </summary>
	/// <param name="includes"> The related entities to include. </param>
	/// <returns> The results of the query including the related entities. </returns>
	IIncludableQueryable<T, T3> Including<T3>(params Expression<Func<T, T3>>[] includes);

	/// <summary>
	/// Removes an entity from the repository.
	/// </summary>
	/// <param name="id"> The ID of the entity to remove. </param>
	bool Remove(T2 id);

	/// <summary>
	/// Removes a set of entities from the repository.
	/// </summary>
	/// <param name="filter"> The filter of the entities to remove. </param>
	int Remove(Expression<Func<T, bool>> filter);

	#endregion
}