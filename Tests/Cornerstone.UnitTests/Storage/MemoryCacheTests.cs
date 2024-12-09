#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Storage;

[TestClass]
public class MemoryCacheTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CacheShouldExpire()
	{
		var cache = new MemoryCache<string, object>(TimeSpan.FromSeconds(1), this);
		var count = 0;

		try
		{
			cache.CollectionChanged += OnCacheOnItemRemoved;
			cache.Set("foo", "bar");
			IsTrue(cache.TryGet("foo", out var item));
			AreEqual("bar", (string) item.Value);

			// Bump to expiration date
			IncrementTime(TimeSpan.FromSeconds(1));
			IsFalse(cache.TryGet("foo", out item));
			IsNull(item);
			AreEqual(1, count);
		}
		finally
		{
			cache.CollectionChanged -= OnCacheOnItemRemoved;
		}

		return;

		void OnCacheOnItemRemoved(object sender, NotifyCollectionChangedEventArgs args)
		{
			if (args.Action == NotifyCollectionChangedAction.Remove)
			{
				count++;
			}
		}
	}

	[TestMethod]
	public void CleanUpShouldWork()
	{
		var count = 0;
		var cache = new MemoryCache<string, object>(TimeSpan.FromSeconds(10), this);

		try
		{
			cache.CollectionChanged += OnCollectionChanged;
			cache.Set("foo", "bar");
			cache.Set("hello", "world");

			AreEqual(2, cache.Count);
			AreEqual(2, count);

			IncrementTime(seconds: 9);

			cache.Cleanup();
			AreEqual(2, cache.Count);

			IncrementTime(seconds: 1);

			cache.Cleanup();
			AreEqual(0, cache.Count);
			AreEqual(4, count);
		}
		finally
		{
			cache.CollectionChanged -= OnCollectionChanged;
		}

		return;

		void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
		{
			count += args.NewItems?.Count ?? 0;
			count += args.OldItems?.Count ?? 0;
		}
	}

	[TestMethod]
	public void ClearShouldWork()
	{
		var count = 0;
		var cache = new MemoryCache<string, object>(TimeSpan.FromSeconds(10), this);

		try
		{
			cache.CollectionChanged += OnCollectionChanged;
			cache.Set("foo", "bar");
			cache.Set("hello", "world");

			AreEqual(2, cache.Count);
			AreEqual(2, count);

			cache.Clear();

			AreEqual(0, cache.Count);
			AreEqual(0, cache.ToArray().Length);
			AreEqual(4, count);
		}
		finally
		{
			cache.CollectionChanged -= OnCollectionChanged;
		}

		return;

		void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
		{
			count += args.NewItems?.Count ?? 0;
			count += args.OldItems?.Count ?? 0;
		}
	}

	[TestMethod]
	public void Constructor()
	{
		var cache = new MemoryCache<string, object>();
		IsTrue(cache.IsEmpty);
		IsTrue(cache.IsSynchronized);
		IsTrue(cache.SlidingExpiration);
		AreEqual(TimeSpan.FromMinutes(15), cache.DefaultTimeout);

		cache = new MemoryCache<string, object>(TimeSpan.FromSeconds(1), this);
		IsTrue(cache.IsEmpty);
		IsTrue(cache.IsSynchronized);
		IsTrue(cache.SlidingExpiration);
		AreEqual(TimeSpan.FromSeconds(1), cache.DefaultTimeout);
	}

	[TestMethod]
	public void CopyTo()
	{
		var array = new MemoryCacheItem<string, object>[2];
		var cache = new MemoryCache<string, object>(TimeSpan.FromSeconds(10), this);
		cache.Set("foo", "bar");
		cache.Set("hello", "world");

		AreEqual(2, cache.Count);

		cache.CopyTo(array, 0);

		AreEqual(cache.ToArray(), array);

		cache.CopyTo((Array) array, 0);

		AreEqual(cache.ToArray(), array);
	}

	[TestMethod]
	public void EnumeratorShouldWork()
	{
		var cache = new MemoryCache<string, object>(TimeSpan.FromSeconds(10), this);
		IsTrue(cache.SlidingExpiration);
		cache.Set("foo", "bar");
		IncrementTime(seconds: 1);
		cache.Set("hello", "world");
		var actual = cache.ToArray();
		AreEqual(2, actual.Length);

		// Enumerating the cache will not change access time therefore will not effect expiration date.
		AreEqual(new DateTime(2000, 01, 02, 03, 04, 10), actual[0].ExpirationDate);
		AreEqual(new DateTime(2000, 01, 02, 03, 04, 11), actual[1].ExpirationDate);

		IncrementTime(seconds: 2);

		var count = 0;
		foreach (MemoryCacheItem<string, object> item in (IEnumerable) cache)
		{
			AreEqual(new DateTime(2000, 01, 02, 03, 04, 10 + count), item.ExpirationDate);
			count++;
		}
		AreEqual(2, count);
	}

	[TestMethod]
	public void ExpiredEntriesShouldBeCleanedUp()
	{
		var cache = new MemoryCache<string, object>(TimeSpan.FromSeconds(10), this);
		cache.Set("foo", "bar");
		IncrementTime(seconds: 1);
		cache.Set("hello", "world");
		IsFalse(cache.IsEmpty);
		AreEqual(2, cache.Count);
		IncrementTime(seconds: 10);
		var internalDictionary = cache.GetMemberValue("_dictionary") as Dictionary<string, MemoryCacheItem<string, object>>;
		IsNotNull(internalDictionary);
		AreEqual(2, internalDictionary?.Count);
		cache.Cleanup();
		AreEqual(0, internalDictionary?.Count);
	}

	[TestMethod]
	public void ExpiredEntriesShouldBeIgnored()
	{
		var cache = new MemoryCache<string, object>(TimeSpan.FromSeconds(10), this);
		cache.Set("foo", "bar");
		IncrementTime(seconds: 1);
		cache.Set("hello", "world");
		IsFalse(cache.IsEmpty);
		AreEqual(2, cache.Count);
		IncrementTime(seconds: 10);
		IsTrue(cache.IsEmpty);
		AreEqual(0, cache.Count);
	}

	[TestMethod]
	public void HasKey()
	{
		var cache = new MemoryCache<string, object>(TimeSpan.FromSeconds(10), this);
		IsFalse(cache.HasKey("foo"));
		cache.Set("foo", "bar");
		IsTrue(cache.HasKey("foo"));
	}
	
	[TestMethod]
	public void Contains()
	{
		var cache = new MemoryCache<string, object>(TimeSpan.FromSeconds(10), this);
		IsFalse(cache.Contains(new MemoryCacheItem<string, object>(cache, "foo", "bar", TimeSpan.Zero, this)));
		cache.Set("foo", "bar");
		IsTrue(cache.HasKey("foo"));
	}

	[TestMethod]
	public void RemoveShouldWork()
	{
		var cache = new MemoryCache<string, object>(TimeSpan.FromSeconds(10), this);
		cache.Set("foo", "bar");
		cache.Set("hello", "world");

		AreEqual(2, cache.Count);

		var actual = cache.Remove("foo");
		IsNotNull(actual);
		AreEqual("foo", actual.Key);

		AreEqual(1, cache.Count);
		AreEqual("hello", cache.First().Key);
	}

	[TestMethod]
	public void RemoveShouldWorkWithInvalidKey()
	{
		var cache = new MemoryCache<string, object>(TimeSpan.FromSeconds(10), this);
		AreEqual(0, cache.Count);

		// Remove key is not available so [default] (null) should be returned
		var actual = cache.Remove("foo");
		IsNull(actual);
	}

	[TestMethod]
	public void SetMultipleCallsShouldUpdateNotAdd()
	{
		var cache = new MemoryCache<string, object>(TimeSpan.FromSeconds(10), this);
		cache.Set("foo", "bar");

		AreEqual(1, cache.Count);
		IsTrue(cache.TryGet("foo", out var item));
		AreEqual("bar", (string) item.Value);

		IncrementTime(seconds: 1);
		cache.Set("foo", "bar2");

		AreEqual(1, cache.Count);
		IsTrue(cache.TryGet("foo", out item));
		AreEqual("bar2", (string) item.Value);
	}

	[TestMethod]
	public void SlidingExpirationDisabledShouldNotAffectItems()
	{
		var cache = new MemoryCache<string, object>(TimeSpan.FromSeconds(10), this)
		{
			SlidingExpiration = false
		};
		IsFalse(cache.SlidingExpiration);
		AreEqual(0, cache.Count);

		cache.Set("foo", "bar");
		IsTrue(cache.TryGet("foo", out var item));
		AreEqual("bar", (string) item.Value);
		AreEqual(new DateTime(2000, 01, 02, 03, 04, 10), item.ExpirationDate);
		AreEqual(1, cache.Count);

		// Accessing the value should not bump expiration
		IncrementTime(seconds: 5);
		cache.TryGet("foo", out item);
		AreEqual(new DateTime(2000, 01, 02, 03, 04, 10), item.ExpirationDate);
		IsFalse(item.HasExpired);
		AreEqual(1, cache.Count);

		// Bump to expiration date
		IncrementTime(seconds: 5);
		IsFalse(cache.TryGet("foo", out item));
		IsNull(item);
		AreEqual(0, cache.Count);
	}

	[TestMethod]
	public void SlidingExpirationShouldWork()
	{
		var cache = new MemoryCache<string, object>(TimeSpan.FromSeconds(10), this);
		IsTrue(cache.SlidingExpiration);

		cache.Set("foo", "bar");
		IsTrue(cache.TryGet("foo", out var item));
		AreEqual("bar", (string) item.Value);
		AreEqual(new DateTime(2000, 01, 02, 03, 04, 10), item.ExpirationDate);

		// Accessing the value should bump expiration
		IncrementTime(seconds: 5);
		cache.TryGet("foo", out item);
		AreEqual(new DateTime(2000, 01, 02, 03, 04, 15), item.ExpirationDate);
		IsFalse(item.HasExpired);

		// Bump to expiration date
		IncrementTime(seconds: 10);
		IsFalse(cache.TryGet("foo", out item));
		IsNull(item);
	}

	[TestMethod]
	public void TryGetInvalidKey()
	{
		var cache = new MemoryCache<string, object>(TimeSpan.FromSeconds(10), this);
		cache.Set("foo", "bar");
		cache.Set("hello", "world");

		AreEqual(2, cache.Count);

		var actual = cache.TryGet("aoeu", out var item);
		IsFalse(actual);
		IsNull(item);
	}

	#endregion
}