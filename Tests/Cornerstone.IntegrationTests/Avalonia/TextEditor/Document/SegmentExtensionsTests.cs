#region References

using Cornerstone.Testing;
using Cornerstone.Text.Document;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.IntegrationTests.Avalonia.TextEditor.Document;

[TestClass]
public class SegmentExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void EndOffset()
	{
		
	}

	[TestMethod]
	public void Contains()
	{
		var scenarios = new (string name, SimpleRange left, SimpleRange right, bool expected)[]
		{
			("1", new SimpleRange(0, 1), new SimpleRange(1, 0), true),
			("2", new SimpleRange(0, 0), new SimpleRange(1, 0), false),
			("3", new SimpleRange(0, 3), new SimpleRange(1, 2), true),
			("4", new SimpleRange(0, 3), new SimpleRange(0, 2), true),
			("5", new SimpleRange(0, 3), new SimpleRange(3, 0), true),
			// right.offset should end of left.endOffset
			("6", new SimpleRange(10, 10), new SimpleRange(9, 1), true),
			// Just one offset to less
			("7", new SimpleRange(10, 10), new SimpleRange(9, 0), false),
			// Just one offset too many
			("8", new SimpleRange(10, 10), new SimpleRange(21, 0), false)
		};

		foreach (var scenario in scenarios)
		{
			scenario.name.Dump();
			var actual = scenario.left.Contains(scenario.right);
			AreEqual(scenario.expected, actual, $"Scenario {scenario.name} {actual}");
		}
	}

	[TestMethod]
	public void GetOverlap()
	{
		var scenarios = new (string name, SimpleRange left, SimpleRange right, SimpleRange expected)[]
		{
			("1", new SimpleRange(0, 1), new SimpleRange(1, 0), new SimpleRange(1, 0)),
			("2", new SimpleRange(0, 0), new SimpleRange(1, 0), SegmentExtensions.Invalid),
			("3", new SimpleRange(0, 3), new SimpleRange(1, 2), new SimpleRange(1, 2)),
			("4", new SimpleRange(0, 3), new SimpleRange(0, 2), new SimpleRange(1, 1)),
			("5", new SimpleRange(0, 3), new SimpleRange(3, 1), new SimpleRange(3, 0)),
			("6", new SimpleRange(10, 10), new SimpleRange(9, 1), new SimpleRange(10, 0)),
			// Just one offset to less
			("7", new SimpleRange(10, 10), new SimpleRange(9, 0), SegmentExtensions.Invalid),
			// Just one offset too many
			("8", new SimpleRange(10, 10), new SimpleRange(21, 0), SegmentExtensions.Invalid)
		};

		foreach (var scenario in scenarios)
		{
			scenario.name.Dump();
			var actual = scenario.left.GetOverlap(scenario.right);
			AreEqual(scenario.expected, actual, $"Scenario {scenario.name} {actual}");
		}
	}

	#endregion
}