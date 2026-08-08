#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

#endregion

namespace Cornerstone.Storage;

public class IncludableQueryable<T, T2> : IIncludableQueryable<T, T2> where T : class
{
	#region Fields

	private readonly IQueryable<T> _query;

	#endregion

	#region Constructors

	/// <summary>
	/// Instantiate an instance of the IncludableQueryable
	/// </summary>
	/// <param name="query"> </param>
	public IncludableQueryable(IQueryable<T> query)
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
		return new IncludableQueryable<T, TProperty>(_query);
	}

	public IIncludableQueryable<T, TProperty> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<TPreviousProperty, TProperty>> include)
	{
		return new IncludableQueryable<T, TProperty>(_query);
	}

	public IIncludableQueryable<T, TProperty> ThenInclude<TProperty>(Expression<Func<T2, TProperty>> include)
	{
		return new IncludableQueryable<T, TProperty>(_query);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion
}