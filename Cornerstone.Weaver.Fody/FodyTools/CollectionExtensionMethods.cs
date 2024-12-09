// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

#region References

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace Cornerstone.Weaver.Fody.FodyTools;

internal static class CollectionExtensionMethods
{
	#region Methods

	public static void AddRange<T>(this IList<T> collection, params T[] values)
	{
		AddRange(collection, (IEnumerable<T>) values);
	}

	public static void AddRange<T>(this IList<T> collection, IEnumerable<T> values)
	{
		foreach (var value in values)
		{
			collection.Add(value);
		}
	}

	public static void InsertRange<T>(this IList<T> collection, int index, params T[] values)
	{
		InsertRange(collection, index, (IEnumerable<T>) values);
	}

	public static void InsertRange<T>(this IList<T> collection, int index, IEnumerable<T> values)
	{
		foreach (var value in values)
		{
			collection.Insert(index++, value);
		}
	}

	public static void RemoveAll<T>(this ICollection<T> target, Func<T, bool> condition)
	{
		target.RemoveAll(target.Where(condition).ToList());
	}

	public static void RemoveAll<T>(this ICollection<T> target, IEnumerable<T> items)
	{
		foreach (var i in items)
		{
			target.Remove(i);
		}
	}

	public static void Replace<T>(this IList<T> collection, IEnumerable<T> values)
	{
		if ((collection == null) || (values == null))
		{
			return;
		}

		collection.Clear();
		collection.AddRange(values);
	}

	public static void ReplaceItems<T>(this IList<T> collection, Func<T, T> selector)
	{
		if (collection == null)
		{
			return;
		}

		for (var i = 0; i < collection.Count; i++)
		{
			collection[i] = selector(collection[i]);
		}
	}

	public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> items)
	{
		return items.ToList().AsReadOnly();
	}

	#endregion
}