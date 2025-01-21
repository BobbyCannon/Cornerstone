#region References

using Cornerstone.Security.SecurityKeys.Apdu.Commands;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Security.SecurityKeys;

[TestClass]
public class ReadBinaryCommandTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ToByteArray()
	{
		var scenarios = new (byte[] expected, ReadBinaryCommand command)[]
		{
			([0xFF, 0xB0, 0x00, 0x00, 0x10], new ReadBinaryCommand(0, 0x10)),
			([0xFF, 0xB0, 0x00, 0xFF, 0x20], new ReadBinaryCommand(0xFF, 0x20))
		};

		foreach (var scenario in scenarios)
		{
			var actual = scenario.command.ToByteArray();
			AreEqual(scenario.expected, actual, () => actual.DumpCSharp());
		}
	}

	#endregion
}