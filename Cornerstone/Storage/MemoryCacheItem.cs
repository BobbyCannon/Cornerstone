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

	private readonly TimeSpan _defaultTimeoutValue;
	private readonly bool _isSlidingExpiration;
	private readonly TimeSpan? _timeout;
	private readonly IDateTimeProvider _timeProvider;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a memory cache item.
	/// </summary>
	/// <param name="key"> The key of the item. </param>
	/// <param name="value"> The value of the item. </param>
	/// <param name="timeout"> The timeout of the item. </param>
	/// <param name="timeProvider"> The service to use for date and time. </param>
	/// <param name="isSlidingExpiration"> Indicates if sliding expiration is enabled. </param>
	/// <param name="defaultTimeoutValue"> The default timeout value. </param>
	public MemoryCacheItem(TKey key, TValue value, TimeSpan? timeout, IDateTimeProvider timeProvider, bool isSlidingExpiration, TimeSpan defaultTimeoutValue)
	{
		Key = key;
		Value = value;
		_timeout = timeout;
		_timeProvider = timeProvider;
		_isSlidingExpiration = isSlidingExpiration;
		_defaultTimeoutValue = defaultTimeoutValue;

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
	public DateTime ExpirationDate
	{
		get
		{
			var effectiveTimeout = _timeout ?? _defaultTimeoutValue;
			if (effectiveTimeout == TimeSpan.MaxValue)
			{
				return DateTime.MaxValue;
			}

			return _isSlidingExpiration
				? LastAccessed.Add(effectiveTimeout)
				: CreatedOn.Add(effectiveTimeout);
		}
	}

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
	public TimeSpan Timeout => _timeout ?? _defaultTimeoutValue;

	/// <summary>
	/// The value of the item.
	/// </summary>
	public TValue Value { get; set; }

	#endregion
}