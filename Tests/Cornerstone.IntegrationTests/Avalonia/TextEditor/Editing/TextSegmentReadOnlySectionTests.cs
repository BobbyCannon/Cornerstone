#region References

using System.Linq;
using Cornerstone.Avalonia.TextEditor.Document;
using Cornerstone.Avalonia.TextEditor.Editing;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.IntegrationTests.Avalonia.TextEditor.Editing;

[TestClass]
public class TextSegmentReadOnlySectionTests : CornerstoneUnitTest
{
	#region Fields

	private TextSegmentReadOnlySectionProvider<TextRange> _provider;
	private TextSegmentCollection<TextRange> _segments;

	#endregion

	#region Methods

	[TestMethod]
	public void DeleteFirstCharacterInReadOnlySegment()
	{
		_segments.Add(new TextRange { StartOffset = 20, Length = 5 });
		var result = _provider.GetDeletableSegments(new SimpleRange(20, 1)).ToList();
		AreEqual(0, result.Count);
		/* // we would need this result for the old Delete code so that the first character doesn't get selected:
		AreEqual(1, result.Count);
		AreEqual(2, result[0].Offset);
		AreEqual(0, result[0].Length);*/
	}

	[TestMethod]
	public void DeleteLastCharacterInReadOnlySegment()
	{
		_segments.Add(new TextRange { StartOffset = 20, Length = 5 });
		var result = _provider.GetDeletableSegments(new SimpleRange(24, 1)).ToList();
		AreEqual(0, result.Count);
		/* // we would need this result for the old Backspace code so that the last character doesn't get selected:
		AreEqual(1, result.Count);
		AreEqual(25, result[0].Offset);
		AreEqual(0, result[0].Length);*/
	}

	[TestMethod]
	public void DeleteWholeReadOnlySegment()
	{
		_segments.Add(new TextRange { StartOffset = 20, Length = 5 });
		var result = _provider.GetDeletableSegments(new SimpleRange(20, 5)).ToList();
		AreEqual(0, result.Count);
	}

	[TestMethod]
	public void DeletionAroundReadOnlySegment()
	{
		_segments.Add(new TextRange { StartOffset = 20, Length = 5 });
		var result = _provider.GetDeletableSegments(new SimpleRange(15, 16)).ToList();
		AreEqual(2, result.Count);
		AreEqual(15, result[0].StartIndex);
		AreEqual(5, result[0].Length);
		AreEqual(25, result[1].StartIndex);
		AreEqual(6, result[1].Length);
	}

	[TestMethod]
	public void DeletionImpossibleInReadOnlySegment()
	{
		_segments.Add(new TextRange { StartOffset = 10, Length = 5 });
		var result = _provider.GetDeletableSegments(new SimpleRange(11, 2)).ToList();
		AreEqual(0, result.Count);
	}

	[TestMethod]
	public void DeletionPossibleWhenNothingIsReadOnly()
	{
		var result = _provider.GetDeletableSegments(new SimpleRange(10, 20)).ToList();
		AreEqual(1, result.Count);
		AreEqual(10, result[0].StartIndex);
		AreEqual(20, result[0].Length);
	}

	[TestMethod]
	public void EmptyDeletionImpossibleInReadOnlySegment()
	{
		_segments.Add(new TextRange { StartOffset = 10, Length = 5 });
		var result = _provider.GetDeletableSegments(new SimpleRange(11, 0)).ToList();
		AreEqual(0, result.Count);
	}

	[TestMethod]
	public void EmptyDeletionPossibleAtEndOfReadOnlySegment()
	{
		_segments.Add(new TextRange { StartOffset = 10, Length = 5 });
		var result = _provider.GetDeletableSegments(new SimpleRange(15, 0)).ToList();
		AreEqual(1, result.Count);
		AreEqual(15, result[0].StartIndex);
		AreEqual(0, result[0].Length);
	}

	[TestMethod]
	public void EmptyDeletionPossibleAtStartOfReadOnlySegment()
	{
		_segments.Add(new TextRange { StartOffset = 10, Length = 5 });
		var result = _provider.GetDeletableSegments(new SimpleRange(10, 0)).ToList();
		AreEqual(1, result.Count);
		AreEqual(10, result[0].StartIndex);
		AreEqual(0, result[0].Length);
	}

	[TestMethod]
	public void EmptyDeletionPossibleWhenNothingIsReadOnly()
	{
		var result = _provider.GetDeletableSegments(new SimpleRange(10, 0)).ToList();
		AreEqual(1, result.Count);
		AreEqual(10, result[0].StartIndex);
		AreEqual(0, result[0].Length);
	}

	[TestMethod]
	public void InsertionImpossibleInsideReadOnlySegment()
	{
		_segments.Add(new TextRange { StartOffset = 10, EndIndex = 15 });
		IsFalse(_provider.CanInsert(11));
		IsFalse(_provider.CanInsert(12));
		IsFalse(_provider.CanInsert(13));
		IsFalse(_provider.CanInsert(14));
	}

	[TestMethod]
	public void InsertionPossibleAtEndOfReadOnlySegment()
	{
		_segments.Add(new TextRange { StartOffset = 10, EndIndex = 15 });
		IsTrue(_provider.CanInsert(15));
	}

	[TestMethod]
	public void InsertionPossibleAtStartOfReadOnlySegment()
	{
		_segments.Add(new TextRange { StartOffset = 10, EndIndex = 15 });
		IsTrue(_provider.CanInsert(10));
	}

	[TestMethod]
	public void InsertionPossibleBeforeReadOnlySegment()
	{
		_segments.Add(new TextRange { StartOffset = 10, EndIndex = 15 });
		IsTrue(_provider.CanInsert(5));
	}

	[TestMethod]
	public void InsertionPossibleBetweenReadOnlySegments()
	{
		_segments.Add(new TextRange { StartOffset = 10, EndIndex = 15 });
		_segments.Add(new TextRange { StartOffset = 15, EndIndex = 20 });
		IsTrue(_provider.CanInsert(15));
	}

	[TestMethod]
	public void InsertionPossibleWhenNothingIsReadOnly()
	{
		IsTrue(_provider.CanInsert(0));
		IsTrue(_provider.CanInsert(100));
	}

	[TestInitialize]
	public void SetUp()
	{
		_segments = [];
		_provider = new TextSegmentReadOnlySectionProvider<TextRange>(_segments);
	}

	#endregion
}