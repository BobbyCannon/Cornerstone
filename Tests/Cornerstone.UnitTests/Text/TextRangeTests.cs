#region References

using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Text;

[TestClass]
public class TextRangeTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void BufferChangesShouldBeReflectedInRanges()
	{
		//                                              01234567890123456789012345678
		//                           012345678901234567890123456789012345678901234567
		var buffer = TextDocument.Load("The quick brown\r\nFox jumped over the lazy dog.");
		var expected = new[]
		{
			new TextRangeData { EndIndex = 15, Length = 15, Remaining = 31, StartIndex = 0 },
			new TextRangeData { EndIndex = 17, Length = 2, Remaining = 29, StartIndex = 15 },
			new TextRangeData { EndIndex = 46, Length = 29, Remaining = 0, StartIndex = 17 }
		};

		//AreEqual(expected, buffer.Tokenizer.Ranges);

		//var expectedValues = new[] { "The quick brown", "\r\n", "Fox jumped over the lazy dog." };
		//var actualValues = buffer.Ranges.Select(x => x.ToString());

		//AreEqual(expectedValues, actualValues);

		//buffer.Remove(0, 4);
		////actual.DumpJson();

		//expected =
		//[
		//	new TextRangeData { EndIndex = 11, Length = 11, Remaining = 31, StartIndex = 0 },
		//	new TextRangeData { EndIndex = 13, Length = 2, Remaining = 29, StartIndex = 11 },
		//	new TextRangeData { EndIndex = 42, Length = 29, Remaining = 0, StartIndex = 13 }
		//];

		//AreEqual(expected, buffer.Ranges);

		//expectedValues = ["quick brown", "\r\n", "Fox jumped over the lazy dog."];
		//actualValues = buffer.Ranges.Select(x => x.ToString());

		//AreEqual(expectedValues, actualValues);

		//// Remove the range, should remove the range from the list.
		//var middle = buffer.Ranges.Skip(1).First();
		//buffer.Remove(middle.StartIndex, middle.Length);
		//buffer.Ranges.DumpJson();

		//expected =
		//[
		//	new TextRangeData { EndIndex = 11, Length = 11, Remaining = 29, StartIndex = 0 },
		//	new TextRangeData { EndIndex = 40, Length = 29, Remaining = 0, StartIndex = 11 }
		//];

		//AreEqual(expected, buffer.Ranges);

		//expectedValues = ["quick brown", "Fox jumped over the lazy dog."];
		//actualValues = buffer.Ranges.Select(x => x.ToString());

		//AreEqual(expectedValues, actualValues);
	}

	#endregion
}