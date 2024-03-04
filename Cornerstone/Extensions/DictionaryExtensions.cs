#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

#endregion

namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for dictionary
/// </summary>
public static class DictionaryExtensions
{
	#region Methods

	/// <summary>
	/// Add a dictionary entry if the key is not found.
	/// </summary>
	/// <typeparam name="T1"> The type of the key. </typeparam>
	/// <typeparam name="T2"> The type of the value. </typeparam>
	/// <param name="dictionary"> The dictionary to update. </param>
	/// <param name="key"> The value of the key. </param>
	/// <param name="action"> The value of the value. </param>
	public static T2 AddIfMissing<T1, T2>(this IDictionary<T1, T2> dictionary, T1 key, Func<T2> action)
	{
		if (dictionary.TryGetValue(key, out var value1))
		{
			return value1;
		}

		var value = action();
		dictionary.Add(key, value);
		return value;
	}

	/// <summary>
	/// Add or update a dictionary entry.
	/// </summary>
	/// <typeparam name="T1"> The type of the key. </typeparam>
	/// <typeparam name="T2"> The type of the value. </typeparam>
	/// <param name="dictionary"> The dictionary to update. </param>
	/// <param name="key"> The value of the key. </param>
	/// <param name="value"> The value of the value. </param>
	[SuppressMessage("ReSharper", "CanSimplifyDictionaryLookupWithTryAdd")]
	public static T2 AddOrUpdate<T1, T2>(this IDictionary<T1, T2> dictionary, T1 key, T2 value)
	{
		if (dictionary.ContainsKey(key))
		{
			dictionary[key] = value;
			return value;
		}

		dictionary.Add(key, value);
		return value;
	}

	/// <summary>
	/// Add or update a dictionary entry if the key is not found in the source.
	/// </summary>
	/// <typeparam name="T1"> The type of the key. </typeparam>
	/// <typeparam name="T2"> The type of the value. </typeparam>
	/// <param name="dictionary"> The dictionary to update. </param>
	/// <param name="key"> The value of the key. </param>
	/// <param name="source"> The dictionary for source values. </param>
	public static void AddOrUpdate<T1, T2>(this IDictionary<T1, T2> dictionary, T1 key, IDictionary<T1, T2> source)
	{
		if (!source.ContainsKey(key))
		{
			return;
		}

		dictionary.AddOrUpdate(key, source[key]);
	}

	/// <summary>
	/// Add or update a dictionary entry.
	/// </summary>
	/// <typeparam name="T1"> The type of the key. </typeparam>
	/// <typeparam name="T2"> The type of the value. </typeparam>
	/// <param name="dictionary"> The dictionary to update. </param>
	/// <param name="key"> The value of the key. </param>
	/// <param name="get"> The function to get the value. </param>
	/// <param name="update"> The function to update the value. </param>
	public static T2 AddOrUpdate<T1, T2>(this IDictionary<T1, T2> dictionary, T1 key, Func<T2> get, Func<T2, T2> update)
	{
		if (dictionary.ContainsKey(key))
		{
			dictionary[key] = update(dictionary[key]);
			return dictionary[key];
		}

		var item = get();
		dictionary.Add(key, item);
		return item;
	}

	/// <summary>
	/// Get a value if the key is found otherwise create a new item, add to dictionary, then return.
	/// </summary>
	/// <typeparam name="T"> The key type. </typeparam>
	/// <typeparam name="T2"> The value type. </typeparam>
	/// <param name="dictionary"> The dictionary to process. </param>
	/// <param name="key"> The key value. </param>
	/// <param name="create"> The function to create a new value. </param>
	/// <returns> The found value or the added value. </returns>
	public static T2 GetOrAdd<T, T2>(this IDictionary<T, T2> dictionary, T key, Func<T, T2> create)
	{
		if (dictionary.TryGetValue(key, out var values))
		{
			return values;
		}

		var response = create(key);
		dictionary.Add(key, response);
		return response;
	}

	/// <summary>
	/// Get a value if the key is found otherwise the default value of T2.
	/// </summary>
	/// <typeparam name="T"> The key type. </typeparam>
	/// <typeparam name="T2"> The value type. </typeparam>
	/// <param name="dictionary"> The dictionary to process. </param>
	/// <param name="key"> The key value. </param>
	/// <returns> The found value or the default value. </returns>
	public static T2 GetOrDefault<T, T2>(this IDictionary<T, T2> dictionary, T key)
	{
		return dictionary.TryGetValue(key, out var value) ? value : default;
	}

	/// <summary>
	/// Get a value if the key is found otherwise the provided default.
	/// </summary>
	/// <typeparam name="T"> The key type. </typeparam>
	/// <typeparam name="T2"> The value type. </typeparam>
	/// <param name="dictionary"> The dictionary to process. </param>
	/// <param name="key"> The key value. </param>
	/// <param name="defaultValue"> The default value if key not found. </param>
	/// <returns> The found value or the default value. </returns>
	public static T2 GetOrDefault<T, T2>(this IDictionary<T, T2> dictionary, T key, T2 defaultValue)
	{
		return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
	}

	/// <summary>
	/// Reconcile one collection with another.
	/// </summary>
	/// <typeparam name="TKey"> The type of the key for the dictionary. </typeparam>
	/// <typeparam name="TValue"> The type of the value for the dictionary. </typeparam>
	/// <param name="dictionary"> The left collection. </param>
	/// <param name="updates"> The right collection. </param>
	public static void Reconcile<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IDictionary<TKey, TValue> updates)
	{
		// Reconcile two collections
		var keysToAdd = updates.Keys.Where(x => !dictionary.ContainsKey(x)).ToList();
		var updateToBeApplied = updates
			.Where(x => dictionary.ContainsKey(x.Key) && !Equals(dictionary[x.Key], updates[x.Key]))
			.ToList();
		var keysToRemove = dictionary.Keys.Where(x => !updates.ContainsKey(x)).ToList();

		foreach (var key in keysToAdd)
		{
			dictionary.Add(key, updates[key]);
		}

		foreach (var updateToApply in updateToBeApplied)
		{
			// todo: support IUpdateable or use reflection?
			// todo: references should not be used?
			dictionary[updateToApply.Key] = updateToApply.Value;
		}

		foreach (var key in keysToRemove)
		{
			dictionary.Remove(key);
		}
	}

	/// <summary>
	/// Just a quick way to convert to a concurrent dictionary.
	/// </summary>
	/// <typeparam name="TSource"> The type of the source. </typeparam>
	/// <typeparam name="TKey"> The type of the key. </typeparam>
	/// <typeparam name="TValue"> The type of the value. </typeparam>
	/// <param name="source"> The source enumeration. </param>
	/// <param name="keySelector"> The selector for the key. </param>
	/// <param name="valueSelector"> The selector for the value. </param>
	/// <returns> The concurrent dictionary. </returns>
	public static ConcurrentDictionary<TKey, TValue> ToConcurrentDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source,
		Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector)
		where TKey : notnull
	{
		return new ConcurrentDictionary<TKey, TValue>(source.ToDictionary(keySelector, valueSelector));
	}

	/// <summary>
	/// Just a quick way to convert to a concurrent dictionary.
	/// </summary>
	/// <typeparam name="TSource"> The type of the source. </typeparam>
	/// <typeparam name="TKey"> The type of the key. </typeparam>
	/// <typeparam name="TValue"> The type of the value. </typeparam>
	/// <param name="source"> The source enumeration. </param>
	/// <param name="keySelector"> The selector for the key. </param>
	/// <param name="valueSelector"> The selector for the value. </param>
	/// <param name="comparer"> The key comparer. </param>
	/// <returns> The concurrent dictionary. </returns>
	public static ConcurrentDictionary<TKey, TValue> ToConcurrentDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source,
		Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector, IEqualityComparer<TKey> comparer)
		where TKey : notnull
	{
		return new ConcurrentDictionary<TKey, TValue>(source.ToDictionary(keySelector, valueSelector, comparer), comparer);
	}

	#endregion
}