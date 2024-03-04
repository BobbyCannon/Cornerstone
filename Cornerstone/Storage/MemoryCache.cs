#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Storage;

/// <summary>
/// Represent a memory cache.
/// </summary>
public class MemoryCache : MemoryCache<string, object>
{
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
	/// <param name="timeService"> The service to use for date and time. </param>
	public MemoryCache(TimeSpan defaultTimeout, ITimeProvider timeService = null)
		: base(defaultTimeout, timeService)
	{
	}

	#endregion
}

/// <summary>
/// Represent a memory cache.
/// </summary>
public class MemoryCache<TKey, TValue> : ICollection<MemoryCacheItem<TKey, TValue>>, ICollection, INotifyCollectionChanged
{
	#region Fields

	private readonly Dictionary<TKey, MemoryCacheItem<TKey, TValue>> _dictionary;
	private readonly ITimeProvider _timeService;

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
	/// <param name="timeService"> The service to use for date and time. </param>
	public MemoryCache(TimeSpan defaultTimeout, ITimeProvider timeService = null)
	{
		_timeService = timeService ?? TimeService.CurrentTime;
		_dictionary = new Dictionary<TKey, MemoryCacheItem<TKey, TValue>>();

		DefaultTimeout = defaultTimeout;
		SlidingExpiration = true;
		SyncRoot = new object();
	}

	#endregion

	#region Properties

	/// <inheritdoc cref="ICollection" />
	public int Count
	{
		get
		{
			lock (SyncRoot)
			{
				return _dictionary.Count(x => !x.Value.HasExpired);
			}
		}
	}

	/// <summary>
	/// The default timeout for items when they are added.
	/// </summary>
	public TimeSpan DefaultTimeout { get; }

	/// <summary>
	/// Indicates whether or not the memory cache is empty.
	/// </summary>
	public bool IsEmpty
	{
		get
		{
			lock (SyncRoot)
			{
				return (_dictionary.Count <= 0)
					|| _dictionary.All(x => x.Value.HasExpired);
			}
		}
	}

	/// <inheritdoc />
	public bool IsReadOnly => false;

	/// <inheritdoc />
	public bool IsSynchronized => true;

	/// <summary>
	/// Determines if the expiration time should be extended when read from the cache.
	/// </summary>
	public bool SlidingExpiration { get; set; }

	/// <inheritdoc />
	public object SyncRoot { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	[Obsolete("Use Set method instead")]
	public void Add(MemoryCacheItem<TKey, TValue> item)
	{
	}

	/// <summary>
	/// Cleanup the cache by removing expired entries.
	/// </summary>
	public void Cleanup()
	{
		lock (SyncRoot)
		{
			var toRemove = new List<MemoryCacheItem<TKey, TValue>>();
			toRemove.AddRange(this.Where(x => x.HasExpired));
			toRemove.ForEach(x => Remove(x));
		}
	}

	/// <inheritdoc />
	public void Clear()
	{
		lock (SyncRoot)
		{
			var values = _dictionary.Values.ToArray();
			_dictionary.Clear();
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, values, 0));
		}
	}

	/// <inheritdoc />
	public bool Contains(MemoryCacheItem<TKey, TValue> item)
	{
		lock (SyncRoot)
		{
			return _dictionary.ContainsValue(item);
		}
	}

	/// <inheritdoc />
	public void CopyTo(MemoryCacheItem<TKey, TValue>[] array, int arrayIndex)
	{
		lock (SyncRoot)
		{
			Array.Copy(_dictionary.Values.ToArray(), 0, array, arrayIndex, _dictionary.Count);
		}
	}

	/// <inheritdoc />
	public void CopyTo(Array array, int index)
	{
		lock (SyncRoot)
		{
			Array.Copy(_dictionary.Values.ToArray(), 0, array, index, _dictionary.Count);
		}
	}

	/// <summary>
	/// Enumerator for the memory cache.
	/// </summary>
	/// <returns> The enumerator for the collection. </returns>
	/// <remarks>
	/// Enumeration should NOT be considered "accessing" items
	/// We only bump last accessed by direct access. This allows
	/// Enumeration of the item to check expiration
	/// </remarks>
	public IEnumerator<MemoryCacheItem<TKey, TValue>> GetEnumerator()
	{
		lock (SyncRoot)
		{
			foreach (var value in _dictionary)
			{
				// See remark in method summary.
				//value.Value.LastAccessed = TimeService.Instance.UtcNow;
				yield return value.Value;
			}
		}
	}

	/// <summary>
	/// Determine if the cache contains the provided key.
	/// </summary>
	/// <param name="key"> The key to check. </param>
	/// <returns> True if the key exists otherwise false. </returns>
	public bool HasKey(TKey key)
	{
		lock (SyncRoot)
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
		MemoryCacheItem<TKey, TValue> response;

		lock (SyncRoot)
		{
			if (!_dictionary.ContainsKey(key))
			{
				return default;
			}

			response = _dictionary[key];
			_dictionary.Remove(key);
		}

		OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, response));
		return response;
	}

	/// <summary>
	/// Remove the entry from the cache.
	/// </summary>
	/// <param name="memoryCacheItem"> The item to remove from the cache. </param>
	public MemoryCacheItem<TKey, TValue> Remove(MemoryCacheItem<TKey, TValue> memoryCacheItem)
	{
		return memoryCacheItem == null
			? null
			: Remove(memoryCacheItem.Key);
	}

	/// <summary>
	/// Set a new entry with a custom timeout. This will add a new entry or update an existing one.
	/// </summary>
	/// <param name="key"> The key of the entry. </param>
	/// <param name="value"> The value of the entry. </param>
	/// <param name="timeout"> The custom timeout of the entry. </param>
	public void Set(TKey key, TValue value, TimeSpan? timeout = null)
	{
		MemoryCacheItem<TKey, TValue> item;
		var updated = false;

		lock (SyncRoot)
		{
			item = _dictionary.AddOrUpdate(key,
				() => new MemoryCacheItem<TKey, TValue>(this, key, value, timeout, _timeService),
				x =>
				{
					updated = true;
					x.Key = key;
					x.Value = value;
					x.LastAccessed = _timeService.UtcNow;
					return x;
				}
			);
		}

		if (!updated)
		{
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
		}
	}

	/// <summary>
	/// Try to get an entry from the cache.
	/// </summary>
	/// <param name="key"> The key of the entry. </param>
	/// <param name="value"> The entry that was found or otherwise null. </param>
	/// <returns> True if the entry was found or otherwise false. </returns>
	public bool TryGet(TKey key, out MemoryCacheItem<TKey, TValue> value)
	{
		lock (SyncRoot)
		{
			if (!_dictionary.TryGetValue(key, out var cachedItem))
			{
				value = null;
				return false;
			}

			if (cachedItem.ExpirationDate <= _timeService.UtcNow)
			{
				value = null;
				Remove(cachedItem);
				return false;
			}

			value = cachedItem;
			value.LastAccessed = _timeService.UtcNow;
			return true;
		}
	}

	/// <summary>
	/// Called when the collection changed.
	/// </summary>
	/// <param name="args"> The changed event. </param>
	protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
	{
		CollectionChanged?.Invoke(this, args);
	}

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	bool ICollection<MemoryCacheItem<TKey, TValue>>.Remove(MemoryCacheItem<TKey, TValue> item)
	{
		var removed = Remove(item);
		return removed == item;
	}

	#endregion

	#region Events

	/// <inheritdoc />
	public event NotifyCollectionChangedEventHandler CollectionChanged;

	#endregion
}