#region References

using System;
using System.Collections.Generic;
using Cornerstone.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Collections;

[TestClass]
public class SpeedyQueueTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ClearResetsQueueToEmptyStateWhilePreservingCapacity()
	{
		var queue = new SpeedyQueue<char>(16);

		queue.Enqueue('x');
		queue.Enqueue('y');
		queue.Enqueue('z');

		queue.Clear();

		AreEqual(0, queue.Count);
		IsTrue(queue.IsEmpty);
		AreEqual(queue.Capacity, 16);
		IsFalse(queue.TryDequeue(out _));
	}

	[TestMethod]
	public void ConstructorWithCapacitySmallerThanFourThrowsArgumentException()
	{
		ExpectedException<ArgumentException>(() => _ = new SpeedyQueue<double>(2));
	}

	[TestMethod]
	public void ConstructorWithNonPowerOfTwoCapacityThrowsArgumentException()
	{
		ExpectedException<ArgumentException>(() => _ = new SpeedyQueue<int>(10));
		ExpectedException<ArgumentException>(() => _ = new SpeedyQueue<int>(100));
	}

	[TestMethod]
	public void ConstructorWithValidPowerOfTwoCapacityCreatesQueueWithExpectedCapacity()
	{
		var queue = new SpeedyQueue<string>(16);
		AreEqual(queue.Capacity, 16);
		AreEqual(0, queue.Count);
		IsTrue(queue.IsEmpty);
	}

	[TestMethod]
	public void EnqueueEmptyData()
	{
		var queue = new SpeedyQueue<string>(8);
		queue.Enqueue(new ReadOnlySpan<string>());
		AreEqual(0, queue.Count);
		queue.Enqueue([]);
		AreEqual(0, queue.Count);
	}

	[TestMethod]
	public void EnqueueManyDequeueReturnsItemsInSameOrder()
	{
		var queue = new SpeedyQueue<string>(8);
		queue.Enqueue(["first", "second", "third"]);

		IsTrue(queue.TryDequeue(out var v1));
		AreEqual(v1, "first");

		IsTrue(queue.TryDequeue(out var v2));
		AreEqual(v2, "second");

		IsTrue(queue.TryDequeue(out var v3));
		AreEqual(v3, "third");

		AreEqual(0, queue.Count);
	}

	[TestMethod]
	public void EnqueueManyItemsDequeueReturnsItemsInSameOrder()
	{
		var queue = new SpeedyQueue<string>(8);
		queue.Enqueue(["first", "second", "third"]);

		IsTrue(queue.TryDequeue(out var v1));
		AreEqual(v1, "first");

		IsTrue(queue.TryDequeue(out var v2));
		AreEqual(v2, "second");

		IsTrue(queue.TryDequeue(out var v3));
		AreEqual(v3, "third");

		AreEqual(0, queue.Count);
	}

	[TestMethod]
	public void EnqueueManyWhenLimitReachedAndGrowthDisabledDropsOldestItem()
	{
		var queue = new SpeedyQueue<string>(16, 4);
		queue.Enqueue(["A", "B", "C", "D", "E", "F"]);

		var items = new List<string>();
		while (queue.TryDequeue(out var item))
		{
			items.Add(item);
		}

		AreEqual(["C", "D", "E", "F"], items.ToArray());
		AreEqual(0, queue.Count);
	}

	[TestMethod]
	public void EnqueueMultipleItemsDequeueReturnsItemsInSameOrder()
	{
		var queue = new SpeedyQueue<string>(8);
		queue.Enqueue("first");
		queue.Enqueue("second");
		queue.Enqueue("third");

		IsTrue(queue.TryDequeue(out var v1));
		AreEqual(v1, "first");

		IsTrue(queue.TryDequeue(out var v2));
		AreEqual(v2, "second");

		IsTrue(queue.TryDequeue(out var v3));
		AreEqual(v3, "third");

		AreEqual(0, queue.Count);
	}

	[TestMethod]
	public void EnqueueSingleItemIncreasesCountAndMakesQueueNotEmpty()
	{
		var queue = new SpeedyQueue<int>(8);
		queue.Enqueue(42);

		AreEqual(queue.Count, 1);
		IsFalse(queue.IsEmpty);
	}

	[TestMethod]
	public void EnqueueWhenLimitReachedAndGrowthDisabledDropsOldestItem()
	{
		var queue = new SpeedyQueue<string>(16, 4);

		queue.Enqueue("A");
		queue.Enqueue("B");
		queue.Enqueue("C");
		queue.Enqueue("D");
		queue.Enqueue("E"); // drops A
		queue.Enqueue("F"); // drops B

		var items = new List<string>();
		while (queue.TryDequeue(out var item))
		{
			items.Add(item);
		}

		AreEqual(["C", "D", "E", "F"], items.ToArray());
		AreEqual(0, queue.Count);
	}

	[TestMethod]
	public void EnqueueWhenLimitReachedShouldWrap()
	{
		var queue = new SpeedyQueue<int>(8, 3);

		queue.Enqueue(1);
		queue.Enqueue(2);
		queue.Enqueue(3);
		queue.Enqueue(4);

		AreEqual(queue.Count, 3);

		queue.TryDequeue(out _);
		queue.TryDequeue(out _);
		queue.TryDequeue(out _);

		AreEqual(queue.Count, 0);
	}

	[TestMethod]
	public void LimitShouldIncreaseCapacity()
	{
		var queue = new SpeedyQueue<int>(8, 16);

		for (var i = 0; i < 8; i++)
		{
			queue.Enqueue(i);
		}

		AreEqual(8, queue.Capacity);
		AreEqual(16, queue.MaxItems);
		AreEqual(8, queue.Count);

		// should trigger growth
		queue.Enqueue(8);

		AreEqual(16, queue.Capacity);
		AreEqual(16, queue.MaxItems);
		AreEqual(9, queue.Count);
	}

	[TestMethod]
	public void LimitShouldNotIncreaseCapacity()
	{
		var queue = new SpeedyQueue<int>(8, 8);

		for (var i = 0; i < 8; i++)
		{
			queue.Enqueue(i);
		}

		AreEqual(8, queue.Capacity);
		AreEqual(8, queue.MaxItems);
		AreEqual(8, queue.Count);

		// should not trigger grow, should be limited
		queue.Enqueue(8);

		AreEqual(8, queue.Capacity);
		AreEqual(8, queue.MaxItems);
		AreEqual(8, queue.Count);
	}

	[TestMethod]
	public void QueueChangedEventIsRaisedAfterEnqueueDequeueAndClear()
	{
		var queue = new SpeedyQueue<int>(16);
		var eventCount = 0;

		queue.QueueChanged += (_, _) => eventCount++;

		queue.Enqueue(100);
		AreEqual(eventCount, 1);

		queue.Enqueue(200);
		AreEqual(eventCount, 2);

		queue.TryDequeue(out _);
		AreEqual(eventCount, 3);

		queue.Clear();
		AreEqual(eventCount, 4);
	}

	[TestMethod]
	public void SettingLimitLowerThanCurrentCountDiscardsOldestItems()
	{
		var queue = new SpeedyQueue<int>(32);

		for (var i = 1; i <= 10; i++)
		{
			queue.Enqueue(i);
		}

		queue.MaxItems = 4;

		AreEqual(queue.Count, 4);

		var values = new List<int>();
		while (queue.TryDequeue(out var v))
		{
			values.Add(v);
		}

		AreEqual([7, 8, 9, 10], values.ToArray());
	}

	[TestMethod]
	public void TryDequeueOnEmptyQueueReturnsFalseAndDefaultValue()
	{
		var queue = new SpeedyQueue<int>(16);
		var success = queue.TryDequeue(out var value);

		IsFalse(success);
		AreEqual(value, 0);
		AreEqual(0, queue.Count);
	}

	#endregion
}