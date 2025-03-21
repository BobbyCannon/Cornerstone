#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Collections;
using Cornerstone.Generators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Collections;

/// <summary>
/// All shared IBuffer interfaces are tested in <see cref="BufferTests" />.
/// </summary>
[TestClass]
[SuppressMessage("ReSharper", "StringLiteralTypo")]
[SuppressMessage("ReSharper", "CollectionNeverUpdated.Local")]
public class GapBufferTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Constructors()
	{
		var buffer = new GapBuffer<int>(new List<int> { 1, 2, 3 });
		AreEqual(new[] { 1, 2, 3 }, buffer);
		AreEqual(GapBuffer<char>.DefaultCapacity, buffer.Capacity);
		AreEqual(3, buffer.Count);
		AreEqual(false, buffer.IsReadOnly);
	}

	[TestMethod]
	public void Contains()
	{
		//                                0    1    2    3    4    5    6    7    8
		var buffer = new GapBuffer<char>('a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i');
		AreEqual(9, buffer.Count);
		IsTrue(buffer.Contains('b'));
		IsFalse(buffer.Contains('B'));
	}

	[TestMethod]
	public void CopyTo()
	{
		//                                012345
		var buffer = new GapBuffer<char>("abcdef");
		var actual = new char[buffer.Count];
		buffer.CopyTo(actual, 0);
		AreEqual("abcdef", new string(actual));

		actual = new char[buffer.Count * 2];
		var startIndex = RandomGenerator.NextInteger(1, (actual.Length - buffer.Count) + 1);
		buffer.CopyTo(actual, startIndex);
		AreEqual("abcdef", new string(actual, startIndex, buffer.Count));

		for (var i = 0; i < actual.Length; i++)
		{
			actual[i] = '\0';
		}

		startIndex = actual.Length - buffer.Count;
		buffer.CopyTo(actual, startIndex);
		var expected = "\0\0\0\0\0\0abcdef".ToCharArray();
		AreEqual(expected, actual);

		ExpectedException<ArgumentException>(() => buffer.CopyTo(actual, startIndex + 1),
			"Destination array was not long enough. Check the destination index, length, and the array's lower bounds."
		);
	}
	
	[TestMethod]
	public void GetEnumerator()
	{
		var buffer = new GapBuffer<char>(4, '1', '2', '3', '4');
		buffer.RemoveAt(2);
		var actual = new List<char>();
		using var enumerator = buffer.GetEnumerator();
		while (enumerator.MoveNext())
		{
			actual.Add(enumerator.Current);
		}

		AreEqual("124", new string(actual.ToArray()));
	}

	[TestMethod]
	[SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
	public void Indexer()
	{
		var buffer = new GapBuffer<char>('1', '2', '3');
		buffer[2] = 'a';
		AreEqual("12a", buffer.ToString());

		buffer.RemoveAt(0);
		buffer[0] = 'b';
		AreEqual("ba", buffer.ToString());

		ExpectedException<IndexOutOfRangeException>(() => buffer[-1] = 'c', Babel.Tower[BabelKeys.IndexOutOfRange]);
	}

	[TestMethod]
	public void Insert()
	{
		var buffer = new GapBuffer<char>();
		buffer.Insert(0, 'a');
		AreEqual(new[] { 'a' }, buffer);
		ExpectedException<IndexOutOfRangeException>(() => buffer.Insert(-1, 'z'), Babel.Tower[BabelKeys.IndexOutOfRange]);

		buffer.InsertRange(0, ['b', 'c']);
		AreEqual("bca", buffer);
	}

	[TestMethod]
	public void Remove()
	{
		//                                0    1    2    3    4    5    6    7    8
		var buffer = new GapBuffer<char>('a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i');
		IsTrue(buffer.Remove('a'));
		AreEqual(8, buffer.Count);
		AreEqual("bcdefghi", buffer.ToString());

		IsTrue(buffer.Remove('i'));
		AreEqual(7, buffer.Count);
		AreEqual("bcdefgh", buffer.ToString());

		IsFalse(buffer.Remove('z'));
		AreEqual(7, buffer.Count);
		AreEqual("bcdefgh", buffer.ToString());
	}

	[TestMethod]
	public void RemoveAt()
	{
		//                                0    1    2    3    4    5    6    7    8
		var buffer = new GapBuffer<char>('a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i');
		buffer.RemoveAt(0);
		AreEqual(8, buffer.Count);
		AreEqual('b', buffer[0]);
		AreEqual('c', buffer[1]);
		AreEqual("bcdefghi", buffer.ToString());
		//        01234567

		buffer.RemoveAt(1);
		AreEqual(7, buffer.Count);
		AreEqual('b', buffer[0]);
		AreEqual('d', buffer[1]);
		AreEqual("bdefghi", buffer.ToString());
	}

	[TestMethod]
	public void RemoveInsertToTestZeroGap()
	{
		//                                   0    1    2
		var buffer = new GapBuffer<char>(3, 'a', 'b', 'c');
		AreEqual(3, buffer.Count);
		AreEqual(3, buffer.Capacity);

		buffer.RemoveAt(0);
		AreEqual(2, buffer.Count);
		AreEqual(3, buffer.Capacity);

		buffer.Insert(0, 'd');
		AreEqual(3, buffer.Count);
		AreEqual(3, buffer.Capacity);
		AreEqual("dbc", buffer);
	}

	[TestMethod]
	public void RemoveRange()
	{
		//                                0    1    2    3    4    5    6    7    8
		var buffer = new GapBuffer<char>('a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i');
		buffer.Remove(3, 3);
		AreEqual(6, buffer.Count);
		AreEqual("abcghi", buffer.ToString());

		buffer.Remove(0, 0);
		AreEqual(6, buffer.Count);
		AreEqual("abcghi", buffer.ToString());
	}

	[TestMethod]
	[SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
	public void ResizeGap()
	{
		var buffer = new GapBuffer<string>(0);
		AreEqual(buffer.Capacity, 0);
		AreEqual(buffer.Count, 0);
		buffer.Add("foo");
		AreEqual(GapBuffer<char>.DefaultCapacity, buffer.Capacity);
		AreEqual(buffer.Count, 1);
	}

	[TestMethod]
	public void ToStringTests()
	{
		var buffer = new GapBuffer<int>();
		AreEqual("Cornerstone.Collections.GapBuffer`1[System.Int32]", buffer.ToString());

		var buffer2 = new GapBuffer<char>('1', '2', '3');
		AreEqual("123", buffer2.ToString());

		buffer2.RemoveAt(0);
		var actual = buffer2.ToString();
		AreEqual("23", actual);

		buffer2 = new GapBuffer<char>('1', '2', '3', 'a', 'b', 'c');
		buffer2.RemoveAt(3);
		actual = buffer2.ToString();
		AreEqual("123bc", actual);
	}

	#endregion
}