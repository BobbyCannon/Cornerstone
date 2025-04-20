#region References

using System.Linq;
using Cornerstone.Security.SecurityKeys;
using Cornerstone.Security.SecurityKeys.Apdu.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Security.SecurityKeys;

[TestClass]
public class LoadKeysCommandTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ToByteArray()
	{
		var scenarios = new (byte[] expected, LoadKeysCommand command)[]
		{
			(
				// FF-82-00-00-06-FF-FF-FF-FF-FF-FF
				[0xFF, 0x82, 0x00, 0x00, 0x06, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF],
				new LoadKeysCommand(MiFareClassicSecurityCardReader.KeyA)
			)
		};

		foreach (var scenario in scenarios)
		{
			var actual = scenario.command.ToByteArray();
			AreEqual(scenario.expected, actual, () => string.Join(",", actual.Select(x => $"0x{x:X2}")));
		}
	}

	#endregion
}