#region References

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cornerstone.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Collections;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
[SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
[SuppressMessage("ReSharper", "CommentTypo")]
[TestClass]
public class GapBufferTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Add()
	{
		var actual = new GapBuffer<int>();
		AreEqual(256, actual.Capacity);

		actual.Add([1, 2, 3, 4, 5, 6], 2, 3);
		AreEqual([3, 4, 5], Enumerable.ToArray(actual));

		// No op add should not fail
		actual.Add([]);
		actual.Add([], -1, 10);
		actual.Add([], 0, -1);
		actual.Add([], 0, 10);
		AreEqual([3, 4, 5], Enumerable.ToArray(actual));
	}

	[TestMethod]
	public void AddRangeAfterGapWasMovedFarAway()
	{
		var buffer = new GapBuffer<char>(16);
		buffer.Add("HelloWorld".ToCharArray());

		buffer.Insert(0, '!');
		buffer.Insert(6, '@');

		buffer.Add("12345".ToCharArray());

		AreEqual("!Hello@World12345".ToCharArray(), buffer.ToArray(), () => buffer.ToString());
	}

	[TestMethod]
	public void Contains()
	{
		var buffer = new GapBuffer<int>(1, 2, 3, 4, 5, 6);
		IsTrue(buffer.Contains(3));
		IsFalse(buffer.Contains(9));
	}

	[TestMethod]
	public void CopyToPartialRangeSpanningGap()
	{
		var buffer = new GapBuffer<char>();
		buffer.Add("0123456789".ToCharArray());

		buffer.Insert(4, '|');

		var dest = new char[10];
		buffer.CopyTo(2, dest, 3, 6);

		AreEqual('\0', dest[0]);
		AreEqual('\0', dest[1]);
		AreEqual('\0', dest[2]);
		AreEqual('2', dest[3]);
		AreEqual('3', dest[4]);
		AreEqual('|', dest[5]);
		AreEqual('4', dest[6]);
		AreEqual('5', dest[7]);
		AreEqual('6', dest[8]);
		AreEqual('\0', dest[9]);
	}

	[TestMethod]
	public void Enumerator()
	{
		var buffer = new GapBuffer<int>(1, 2, 3, 4, 5, 6);
		var actual = new List<int>();

		foreach (var item in (IEnumerable) buffer)
		{
			actual.Add((int) item);
		}

		AreEqual([1, 2, 3, 4, 5, 6], actual.ToArray());
	}

	[TestMethod]
	public void GapMovementWhenGapSizeBecomesZeroShouldRecover()
	{
		var buffer = new GapBuffer<char>(8);

		buffer.Add("12345678".ToCharArray());

		buffer.Insert(4, 'X');
		buffer.RemoveAt(5);
		buffer.Insert(0, '!');
		buffer.Insert(buffer.Count, '?');

		AreEqual("!1234X678?".ToArray(), buffer.ToArray());
	}

	[TestMethod]
	public void IndexerAndCountAfterGapMovements()
	{
		var buffer = new GapBuffer<string>(4);

		buffer.Add(["A", "B", "C"]);

		AreEqual(3, buffer.Count);
		AreEqual("A", buffer[0]);
		AreEqual("C", buffer[2]);

		buffer.Insert(1, "X");
		AreEqual("X", buffer[1]);
		AreEqual("C", buffer[3]);

		buffer.RemoveAt(0);
		AreEqual("X", buffer[0]);
		AreEqual(3, buffer.Count);

		buffer.Insert(3, "End");
		AreEqual("End", buffer[3]);
	}

	[TestMethod]
	public void IndexerGetSetAroundGapBoundaries()
	{
		var buffer = new GapBuffer<string>(8);
		buffer.Add(["A", "B", "C", "D", "E"]);

		buffer.Insert(2, "X"); // A B X C D E

		buffer[1] = "b"; // write before gap
		buffer[3] = "c"; // write after gap

		AreEqual("A", buffer[0]);
		AreEqual("b", buffer[1]);
		AreEqual("X", buffer[2]);
		AreEqual("c", buffer[3]);
		AreEqual("E", buffer[5]);
	}

	[TestMethod]
	public void IndexOf()
	{
		//                              0  1  2  3  4  5
		var actual = new GapBuffer<int>(1, 2, 3, 4, 5, 6);
		AreEqual(1, actual.IndexOf(2));
		AreEqual(1, actual.IndexOf(2, 1));
		AreEqual(-1, actual.IndexOf(2, 2));
	}

	[TestMethod]
	public void InsertWhenGapSizeZero()
	{
		var buffer = new GapBuffer<char>(8);
		buffer.Add("1234".ToCharArray());
		buffer.Insert(2, 'A');
		buffer.Insert(0, '!');
		buffer.Insert(buffer.Count, 'Z');

		AreEqual("!12A34Z".ToCharArray(), buffer.ToArray(), () => buffer.ToString());
	}

	[TestMethod]
	public void MoveGapLeftAndRightMultipleTimes()
	{
		var buffer = new GapBuffer<char>();
		buffer.Add("Hello World".ToCharArray());
		AreEqual("Hello World".ToArray(), buffer.ToArray(),
			() => new string(buffer.ToArray()));

		buffer.Insert(5, '|');
		AreEqual("Hello| World".ToArray(), buffer.ToArray(),
			() => new string(buffer.ToArray()));

		buffer.Insert(2, 'x');
		AreEqual("Hexllo| World".ToArray(), buffer.ToArray(),
			() => new string(buffer.ToArray()));
		buffer.Insert(0, 'A');
		AreEqual("AHexllo| World".ToArray(), buffer.ToArray(),
			() => new string(buffer.ToArray()));

		buffer.Insert(10, 'Z');
		AreEqual("AHexllo| WZorld".ToArray(), buffer.ToArray(),
			() => new string(buffer.ToArray()));

		buffer.Insert(buffer.Count, '!');
		AreEqual("AHexllo| WZorld!".ToArray(), buffer.ToArray(),
			() => new string(buffer.ToArray()));

		// Now force several gap movements back and forth
		for (var i = 0; i < 20; i++)
		{
			var pos = (i % 3) switch
			{
				0 => 4,
				1 => buffer.Count - 3,
				_ => buffer.Count / 2
			};

			buffer.Insert(pos, (char) ('a' + (i % 26)));
		}

		// Just make sure we didn't corrupt anything
		IsTrue(buffer.Count > 30);
		AreEqual('A', buffer[0]);
		AreEqual('!', buffer[^1]);

		AreEqual("AHexspmjgdallo|flroic WZorbehknqtld!".ToArray(),
			buffer.ToArray(),
			() => new string(buffer.ToArray()));
	}

	[TestMethod]
	public void RemoveAtAtBothGapBordersMultipleTimes()
	{
		var buffer = new GapBuffer<char>();
		buffer.Add("abcdefgh".ToCharArray());

		buffer.RemoveAt(3);
		AreEqual("abcefgh".ToCharArray(), buffer.ToArray(), () => buffer.ToString());
		buffer.RemoveAt(3);
		AreEqual("abcfgh".ToCharArray(), buffer.ToArray(), () => buffer.ToString());
		buffer.RemoveAt(3);
		AreEqual("abcgh".ToCharArray(), buffer.ToArray(), () => buffer.ToString());
		buffer.RemoveAt(2);
		AreEqual("abgh".ToCharArray(), buffer.ToArray(), () => buffer.ToString());
		buffer.RemoveAt(0);
		AreEqual("bgh".ToCharArray(), buffer.ToArray(), () => buffer.ToString());
		buffer.RemoveAt(buffer.Count - 1);
		AreEqual("bg".ToCharArray(), buffer.ToArray(), () => buffer.ToString());
	}

	[TestMethod]
	public void RemoveAtBorderCasesExpandGapBothDirections()
	{
		var buffer = new GapBuffer<char>();
		buffer.Add("abcdefgh".ToCharArray()); // 01234567

		// Delete just before gap (should expand left)
		buffer.RemoveAt(3); // a b c   e f g h     (gap after 'c')
		AreEqual("abcefgh".ToCharArray(), buffer.ToArray());

		// Delete just after gap (should expand right)
		buffer.RemoveAt(4); // a b c e   g h
		AreEqual("abcegh".ToCharArray(), buffer.ToArray());

		// Delete far away → should move gap
		buffer.RemoveAt(0); //  b c e   g h
		AreEqual("bcegh".ToCharArray(), buffer.ToArray());

		buffer.RemoveAt(buffer.Count - 1);
		AreEqual("bceg".ToCharArray(), buffer.ToArray());
	}

	[TestMethod]
	public void ResetBuffer()
	{
		//                              0  1  2  3  4  5
		var actual = new GapBuffer<int>(1, 2, 3, 4, 5, 6);
		actual.Reset([9, 8, 7, 6, 5, 4]);
		AreEqual([9, 8, 7, 6, 5, 4], actual.ToArray());
	}

	[TestMethod]
	public void StressInsertDeleteAlternatingAtSamePosition()
	{
		var buffer = new GapBuffer<char>(32);
		buffer.Add("base".ToCharArray());
		
		for (var i = 0; i < 500; i++)
		{
			buffer.Insert(2, (char) ('a' + (i % 26)));
			buffer.RemoveAt(2);
		}

		AreEqual("base".ToCharArray(), buffer.ToArray(),
			() => "Buffer got corrupted after many insert+remove at same spot");
	}

	[TestMethod]
	public void ValidateToString()
	{
		var actual = new GapBuffer<int>();
		actual.Add([1, 2, 3, 4, 5, 6], 2, 3);
		AreEqual("Cornerstone.Collections.GapBuffer`1[System.Int32]", actual.ToString());
		
		var actual2 = new GapBuffer<char>();
		actual2.Add("hello world");
		AreEqual("hello world", actual2.ToString());
		
		var actual3 = new GapBuffer<char>();
		AreEqual(string.Empty, actual3.ToString());
	}

	#endregion
}