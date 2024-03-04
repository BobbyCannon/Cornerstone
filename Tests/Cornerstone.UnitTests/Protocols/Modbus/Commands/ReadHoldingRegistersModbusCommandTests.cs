#region References

using Cornerstone.Protocols.Modbus.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Protocols.Modbus.Commands;

[TestClass]
public class ReadHoldingRegistersModbusCommandTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ToBuffer()
	{
		var command = new ReadHoldingRegistersModbusCommand(1, 18, 1);
		var expected = new byte[] { 0x01, 0x03, 0x00, 0x12, 0x00, 0x01, 0x24, 0x0F };
		var actual = command.ToBuffer();
		AreEqual(expected, actual);
	}

	#endregion
}

[TestClass]
public class ReadHoldingRegistersModbusResponseTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void FromToBuffer()
	{
		var command = new ReadHoldingRegistersModbusCommand(1, 18, 1);
		var expected = new byte[] { 0x01, 0x03, 0x00, 0x12, 0x00, 0x01, 0x24, 0x0F };
		var actual = command.ToBuffer();
		AreEqual(expected, actual);
	}

	#endregion
}