#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Backends.Skia;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia;

[TestClass]
public class TwoLevelCacheTests
{
	#region Methods

	[PresentationTestMethod]
	public void ClearAndDisposeClearsAllEntries()
	{
		var cache = new TwoLevelCache<string, object>(2);

		cache.GetOrAdd("key1", _ => new object());
		cache.GetOrAdd("key2", _ => new object());
		cache.ClearAndDispose();

		CornerstoneTest.IsFalse(cache.TryGet("key1", out _));
		CornerstoneTest.IsFalse(cache.TryGet("key2", out _));
	}

	[PresentationTestMethod]
	public void ClearAndDisposeEmptyCacheDoesNotThrow()
	{
		var cache = new TwoLevelCache<string, object>();
		cache.ClearAndDispose();
	}

	[PresentationTestMethod]
	public void ClearAndDisposeWithValuesCallsEvictionActionForAll()
	{
		var evictedValues = new List<object>();
		var cache = new TwoLevelCache<string, object>(
			2,
			v => evictedValues.Add(v));

		var value1 = new object();
		var value2 = new object();
		var value3 = new object();

		cache.GetOrAdd("key1", _ => value1);
		cache.GetOrAdd("key2", _ => value2);
		cache.GetOrAdd("key3", _ => value3);

		cache.ClearAndDispose();

		CornerstoneTest.AreEqual(3, evictedValues.Count);
		CornerstoneTest.Contains(evictedValues, value1);
		CornerstoneTest.Contains(evictedValues, value2);
		CornerstoneTest.Contains(evictedValues, value3);
	}

	[PresentationTestMethod]
	public void ConstructorWithNegativeSecondarySizeThrowsArgumentOutOfRangeException()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new TwoLevelCache<string, object>(-1));
	}

	[PresentationTestMethod]
	public void ConstructorWithZeroSecondarySizeDoesNotThrow()
	{
		var cache = new TwoLevelCache<string, object>(0);
		CornerstoneTest.IsNotNull(cache);
	}

	[PresentationTestMethod]
	public void FactoryFunctionReceivesCorrectKey()
	{
		var cache = new TwoLevelCache<string, string>();
		string capturedKey = null;

		cache.GetOrAdd("testKey", key =>
		{
			capturedKey = key;
			return "value";
		});

		CornerstoneTest.AreEqual("testKey", capturedKey);
	}

	[PresentationTestMethod]
	public void GetOrAddDuplicateKeyInSecondaryReturnsExistingWithoutCallingFactory()
	{
		var cache = new TwoLevelCache<string, object>(2);
		var value1 = new object();
		var value2 = new object();
		var factoryCalled = false;

		cache.GetOrAdd("key1", _ => value1);
		cache.GetOrAdd("key2", _ => value2);

		// Try to add key2 again - factory should not be called
		var result = cache.GetOrAdd("key2", _ =>
		{
			factoryCalled = true;
			return new object();
		});

		CornerstoneTest.Same(value2, result);
		CornerstoneTest.IsFalse(factoryCalled);
	}

	[PresentationTestMethod]
	public void GetOrAddDuplicateKeyReturnsExistingWithoutCallingFactory()
	{
		var cache = new TwoLevelCache<string, object>();
		var value1 = new object();
		var factoryCalled = false;

		// Add initial value
		cache.GetOrAdd("key", _ => value1);

		// Try to add again - factory should not be called
		var result = cache.GetOrAdd("key", _ =>
		{
			factoryCalled = true;
			return new object();
		});

		// Should return first value without calling factory
		CornerstoneTest.Same(value1, result);
		CornerstoneTest.IsFalse(factoryCalled);
	}

	[PresentationTestMethod]
	public void GetOrAddExceedsCapacityCallsEvictionAction()
	{
		var evictedValues = new List<object>();
		var cache = new TwoLevelCache<string, object>(
			2,
			v => evictedValues.Add(v));

		var value1 = new object();
		var value2 = new object();
		var value3 = new object();
		var value4 = new object();

		cache.GetOrAdd("key1", _ => value1);
		cache.GetOrAdd("key2", _ => value2);
		cache.GetOrAdd("key3", _ => value3);

		// No evictions yet
		CornerstoneTest.Empty(evictedValues);

		// This should cause eviction
		cache.GetOrAdd("key4", _ => value4);

		CornerstoneTest.Single(evictedValues);
		CornerstoneTest.Same(value2, evictedValues[0]);
	}

	[PresentationTestMethod]
	public void GetOrAddFirstItemStoresInPrimary()
	{
		var cache = new TwoLevelCache<string, object>();
		var value = new object();

		var result = cache.GetOrAdd("key1", _ => value);

		CornerstoneTest.Same(value, result);
		CornerstoneTest.IsTrue(cache.TryGet("key1", out var retrieved));
		CornerstoneTest.Same(value, retrieved);
	}

	[PresentationTestMethod]
	public void GetOrAddIntKeysWorksCorrectly()
	{
		var cache = new TwoLevelCache<int, object>(2);

		var value1 = new object();
		var value2 = new object();
		var value3 = new object();

		cache.GetOrAdd(1, _ => value1);
		cache.GetOrAdd(2, _ => value2);
		cache.GetOrAdd(3, _ => value3);

		CornerstoneTest.IsTrue(cache.TryGet(1, out var retrieved1));
		CornerstoneTest.Same(value1, retrieved1);
		CornerstoneTest.IsTrue(cache.TryGet(2, out var retrieved2));
		CornerstoneTest.Same(value2, retrieved2);
		CornerstoneTest.IsTrue(cache.TryGet(3, out var retrieved3));
		CornerstoneTest.Same(value3, retrieved3);
	}

	[PresentationTestMethod]
	public void GetOrAddMultipleItemsStoresCorrectly()
	{
		var cache = new TwoLevelCache<string, object>(3);
		var values = new object[4];
		for (var i = 0; i < 4; i++)
		{
			values[i] = new object();
			cache.GetOrAdd($"key{i}", _ => values[i]);
		}

		// All should be retrievable
		for (var i = 0; i < 4; i++)
		{
			CornerstoneTest.IsTrue(cache.TryGet($"key{i}", out var retrieved));
			CornerstoneTest.Same(values[i], retrieved);
		}
	}

	[PresentationTestMethod]
	public void GetOrAddNullEvictionActionDoesNotThrow()
	{
		var cache = new TwoLevelCache<string, object>(
			1,
			null);

		cache.GetOrAdd("key1", _ => new object());
		cache.GetOrAdd("key2", _ => new object());
		cache.GetOrAdd("key3", _ => new object()); // Should evict without error

		cache.ClearAndDispose(); // Should also not throw
	}

	[PresentationTestMethod]
	public void GetOrAddRotatesSecondaryCorrectly()
	{
		var evictedValues = new List<object>();
		var cache = new TwoLevelCache<int, object>(
			2,
			v => evictedValues.Add(v));

		var values = new object[5];
		for (var i = 0; i < 5; i++)
		{
			values[i] = new object();
			cache.GetOrAdd(i, _ => values[i]);
		}

		// Primary: 0, Secondary: [1, 2]
		// After adding 3: Primary: 0, Secondary: [3, 1] (evicts 2)
		// After adding 4: Primary: 0, Secondary: [4, 3] (evicts 1)

		CornerstoneTest.AreEqual(2, evictedValues.Count);
		CornerstoneTest.Contains(evictedValues, values[2]);
		CornerstoneTest.Contains(evictedValues, values[1]);

		// These should still be in cache
		CornerstoneTest.IsTrue(cache.TryGet(0, out _));
		CornerstoneTest.IsTrue(cache.TryGet(3, out _));
		CornerstoneTest.IsTrue(cache.TryGet(4, out _));

		// These should be evicted
		CornerstoneTest.IsFalse(cache.TryGet(1, out _));
		CornerstoneTest.IsFalse(cache.TryGet(2, out _));
	}

	[PresentationTestMethod]
	public void GetOrAddSameKeyReturnsExistingValue()
	{
		var cache = new TwoLevelCache<string, object>();
		var value1 = new object();
		var value2 = new object();

		cache.GetOrAdd("key", _ => value1);
		var result = cache.GetOrAdd("key", _ => value2);

		CornerstoneTest.Same(value1, result);
	}

	[PresentationTestMethod]
	public void GetOrAddSecondItemStoresInSecondary()
	{
		var cache = new TwoLevelCache<string, object>(3);
		var value1 = new object();
		var value2 = new object();

		cache.GetOrAdd("key1", _ => value1);
		cache.GetOrAdd("key2", _ => value2);

		CornerstoneTest.IsTrue(cache.TryGet("key1", out var retrieved1));
		CornerstoneTest.Same(value1, retrieved1);
		CornerstoneTest.IsTrue(cache.TryGet("key2", out var retrieved2));
		CornerstoneTest.Same(value2, retrieved2);
	}

	[PresentationTestMethod]
	public void GetOrAddWithCustomComparerUsesComparer()
	{
		var comparer = StringComparer.OrdinalIgnoreCase;
		var cache = new TwoLevelCache<string, object>(comparer: comparer);

		var value = new object();
		cache.GetOrAdd("KEY", _ => value);

		CornerstoneTest.IsTrue(cache.TryGet("key", out var retrieved));
		CornerstoneTest.Same(value, retrieved);
	}

	[PresentationTestMethod]
	public void GetOrAddZeroSecondarySizeEvictsPrimaryImmediately()
	{
		var evictedValues = new List<object>();
		var cache = new TwoLevelCache<string, object>(
			0,
			v => evictedValues.Add(v));

		var value1 = new object();
		var value2 = new object();

		cache.GetOrAdd("key1", _ => value1);
		cache.GetOrAdd("key2", _ => value2);

		CornerstoneTest.Single(evictedValues);
		CornerstoneTest.Same(value1, evictedValues[0]);

		// Only the latest value should be retrievable
		CornerstoneTest.IsFalse(cache.TryGet("key1", out _));
		CornerstoneTest.IsTrue(cache.TryGet("key2", out var retrieved));
		CornerstoneTest.Same(value2, retrieved);
	}

	[PresentationTestMethod]
	public void TryGetEmptyCacheReturnsFalse()
	{
		var cache = new TwoLevelCache<string, object>();

		var result = cache.TryGet("key", out var value);

		CornerstoneTest.IsFalse(result);
		CornerstoneTest.IsNull(value);
	}

	[PresentationTestMethod]
	public void TryGetWithCustomComparerUsesComparer()
	{
		var comparer = StringComparer.OrdinalIgnoreCase;
		var cache = new TwoLevelCache<string, object>(
			2,
			comparer: comparer);

		var value1 = new object();
		var value2 = new object();

		cache.GetOrAdd("PRIMARY", _ => value1);
		cache.GetOrAdd("SECONDARY", _ => value2);

		CornerstoneTest.IsTrue(cache.TryGet("primary", out var retrieved1));
		CornerstoneTest.Same(value1, retrieved1);
		CornerstoneTest.IsTrue(cache.TryGet("secondary", out var retrieved2));
		CornerstoneTest.Same(value2, retrieved2);
	}

	#endregion
}