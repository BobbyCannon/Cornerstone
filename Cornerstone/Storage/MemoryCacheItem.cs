#region References

using System;

#endregion

namespace Cornerstone.Storage;

/// <summary>
/// Represents an item for a memory cache.
/// </summary>
public class MemoryCacheItem<TKey, TValue>
{
	#region Fields

	private readonly MemoryCache<TKey, TValue> _cache;
	private readonly TimeSpan? _timeout;
	private readonly ITimeProvider _timeService;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a memory cache item.
	/// </summary>
	/// <param name="cache"> The cache this item is for. </param>
	/// <param name="key"> The key of the item. </param>
	/// <param name="value"> The value of the item. </param>
	/// <param name="timeout"> The timeout of the item. </param>
	/// <param name="timeService"> The service to use for date and time. </param>
	public MemoryCacheItem(MemoryCache<TKey, TValue> cache, TKey key, TValue value, TimeSpan? timeout, ITimeProvider timeService)
	{
		_cache = cache;
		_timeout = timeout;
		_timeService = timeService;

		Key = key;
		Value = value;
		CreatedOn = timeService.UtcNow;
		LastAccessed = CreatedOn;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The date and time the cached item was created.
	/// </summary>
	public DateTime CreatedOn { get; }

	/// <summary>
	/// The date and time the item will expire.
	/// </summary>
	public DateTime ExpirationDate =>
		Timeout == TimeSpan.MaxValue
			? DateTime.MaxValue
			: _cache?.SlidingExpiration == true
				? LastAccessed.Add(Timeout)
				: CreatedOn.Add(Timeout);

	/// <summary>
	/// Indicates if the item has expired.
	/// </summary>
	public bool HasExpired => _timeService.UtcNow >= ExpirationDate;

	/// <summary>
	/// The key of the item.
	/// </summary>
	public TKey Key { get; set; }

	/// <summary>
	/// The last time the item was accessed.
	/// </summary>
	public DateTime LastAccessed { get; set; }

	/// <summary>
	/// The timeout value of the item.
	/// </summary>
	public TimeSpan Timeout => _timeout ?? _cache.DefaultTimeout;

	/// <summary>
	/// The value of the item.
	/// </summary>
	public TValue Value { get; set; }

	#endregion
}