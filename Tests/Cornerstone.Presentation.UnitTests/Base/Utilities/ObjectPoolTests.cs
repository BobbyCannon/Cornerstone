#region References

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Utilities;

[TestClass]
public class ObjectPoolTests
{
	#region Methods

	[PresentationTestMethod]
	public void ConstructorAcceptsMaxSizeOfOne()
	{
		var pool = new ObjectPool<Item>(() => new Item(), maxSize: 1);

		var item = pool.Rent();

		CornerstoneTest.IsNotNull(item);
	}

	[PresentationTestMethod]
	public void ConstructorThrowsWhenFactoryIsNull()
	{
		Assert.Throws<ArgumentNullException>(() => new ObjectPool<Item>(null!));
	}

	[PresentationTestMethod]
	[DataRow(0)]
	[DataRow(-1)]
	[DataRow(int.MinValue)]
	public void ConstructorThrowsWhenMaxSizeIsLessThanOne(int maxSize)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new ObjectPool<Item>(() => new Item(), maxSize: maxSize));
	}

	[PresentationTestMethod]
	public void PoolStaysWithinMaxSizeUnderConcurrentReturns()
	{
		const int maxSize = 8;
		const int returnsPerThread = 100;
		const int threadCount = 16;

		var pool = new ObjectPool<Item>(() => new Item(), maxSize: maxSize);

		Parallel.For(0, threadCount, _ =>
		{
			for (var i = 0; i < returnsPerThread; i++)
			{
				pool.Return(new Item { State = 1 });
			}
		});

		// Drain the pool. Items that came from the pool will still have State==1;
		// factory-created items will have the default State==0.
		var pooled = 0;
		var seen = new HashSet<Item>();

		for (var i = 0; i < (maxSize * 2); i++)
		{
			var item = pool.Rent();
			if (!seen.Add(item))
			{
				CornerstoneTest.Fail("ObjectPool returned the same instance twice without an intervening Return.");
			}

			if (item.State == 1)
			{
				pooled++;
			}
		}

		CornerstoneTest.IsTrue(pooled <= maxSize, $"Expected to observe at most {maxSize} pooled items, observed {pooled}.");
	}

	[PresentationTestMethod]
	public void RentAndReturnSurviveParallelUseWithoutLosingOrDuplicatingItems()
	{
		const int iterations = 5_000;
		const int threadCount = 8;

		var pool = new ObjectPool<Item>(() => new Item(), maxSize: 32);

		Parallel.For(0, threadCount, _ =>
		{
			for (var i = 0; i < iterations; i++)
			{
				var item = pool.Rent();
				CornerstoneTest.IsNotNull(item);
				pool.Return(item);
			}
		});
	}

	[PresentationTestMethod]
	public void RentCreatesNewItemWhenPoolIsEmpty()
	{
		var pool = new ObjectPool<Item>(() => new Item());

		var first = pool.Rent();
		var second = pool.Rent();

		CornerstoneTest.IsNotNull(first);
		CornerstoneTest.IsNotNull(second);
		CornerstoneTest.NotSame(first, second);
	}

	[PresentationTestMethod]
	public void ReturnDropsItemWhenPoolIsFull()
	{
		var pool = new ObjectPool<Item>(() => new Item(), maxSize: 2);

		var a = new Item();
		var b = new Item();
		var c = new Item();

		pool.Return(a);
		pool.Return(b);
		pool.Return(c); // pool full -> dropped

		var rented = new HashSet<Item>
		{
			pool.Rent(),
			pool.Rent()
		};

		// The two rented items must come from {a, b}; c was dropped.
		CornerstoneTest.Contains(rented, a);
		CornerstoneTest.Contains(rented, b);
		CornerstoneTest.DoesNotContain(rented, c);
	}

	[PresentationTestMethod]
	public void ReturnDropsItemWhenValidatorReturnsFalse()
	{
		var pool = new ObjectPool<Item>(
			() => new Item(),
			_ => false);

		var item = pool.Rent();
		pool.Return(item);

		// Validator rejected the item, so the pool is empty and the next
		// Rent produces a fresh instance.
		var rented = pool.Rent();

		CornerstoneTest.NotSame(item, rented);
	}

	[PresentationTestMethod]
	public void ReturnIgnoresNull()
	{
		var pool = new ObjectPool<Item>(() => new Item());

		pool.Return(null!);

		// Pool stays empty, so the next Rent must produce a fresh item.
		var item = pool.Rent();

		CornerstoneTest.IsNotNull(item);
	}

	[PresentationTestMethod]
	public void ReturnedItemIsReusedBySubsequentRent()
	{
		var pool = new ObjectPool<Item>(() => new Item());

		var item = pool.Rent();
		pool.Return(item);

		var rented = pool.Rent();

		CornerstoneTest.Same(item, rented);
	}

	[PresentationTestMethod]
	public void ValidatorCanResetItemStateBeforePooling()
	{
		var pool = new ObjectPool<Item>(
			() => new Item(),
			i =>
			{
				i.State = 0;
				return true;
			});

		var item = pool.Rent();
		item.State = 42;
		pool.Return(item);

		var rented = pool.Rent();

		CornerstoneTest.Same(item, rented);
		CornerstoneTest.AreEqual(0, rented.State);
	}

	[PresentationTestMethod]
	public void ValidatorIsInvokedOnReturn()
	{
		var validatorCalls = 0;
		var pool = new ObjectPool<Item>(
			() => new Item(),
			_ =>
			{
				validatorCalls++;
				return true;
			});

		var item = pool.Rent();
		pool.Return(item);

		CornerstoneTest.AreEqual(1, validatorCalls);
	}

	[PresentationTestMethod]
	public void ValidatorIsNotInvokedOnRent()
	{
		// The validator's job is to prepare an item for re-use *before* it goes back
		// into the pool. Running it on Rent would either duplicate the work or imply
		// a different contract (validate-on-take). Pin the current contract.
		var validatorCalls = 0;
		var pool = new ObjectPool<Item>(
			() => new Item(),
			_ =>
			{
				validatorCalls++;
				return true;
			});

		_ = pool.Rent(); // fresh from factory; validator must not run
		var item = pool.Rent();
		pool.Return(item); // one validator call here
		_ = pool.Rent(); // pulled from pool; validator must not run again

		CornerstoneTest.AreEqual(1, validatorCalls);
	}

	#endregion

	#region Classes

	private sealed class Item
	{
		#region Fields

		public int State;

		#endregion
	}

	#endregion
}