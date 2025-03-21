#region References

using System;
using System.Collections.Generic;
using Cornerstone.Collections;
using Cornerstone.Extensions;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Collections;

/// <summary>
/// These test are for <seealso cref="IBuffer{T}" /> interface.
/// </summary>
[TestClass]
public class BufferTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void BoundsCheck()
	{
		ForEachBuffers(x =>
			{
				x.VerifyRange(0);
				x.VerifyRange(4);

				ExpectedException<IndexOutOfRangeException>(
					() => x.VerifyRange(5),
					Babel.Tower[BabelKeys.IndexOutOfRange]
				);
			},
			1, 2, 3, 4, 5
		);
	}

	[TestMethod]
	public void Clear()
	{
		ForEachBuffers(buffer =>
			{
				//  0    1    2    3    4    5    6    7    8
				// 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i'
				AreEqual(9, buffer.Count);

				if (buffer is GapBuffer<char> gapBuffer)
				{
					AreEqual(GapBuffer<char>.DefaultCapacity, gapBuffer.Capacity);
				}

				buffer.Clear();
				AreEqual(0, buffer.Count);

				if (buffer is GapBuffer<char> gapBuffer2)
				{
					AreEqual(GapBuffer<char>.DefaultCapacity, gapBuffer2.Capacity);
				}
			},
			'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i'
		);
	}

	[TestMethod]
	public void IndexOf()
	{
		ForEachBuffers(buffer =>
			{
				//  0    1    2    3    4    5    6    7    8
				// 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i'
				AreEqual(9, buffer.Count);
				AreEqual("abcdefghi", buffer.ToString());

				if (buffer is GapBuffer<char>)
				{
					AreEqual(9, buffer.GetMemberValue("_gapStartIndex"));
					AreEqual(256, buffer.GetMemberValue("_gapEndIndex"));
				}

				AreEqual(0, buffer.IndexOf('a'));
				AreEqual(5, buffer.IndexOf('f'));
				AreEqual(-1, buffer.IndexOf('z'));

				// With start index
				AreEqual(0, buffer.IndexOf('a', 0, 9));
				AreEqual(8, buffer.IndexOf('i', 0, 9));
				AreEqual(0, buffer.IndexOf('a', 0, 1));
				AreEqual(8, buffer.IndexOf('i', 8, 1));
				AreEqual(-1, buffer.IndexOf('i', 8, 0));
				AreEqual(-1, buffer.IndexOf('a', 0, 0));
				AreEqual(-1, buffer.IndexOf('a', 1, 8));
				AreEqual(-1, buffer.IndexOf('z', 0, 9));

				//                    0    1    2    3    4    5    6    7
				// New Buffer State ('a', 'b', 'c', 'd', 'f', 'g', 'h', 'i');
				// Offset Starts      0    1    2    3    252  253  254  255
				buffer.RemoveAt(4);
				AreEqual("abcdfghi", buffer.ToString());

				if (buffer is GapBuffer<char>)
				{
					AreEqual(4, buffer.GetMemberValue("_gapStartIndex"));
					AreEqual(252, buffer.GetMemberValue("_gapEndIndex"));
				}

				AreEqual(8, buffer.Count);
				AreEqual(0, buffer.IndexOf('a'));
				AreEqual(1, buffer.IndexOf('b'));
				AreEqual(3, buffer.IndexOf('d'));
				AreEqual(-1, buffer.IndexOf('e'));
				AreEqual(4, buffer.IndexOf('f'));
				AreEqual(5, buffer.IndexOf('g'));

				// With start index
				AreEqual(3, buffer.IndexOf('d', 2, 6));
				AreEqual(6, buffer.IndexOf('h', 2, 6));
				AreEqual(6, buffer.IndexOf('h', 5, 2));
				AreEqual(6, buffer.IndexOf('h', 6, 1));
				AreEqual(-1, buffer.IndexOf('h', 6, 0));
				AreEqual(-1, buffer.IndexOf('z', 0, 8));

				//                    0    1    2    3    4    5    6 
				// New Buffer State ('a', 'b', 'c', 'd', 'f', 'g', 'h');
				buffer.RemoveAt(7);
				AreEqual("abcdfgh", buffer.ToString());

				if (buffer is GapBuffer<char>)
				{
					AreEqual(7, buffer.GetMemberValue("_gapStartIndex"));
					AreEqual(256, buffer.GetMemberValue("_gapEndIndex"));
				}

				// With start index
				AreEqual(-1, buffer.IndexOf('b', 2, 2));
				AreEqual(-1, buffer.IndexOf('b', 0, 1));
				AreEqual(1, buffer.IndexOf('b', 0, 2));
				AreEqual(-1, buffer.IndexOf('b', 0, 1));
				AreEqual(-1, buffer.IndexOf('z', 0, 7));
			},
			'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i'
		);
	}

	private void ForEachBuffers<T>(Action<IBuffer<T>> action, params T[] value)
	{
		var buffers = GetBuffers(value);
		foreach (var buffer in buffers)
		{
			buffer.GetType().ToAssemblyName().Dump();
			action(buffer);
		}
	}

	private IEnumerable<IBuffer<T>> GetBuffers<T>(params T[] values)
	{
		yield return new GapBuffer<T>(values);
	}

	#endregion
}