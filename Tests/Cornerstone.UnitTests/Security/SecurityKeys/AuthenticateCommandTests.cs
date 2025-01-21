#region References

using Cornerstone.Security.SecurityKeys.Apdu.Commands;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Security.SecurityKeys;

[TestClass]
public class AuthenticateCommandTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ToByteArray()
	{
		var scenarios = new (byte[] expected, AuthenticateCommand command)[]
		{
			// FF-86-00-00-05-01-00-00-60-00
			([0xFF, 0x86, 0x00, 0x00, 0x05, 0x01, 0x00, 0x00, 0x60, 0x00], new AuthenticateCommand(0)),
			([0xFF, 0x86, 0x00, 0x00, 0x05, 0x01, 0x12, 0x34, 0x60, 0x00], new AuthenticateCommand(0x1234))
		};

		foreach (var scenario in scenarios)
		{
			var actual = scenario.command.ToByteArray();
			AreEqual(scenario.expected, actual, () => actual.DumpJson());
		}
	}

	#endregion
}