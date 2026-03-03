#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cornerstone.Collections;
using Cornerstone.Compare;
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
	/// Reconciles a collection with an expected collection by adding missing items, updating existing ones, and removing extras.
	/// </summary>
	/// <typeparam name="T"> The type of the collections. </typeparam>
	/// <param name="collection"> The collection to modify. </param>
	/// <param name="expected"> The expected collection state. </param>
	/// <exception cref="ArgumentNullException"> Thrown if <paramref name="collection" /> or <paramref name="expected" /> is null. </exception>
	public static void Reconcile<T>(this ISpeedyList<T> collection, IEnumerable expected)
	{
		if (collection == null)
		{
			throw new ArgumentNullException(nameof(collection));
		}
		if (expected == null)
		{
			throw new ArgumentNullException(nameof(expected));
		}
		
		var expectedList = expected.Cast<T>().ToSpeedyList();
		if ((collection.Count == 0) && (expectedList.Count == 0))
		{
			return;
		}

		if (collection.Count == 0)
		{
			collection.Load(expectedList);
			return;
		}

		var comparer = collection.DistinctCheck ?? EqualityComparer<T>.Default;
		var toAdd = expectedList.Except(collection, comparer).ToList();
		var toUpdate = expectedList.Intersect(collection, comparer).ToList();
		var toRemove = collection.Except(expectedList, comparer).ToList();
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

			foreach (var update in toUpdate)
			{
				var existing = collection.FirstOrDefault(c => comparer.Equals(c, update));
				if (existing == null)
				{
					collection.Add(update);
					continue;
				}

				existing.UpdateWith(update, updateSettings);
			}
		});
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

	#endregion
}