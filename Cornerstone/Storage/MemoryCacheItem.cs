#region References

using System;
using Cornerstone.Runtime;

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
	private readonly IDateTimeProvider _timeProvider;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a memory cache item.
	/// </summary>
	/// <param name="cache"> The cache this item is for. </param>
	/// <param name="key"> The key of the item. </param>
	/// <param name="value"> The value of the item. </param>
	/// <param name="timeout"> The timeout of the item. </param>
	/// <param name="timeProvider"> The service to use for date and time. </param>
	public MemoryCacheItem(MemoryCache<TKey, TValue> cache, TKey key, TValue value, TimeSpan? timeout, IDateTimeProvider timeProvider)
	{
		_cache = cache;
		_timeout = timeout;
		_timeProvider = timeProvider;

		Key = key;
		Value = value;
		CreatedOn = timeProvider.UtcNow;
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
	public bool HasExpired => _timeProvider.UtcNow >= ExpirationDate;

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