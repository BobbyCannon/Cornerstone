#region References

using System;
using System.Linq;
using System.Linq.Expressions;
using Cornerstone.Collections;
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
	public static PagedResults<T> GetPagedResults<T>(this IQueryable<T> query, PagedRequest request)
	{
		var total = query.Count();
		var results = query
			.Skip((request.Page - 1) * request.PerPage)
			.Take(request.PerPage)
			.ToArray();

		var response = Activator.CreateInstance<PagedResults<T>>(request, total, results);
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
	public static PagedResults<T2> GetPagedResults<T1, T2>(this IQueryable<T1> query, PagedRequest request, Expression<Func<T1, T2>> transform)
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
	public static PagedResults<T2> GetPagedResultsClientTransform<T1, T2>(this IQueryable<T1> query, PagedRequest request, Func<T1, T2> transform)
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

	/// <summary>
	/// Gets paged results. Transform is executed on the client after the results are queried.
	/// </summary>
	/// <typeparam name="T1"> The type of item in the query. </typeparam>
	/// <typeparam name="T2"> The type of the item returned. </typeparam>
	/// <param name="query"> The queryable collection. </param>
	/// <param name="request"> The request values. </param>
	/// <param name="transform"> The function to transfer the results. </param>
	/// <param name="order"> An optional order of the collection. </param>
	/// <returns> The paged results. </returns>
	public static PagedResults<T2> GetPagedResultsClientTransform<T1, T2>(this IQueryable<T1> query, PagedRequest request, Func<T1, T2> transform, Expression<Func<T1, object>> order)
	{
		return GetPagedResultsClientTransform(query, request, transform, order == null ? null : new OrderBy<T1>(order));
	}

	/// <summary>
	/// Gets paged results. Transform is executed on the client after the results are queried.
	/// </summary>
	/// <typeparam name="T1"> The type of item in the query. </typeparam>
	/// <typeparam name="T2"> The type of the item returned. </typeparam>
	/// <param name="query"> The queryable collection. </param>
	/// <param name="request"> The request values. </param>
	/// <param name="transform"> The function to transfer the results. </param>
	/// <param name="order"> The order of the collection. </param>
	/// <param name="thenBys"> An optional then bys to order the collection. </param>
	/// <returns> The paged results. </returns>
	public static PagedResults<T2> GetPagedResultsClientTransform<T1, T2>(this IQueryable<T1> query, PagedRequest request, Func<T1, T2> transform, OrderBy<T1> order, params OrderBy<T1>[] thenBys)
	{
		var orderedQuery = order?.Process(query, thenBys) ?? query;
		var total = query.Count();
		var list = orderedQuery
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