#region References

using Cornerstone.Security.SecurityKeys.Apdu.Commands;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Security.SecurityKeys;

[TestClass]
public class WriteBytesCommandTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ToByteArray()
	{
		var scenarios = new (byte[] expected, WriteBytesCommand command)[]
		{
			(
				[0xFF, 0xD6, 0x00, 1, 16, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16],
				new WriteBytesCommand(1, [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16])
			)
		};

		foreach (var scenario in scenarios)
		{
			var actual = scenario.command.ToByteArray();
			AreEqual(scenario.expected, actual, () => actual.DumpCSharp());
		}
	}

	#endregion
}