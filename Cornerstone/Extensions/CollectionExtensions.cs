#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Text.Human;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for collections.
/// </summary>
public static class CollectionExtensions
{
	#region Methods

	/// <summary>
	/// Add multiple items to a collection
	/// </summary>
	/// <param name="set"> The set to add items to. </param>
	/// <param name="items"> The items to add. </param>
	public static IList Add(this IList set, IEnumerable items)
	{
		foreach (var item in items)
		{
			set.Add(item);
		}

		return set;
	}

	/// <summary>
	/// Add multiple items to a collection
	/// </summary>
	/// <typeparam name="T"> The type of the items in the collection. </typeparam>
	/// <param name="set"> The set to add items to. </param>
	/// <param name="items"> The items to add. </param>
	public static IList<T> Add<T>(this IList<T> set, params T[] items)
	{
		foreach (var item in items)
		{
			set.Add(item);
		}

		return set;
	}

	/// <summary>
	/// Add multiple items to a collection
	/// </summary>
	/// <param name="set"> The set to add items to. </param>
	/// <param name="items"> The items to add. </param>
	/// <typeparam name="T"> The type of the items in the collection. </typeparam>
	public static ICollection<T> Add<T>(this ICollection<T> set, params T[] items)
	{
		foreach (var item in items)
		{
			set.Add(item);
		}

		return set;
	}

	/// <summary>
	/// Add multiple items to a collection
	/// </summary>
	/// <param name="set"> The set to add items to. </param>
	/// <param name="items"> The items to add. </param>
	/// <typeparam name="T"> The type of the items in the collection. </typeparam>
	public static ICollection<T> Add<T>(this ICollection<T> set, IEnumerable<T> items)
	{
		foreach (var item in items)
		{
			set.Add(item);
		}

		return set;
	}

	/// <summary>
	/// Execute the action on each entity in the collection.
	/// </summary>
	/// <param name="items"> The collection of items to process. </param>
	/// <param name="action"> The action to execute for each item. </param>
	public static void ForEach(this IEnumerable items, Action<object> action)
	{
		foreach (var item in items)
		{
			action(item);
		}
	}

	/// <summary>
	/// Execute the action on each entity in the collection.
	/// </summary>
	/// <typeparam name="T"> The type of item in the collection. </typeparam>
	/// <param name="items"> The collection of items to process. </param>
	/// <param name="action"> The action to execute for each item. </param>
	public static void ForEach<T>(this IEnumerable<T> items, Action<T> action)
	{
		foreach (var item in items)
		{
			action(item);
		}
	}

	/// <summary>
	/// Detect the equality comparer to use to validate the provided type.
	/// </summary>
	/// <typeparam name="T"> The type to get comparer for. </typeparam>
	/// <returns> The equality func for the provided type. </returns>
	public static Func<T, T, bool> GetEqualityComparer<T>()
	{
		return (a, b) =>
		{
			return a switch
			{
				IEquatable<T> x => x.Equals(b),
				IEqualityComparer<T> x => x.Equals(a, b),
				IEqualityComparer x => x.Equals(a, b),
				_ => Equals(a, b)
			};
		};
	}

	/// <summary>
	/// Return items missing from the expected list.
	/// </summary>
	/// <param name="expected"> The expected list. </param>
	/// <param name="values"> The list to validate. </param>
	/// <param name="compare"> An optional compare function. Defaults to Equals. </param>
	/// <returns> The items missing from the expected list. </returns>
	public static IList MissingIn(this IList expected, IEnumerable values, Func<object, object, bool> compare = null)
	{
		return (IList) MissingIn(expected.Cast<object>(), values.Cast<object>(), compare);
	}

	/// <summary>
	/// Return items missing from the expected list.
	/// </summary>
	/// <param name="values"> The list to validate. </param>
	/// <param name="expected"> The expected list. </param>
	/// <param name="compare"> An optional compare function. Defaults to Equals. </param>
	/// <returns> The items missing from the expected list. </returns>
	public static IList<T> MissingIn<T>(this IEnumerable<T> expected, IEnumerable<T> values, Func<T, T, bool> compare = null)
	{
		var missingValues = expected.ToList();
		compare ??= GetEqualityComparer<T>();

		foreach (var item in values)
		{
			foreach (var v in missingValues)
			{
				if (!compare(item, v))
				{
					continue;
				}

				missingValues.Remove(v);
				break;
			}
		}

		return missingValues;
	}

	/// <summary>
	/// Reconcile one collection with another.
	/// </summary>
	/// <param name="collection"> The left collection. </param>
	/// <param name="expected"> The right collection. </param>
	/// <param name="compare"> An optional compare function. Defaults to Equals. </param>
	public static void Reconcile(this IList collection, IEnumerable expected, Func<object, object, bool> compare = null)
	{
		var expectedList = expected.Cast<object>().ToList();
		compare ??= GetEqualityComparer<object>();

		// Reconcile two collections
		var updatesToBeAdded = expectedList.MissingIn(collection, compare);
		var updateToBeApplied = expectedList
			.Select(update => new { item = collection.Cast<object>().FirstOrDefault(item => compare(item, update)), update })
			.Where(x => x.item != null);
		var itemsToRemove = collection.MissingIn(expectedList, compare);

		foreach (var newItem in updatesToBeAdded)
		{
			collection.Add(newItem);
		}

		foreach (var updateToApply in updateToBeApplied)
		{
			ApplyUpdate(updateToApply.item, updateToApply.update);
		}

		foreach (var deviceToRemove in itemsToRemove)
		{
			collection.Remove(deviceToRemove);
		}
	}

	/// <summary>
	/// Reconcile one collection with another.
	/// </summary>
	/// <typeparam name="T"> The type of the collections. </typeparam>
	/// <param name="collection"> The left collection. </param>
	/// <param name="expected"> The right collection. </param>
	/// <param name="compare"> An optional compare function. Defaults to Equals. </param>
	public static void Reconcile<T>(this IList<T> collection, IEnumerable<T> expected, Func<T, T, bool> compare = null)
	{
		var expectedList = expected?.ToList() ?? [];
		compare ??= GetEqualityComparer<T>();

		// Reconcile two collections
		var updatesToBeAdded = expectedList.MissingIn(collection, compare);
		var updateToBeApplied = expectedList
			.Select(update => new { item = collection.FirstOrDefault(item => compare(item, update)), update })
			.Where(x => x.item != null)
			.ToList();
		var itemsToRemove = collection.MissingIn(expectedList, compare);

		foreach (var newItem in updatesToBeAdded)
		{
			collection.Add(newItem);
		}

		foreach (var updateToApply in updateToBeApplied)
		{
			ApplyUpdate(updateToApply.item, updateToApply.update);
		}

		foreach (var deviceToRemove in itemsToRemove)
		{
			collection.Remove(deviceToRemove);
		}
	}

	/// <summary>
	/// Reconcile one collection with another.
	/// </summary>
	/// <typeparam name="T"> The type of the collections. </typeparam>
	/// <param name="collection"> The left collection. </param>
	/// <param name="updates"> The right collection. </param>
	public static HashSet<T> Reconcile<T>(this HashSet<T> collection, IEnumerable<T> updates)
	{
		collection.Clear();
		collection.Add(updates);
		return collection;
	}

	/// <summary>
	/// Remove items from a collection based on the provided filter.
	/// </summary>
	/// <param name="collection"> The collection to update. </param>
	/// <param name="filter"> The filter of the items to remove. </param>
	public static void RemoveWhere<T>(this ICollection<T> collection, Func<T, bool> filter)
	{
		var itemsToRemove = collection.Where(filter).ToList();

		foreach (var item in itemsToRemove)
		{
			collection.Remove(item);
		}
	}

	/// <summary>
	/// Sorts the enumerable
	/// </summary>
	/// <typeparam name="T"> </typeparam>
	/// <param name="source"> </param>
	/// <param name="dependencies"> </param>
	/// <param name="throwOnCycle"> </param>
	/// <returns> </returns>
	public static IEnumerable<T> Sort<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> dependencies, bool throwOnCycle = false)
	{
		var sorted = new List<T>();
		var visited = new HashSet<T>();

		foreach (var item in source)
		{
			Visit(item, visited, sorted, dependencies, throwOnCycle);
		}

		return sorted;
	}

	/// <summary>
	/// Sort one collection based on keys defined in another
	/// </summary>
	/// <returns> Items sorted </returns>
	public static IEnumerable<TResult> SortBy<TResult, TKey>(this IEnumerable<TResult> itemsToSort, IEnumerable<TKey> sortKeys, Func<TResult, TKey> matchFunc)
	{
		return sortKeys.Join(itemsToSort, key => key, matchFunc, (key, item) => item);
	}

	/// <summary>
	/// Gets the base 64 version of the byte array.
	/// </summary>
	/// <param name="data"> The data to process. </param>
	/// <returns> The base 64 string of the data. </returns>
	public static string ToBase64String(this byte[] data)
	{
		return System.Convert.ToBase64String(data);
	}

	/// <summary>
	/// Appends new values to an existing HashSet.
	/// </summary>
	/// <typeparam name="T"> The type of value in the set. </typeparam>
	/// <param name="values"> The set to append to. </param>
	/// <param name="additions"> The values to add. </param>
	/// <returns> A new HashSet containing the new values. </returns>
	public static HashSet<T> ToHashSet<T>(this IEnumerable<T> values, params T[] additions)
	{
		return values is HashSet<T> hashSet
			? new HashSet<T>(values.Union(additions), hashSet.Comparer)
			: [..values.Union(additions)];
	}

	/// <summary>
	/// Converts the byte array to a hex string.
	/// </summary>
	/// <param name="data"> The data to convert. </param>
	/// <returns> The data in a hex string format. </returns>
	public static string ToHexString(this byte[] data)
	{
		var response = new char[data.Length * 2];
		for (int i = 0, j = 0; i < data.Length; i++)
		{
			response[j++] = StringFormatter.HexCharacters[data[i] >> 4];
			response[j++] = StringFormatter.HexCharacters[data[i] & 0xF];
		}
		return new string(response);
	}

	/// <summary>
	/// Convert the collection to an object.
	/// </summary>
	/// <param name="collection"> The collection to convert. </param>
	/// <returns> The collection in array format. </returns>
	public static object[] ToObjectArray(this ICollection collection)
	{
		var response = new object[collection.Count];
		var index = 0;
		var enumerator = collection.GetEnumerator();

		while (enumerator.MoveNext())
		{
			response[index++] = enumerator.Current;
		}

		if (enumerator is IDisposable disposable)
		{
			disposable.Dispose();
		}

		return response;
	}

	/// <summary>
	/// Converts a set into a readonly set.
	/// </summary>
	/// <param name="set"> The set to protect as readonly. </param>
	/// <returns> The provided set as a readonly version. </returns>
	public static ReadOnlySet<T> ToReadOnlySet<T>(this ISet<T> set)
	{
		return set as ReadOnlySet<T> ?? new ReadOnlySet<T>(set);
	}

	/// <summary>
	/// Converts a collection into a readonly set.
	/// </summary>
	/// <param name="collection"> The collection to protect as readonly. </param>
	/// <returns> The provided set as a readonly version. </returns>
	public static ReadOnlySet<T> ToReadOnlySet<T>(this IEnumerable<T> collection)
	{
		if (collection is ISet<T> readOnlySet)
		{
			return new ReadOnlySet<T>(readOnlySet);
		}

		var array = collection.ToArray();
		return new ReadOnlySet<T>(array);
	}

	/// <summary>
	/// Converts a collection into a SpeedyList.
	/// </summary>
	/// <param name="collection"> The collection to convert to a SpeedyList. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	/// <param name="orderBy"> The optional set of order by settings. </param>
	/// <returns> The SpeedyList containing the collection. </returns>
	public static SpeedyList<T> ToSpeedyList<T>(this IEnumerable<T> collection, IDispatcher dispatcher = null, params OrderBy<T>[] orderBy)
	{
		var response = new SpeedyList<T>(dispatcher, orderBy);
		response.Load(collection);
		return response;
	}

	/// <summary>
	/// Check index and length to ensure it is within bounds of the list.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool ValidRange<T>(this IList<T> list, int index, int length)
	{
		if (list.Count <= 0)
		{
			return false;
		}

		if ((index < 0) || (index >= list.Count))
		{
			return false;
		}

		var end = index + length;
		if ((end < 0) || (end > list.Count))
		{
			return false;
		}

		return true;
	}

	private static void ApplyUpdate<T>(T toUpdate, T update)
	{
		switch (toUpdate)
		{
			case IUpdateable updatable:
			{
				updatable.UpdateWith(update);
				break;
			}
			default:
			{
				toUpdate.UpdateWithUsingReflection(update);
				break;
			}
		}
	}

	private static void Visit<T>(T item, HashSet<T> visited, List<T> sorted, Func<T, IEnumerable<T>> dependencies, bool throwOnCycle)
	{
		if (!visited.Contains(item))
		{
			visited.Add(item);

			foreach (var dep in dependencies(item))
			{
				Visit(dep, visited, sorted, dependencies, throwOnCycle);
			}

			sorted.Add(item);
		}
		else
		{
			if (throwOnCycle && !sorted.Contains(item))
			{
				throw new Exception("Cyclic dependency found");
			}
		}
	}

	#endregion
}