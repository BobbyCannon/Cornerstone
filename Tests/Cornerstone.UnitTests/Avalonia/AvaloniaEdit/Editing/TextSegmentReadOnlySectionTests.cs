#region References

using System.Linq;
using Cornerstone.Avalonia.AvaloniaEdit.Editing;
using Cornerstone.Text.Document;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.AvaloniaEdit.Editing;

[TestClass]
public class TextSegmentReadOnlySectionTests : CornerstoneUnitTest
{
	#region Fields

	private TextSegmentReadOnlySectionProvider<TextSegment> _provider;
	private TextSegmentCollection<TextSegment> _segments;

	#endregion

	#region Methods

	[TestMethod]
	public void DeleteFirstCharacterInReadOnlySegment()
	{
		_segments.Add(new TextSegment { StartOffset = 20, Length = 5 });
		var result = _provider.GetDeletableSegments(new SimpleSegment(20, 1)).ToList();
		AreEqual(0, result.Count);
		/* // we would need this result for the old Delete code so that the first character doesn't get selected:
		AreEqual(1, result.Count);
		AreEqual(2, result[0].Offset);
		AreEqual(0, result[0].Length);*/
	}

	[TestMethod]
	public void DeleteLastCharacterInReadOnlySegment()
	{
		_segments.Add(new TextSegment { StartOffset = 20, Length = 5 });
		var result = _provider.GetDeletableSegments(new SimpleSegment(24, 1)).ToList();
		AreEqual(0, result.Count);
		/* // we would need this result for the old Backspace code so that the last character doesn't get selected:
		AreEqual(1, result.Count);
		AreEqual(25, result[0].Offset);
		AreEqual(0, result[0].Length);*/
	}

	[TestMethod]
	public void DeleteWholeReadOnlySegment()
	{
		_segments.Add(new TextSegment { StartOffset = 20, Length = 5 });
		var result = _provider.GetDeletableSegments(new SimpleSegment(20, 5)).ToList();
		AreEqual(0, result.Count);
	}

	[TestMethod]
	public void DeletionAroundReadOnlySegment()
	{
		_segments.Add(new TextSegment { StartOffset = 20, Length = 5 });
		var result = _provider.GetDeletableSegments(new SimpleSegment(15, 16)).ToList();
		AreEqual(2, result.Count);
		AreEqual(15, result[0].Offset);
		AreEqual(5, result[0].Length);
		AreEqual(25, result[1].Offset);
		AreEqual(6, result[1].Length);
	}

	[TestMethod]
	public void DeletionImpossibleInReadOnlySegment()
	{
		_segments.Add(new TextSegment { StartOffset = 10, Length = 5 });
		var result = _provider.GetDeletableSegments(new SimpleSegment(11, 2)).ToList();
		AreEqual(0, result.Count);
	}

	[TestMethod]
	public void DeletionPossibleWhenNothingIsReadOnly()
	{
		var result = _provider.GetDeletableSegments(new SimpleSegment(10, 20)).ToList();
		AreEqual(1, result.Count);
		AreEqual(10, result[0].Offset);
		AreEqual(20, result[0].Length);
	}

	[TestMethod]
	public void EmptyDeletionImpossibleInReadOnlySegment()
	{
		_segments.Add(new TextSegment { StartOffset = 10, Length = 5 });
		var result = _provider.GetDeletableSegments(new SimpleSegment(11, 0)).ToList();
		AreEqual(0, result.Count);
	}

	[TestMethod]
	public void EmptyDeletionPossibleAtEndOfReadOnlySegment()
	{
		_segments.Add(new TextSegment { StartOffset = 10, Length = 5 });
		var result = _provider.GetDeletableSegments(new SimpleSegment(15, 0)).ToList();
		AreEqual(1, result.Count);
		AreEqual(15, result[0].Offset);
		AreEqual(0, result[0].Length);
	}

	[TestMethod]
	public void EmptyDeletionPossibleAtStartOfReadOnlySegment()
	{
		_segments.Add(new TextSegment { StartOffset = 10, Length = 5 });
		var result = _provider.GetDeletableSegments(new SimpleSegment(10, 0)).ToList();
		AreEqual(1, result.Count);
		AreEqual(10, result[0].Offset);
		AreEqual(0, result[0].Length);
	}

	[TestMethod]
	public void EmptyDeletionPossibleWhenNothingIsReadOnly()
	{
		var result = _provider.GetDeletableSegments(new SimpleSegment(10, 0)).ToList();
		AreEqual(1, result.Count);
		AreEqual(10, result[0].Offset);
		AreEqual(0, result[0].Length);
	}

	[TestMethod]
	public void InsertionImpossibleInsideReadOnlySegment()
	{
		_segments.Add(new TextSegment { StartOffset = 10, EndOffset = 15 });
		Assert.IsFalse(_provider.CanInsert(11));
		Assert.IsFalse(_provider.CanInsert(12));
		Assert.IsFalse(_provider.CanInsert(13));
		Assert.IsFalse(_provider.CanInsert(14));
	}

	[TestMethod]
	public void InsertionPossibleAtEndOfReadOnlySegment()
	{
		_segments.Add(new TextSegment { StartOffset = 10, EndOffset = 15 });
		Assert.IsTrue(_provider.CanInsert(15));
	}

	[TestMethod]
	public void InsertionPossibleAtStartOfReadOnlySegment()
	{
		_segments.Add(new TextSegment { StartOffset = 10, EndOffset = 15 });
		Assert.IsTrue(_provider.CanInsert(10));
	}

	[TestMethod]
	public void InsertionPossibleBeforeReadOnlySegment()
	{
		_segments.Add(new TextSegment { StartOffset = 10, EndOffset = 15 });
		Assert.IsTrue(_provider.CanInsert(5));
	}

	[TestMethod]
	public void InsertionPossibleBetweenReadOnlySegments()
	{
		_segments.Add(new TextSegment { StartOffset = 10, EndOffset = 15 });
		_segments.Add(new TextSegment { StartOffset = 15, EndOffset = 20 });
		Assert.IsTrue(_provider.CanInsert(15));
	}

	[TestMethod]
	public void InsertionPossibleWhenNothingIsReadOnly()
	{
		Assert.IsTrue(_provider.CanInsert(0));
		Assert.IsTrue(_provider.CanInsert(100));
	}

	[TestInitialize]
	public void SetUp()
	{
		_segments = [];
		_provider = new TextSegmentReadOnlySectionProvider<TextSegment>(_segments);
	}

	#endregion
}