#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Cornerstone.Storage;
using Microsoft.EntityFrameworkCore;

#endregion

namespace Cornerstone.EntityFramework;

public class EntityIncludableQueryable<T, T2> : IIncludableQueryable<T, T2> where T : class
{
	#region Fields

	private readonly Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<T, T2> _query;

	#endregion

	#region Constructors

	public EntityIncludableQueryable(Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<T, T2> query)
	{
		_query = query;
	}

	#endregion

	#region Properties

	public Type ElementType => _query.ElementType;

	public Expression Expression => _query.Expression;

	public IQueryProvider Provider => _query.Provider;

	#endregion

	#region Methods

	public IEnumerator<T> GetEnumerator()
	{
		return _query.GetEnumerator();
	}

	public IIncludableQueryable<T, TProperty> Include<TProperty>(Expression<Func<T, TProperty>> include)
	{
		return new EntityIncludableQueryable<T, TProperty>(_query.Include(include));
	}

	public IIncludableQueryable<T, TProperty> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<TPreviousProperty, TProperty>> include)
	{
		return typeof(IEnumerable<TPreviousProperty>).IsAssignableFrom(typeof(T2))
			? new EntityIncludableQueryable<T, TProperty>(((Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<T, IEnumerable<TPreviousProperty>>) _query).ThenInclude(include))
			: null;
	}

	public IIncludableQueryable<T, TProperty> ThenInclude<TProperty>(Expression<Func<T2, TProperty>> include)
	{
		return new EntityIncludableQueryable<T, TProperty>(_query.ThenInclude(include));
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion
}