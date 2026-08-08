#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Sync;

#endregion

namespace Cornerstone.Collections;

public static class HierarchyExtensions
{
	#region Methods

	public static T1[] Order<T1>(T1[] items)
		where T1 : IHierarchySyncItem
	{
		// Create a dictionary for quick lookup
		var lookup = items.ToDictionary(x => x.SyncId, x => x);

		// Group items by Parent
		var byParent = items
			.GroupBy(x => x.ParentSyncId)
			.ToDictionary(x => x.Key ?? Guid.Empty, x => x.OrderBy(c => c.Order).ToList());

		var result = new List<T1>();

		// Start with root items (ParentId is null)
		if (byParent.TryGetValue(Guid.Empty, out var value))
		{
			foreach (var root in value)
			{
				AddItemAndChildren(root, byParent, result);
			}
		}

		// Handle orphaned items (items with non-existent Parent)
		foreach (var item in items)
		{
			if (!result.Contains(item) && !lookup.ContainsKey(item.ParentSyncId ?? Guid.Empty))
			{
				AddItemAndChildren(item, byParent, result);
			}
		}

		return result.ToArray();
	}

	private static void AddItemAndChildren<T1>(T1 item, Dictionary<Guid, List<T1>> byParent, List<T1> result)
		where T1 : IHierarchySyncItem
	{
		// Add the current item
		result.Add(item);

		// Add its children recursively
		if (byParent.TryGetValue(item.SyncId, out var value))
		{
			foreach (var child in value)
			{
				AddItemAndChildren(child, byParent, result);
			}
		}
	}

	#endregion
}