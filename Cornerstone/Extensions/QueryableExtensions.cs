#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for queryable
/// </summary>
public static class QueryableExtensions
{
	#region Methods

	public static int GetInsertIndex<T>(this IList<T> list, T item, params OrderBy<T>[] orderBy)
	{
		ArgumentNullException.ThrowIfNull(list);

		if (orderBy is null || (orderBy.Length == 0))
		{
			throw new ArgumentException("At least one ordering must be provided.", nameof(orderBy));
		}

		var selectors = new Func<T, object>[orderBy.Length];
		var descending = new bool[orderBy.Length];

		for (var i = 0; i < orderBy.Length; i++)
		{
			selectors[i] = orderBy[i].CompiledSelector;
			descending[i] = orderBy[i].Descending;
		}

		var left = 0;
		var right = list.Count - 1;

		while (left <= right)
		{
			var mid = left + ((right - left) >> 1);
			var comparison = Compare(item, list[mid], selectors, descending);

			if (comparison == 0)
			{
				return mid;
			}

			if (comparison < 0)
			{
				right = mid - 1;
			}
			else
			{
				left = mid + 1;
			}
		}

		return left;
	}

	public static IEnumerable<T> Order<T>(this IEnumerable<T> source, OrderBy<T>[] orderBys)
	{
		if (source == null)
		{
			throw new ArgumentNullException(nameof(source));
		}
		if ((orderBys == null) || (orderBys.Length == 0))
		{
			return source;
		}

		var ordered = orderBys[0].Descending
			? source.OrderByDescending(orderBys[0].CompiledSelector!)
			: source.OrderBy(orderBys[0].CompiledSelector!);

		for (var i = 1; i < orderBys.Length; i++)
		{
			var localOrderBy = orderBys[i];
			ordered = localOrderBy.Descending
				? ordered.ThenByDescending(localOrderBy.CompiledSelector!)
				: ordered.ThenBy(localOrderBy.CompiledSelector!);
		}

		return ordered;
	}

	public static IOrderedQueryable<T> Order<T>(this IQueryable<T> query, OrderBy<T>[] orderBys)
	{
		ArgumentNullException.ThrowIfNull(query);
		ArgumentNullException.ThrowIfNull(orderBys);

		if (orderBys.Length == 0)
		{
			throw new InvalidOperationException("At least one ordering must be provided.");
		}

		IOrderedQueryable<T> orderedQuery = null!;

		for (var i = 0; i < orderBys.Length; i++)
		{
			ref var orderBy = ref orderBys[i]; // small perf win if OrderBy<T> is struct
			var keySelector = orderBy.KeySelector;

			orderedQuery = i == 0
				? orderBy.Descending
					? query.OrderByDescending(keySelector)
					: query.OrderBy(keySelector)
				: orderBy.Descending
					? orderedQuery.ThenByDescending(keySelector)
					: orderedQuery.ThenBy(keySelector);
		}

		return orderedQuery;
	}

	private static int Compare<T>(T leftItem, T rightItem, Func<T, object>[] selectors, bool[] descendings)
	{
		for (var i = 0; i < selectors.Length; i++)
		{
			var cmp = Comparer<object>.Default.Compare(selectors[i](leftItem), selectors[i](rightItem));
			if (cmp != 0)
			{
				return descendings[i] ? -cmp : cmp;
			}
		}
		return 0;
	}

	#endregion
}