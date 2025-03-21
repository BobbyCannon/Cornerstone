#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Storage;

/// <summary>
/// Represent a memory cache.
/// </summary>
public class MemoryCache<TKey, TValue>
{
	#region Fields

	private readonly SpeedyDictionary<TKey, MemoryCacheItem<TKey, TValue>> _dictionary;
	private readonly IDateTimeProvider _timeProvider;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a memory cache with a default 15 timeout.
	/// </summary>
	public MemoryCache() : this(TimeSpan.FromMinutes(15))
	{
	}

	/// <summary>
	/// Initializes a memory cache.
	/// </summary>
	/// <param name="defaultTimeout"> The default timeout of new entries. </param>
	/// <param name="timeProvider"> The service to use for date and time. </param>
	public MemoryCache(TimeSpan defaultTimeout, IDateTimeProvider timeProvider = null)
	{
		_timeProvider = timeProvider ?? DateTimeProvider.RealTime;
		_dictionary = new SpeedyDictionary<TKey, MemoryCacheItem<TKey, TValue>>();

		DefaultTimeout = defaultTimeout;
		SlidingExpiration = true;
	}

	#endregion

	#region Properties

	/// <inheritdoc cref="ICollection" />
	public int Count
	{
		get { return _dictionary.Count(x => !x.Value.HasExpired); }
	}

	/// <summary>
	/// The default timeout for items when they are added.
	/// </summary>
	public TimeSpan DefaultTimeout { get; }

	/// <summary>
	/// Indicates if the memory cache is empty.
	/// </summary>
	public bool IsEmpty
	{
		get
		{
			return (_dictionary.Count <= 0)
				|| _dictionary.All(x => x.Value.HasExpired);
		}
	}

	/// <summary>
	/// Determines if the expiration time should be extended when read from the cache.
	/// </summary>
	public bool SlidingExpiration { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Set a new entry with a custom timeout. This will add a new entry or update an existing one.
	/// </summary>
	/// <param name="key"> The key of the entry. </param>
	/// <param name="value"> The value of the entry. </param>
	/// <param name="timeout"> The custom timeout of the entry. </param>
	public void AddOrUpdate(TKey key, TValue value, TimeSpan? timeout = null)
	{
		_dictionary.AddOrUpdate(key,
			() => new MemoryCacheItem<TKey, TValue>(this, key, value, timeout, _timeProvider),
			x =>
			{
				x.Key = key;
				x.Value = value;
				x.LastAccessed = _timeProvider.UtcNow;
				return x;
			}
		);
	}

	/// <summary>
	/// Cleanup the cache by removing expired entries.
	/// </summary>
	public void Cleanup()
	{
		var toRemove = new List<MemoryCacheItem<TKey, TValue>>();
		toRemove.AddRange(_dictionary.Values.Where(x => x.HasExpired));
		toRemove.ForEach(x => _dictionary.Remove(x.Key));
	}

	public void Clear()
	{
		_dictionary.Clear();
	}

	/// <summary>
	/// Determine if the cache contains the provided key.
	/// </summary>
	/// <param name="key"> The key to check. </param>
	/// <returns> True if the key exists otherwise false. </returns>
	public bool HasKey(TKey key)
	{
		{
			return _dictionary.ContainsKey(key);
		}
	}

	/// <summary>
	/// Remove an entry by the key name.
	/// </summary>
	/// <param name="key"> The name of the key. </param>
	public MemoryCacheItem<TKey, TValue> Remove(TKey key)
	{
		if (!_dictionary.TryGetValue(key, out var value))
		{
			return null;
		}

		var response = value;
		_dictionary.Remove(key);

		return response;
	}

	/// <summary>
	/// Try to get an entry from the cache.
	/// </summary>
	/// <param name="key"> The key of the entry. </param>
	/// <param name="value"> The entry that was found or otherwise null. </param>
	/// <returns> True if the entry was found or otherwise false. </returns>
	public bool TryGet(TKey key, out MemoryCacheItem<TKey, TValue> value)
	{
		if (!_dictionary.TryGetValue(key, out var cachedItem))
		{
			value = null;
			return false;
		}

		if (cachedItem.ExpirationDate <= _timeProvider.UtcNow)
		{
			value = null;
			Remove(cachedItem.Key);
			return false;
		}

		value = cachedItem;
		value.LastAccessed = _timeProvider.UtcNow;
		return true;
	}

	#endregion
}