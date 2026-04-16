#region References

using System;
using System.Collections;
using System.Text;
using Cornerstone.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Collections;

[TestClass]
public class SpeedyListTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AddArgumentExceptions()
	{
		using var buffer = new SpeedyList<char>(16);
		var data = "123456".ToCharArray();
		ExpectedException<ArgumentNullException>(() => buffer.Add(null, 0, 1));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.Add(data, -1, 1));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.Add(data, 0, 0));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.Add(data, 6, 1));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.Add(data, 5, 2));
	}

	[TestMethod]
	public void AddArrayOverloadsThrowCorrectExceptions()
	{
		using var buffer = new SpeedyList<int>(16);
		int[] data = [1, 2, 3];

		ExpectedException<ArgumentNullException>(() => buffer.Add(null, 0, 1));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.Add(data, -1, 1));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.Add(data, 0, 0));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.Add(data, 2, 2)); // offset + count > length
	}

	[TestMethod]
	public void AddArraySegmentCopiesCorrectlyAndAdvancesPosition()
	{
		using var buffer = new SpeedyList<int>(16);
		int[] data = [10, 20, 30, 40, 50];
		buffer.Add(data, 1, 3);
		AreEqual(3, buffer.Count);
		AreEqual(20, buffer[0]);
		AreEqual(30, buffer[1]);
		AreEqual(40, buffer[2]);
		AreEqual("Cornerstone.Collections.SpeedyList`1[System.Int32]", buffer.ToString());
	}

	[TestMethod]
	public void AddManyItemsThatTriggerMultipleResizes()
	{
		ForEachList<int>(buffer =>
		{
			for (var i = 0; i < 10000; i++)
			{
				buffer.Add(i);
			}

			AreEqual(10000, buffer.Count);
			AreEqual(10000, buffer.ToArray().Length);
		}, 16);
	}

	[TestMethod]
	public void AddReadOnlySpanCopiesCorrectly()
	{
		using var buffer = new SpeedyList<char>(32);
		ReadOnlySpan<char> span = ['H', 'e', 'l', 'l', 'o'];
		buffer.Add(span);
		AreEqual(5, buffer.Count);
		AreEqual(['H', 'e', 'l', 'l', 'o'], buffer.AsSpan().ToArray());
		AreEqual("Hello", buffer.ToString());
	}

	[TestMethod]
	public void AddSingleItemIncreasesLengthAndPosition()
	{
		using var buffer = new SpeedyList<int>(8);
		buffer.Add(42);
		AreEqual(1, buffer.Count);
		AreEqual(42, buffer[0]);
	}

	[TestMethod]
	public void AddSpanWithResizeGrowsCorrectlyAndCopiesData()
	{
		ForEachList<int>(buffer =>
		{
			// Fill to capacity
			for (var i = 0; i < 16; i++)
			{
				buffer.Add(i);
			}

			AreEqual(16, buffer.Count);
			AreEqual(16, buffer.Capacity);

			// This will trigger AddSpanWithResize
			buffer.Add(new[] { 100, 200, 300, 400 });

			AreEqual(20, buffer.Count);
			AreEqual(32, buffer.Capacity); // doubled
			AreEqual(100, buffer[16]);
			AreEqual(400, buffer[19]);
		}, 16);
	}

	[TestMethod]
	public void AsSpan()
	{
		using var buffer = new SpeedyList<byte>(32);
		var data = "ABCDEF"u8.ToArray();
		buffer.Add(data);
		var span = buffer.AsSpan();
		AreEqual(6, span.Length);
		AreEqual(data, span.ToArray());

		span = buffer.AsSpan(0);
		AreEqual(6, span.Length);
		AreEqual("ABCDEF"u8.ToArray(), span.ToArray());
		
		span = buffer.AsSpan(3);
		AreEqual(3, span.Length);
		AreEqual("DEF"u8.ToArray(), span.ToArray());
		
		span = buffer.AsSpan(0, 3);
		AreEqual(3, span.Length);
		AreEqual("ABC"u8.ToArray(), span.ToArray());

		span = buffer.AsSpan(4, 2);
		AreEqual(2, span.Length);
		AreEqual("EF"u8.ToArray(), span.ToArray());
	}

	[TestMethod]
	public void AsSpanArgumentChecks()
	{
		using var buffer = new SpeedyList<char>(32);
		buffer.Add("ABCDEF");

		ExpectedException<ArgumentOutOfRangeException>(() => buffer.AsSpan(-1, 2));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.AsSpan(0, -1));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.AsSpan(0, 0));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.AsSpan(0, 7));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.AsSpan(1, 6));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.AsSpan(5, 2));
	}

	[TestMethod]
	public void AsSpanReturnsCorrectSlice()
	{
		ForEachList<byte>(buffer =>
		{
			buffer.Add("Hello World"u8.ToArray());

			// Full span
			var full = buffer.AsSpan();
			AreEqual(11, full.Length);
			AreEqual("Hello World"u8.ToArray(), full.ToArray());

			// From start with length
			var slice1 = buffer.AsSpan(0, 5);
			AreEqual(5, slice1.Length);
			AreEqual("Hello"u8.ToArray(), slice1.ToArray());

			// From middle
			var slice2 = buffer.AsSpan(6, 5);
			AreEqual(5, slice2.Length);
			AreEqual("World"u8.ToArray(), slice2.ToArray());
		});
	}

	[TestMethod]
	public void AsSpanThrowsOnInvalidArguments()
	{
		using var buffer = new SpeedyList<char>(32);
		buffer.Add("ABCDEF");

		ExpectedException<ArgumentOutOfRangeException>(() => buffer.AsSpan(-1));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.AsSpan(0, -1));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.AsSpan(0, 0)); // length == 0 is now invalid per your code
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.AsSpan(7, 1));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.AsSpan(4, 3)); // 4+3 > 6
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.AsSpan(10));
	}

	[TestMethod]
	public void ClearResetsLengthAndPosition()
	{
		using var buffer = new SpeedyList<int>(16);
		buffer.Add([1, 2, 3, 4]);
		buffer.Clear();
		AreEqual(0, buffer.Count);
		AreEqual(buffer.Capacity, buffer.Remaining);
	}

	[TestMethod]
	public void ClearRespectsClearOnCleanupFlag()
	{
		// Test with value type that should be cleared
		ForEachList<int>(buffer =>
		{
			buffer.Add([1, 2, 3, 4]);
			buffer.Clear();

			// After clear, the underlying buffer slots should be defaulted only if _clearOnCleanup was true
			// (hard to assert directly without internals, but you can check behavior on reuse)
		}, includeClearing: true, includeNoClearing: true);
	}

	[TestMethod]
	public void ConstructorWithCustomCapacityCreatesBufferWithRequestedSize()
	{
		using var buffer = new SpeedyList<int>(4000);
		AreEqual(4096, buffer.Capacity);
		AreEqual(0, buffer.Count);
	}

	[TestMethod]
	public void ConstructorWithDefaultCapacityCreatesBufferWithDefaultSize()
	{
		using var buffer = new SpeedyList<int>();
		AreEqual(SpeedyList.DefaultCapacity, buffer.Capacity);
		AreEqual(0, buffer.Count);
		AreEqual(SpeedyList.DefaultCapacity, buffer.Remaining);
	}

	[TestMethod]
	public void ConstructorWithNegativeCapacityThrowsArgumentOutOfRangeException()
	{
		ExpectedException<ArgumentOutOfRangeException>(() => _ = new SpeedyList<int>(-1));
	}

	[TestMethod]
	public void Contains()
	{
		using var buffer = new SpeedyList<int>(16);
		buffer.Add([1, 2, 3, 4]);
		IsTrue(buffer.Contains(2));
		IsTrue(buffer.Contains(4));
		IsFalse(buffer.Contains(0));
		IsFalse(buffer.Contains(5));
		IsFalse(buffer.Contains(-1));
	}

	[TestMethod]
	public void CopyTo()
	{
		using var buffer = new SpeedyList<int>(32);
		buffer.Add([10, 20, 30, 40, 50]);
		var dest = new int[5];
		buffer.CopyTo(dest, 0);
		AreEqual(0, buffer.ReadPosition);
		AreEqual(5, buffer.WritePosition);
		AreEqual([10, 20, 30, 40, 50], dest);
	}

	[TestMethod]
	public void CopyToArgumentsCheck()
	{
		using var buffer = new SpeedyList<int>(32);
		var destination = new int[10];
		ExpectedException<ArgumentNullException>(() => buffer.CopyTo(null, 0));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.CopyTo(destination, -1));
		buffer.Add([1, 2, 3, 4, 5]);
		ExpectedException<ArgumentException>(
			() => buffer.CopyTo(destination, 6),
			Babel.Tower[BabelKeys.ArgumentTooSmall]
		);
	}

	[TestMethod]
	public void Dispose()
	{
		var buffer = new SpeedyList<int>(16);
		buffer.Dispose();
		buffer.Dispose();
		buffer.Dispose();
	}

	[TestMethod]
	public void DisposeReturnsRentedBufferToPool()
	{
		// This is tricky to assert without internals, but you can test reuse behavior
		var buffer = new SpeedyList<byte>(32, false);
		buffer.Add([1, 2, 3]);
		buffer.Dispose();

		// Subsequent use should still work (new instance)
		using var newBuffer = new SpeedyList<byte>(32, false);
		newBuffer.Add(42);
		AreEqual(1, newBuffer.Count);
	}

	[TestMethod]
	public void EnsureCapacityDoesNotShrink()
	{
		using var buffer = new SpeedyList<int>(32);
		buffer.EnsureCapacity(100);
		AreEqual(128, buffer.Capacity);

		buffer.EnsureCapacity(50); // should not shrink
		AreEqual(128, buffer.Capacity);
	}

	[TestMethod]
	public void GetEnumerator()
	{
		using var buffer = new SpeedyList<char>(32);
		buffer.Add("Hello");
		var actual = buffer.GetEnumerator();
		var index = 0;
		while (actual.MoveNext())
		{
			AreEqual(buffer[index++], actual.Current);
		}

		index = 0;
		var enumerable = (IEnumerable) buffer;
		foreach (var item in enumerable)
		{
			AreEqual(buffer[index++], item);
		}
	}

	[TestMethod]
	public void GetWriteSpanAdvancesWritePositionAndAllowsDirectWrite()
	{
		using var buffer = new SpeedyList<int>(8);
		var span = buffer.GetWriteSpan(5);

		AreEqual(5, buffer.Count);
		AreEqual(5, span.Length);

		for (var i = 0; i < span.Length; i++)
		{
			span[i] = i * 10;
		}

		AreEqual([0, 10, 20, 30, 40], buffer.ToArray());
	}

	[TestMethod]
	public void GetWriteSpanTriggersGrowthWhenNeeded()
	{
		ForEachList<int>(buffer =>
		{
			// Fill almost to capacity
			for (var i = 0; i < 15; i++)
			{
				buffer.Add(i);
			}

			var span = buffer.GetWriteSpan(10);

			AreEqual(25, buffer.Count);
			AreEqual(32, buffer.Capacity);

			span[^1] = 9999; // write to last element
			AreEqual(9999, buffer[24]);
		}, 16);
	}

	[TestMethod]
	public void GetWriteSpanWritesDirectlyAndAdvancesCount()
	{
		using var buffer = new SpeedyList<int>(8);

		var span = buffer.GetWriteSpan(5);
		AreEqual(5, buffer.Count);
		AreEqual(5, span.Length);

		span[0] = 100;
		span[1] = 200;
		span[2] = 300;
		span[3] = 400;
		span[4] = 500;

		AreEqual([100, 200, 300, 400, 500], buffer.ToArray());
	}

	[TestMethod]
	public void GrowDoublesCapacityWhenNeeded()
	{
		ForEachList<int>(buffer =>
		{
			for (var i = 0; i < buffer.Capacity; i++)
			{
				buffer.Add(i);
			}

			var oldCapacity = buffer.Capacity;
			buffer.Add(999);

			AreEqual(oldCapacity * 2, buffer.Capacity);
			AreEqual(oldCapacity + 1, buffer.Count);
			AreEqual(999, buffer[oldCapacity]);
		});
	}

	[TestMethod]
	public void Indexer()
	{
		using var buffer = new SpeedyList<int>(16);
		buffer.Add([1, 2, 3, 4]);
		AreEqual(1, buffer[0]);
		AreEqual(4, buffer[3]);
		buffer[3] = 9;
		AreEqual(9, buffer[3]);
	}

	[TestMethod]
	public void IndexerThrowsOnOutOfRange()
	{
		using var buffer = new SpeedyList<int>(16);
		buffer.Add([1, 2]);

		ExpectedException<IndexOutOfRangeException>(() => _ = buffer[-1]);
		ExpectedException<IndexOutOfRangeException>(() => _ = buffer[2]);
		ExpectedException<IndexOutOfRangeException>(() => buffer[5] = 99);
	}

	[TestMethod]
	public void IndexOf()
	{
		using var buffer = new SpeedyList<int>(16);
		buffer.Add([1, 2, 3, 4]);
		AreEqual(0, buffer.IndexOf(1));
		AreEqual(1, buffer.IndexOf(2));
		AreEqual(2, buffer.IndexOf(3));
		AreEqual(3, buffer.IndexOf(4));
		AreEqual(-1, buffer.IndexOf(5));
		AreEqual(-1, buffer.IndexOf(0));
	}

	[TestMethod]
	public void InsertArgumentChecks()
	{
		using var buffer = new SpeedyList<int>(16);
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.Insert(17, 1));
	}

	[TestMethod]
	public void InsertRangeAtBeginningMiddleAndEnd()
	{
		ForEachList<int>(buffer =>
		{
			buffer.Add([10, 20, 30, 40]);
			buffer.Insert(0, [1, 2, 3]); // beginning
			AreEqual([1, 2, 3, 10, 20, 30, 40], buffer.ToArray());
			buffer.Insert(4, [99]); // middle
			AreEqual([1, 2, 3, 10, 99, 20, 30, 40], buffer.ToArray());
			buffer.Insert(buffer.Count, [50, 60]); // end
			AreEqual([1, 2, 3, 10, 99, 20, 30, 40, 50, 60], buffer.ToArray());
		});
	}

	[TestMethod]
	public void InsertRangeEmptySpanDoesNothing()
	{
		using var buffer = new SpeedyList<int>(16);
		buffer.Add(42);
		buffer.Insert(0, ReadOnlySpan<int>.Empty);
		AreEqual(1, buffer.Count);
		AreEqual(42, buffer[0]);
	}

	[TestMethod]
	public void InsertRangeThrowsOnInvalidIndex()
	{
		using var buffer = new SpeedyList<int>(16);
		buffer.Add([10, 20]);

		ExpectedException<ArgumentOutOfRangeException>(() => buffer.Insert(3, new[] { 1, 2 }.AsSpan()));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.Insert(-1, new[] { 1, 2 }.AsSpan()));
	}

	[TestMethod]
	public void InsertShouldTriggerResizeWorkCorrectly()
	{
		ForEachList<int>(buffer =>
		{
			AreEqual(16, buffer.Capacity);
			for (var i = 0; i < 16; i++)
			{
				buffer.Insert(0, i);
			}
			AreEqual(16, buffer.Capacity);
			buffer.Insert(0, 17); // triggers resize
			AreEqual(17, buffer.Count);
			AreEqual(32, buffer.Capacity);
		}, 16);
	}

	[TestMethod]
	public void InsertThrowsOnInvalidIndex()
	{
		using var buffer = new SpeedyList<int>(16);
		buffer.Add([10, 20, 30]);

		ExpectedException<ArgumentOutOfRangeException>(() => buffer.Insert(-1, 99));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.Insert(4, 99)); // one past end is NOT allowed for single Insert
	}

	[TestMethod]
	public void IsReadOnly()
	{
		using var buffer = new SpeedyList<int>(16);
		IsFalse(buffer.IsReadOnly);
	}

	[TestMethod]
	public void MultipleAddsThatTriggerResizeWorkCorrectly()
	{
		ForEachList<int>(buffer =>
		{
			AreEqual(16, buffer.Capacity);
			for (var i = 0; i < 16; i++)
			{
				buffer.Add(i + 1);
			}
			AreEqual(16, buffer.Capacity);
			buffer.Add(17); // triggers resize
			AreEqual(17, buffer.Count);
			AreEqual(32, buffer.Capacity);
		}, 16);
	}

	[TestMethod]
	public void Read_IndexAndLength_ReturnsCorrectSlice()
	{
		using var buffer = new SpeedyList<int>(32);
		buffer.Add([10, 20, 30, 40, 50, 60, 70]);

		var slice = buffer.Read(2, 3); // should return [30, 40, 50]
		AreEqual(3, slice.Length);
		AreEqual([30, 40, 50], slice);

		// Edge: read from start
		slice = buffer.Read(0, 2);
		AreEqual([10, 20], slice);

		// Edge: read to end
		slice = buffer.Read(5, 2);
		AreEqual([60, 70], slice);
	}

	[TestMethod]
	public void ReadArgumentChecks()
	{
		using var buffer = new SpeedyList<int>(16);
		var destination = new int[16];
		ExpectedException<ArgumentNullException>(() => buffer.Read(0, null, 0, 0));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.Read(-1, destination, 0, 0));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.Read(0, destination, -1, 0));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.Read(0, destination, 0, -1));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.Read(0, destination, 16, 2));
		destination = new int[32];
		ExpectedException<ArgumentOutOfRangeException>(
			() => buffer.Read(0, destination, 0, 17),
			Babel.Tower[BabelKeys.IndexAndLengthOutOfRange]
		);
	}

	[TestMethod]
	public void ReadIndexAndLengthThrowsOnInvalidArguments()
	{
		using var buffer = new SpeedyList<int>(32);
		buffer.Add([1, 2, 3, 4, 5]);

		ExpectedException<ArgumentOutOfRangeException>(() => buffer.Read(-1, 2));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.Read(0, -1));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.Read(0, 0));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.Read(3, 10)); // exceeds count
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.Read(6, 1));
	}

	[TestMethod]
	public void ReadPositionDoesNotAffectCountOrIndexer()
	{
		using var buffer = new SpeedyList<int>(16);
		buffer.Add([10, 20, 30, 40, 50]);
		buffer.Seek(2);
		AreEqual(2, buffer.ReadPosition);
		AreEqual(5, buffer.Count); // Count unchanged
		AreEqual(5, buffer.WritePosition);
		AreEqual(30, buffer.Read()); // advances ReadPosition
		AreEqual(3, buffer.ReadPosition);
		AreEqual(40, buffer[3]); // indexer still works on full data
	}

	[TestMethod]
	public void ReadSingleItemReturnsCorrectValueAndAdvancesPosition()
	{
		using var buffer = new SpeedyList<int>(16);
		buffer.Add(100);
		buffer.Add(200);
		buffer.Add(300);
		var value = buffer.Read();
		AreEqual(100, value);
		AreEqual(1, buffer.ReadPosition);
		AreEqual(3, buffer.WritePosition);
		AreEqual(3, buffer.Count);
		AreEqual(200, buffer.Read());
		AreEqual(2, buffer.ReadPosition);
		AreEqual(3, buffer.WritePosition);
		AreEqual(3, buffer.Count);
	}

	[TestMethod]
	public void ReadToArrayCopiesCorrectAmount()
	{
		using var buffer = new SpeedyList<int>(32);
		buffer.Add([10, 20, 30, 40, 50]);
		var dest = new int[10];
		buffer.Read(2, dest, 5, 3);
		AreEqual(5, buffer.ReadPosition);
		AreEqual(5, buffer.WritePosition);
		AreEqual([0, 0, 0, 0, 0, 30, 40, 50, 0, 0], dest);
	}

	[TestMethod]
	public void ReadWhenPositionAtEndThrowsInvalidOperationException()
	{
		using var buffer = new SpeedyList<int>(8);
		buffer.Add(1);
		buffer.Seek(0);
		buffer.Read();
		ExpectedException<InvalidOperationException>(() => buffer.Read());
	}

	[TestMethod]
	public void RemoveAt()
	{
		using var buffer = new SpeedyList<int>(32);
		buffer.Add([10, 20, 30, 40, 50]);
		AreEqual([10, 20, 30, 40, 50], buffer.ToArray());
		AreEqual(0, buffer.ReadPosition);
		AreEqual(5, buffer.WritePosition);
		buffer.RemoveAt(1);
		AreEqual([10, 30, 40, 50], buffer.ToArray());
		AreEqual(0, buffer.ReadPosition);
		AreEqual(4, buffer.WritePosition);

		buffer.RemoveAt(2);
		AreEqual([10, 30, 50], buffer.ToArray());
		AreEqual(0, buffer.ReadPosition);
		AreEqual(3, buffer.WritePosition);
	}

	[TestMethod]
	public void RemoveAtArgumentChecks()
	{
		using var buffer = new SpeedyList<int>(32);
		buffer.Add([10, 20, 30, 40, 50]);

		ExpectedException<ArgumentOutOfRangeException>(() => buffer.RemoveAt(-1));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.RemoveAt(5));
	}

	[TestMethod]
	public void RemoveItemShouldOnlyRemoveOne()
	{
		ForEachList<int>(buffer =>
		{
			buffer.Add([10, 20, 30, 40, 50]);
			AreEqual([10, 20, 30, 40, 50], buffer.ToArray());
			AreEqual(0, buffer.ReadPosition);
			AreEqual(5, buffer.WritePosition);
			IsTrue(buffer.Remove(20));
			AreEqual([10, 30, 40, 50], buffer.ToArray());
			AreEqual(0, buffer.ReadPosition);
			AreEqual(4, buffer.WritePosition);
			IsFalse(buffer.Remove(20));
			AreEqual([10, 30, 40, 50], buffer.ToArray());
			AreEqual(0, buffer.ReadPosition);
			AreEqual(4, buffer.WritePosition);
		});
	}

	[TestMethod]
	public void RemoveRangeArgumentChecks()
	{
		using var buffer = new SpeedyList<int>(32);
		buffer.Add([1, 2, 3, 4, 5]);

		ExpectedException<ArgumentOutOfRangeException>(() => buffer.RemoveRange(-1, 1));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.RemoveRange(0, -1));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.RemoveRange(3, 10)); // too many
	}

	[TestMethod]
	public void RemoveRangeEmptyRangeOnEmptyListDoesNotThrow()
	{
		using var buffer = new SpeedyList<int>(16);
		buffer.RemoveRange(0, 0); // should not throw even on empty list
		AreEqual(0, buffer.Count);
	}

	[TestMethod]
	public void RemoveRangeThrowsOnInvalidArguments()
	{
		using var buffer = new SpeedyList<int>(32);
		buffer.Add([10, 20, 30, 40, 50]);

		ExpectedException<ArgumentOutOfRangeException>(() => buffer.RemoveRange(-1, 2));
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.RemoveRange(0, -1));
		// this is valid, should it not be?
		//ExpectedException<ArgumentOutOfRangeException>(() => buffer.RemoveRange(0, 0)); // zero length
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.RemoveRange(3, 10)); // exceeds count
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.RemoveRange(6, 1));
	}

	[TestMethod]
	public void RemoveRangeWorksCorrectly()
	{
		ForEachList<int>(buffer =>
		{
			buffer.Add([10, 20, 30, 40, 50, 60]);
			buffer.RemoveRange(1, 3); // remove 20,30,40
			AreEqual([10, 50, 60], buffer.ToArray());
			AreEqual(3, buffer.Count);
			buffer.RemoveRange(0, 1); // remove first
			AreEqual([50, 60], buffer.ToArray());
			buffer.RemoveRange(1, 1); // remove last
			AreEqual([50], buffer.ToArray());
		});
	}

	[TestMethod]
	public void SeekBeyondLengthThrowsArgumentOutOfRangeException()
	{
		using var buffer = new SpeedyList<int>(8);
		buffer.Add([1, 2]);

		ExpectedException<ArgumentOutOfRangeException>(() => buffer.Seek(3));
	}

	[TestMethod]
	public void SeekNegativePositionThrowsArgumentOutOfRangeException()
	{
		using var buffer = new SpeedyList<int>(8);
		ExpectedException<ArgumentOutOfRangeException>(() => buffer.Seek(-1));
	}

	[TestMethod]
	public void SeekValidPositionChangesPositionCorrectly()
	{
		using var buffer = new SpeedyList<int>(16);
		buffer.Add([1, 2, 3, 4, 5]);
		buffer.Seek(2);
		AreEqual(2, buffer.ReadPosition);
		AreEqual(5, buffer.WritePosition);
		AreEqual(3, buffer.Read());
	}

	[TestMethod]
	public void ThreadLocalInstanceReusesAndResetsInstance()
	{
		var inst1 = SpeedyList<int>.GetThreadLocalInstance(512);
		inst1.Add(777);
		var inst2 = SpeedyList<int>.GetThreadLocalInstance(512);
		AreEqual(inst1, inst2);
		AreEqual(0, inst2.Count);
	}

	[TestMethod]
	public void ToArrayEmptyListReturnsEmptyArray()
	{
		ForEachList<int>(buffer =>
		{
			var array = buffer.ToArray();
			AreEqual(0, array.Length);
			AreEqual(Array.Empty<int>(), array);
		});
	}

	[TestMethod]
	public void ToArrayReturnsCorrectCopy()
	{
		ForEachList<int>(buffer =>
		{
			buffer.Add([5, 10, 15, 20]);

			var array = buffer.ToArray();
			AreEqual(4, array.Length);
			AreEqual([5, 10, 15, 20], array);

			// Modify original list - ToArray should be independent
			buffer[0] = 99;
			AreEqual(5, array[0]); // copy, not reference
		});
	}

	[TestMethod]
	public void ToStringForByteBufferReturnUnicodeString()
	{
		using var buffer = new SpeedyList<byte>(64);
		var text = "Hello 世界";
		buffer.Add(Encoding.Unicode.GetBytes(text));
		var result = buffer.ToString();
		AreEqual(text, result);
	}

	[TestMethod]
	public void ToStringForCharBufferReturnsCorrectString()
	{
		ForEachList<char>(x =>
		{
			x.Add("Test ω");
			var result = x.ToString();
			AreEqual("Test ω", result);
		});
	}

	private void ForEachList<T>(
		Action<SpeedyList<T>> test,
		int size = SpeedyList.DefaultCapacity,
		bool testLongLived = true,
		bool testRented = true,
		bool includeClearing = true,
		bool includeNoClearing = true)
	{
		if (testLongLived)
		{
			if (includeClearing)
			{
				using (var l = new SpeedyList<T>(size, true, true))
				{
					test(l);
				}
			}

			if (includeNoClearing)
			{
				using (var l = new SpeedyList<T>(size, true, false))
				{
					test(l);
				}
			}
		}

		if (testRented)
		{
			if (includeClearing)
			{
				using (var l = new SpeedyList<T>(size, false, true))
				{
					test(l);
				}
			}

			if (includeNoClearing)
			{
				using (var l = new SpeedyList<T>(size, false, false))
				{
					test(l);
				}
			}
		}
	}

	#endregion
}