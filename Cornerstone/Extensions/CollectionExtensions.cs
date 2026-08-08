#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for collections.
/// </summary>
public static class CollectionExtensions
{
	#region Methods

	/// <summary>
	/// Add multiple items to a collection.
	/// </summary>
	/// <param name="collection"> The collection to add items to. </param>
	/// <param name="values"> The items to add. </param>
	/// <typeparam name="T"> The type of the items in the collection. </typeparam>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static IList<T> AddRange<T>(this IList<T> collection, IEnumerable<T> values)
	{
		foreach (var value in values)
		{
			collection.Add(value);
		}

		return collection;
	}

	/// <summary>
	/// Splits the values into an array using the delimiter
	/// </summary>
	/// <param name="value"> The roles for the account. </param>
	/// <param name="delimiter"> The delimiter to split on </param>
	/// <returns> The array of values. </returns>
	public static string CombineIntoTags<T>(this IEnumerable<T> value, string delimiter = ",")
	{
		return delimiter + string.Join(delimiter, value.Select(x => x?.ToString())) + delimiter;
	}

	/// <summary>
	/// Execute the action on each entity in the collection.
	/// </summary>
	/// <typeparam name="T"> The type of item in the collection. </typeparam>
	/// <param name="items"> The collection of items to process. </param>
	/// <param name="action"> The action to execute for each item. </param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ForEach<T>(this IEnumerable<T> items, Action<T> action)
	{
		if ((items == null) || (action == null))
		{
			return;
		}

		foreach (var item in items)
		{
			action(item);
		}
	}

	/// <summary>
	/// Reconciles a collection with an expected collection by adding missing items and removing extras.
	/// Does not update existing items or reorder the collection.
	/// </summary>
	/// <typeparam name="T"> The type of the collections. </typeparam>
	/// <param name="collection"> The collection to modify. </param>
	/// <param name="expected"> The expected collection state. </param>
	/// <param name="comparer"> Optional equality comparer used to determine distinct items. Defaults to <see cref="EqualityComparer{T}.Default" />. </param>
	public static void ReconcileList<T>(this IList<T> collection, IEnumerable<T> expected, IEqualityComparer<T> comparer = null)
	{
		if (collection == null)
		{
			throw new ArgumentNullException(nameof(collection));
		}
		if (expected == null)
		{
			throw new ArgumentNullException(nameof(expected));
		}

		comparer ??= EqualityComparer<T>.Default;

		// Ordered unique items from expected
		var expectedUnique = new List<T>();
		var expectedSet = new HashSet<T>(comparer);
		foreach (var item in expected)
		{
			if (expectedSet.Add(item))
			{
				expectedUnique.Add(item);
			}
		}

		if (collection.Count == 0)
		{
			foreach (var item in expectedUnique)
			{
				collection.Add(item);
			}
			return;
		}

		// Remove items not present in expected (backwards so indices stay valid)
		for (var i = collection.Count - 1; i >= 0; i--)
		{
			if (!expectedSet.Contains(collection[i]))
			{
				collection.RemoveAt(i);
			}
		}

		// Add missing items (preserving expected order)
		var existingSet = new HashSet<T>(collection, comparer);
		foreach (var item in expectedUnique)
		{
			if (existingSet.Add(item))
			{
				collection.Add(item);
			}
		}
	}

	/// <summary>
	/// Reconciles a collection with an expected collection by adding missing items, updating existing ones, and removing extras.
	/// </summary>
	/// <typeparam name="T"> The type of the collections. </typeparam>
	/// <param name="collection"> The collection to modify. </param>
	/// <param name="expected"> The expected collection state. </param>
	/// <param name="distinctCheck"> </param>
	/// <param name="hasChanged"> The check to see if an item has changed. </param>
	/// <exception cref="ArgumentNullException"> Thrown if <paramref name="collection" /> or <paramref name="expected" /> is null. </exception>
	public static void ReconcileListAndItems<T>(this IList<T> collection, IEnumerable expected,
		IEqualityComparer<T> distinctCheck = null, Func<T, T, bool> hasChanged = null)
	{
		if (collection == null)
		{
			throw new ArgumentNullException(nameof(collection));
		}

		if (expected == null)
		{
			throw new ArgumentNullException(nameof(expected));
		}

		// Skip null entries — Dictionary does not allow null keys, and concurrent
		// snapshots can leave default slots when a source list shrinks mid-copy.
		var expectedList = expected.Cast<T>().Where(static x => x is not null).ToList();

		if ((collection.Count == 0) && (expectedList.Count == 0))
		{
			return;
		}

		if (collection.Count == 0)
		{
			foreach (var item in expectedList)
			{
				collection.Add(item);
			}

			return;
		}

		var comparer = distinctCheck ?? EqualityComparer<T>.Default;

		var existingMap = new Dictionary<T, int>(comparer);
		for (var i = 0; i < collection.Count; i++)
		{
			var item = collection[i];
			if ((item is null) || existingMap.ContainsKey(item))
			{
				continue;
			}

			existingMap[item] = i;
		}

		var matchedIndices = new bool[collection.Count];
		var toAdd = new List<T>();
		var toRemove = new List<int>();
		var toUpdatePairs = new List<(int Index, T Expected)>();

		foreach (var expectedItem in expectedList)
		{
			if (existingMap.TryGetValue(expectedItem, out var matchIndex))
			{
				if (!matchedIndices[matchIndex])
				{
					matchedIndices[matchIndex] = true;
					toUpdatePairs.Add((matchIndex, expectedItem));
					continue;
				}
			}

			toAdd.Add(expectedItem);
		}

		for (var i = 0; i < collection.Count; i++)
		{
			if (!matchedIndices[i])
			{
				toRemove.Add(i);
			}
		}

		for (var i = toRemove.Count - 1; i >= 0; i--)
		{
			collection.RemoveAt(toRemove[i]);
		}

		foreach (var item in toAdd)
		{
			collection.Add(item);
		}

		foreach (var pair in toUpdatePairs)
		{
			if (hasChanged?.Invoke(pair.Expected, collection[pair.Index]) == false)
			{
				continue;
			}

			collection[pair.Index] = pair.Expected;
		}

		for (var targetIndex = 0; targetIndex < expectedList.Count; targetIndex++)
		{
			var expectedItem = expectedList[targetIndex];
			var currentIndex = -1;

			for (var i = targetIndex; i < collection.Count; i++)
			{
				if (comparer.Equals(collection[i], expectedItem))
				{
					currentIndex = i;
					break;
				}
			}

			if ((currentIndex != -1) && (currentIndex != targetIndex))
			{
				var itemToMove = collection[currentIndex];
				collection.RemoveAt(currentIndex);
				collection.Insert(targetIndex, itemToMove);
			}
		}
	}

	/// <summary>
	/// Reconciles a collection with an expected collection by adding missing items, updating existing ones, and removing extras.
	/// </summary>
	/// <typeparam name="T"> The type of the collections. </typeparam>
	/// <param name="collection"> The collection to modify. </param>
	/// <param name="expected"> The expected collection state. </param>
	/// <param name="distinctCheck"> </param>
	/// <param name="hasChanged"> The check to see if an item has changed. </param>
	/// <exception cref="ArgumentNullException"> Thrown if <paramref name="collection" /> or <paramref name="expected" /> is null. </exception>
	public static void ReconcileListAndItems<T>(this IPresentationList<T> collection, IEnumerable expected,
		IEqualityComparer<T> distinctCheck = null, Func<T, T, bool> hasChanged = null)
	{
		if (collection == null)
		{
			throw new ArgumentNullException(nameof(collection));
		}

		if (expected == null)
		{
			throw new ArgumentNullException(nameof(expected));
		}

		// Skip null entries — Dictionary does not allow null keys, and concurrent
		// snapshots can leave default slots when a source list shrinks mid-copy.
		var expectedList = expected.Cast<T>().Where(static x => x is not null).ToPresentationList();
		if ((collection.Count == 0) && (expectedList.Count == 0))
		{
			return;
		}

		if (collection.Count == 0)
		{
			collection.Load(expectedList);
			return;
		}

		var comparer = distinctCheck ?? collection.DistinctCheck ?? EqualityComparer<T>.Default;
		var existingMap = new Dictionary<T, int>(comparer);

		for (var i = 0; i < collection.Count; i++)
		{
			var item = collection[i];
			if ((item is null) || existingMap.ContainsKey(item))
			{
				continue;
			}

			existingMap[item] = i;
		}

		var matchedIndices = new bool[collection.Count];
		var toAdd = new List<T>();
		var toUpdatePairs = new List<(T Existing, T Expected)>();
		var toRemove = new List<T>();

		foreach (var expectedItem in expectedList)
		{
			if (existingMap.TryGetValue(expectedItem, out var matchIndex))
			{
				if (!matchedIndices[matchIndex])
				{
					matchedIndices[matchIndex] = true;
					toUpdatePairs.Add((collection[matchIndex], expectedItem));
					continue;
				}
			}

			toAdd.Add(expectedItem);
		}

		for (var i = 0; i < collection.Count; i++)
		{
			if (!matchedIndices[i])
			{
				toRemove.Add(collection[i]);
			}
		}

		var updateSettings = typeof(T).GetIncludeExcludeSettings(UpdateableAction.Updateable);

		collection.ProcessThenOrder(() =>
		{
			foreach (var item in toRemove)
			{
				collection.Remove(item);
			}

			foreach (var item in toAdd)
			{
				collection.Add(item);
			}

			foreach (var pair in toUpdatePairs)
			{
				if (hasChanged?.Invoke(pair.Expected, pair.Existing) == false)
				{
					continue;
				}

				pair.Existing.UpdateWith(pair.Expected, updateSettings);
			}

			if ((collection.OrderBy != null)
				&& !(collection.OrderBy?.Length <= 0))
			{
				return;
			}

			for (var targetIndex = 0; targetIndex < expectedList.Count; targetIndex++)
			{
				var expectedItem = expectedList[targetIndex];

				var currentIndex = -1;
				for (var i = targetIndex; i < collection.Count; i++)
				{
					if (!comparer.Equals(collection[i], expectedItem))
					{
						continue;
					}
					currentIndex = i;
					break;
				}

				if ((currentIndex != -1) && (currentIndex != targetIndex))
				{
					collection.Move(currentIndex, targetIndex);
				}
			}
		});
	}

	/// <summary>
	/// Converts a collection into a PresentationList.
	/// </summary>
	/// <param name="collection"> The collection to convert to a PresentationList. </param>
	/// <param name="dispatcher"> The optional dispatcher to use. </param>
	/// <param name="orderBy"> The optional set of order by settings. </param>
	/// <returns> The PresentationList containing the collection. </returns>
	public static PresentationList<T> ToPresentationList<T>(this IEnumerable<T> collection, IDispatcher dispatcher = null, params OrderBy<T>[] orderBy)
	{
		var response = new PresentationList<T>(dispatcher, orderBy);
		response.Load(collection);
		return response;
	}

	#endregion
}