#region References

using Cornerstone.Protocols.Modbus.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Protocols.Modbus.Commands;

[TestClass]
public class ReadInputRegistersModbusCommandTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ToBuffer()
	{
		var command = new ReadInputRegistersModbusCommand(1, 20, 1);
		var expected = new byte[] { 0x01, 0x04, 0x00, 0x14, 0x00, 0x01, 0x71, 0xCE };
		var actual = command.ToBuffer();
		AreEqual(expected, actual);
	}

	#endregion
}