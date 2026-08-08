#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using Cornerstone.Storage;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Cornerstone.EntityFramework;

/// <summary>
/// Represents a read only collection of entities for a Cornerstone database.
/// </summary>
/// <typeparam name="T"> The entity type this collection is for. </typeparam>
/// <typeparam name="T2"> The type of the entity key. </typeparam>
[ExcludeFromCodeCoverage]
public class ReadOnlyEntityFrameworkRepository<T, T2> : IRepository<T, T2> where T : Entity<T2>
{
	#region Fields

	private readonly EntityFrameworkDatabase _database;
	private readonly IQueryable<T> _query;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a repository.
	/// </summary>
	/// <param name="database"> The database where this repository resides. </param>
	/// <param name="set"> The database set this repository is for. </param>
	public ReadOnlyEntityFrameworkRepository(EntityFrameworkDatabase database, DbSet<T> set)
	{
		_database = database;
		_query = set.AsNoTracking().AsQueryable();
	}

	#endregion

	#region Properties

	public int Count => _query.Count();

	/// <summary>
	/// Gets the type of the element(s) that are returned when the expression tree associated with this instance of
	/// <see cref="T:System.Linq.IQueryable" /> is executed.
	/// </summary>
	/// <returns>
	/// A <see cref="T:System.Type" /> that represents the type of the element(s) that are returned when the expression tree
	/// associated with this object is executed.
	/// </returns>
	public Type ElementType => _query.ElementType;

	/// <summary>
	/// Gets the expression tree that is associated with the instance of <see cref="T:System.Linq.IQueryable" />.
	/// </summary>
	/// <returns>
	/// The <see cref="T:System.Linq.Expressions.Expression" /> that is associated with this instance of
	/// <see cref="T:System.Linq.IQueryable" />.
	/// </returns>
	public Expression Expression => _query.Expression;

	public bool IsReadOnly => true;

	/// <summary>
	/// Gets the query provider that is associated with this data source.
	/// </summary>
	/// <returns>
	/// The <see cref="T:System.Linq.IQueryProvider" /> that is associated with this data source.
	/// </returns>
	public IQueryProvider Provider => _query.Provider;

	#endregion

	#region Methods

	public void Add(T entity)
	{
		throw new NotSupportedException();
	}

	public void AddOrUpdate(T entity)
	{
		throw new NotSupportedException();
	}

	public void Clear()
	{
		throw new NotSupportedException();
	}

	public bool Contains(T item)
	{
		throw new NotSupportedException();
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		var results = _query.ToArray();
		Array.Copy(results, 0, array, arrayIndex, results.Length);
	}

	public IEnumerator<T> GetEnumerator()
	{
		return _query.GetEnumerator();
	}

	public IIncludableQueryable<T, T3> Include<T3>(Expression<Func<T, T3>> include)
	{
		return new EntityIncludableQueryable<T, T3>(_query.Include(include));
	}

	public IIncludableQueryable<T, object> Including(params Expression<Func<T, object>>[] includes)
	{
		return new EntityIncludableQueryable<T, object>((Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<T, object>) includes.Aggregate(_query, (current, include) => current.Include(include)));
	}

	public IIncludableQueryable<T, T3> Including<T3>(params Expression<Func<T, T3>>[] includes)
	{
		return new EntityIncludableQueryable<T, T3>((Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<T, T3>) includes.Aggregate(_query, (current, include) => current.Include(include)));
	}

	public bool Remove(T2 id)
	{
		throw new NotSupportedException();
	}

	public bool Remove(T entity)
	{
		throw new NotSupportedException();
	}

	public int Remove(Expression<Func<T, bool>> filter)
	{
		throw new NotImplementedException();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion
}