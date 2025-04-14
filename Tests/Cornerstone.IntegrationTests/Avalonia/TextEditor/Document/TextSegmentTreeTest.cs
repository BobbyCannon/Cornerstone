#region References

using System;
using System.Collections.Generic;
using Cornerstone.Avalonia.TextEditor.Document;
using Cornerstone.Generators;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework.Legacy;

#endregion

namespace Cornerstone.IntegrationTests.Avalonia.TextEditor.Document;

[TestClass]
public class TextSegmentTreeTest : CornerstoneUnitTest
{
	#region Fields

	private List<TestTextRange> _expectedSegments;
	private static Random _random;
	private TextSegmentCollection<TestTextRange> _tree;

	#endregion

	#region Methods

	[TestMethod]
	public void AddSegments()
	{
		var s1 = AddSegment(10, 20);
		var s2 = AddSegment(15, 10);
		CheckSegments();
	}

	[TestMethod]
	public void FindFirstSegmentWithStartAfter()
	{
		var s1 = new TestTextRange(5, 10);
		var s2 = new TestTextRange(10, 10);
		_tree.Add(s1);
		_tree.Add(s2);
		ClassicAssert.AreSame(s1, _tree.FindFirstSegmentWithStartAfter(-100));
		ClassicAssert.AreSame(s1, _tree.FindFirstSegmentWithStartAfter(0));
		ClassicAssert.AreSame(s1, _tree.FindFirstSegmentWithStartAfter(4));
		ClassicAssert.AreSame(s1, _tree.FindFirstSegmentWithStartAfter(5));
		ClassicAssert.AreSame(s2, _tree.FindFirstSegmentWithStartAfter(6));
		ClassicAssert.AreSame(s2, _tree.FindFirstSegmentWithStartAfter(9));
		ClassicAssert.AreSame(s2, _tree.FindFirstSegmentWithStartAfter(10));
		ClassicAssert.AreSame(null, _tree.FindFirstSegmentWithStartAfter(11));
		ClassicAssert.AreSame(null, _tree.FindFirstSegmentWithStartAfter(100));
	}

	[TestMethod]
	public void FindFirstSegmentWithStartAfterWithDuplicates()
	{
		var s1 = new TestTextRange(5, 10);
		var s1B = new TestTextRange(5, 7);
		var s2 = new TestTextRange(10, 10);
		var s2B = new TestTextRange(10, 7);
		_tree.Add(s1);
		_tree.Add(s1B);
		_tree.Add(s2);
		_tree.Add(s2B);
		ClassicAssert.AreSame(s1B, _tree.GetNextSegment(s1));
		ClassicAssert.AreSame(s2B, _tree.GetNextSegment(s2));
		ClassicAssert.AreSame(s1, _tree.FindFirstSegmentWithStartAfter(-100));
		ClassicAssert.AreSame(s1, _tree.FindFirstSegmentWithStartAfter(0));
		ClassicAssert.AreSame(s1, _tree.FindFirstSegmentWithStartAfter(4));
		ClassicAssert.AreSame(s1, _tree.FindFirstSegmentWithStartAfter(5));
		ClassicAssert.AreSame(s2, _tree.FindFirstSegmentWithStartAfter(6));
		ClassicAssert.AreSame(s2, _tree.FindFirstSegmentWithStartAfter(9));
		ClassicAssert.AreSame(s2, _tree.FindFirstSegmentWithStartAfter(10));
		ClassicAssert.AreSame(null, _tree.FindFirstSegmentWithStartAfter(11));
		ClassicAssert.AreSame(null, _tree.FindFirstSegmentWithStartAfter(100));
	}

	[TestMethod]
	public void FindFirstSegmentWithStartAfterWithDuplicates2()
	{
		var s1 = new TestTextRange(5, 1);
		var s2 = new TestTextRange(5, 2);
		var s3 = new TestTextRange(5, 3);
		var s4 = new TestTextRange(5, 4);
		_tree.Add(s1);
		_tree.Add(s2);
		_tree.Add(s3);
		_tree.Add(s4);
		ClassicAssert.AreSame(s1, _tree.FindFirstSegmentWithStartAfter(0));
		ClassicAssert.AreSame(s1, _tree.FindFirstSegmentWithStartAfter(1));
		ClassicAssert.AreSame(s1, _tree.FindFirstSegmentWithStartAfter(4));
		ClassicAssert.AreSame(s1, _tree.FindFirstSegmentWithStartAfter(5));
		ClassicAssert.AreSame(null, _tree.FindFirstSegmentWithStartAfter(6));
	}

	[TestMethod]
	public void FindInEmptyTree()
	{
		ClassicAssert.AreSame(null, _tree.FindFirstSegmentWithStartAfter(0));
		AreEqual(0, _tree.FindSegmentsContaining(0).Count);
		AreEqual(0, _tree.FindOverlappingSegments(10, 20).Count);
	}

	[ClassInitialize]
	public static void ClassInitialize(TestContext context)
	{
		var seed = Environment.TickCount;
		Console.WriteLine("TextSegmentTreeTest Seed: " + seed);
		_random = new Random(seed);
	}

	[TestMethod]
	public void InsertionAfterAllSegments()
	{
		var s1 = AddSegment(10, 20);
		var s2 = AddSegment(15, 10);
		ChangeDocument(new OffsetChangeMapEntry(45, 0, 2));
		CheckSegments();
	}

	[TestMethod]
	public void InsertionBeforeAllSegments()
	{
		var s1 = AddSegment(10, 20);
		var s2 = AddSegment(15, 10);
		ChangeDocument(new OffsetChangeMapEntry(5, 0, 2));
		CheckSegments();
	}

	[TestMethod]
	public void RandomizedCloseNoDocumentChanges()
	{
		// Lots of segments in a short document. Tests how the tree copes with multiple identical segments.
		for (var i = 0; i < 1000; i++)
		{
			switch (RandomGenerator.NextInteger(0, 3))
			{
				case 0:
					AddSegment(_random.Next(20), _random.Next(10));
					break;
				case 1:
					AddSegment(_random.Next(20), _random.Next(20));
					break;
				case 2:
					if (_tree.Count > 0)
					{
						RemoveSegment(_expectedSegments[_random.Next(_tree.Count)]);
					}
					break;
			}
			CheckSegments();
		}
	}

	[TestMethod]
	public void RandomizedNoDocumentChanges()
	{
		for (var i = 0; i < 1000; i++)
		{
			//				Console.WriteLine(tree.GetTreeAsString());
			//				Console.WriteLine("Iteration " + i);

			switch (_random.Next(3))
			{
				case 0:
					AddSegment(_random.Next(500), _random.Next(30));
					break;
				case 1:
					AddSegment(_random.Next(500), _random.Next(300));
					break;
				case 2:
					if (_tree.Count > 0)
					{
						RemoveSegment(_expectedSegments[_random.Next(_tree.Count)]);
					}
					break;
			}
			CheckSegments();
		}
	}

	[TestMethod]
	public void RandomizedRetrievalTest()
	{
		for (var i = 0; i < 1000; i++)
		{
			AddSegment(_random.Next(500), _random.Next(300));
		}
		CheckSegments();
		for (var i = 0; i < 1000; i++)
		{
			TestRetrieval(_random.Next(1000) - 100, _random.Next(500));
		}
	}

	[TestMethod]
	public void RandomizedWithDocumentChanges()
	{
		for (var i = 0; i < 500; i++)
		{
			//				Console.WriteLine(tree.GetTreeAsString());
			//				Console.WriteLine("Iteration " + i);

			switch (_random.Next(6))
			{
				case 0:
					AddSegment(_random.Next(500), _random.Next(30));
					break;
				case 1:
					AddSegment(_random.Next(500), _random.Next(300));
					break;
				case 2:
					if (_tree.Count > 0)
					{
						RemoveSegment(_expectedSegments[_random.Next(_tree.Count)]);
					}
					break;
				case 3:
					ChangeDocument(new OffsetChangeMapEntry(_random.Next(800), _random.Next(50), _random.Next(50)));
					break;
				case 4:
					ChangeDocument(new OffsetChangeMapEntry(_random.Next(800), 0, _random.Next(50)));
					break;
				case 5:
					ChangeDocument(new OffsetChangeMapEntry(_random.Next(800), _random.Next(50), 0));
					break;
			}
			CheckSegments();
		}
	}

	[TestMethod]
	public void RandomizedWithDocumentChangesClose()
	{
		for (var i = 0; i < 500; i++)
		{
			//				Console.WriteLine(tree.GetTreeAsString());
			//				Console.WriteLine("Iteration " + i);

			switch (_random.Next(6))
			{
				case 0:
					AddSegment(_random.Next(50), _random.Next(30));
					break;
				case 1:
					AddSegment(_random.Next(50), _random.Next(3));
					break;
				case 2:
					if (_tree.Count > 0)
					{
						RemoveSegment(_expectedSegments[_random.Next(_tree.Count)]);
					}
					break;
				case 3:
					ChangeDocument(new OffsetChangeMapEntry(_random.Next(80), _random.Next(10), _random.Next(10)));
					break;
				case 4:
					ChangeDocument(new OffsetChangeMapEntry(_random.Next(80), 0, _random.Next(10)));
					break;
				case 5:
					ChangeDocument(new OffsetChangeMapEntry(_random.Next(80), _random.Next(10), 0));
					break;
			}
			CheckSegments();
		}
	}

	[TestMethod]
	public void ReplacementAtEndOfSegment()
	{
		var s1 = AddSegment(10, 20);
		var s2 = AddSegment(15, 10);
		ChangeDocument(new OffsetChangeMapEntry(24, 6, 10));
		CheckSegments();
	}

	[TestMethod]
	public void ReplacementBeforeAllSegmentsTouchingFirstSegment()
	{
		var s1 = AddSegment(10, 20);
		var s2 = AddSegment(15, 10);
		ChangeDocument(new OffsetChangeMapEntry(5, 5, 2));
		CheckSegments();
	}

	[TestMethod]
	public void ReplacementOfWholeSegment()
	{
		var s1 = AddSegment(10, 20);
		var s2 = AddSegment(15, 10);
		ChangeDocument(new OffsetChangeMapEntry(10, 20, 30));
		CheckSegments();
	}

	[TestMethod]
	public void ReplacementOverlappingWithStartOfSegment()
	{
		var s1 = AddSegment(10, 20);
		var s2 = AddSegment(15, 10);
		ChangeDocument(new OffsetChangeMapEntry(9, 7, 2));
		CheckSegments();
	}

	[TestInitialize]
	public void SetUp()
	{
		_tree = [];
		_expectedSegments = [];
	}

	private TestTextRange AddSegment(int offset, int length)
	{
		//			Console.WriteLine("Add " + offset + ", " + length);
		var s = new TestTextRange(offset, length);
		_tree.Add(s);
		_expectedSegments.Add(s);
		return s;
	}

	private void ChangeDocument(OffsetChangeMapEntry change)
	{
		_tree.UpdateOffsets(change);
		foreach (var s in _expectedSegments)
		{
			var endOffset = s.ExpectedOffset + s.ExpectedLength;
			s.ExpectedOffset = change.GetNewOffset(s.ExpectedOffset, AnchorMovementType.AfterInsertion);
			s.ExpectedLength = Math.Max(0, change.GetNewOffset(endOffset, AnchorMovementType.BeforeInsertion) - s.ExpectedOffset);
		}
	}

	private void CheckSegments()
	{
		AreEqual(_expectedSegments.Count, _tree.Count);
		foreach (var s in _expectedSegments)
		{
			AreEqual(s.ExpectedOffset, s.StartOffset /*, "startoffset for " + s*/);
			AreEqual(s.ExpectedLength, s.Length /*, "length for " + s*/);
		}
	}

	private void RemoveSegment(TestTextRange s)
	{
		//			Console.WriteLine("Remove " + s);
		_expectedSegments.Remove(s);
		_tree.Remove(s);
	}

	private void TestRetrieval(int offset, int length)
	{
		var actual = new HashSet<TestTextRange>(_tree.FindOverlappingSegments(offset, length));
		var expected = new HashSet<TestTextRange>();
		foreach (var e in _expectedSegments)
		{
			if ((e.ExpectedOffset + e.ExpectedLength) < offset)
			{
				continue;
			}
			if (e.ExpectedOffset > (offset + length))
			{
				continue;
			}
			expected.Add(e);
		}
		IsTrue(actual.IsSubsetOf(expected));
		IsTrue(expected.IsSubsetOf(actual));
	}

	#endregion

	#region Classes

	private class TestTextRange : TextRange
	{
		#region Fields

		internal int ExpectedOffset, ExpectedLength;

		#endregion

		#region Constructors

		public TestTextRange(int expectedOffset, int expectedLength)
		{
			ExpectedOffset = expectedOffset;
			ExpectedLength = expectedLength;
			StartOffset = expectedOffset;
			Length = expectedLength;
		}

		#endregion
	}

	#endregion
}