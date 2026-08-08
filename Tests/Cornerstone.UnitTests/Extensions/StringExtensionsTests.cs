#region References

using Cornerstone.Extensions;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class StringExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void StableHashCode()
	{
		var scenarios = new (string Value, int HashCode)[]
		{
			("aoeu", 701784124),
			("foo bar", 1715228275),
			("Foo Bar", 1716311859),
			("hello world", 1118511802),
			("The quick brown foxed jumped over the lazy dog's back.", -1650481971)
		};

		foreach (var scenario in scenarios)
		{
			scenario.Value.Dump();
			var actual = scenario.Value.GetStableHashCode();
			AreEqual(scenario.HashCode, actual,
				() => $"{scenario.Value} - {scenario.HashCode} != {actual}"
			);
		}
	}

	#endregion
}