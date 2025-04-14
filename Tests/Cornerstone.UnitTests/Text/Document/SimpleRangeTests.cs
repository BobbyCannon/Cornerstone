#region References

using Cornerstone.Avalonia.TextEditor.Document;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Text.Document;

[TestClass]
public class SimpleRangeTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void FromIndexes()
	{
		var scenarios = new (int start, int end, int length)[]
		{
			(0, 0, 0),
			(1, 2, 1),
			(5, 8, 3),
			(6, 10, 4)
		};

		foreach (var scenario in scenarios)
		{
			scenario.DumpJson();

			var expected = new SimpleRange(scenario.start, scenario.length);
			AreEqual(scenario.start, expected.Offset);
			AreEqual(scenario.end, expected.EndIndex);
			AreEqual(scenario.length, expected.Length);

			var actual = SimpleRange.FromIndexes(scenario.start, scenario.end);
			AreEqual(scenario.start, actual.Offset);
			AreEqual(scenario.end, actual.EndIndex);
			AreEqual(scenario.length, actual.Length);

			AreEqual(expected, actual);
		}
	}

	#endregion
}