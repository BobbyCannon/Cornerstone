#region References

using Cornerstone.Security.SecurityKeys.Apdu.Commands;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Security.SecurityKeys;

[TestClass]
public class GetDataCommandTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ToByteArray()
	{
		var scenarios = new (byte[] expected, GetDataCommand command)[]
		{
			([0xFF, 0xCA, 0x00, 0x00, 0x00], new GetDataCommand(GetDataCommand.GetDataDataType.Uid)),
			([0xFF, 0xCA, 0x01, 0x00, 0x00], new GetDataCommand(GetDataCommand.GetDataDataType.HistoricalBytes))
		};

		foreach (var scenario in scenarios)
		{
			var actual = scenario.command.ToByteArray();
			AreEqual(scenario.expected, actual, () => actual.DumpCSharp());
		}
	}

	#endregion
}