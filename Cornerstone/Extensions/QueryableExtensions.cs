#region References

using System;
using System.Linq;
using System.Linq.Expressions;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for queryable
/// </summary>
public static class QueryableExtensions
{
	#region Methods

	/// <summary>
	/// Gets paged results.
	/// </summary>
	/// <typeparam name="T"> The type of the item returned. </typeparam>
	/// <param name="query"> The queryable collection. </param>
	/// <param name="request"> The request values. </param>
	/// <returns> The paged results. </returns>
	public static PagedResults<T> GetPagedResults<T>(this IOrderedQueryable<T> query, PagedRequest request)
	{
		var total = query.Count();
		var results = query
			.Skip((request.Page - 1) * request.PerPage)
			.Take(request.PerPage)
			.ToArray();

		var response = (PagedResults<T>) typeof(PagedResults<T>).CreateInstance(request, total, results);
		return response;
	}

	/// <summary>
	/// Gets paged results. Transform is executed as part of the query.
	/// </summary>
	/// <typeparam name="T1"> The type of item in the query. </typeparam>
	/// <typeparam name="T2"> The type of the item returned. </typeparam>
	/// <param name="query"> The queryable collection. </param>
	/// <param name="request"> The request values. </param>
	/// <param name="transform"> The function to transform the results. </param>
	/// <returns> The paged results. </returns>
	public static PagedResults<T2> GetPagedResults<T1, T2>(this IOrderedQueryable<T1> query, PagedRequest request, Expression<Func<T1, T2>> transform)
	{
		var total = query.Count();
		var results = query
			.Skip((request.Page - 1) * request.PerPage)
			.Take(request.PerPage)
			.Select(transform)
			.ToArray();

		var response = (PagedResults<T2>) typeof(PagedResults<T2>).CreateInstance(request, total, results);
		return response;
	}

	/// <summary>
	/// Gets paged results. Transform is executed on the client after the results are queried.
	/// </summary>
	/// <typeparam name="T1"> The type of item in the query. </typeparam>
	/// <typeparam name="T2"> The type of the item returned. </typeparam>
	/// <param name="query"> The queryable collection. </param>
	/// <param name="request"> The request values. </param>
	/// <param name="transform"> The function to transfer the results. </param>
	/// <returns> The paged results. </returns>
	public static PagedResults<T2> GetPagedResultsClientTransform<T1, T2>(this IOrderedQueryable<T1> query, PagedRequest request, Func<T1, T2> transform)
	{
		var total = query.Count();
		var list = query
			.Skip((request.Page - 1) * request.PerPage)
			.Take(request.PerPage)
			.ToList();
		var results = list
			.Select(transform)
			.ToArray();

		var response = (PagedResults<T2>) typeof(PagedResults<T2>).CreateInstance(request, total, results);
		return response;
	}

	#endregion
}