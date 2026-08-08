#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Convert;

#endregion

namespace Cornerstone.Logging;

/// <summary>
/// Extensions for tracker profiler
/// </summary>
public static class TrackerExtensions
{
	#region Methods

	/// <summary>
	/// Add or update the collection with the path value.
	/// </summary>
	/// <param name="collection"> The collection to update. </param>
	/// <param name="name"> The path name to add or update. </param>
	/// <param name="value"> The path value to add or update. </param>
	public static void AddOrUpdate(this ICollection<TrackerPathValue> collection, string name, object value)
	{
		var sValue = value.TryConvertTo(typeof(string), out var v) ? (string) v : value.ToString();
		AddOrUpdate(collection, name, sValue);
	}

	/// <summary>
	/// Add or update the collection with the path value.
	/// </summary>
	/// <param name="collection"> The collection to update. </param>
	/// <param name="name"> The path name to add or update. </param>
	/// <param name="value"> The path value to add or update. </param>
	public static void AddOrUpdate(this ICollection<TrackerPathValue> collection, string name, string value)
	{
		var foundItem = collection.FirstOrDefault(x => x.Name == name);
		if (foundItem != null)
		{
			foundItem.Value = value ?? string.Empty;
			return;
		}

		collection.Add(new TrackerPathValue(name, value));
	}

	/// <summary>
	/// Add or update the collection with the path value.
	/// </summary>
	/// <param name="collection"> The collection to update. </param>
	/// <param name="pathValue"> The path value to add or update. </param>
	public static void AddOrUpdate(this ICollection<TrackerPathValue> collection, TrackerPathValue pathValue)
	{
		var foundItem = collection.FirstOrDefault(x => x.Name == pathValue.Name);
		if (foundItem != null)
		{
			foundItem.Value = pathValue.Value;
			return;
		}

		collection.Add(pathValue);
	}

	/// <summary>
	/// Adds or updates the item in the collection.
	/// </summary>
	/// <param name="collection"> The collection to be updated. </param>
	/// <param name="items"> The items to be added or updated. </param>
	public static void AddOrUpdate(this ICollection<TrackerPathValue> collection, params TrackerPathValue[] items)
	{
		foreach (var item in items)
		{
			collection.AddOrUpdate(item);
		}
	}

	/// <summary>
	/// Add or update the collection with the path value.
	/// </summary>
	/// <param name="collection"> The collection to update. </param>
	/// <param name="name"> The path name to get. </param>
	/// <param name="defaultValue"> The default value to return if the value is not found. </param>
	/// <returns> The found value otherwise null. </returns>
	public static string GetValue(this ICollection<TrackerPathValue> collection, string name, string defaultValue)
	{
		var foundItem = collection.FirstOrDefault(x => x.Name == name);
		if (foundItem != null)
		{
			return foundItem.Value;
		}

		return defaultValue ?? string.Empty;
	}

	/// <summary>
	/// Add or update the collection with the path value.
	/// </summary>
	/// <param name="collection"> The collection to update. </param>
	/// <param name="name"> The path name to get. </param>
	/// <param name="defaultValue"> The default value to return if the value is not found. </param>
	/// <returns> The found value otherwise null. </returns>
	public static T GetValue<T>(this ICollection<TrackerPathValue> collection, string name, T defaultValue)
	{
		var foundItem = collection.FirstOrDefault(x => x.Name == name);
		if (foundItem != null)
		{
			return foundItem.Value.TryConvertTo<T>(out var result) ? result : defaultValue;
		}

		return defaultValue ?? default;
	}

	#endregion
}