#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Collections;

/// <summary>
/// Extensions for SpeedyTree collections.
/// </summary>
public static class SpeedyTreeExtensions
{
	#region Methods

	/// <summary>
	/// Reconciles a tree with an expected collection by adding missing items, updating existing ones, and removing extras recursively.
	/// </summary>
	/// <typeparam name="T"> The type of the tree nodes. </typeparam>
	/// <param name="collection"> The tree to modify. </param>
	/// <param name="expected"> The expected collection state for the children. </param>
	/// <param name="distinctCheck"> </param>
	/// <param name="hasChanged"> The check to see if an item has changed. </param>
	/// <exception cref="ArgumentNullException"> Thrown if <paramref name="collection" /> or <paramref name="expected" /> is null. </exception>
	public static void Reconcile<T>(this ISpeedyTree<T> collection, IEnumerable expected,
		EqualityComparer<T> distinctCheck = null, Func<T, T, bool> hasChanged = null)
		where T : class, ISpeedyTree<T>
	{
		if (collection == null)
		{
			throw new ArgumentNullException(nameof(collection));
		}

		if (expected == null)
		{
			throw new ArgumentNullException(nameof(expected));
		}

		var expectedItems = expected.Cast<T>().ToList();

		collection.Children.ReconcileListAndItems(expectedItems, distinctCheck, hasChanged);

		var comparer = distinctCheck ?? collection.DistinctCheck ?? EqualityComparer<T>.Default;

		foreach (var expectedItem in expectedItems)
		{
			var actualItem = collection.Children.FirstOrDefault(child => comparer.Equals(child, expectedItem));
			if ((actualItem == null) || (expectedItem.Children == null))
			{
				continue;
			}

			Reconcile(actualItem, expectedItem.Children, distinctCheck, hasChanged);
		}
	}

	#endregion
}