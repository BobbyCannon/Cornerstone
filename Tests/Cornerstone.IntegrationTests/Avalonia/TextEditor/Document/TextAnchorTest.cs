#region References

using System;
using System.Collections.Generic;
using Cornerstone.Text.Document;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.IntegrationTests.Avalonia.TextEditor.Document;

[TestClass]
public class TextAnchorTest : CornerstoneUnitTest
{
	#region Fields

	private TextEditorDocument _document;
	private static Random _random;

	#endregion

	#region Methods

	[TestMethod]
	public void AnchorInEmptyDocument()
	{
		var a1 = _document.CreateAnchor(0);
		var a2 = _document.CreateAnchor(0);
		a1.MovementType = AnchorMovementType.BeforeInsertion;
		a2.MovementType = AnchorMovementType.AfterInsertion;
		AreEqual(0, a1.Offset);
		AreEqual(0, a2.Offset);
		_document.Insert(0, "x");
		AreEqual(0, a1.Offset);
		AreEqual(1, a2.Offset);
	}

	[TestMethod]
	public void AnchorsSurviveDeletion()
	{
		_document.Text = new string(' ', 10);
		var a1 = new TextAnchor[11];
		var a2 = new TextAnchor[11];
		for (var i = 0; i < 11; i++)
		{
			//Console.WriteLine("Insert first at i = " + i);
			a1[i] = _document.CreateAnchor(i);
			a1[i].SurviveDeletion = true;
			//Console.WriteLine(document.GetTextAnchorTreeAsString());
			//Console.WriteLine("Insert second at i = " + i);
			a2[i] = _document.CreateAnchor(i);
			a2[i].SurviveDeletion = false;
			//Console.WriteLine(document.GetTextAnchorTreeAsString());
		}
		for (var i = 0; i < 11; i++)
		{
			AreEqual(i, a1[i].Offset);
			AreEqual(i, a2[i].Offset);
		}
		_document.Remove(1, 8);
		for (var i = 0; i < 11; i++)
		{
			if (i <= 1)
			{
				IsFalse(a1[i].IsDeleted);
				IsFalse(a2[i].IsDeleted);
				AreEqual(i, a1[i].Offset);
				AreEqual(i, a2[i].Offset);
			}
			else if (i <= 8)
			{
				IsFalse(a1[i].IsDeleted);
				IsTrue(a2[i].IsDeleted);
				AreEqual(1, a1[i].Offset);
			}
			else
			{
				IsFalse(a1[i].IsDeleted);
				IsFalse(a2[i].IsDeleted);
				AreEqual(i - 8, a1[i].Offset);
				AreEqual(i - 8, a2[i].Offset);
			}
		}
	}

	[ClassInitialize]
	public static void ClassInitialize(TestContext context)
	{
		var seed = Environment.TickCount;
		Console.WriteLine("TextAnchorTest Seed: " + seed);
		_random = new Random(seed);
	}

	[TestMethod]
	public void CreateAnchors()
	{
		List<TextAnchor> anchors = [];
		var expectedOffsets = new List<int>();
		_document.Text = new string(' ', 1000);
		for (var i = 0; i < 1000; i++)
		{
			var offset = _random.Next(1000);
			anchors.Add(_document.CreateAnchor(offset));
			expectedOffsets.Add(offset);
		}
		for (var i = 0; i < anchors.Count; i++)
		{
			AreEqual(expectedOffsets[i], anchors[i].Offset);
		}
		GC.KeepAlive(anchors);
	}

	[TestMethod]
	public void CreateAndGarbageCollectAnchors()
	{
		List<TextAnchor> anchors = [];
		var expectedOffsets = new List<int>();
		_document.Text = new string(' ', 1000);
		for (var t = 0; t < 250; t++)
		{
			var c = _random.Next(50);
			if (_random.Next(2) == 0)
			{
				for (var i = 0; i < c; i++)
				{
					var offset = _random.Next(1000);
					anchors.Add(_document.CreateAnchor(offset));
					expectedOffsets.Add(offset);
				}
			}
			else if (c <= anchors.Count)
			{
				anchors.RemoveRange(0, c);
				expectedOffsets.RemoveRange(0, c);
				GC.Collect();
			}
			for (var j = 0; j < anchors.Count; j++)
			{
				AreEqual(expectedOffsets[j], anchors[j].Offset);
			}
		}
		GC.KeepAlive(anchors);
	}

	[TestMethod]
	public void CreateAndMoveAnchors()
	{
		List<TextAnchor> anchors = [];
		var expectedOffsets = new List<int>();
		_document.Text = new string(' ', 1000);
		for (var t = 0; t < 250; t++)
		{
			//Console.Write("t = " + t + " ");
			var c = _random.Next(50);
			switch (_random.Next(5))
			{
				case 0:
					//Console.WriteLine("Add c=" + c + " anchors");
					for (var i = 0; i < c; i++)
					{
						var offset = _random.Next(_document.TextLength);
						var anchor = _document.CreateAnchor(offset);
						if (_random.Next(2) == 0)
						{
							anchor.MovementType = AnchorMovementType.BeforeInsertion;
						}
						else
						{
							anchor.MovementType = AnchorMovementType.AfterInsertion;
						}
						anchor.SurviveDeletion = _random.Next(2) == 0;
						anchors.Add(anchor);
						expectedOffsets.Add(offset);
					}
					break;
				case 1:
					if (c <= anchors.Count)
					{
						//Console.WriteLine("Remove c=" + c + " anchors");
						anchors.RemoveRange(0, c);
						expectedOffsets.RemoveRange(0, c);
						GC.Collect();
					}
					break;
				case 2:
					var insertOffset = _random.Next(_document.TextLength);
					var insertLength = _random.Next(1000);
					//Console.WriteLine("insertOffset=" + insertOffset + " insertLength="+insertLength);
					_document.Insert(insertOffset, new string(' ', insertLength));
					for (var i = 0; i < anchors.Count; i++)
					{
						if (anchors[i].MovementType == AnchorMovementType.BeforeInsertion)
						{
							if (expectedOffsets[i] > insertOffset)
							{
								expectedOffsets[i] += insertLength;
							}
						}
						else
						{
							if (expectedOffsets[i] >= insertOffset)
							{
								expectedOffsets[i] += insertLength;
							}
						}
					}
					break;
				case 3:
					var removalOffset = _random.Next(_document.TextLength);
					var removalLength = _random.Next(_document.TextLength - removalOffset);
					//Console.WriteLine("RemovalOffset=" + removalOffset + " RemovalLength="+removalLength);
					_document.Remove(removalOffset, removalLength);
					for (var i = anchors.Count - 1; i >= 0; i--)
					{
						if ((expectedOffsets[i] > removalOffset) && (expectedOffsets[i] < (removalOffset + removalLength)))
						{
							if (anchors[i].SurviveDeletion)
							{
								expectedOffsets[i] = removalOffset;
							}
							else
							{
								IsTrue(anchors[i].IsDeleted);
								anchors.RemoveAt(i);
								expectedOffsets.RemoveAt(i);
							}
						}
						else if (expectedOffsets[i] > removalOffset)
						{
							expectedOffsets[i] -= removalLength;
						}
					}
					break;
				case 4:
					var replaceOffset = _random.Next(_document.TextLength);
					var replaceRemovalLength = _random.Next(_document.TextLength - replaceOffset);
					var replaceInsertLength = _random.Next(1000);
					//Console.WriteLine("ReplaceOffset=" + replaceOffset + " RemovalLength="+replaceRemovalLength + " InsertLength=" + replaceInsertLength);
					_document.Replace(replaceOffset, replaceRemovalLength, new string(' ', replaceInsertLength));
					for (var i = anchors.Count - 1; i >= 0; i--)
					{
						if ((expectedOffsets[i] > replaceOffset) && (expectedOffsets[i] < (replaceOffset + replaceRemovalLength)))
						{
							if (anchors[i].SurviveDeletion)
							{
								if (anchors[i].MovementType == AnchorMovementType.AfterInsertion)
								{
									expectedOffsets[i] = replaceOffset + replaceInsertLength;
								}
								else
								{
									expectedOffsets[i] = replaceOffset;
								}
							}
							else
							{
								IsTrue(anchors[i].IsDeleted);
								anchors.RemoveAt(i);
								expectedOffsets.RemoveAt(i);
							}
						}
						else if (expectedOffsets[i] > replaceOffset)
						{
							expectedOffsets[i] += replaceInsertLength - replaceRemovalLength;
						}
						else if ((expectedOffsets[i] == replaceOffset) && (replaceRemovalLength == 0) && (anchors[i].MovementType == AnchorMovementType.AfterInsertion))
						{
							expectedOffsets[i] += replaceInsertLength - replaceRemovalLength;
						}
					}
					break;
			}
			AreEqual(anchors.Count, expectedOffsets.Count);
			for (var j = 0; j < anchors.Count; j++)
			{
				AreEqual(expectedOffsets[j], anchors[j].Offset);
			}
		}
		GC.KeepAlive(anchors);
	}

	[TestMethod]
	public void MoveAnchorsDuringReplace()
	{
		_document.Text = "abcd";
		var start = _document.CreateAnchor(1);
		var middleDeletable = _document.CreateAnchor(2);
		var middleSurvivorLeft = _document.CreateAnchor(2);
		middleSurvivorLeft.SurviveDeletion = true;
		middleSurvivorLeft.MovementType = AnchorMovementType.BeforeInsertion;
		var middleSurvivorRight = _document.CreateAnchor(2);
		middleSurvivorRight.SurviveDeletion = true;
		middleSurvivorRight.MovementType = AnchorMovementType.AfterInsertion;
		var end = _document.CreateAnchor(3);
		_document.Replace(1, 2, "BxC");

		AreEqual(1, start.Offset);
		IsTrue(middleDeletable.IsDeleted);
		AreEqual(1, middleSurvivorLeft.Offset);
		AreEqual(4, middleSurvivorRight.Offset);
		AreEqual(4, end.Offset);
	}

	[TestMethod]
	public void RepeatedTextDragDrop()
	{
		_document.Text = new string(' ', 1000);
		for (var i = 0; i < 20; i++)
		{
			var a = _document.CreateAnchor(144);
			var b = _document.CreateAnchor(157);
			_document.Insert(128, new string('a', 13));
			_document.Remove(157, 13);
			a = _document.CreateAnchor(128);
			b = _document.CreateAnchor(141);

			_document.Insert(157, new string('b', 13));
			_document.Remove(128, 13);

			a = null;
			b = null;
			if ((i % 5) == 0)
			{
				GC.Collect();
			}
		}
	}

	[TestMethod]
	public void ReplaceSpacesWithTab()
	{
		_document.Text = "a    b";
		var before = _document.CreateAnchor(1);
		before.MovementType = AnchorMovementType.AfterInsertion;
		var after = _document.CreateAnchor(5);
		var survivingMiddle = _document.CreateAnchor(2);
		var deletedMiddle = _document.CreateAnchor(3);

		_document.Replace(1, 4, "\t", OffsetChangeMappingType.CharacterReplace);
		AreEqual("a\tb", _document.Text);
		// yes, the movement is a bit strange; but that's how CharacterReplace works when the text gets shorter
		AreEqual(1, before.Offset);
		AreEqual(2, after.Offset);
		AreEqual(2, survivingMiddle.Offset);
		AreEqual(2, deletedMiddle.Offset);
	}

	[TestMethod]
	public void ReplaceTwoCharactersWithThree()
	{
		_document.Text = "a12b";
		var before = _document.CreateAnchor(1);
		before.MovementType = AnchorMovementType.AfterInsertion;
		var after = _document.CreateAnchor(3);
		before.MovementType = AnchorMovementType.BeforeInsertion;
		var middleB = _document.CreateAnchor(2);
		before.MovementType = AnchorMovementType.BeforeInsertion;
		var middleA = _document.CreateAnchor(2);
		before.MovementType = AnchorMovementType.AfterInsertion;

		_document.Replace(1, 2, "123", OffsetChangeMappingType.CharacterReplace);
		AreEqual("a123b", _document.Text);
		AreEqual(1, before.Offset);
		AreEqual(4, after.Offset);
		AreEqual(2, middleA.Offset);
		AreEqual(2, middleB.Offset);
	}

	[TestInitialize]
	public void SetUp()
	{
		_document = new TextEditorDocument();
	}

	#endregion
}