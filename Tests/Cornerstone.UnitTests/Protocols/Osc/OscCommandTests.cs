#region References

using System;
using System.Collections.Generic;
using System.IO.Ports;
using Cornerstone.Compare;
using Cornerstone.Data;
using Cornerstone.Protocols.Osc;
using Cornerstone.Testing;
using Cornerstone.UnitTests.Protocols.Samples;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Protocols.Osc;

[TestClass]
public class OscCommandTests : CornerstoneUnitTest
{
	#region Constructors

	public OscCommandTests()
	{
		ComparerSettings = new ComparerSettings
		{
			IncludeExcludeOptions = new Dictionary<Type, IncludeExcludeSettings>
			{
				{ typeof(TestOscCommand), new IncludeExcludeSettings(null, [nameof(OscCommand.HasBeenRead), nameof(OscCommand.HasBeenUpdated)]) }
			}
		};
	}

	#endregion

	#region Properties

	public ComparerSettings ComparerSettings { get; }

	#endregion

	#region Methods

	[TestMethod]
	public void GetArgumentWithAllTypes()
	{
		var message = new OscMessage(TestOscCommand.Command, 1, (ulong) 23, "John", 20, new DateTime(2000, 01, 15, 0, 0, 0, DateTimeKind.Utc), 5.11f, 164.32,
			(byte) 4, new byte[] { 0, 1, 1, 2, 3, 5, 8, 13 }, true, Guid.Parse("E3966202-40FA-443D-B21F-E1528A1E6DFE"),
			uint.MaxValue, long.MaxValue, TimeSpan.Parse("12:34:56"), SerialError.Overrun, 123.4567890123m,
			decimal.MinValue, decimal.MaxValue, (sbyte) 5
		);

		var command = new TestOscCommand();
		command.Load(message);
		command.StartArgumentProcessing();
		AreEqual(1, command.GetArgument<int>());
		AreEqual((ulong) 23, command.GetArgument<ulong>());
		AreEqual("John", command.GetArgument<string>());
		AreEqual(20, command.GetArgument<int>());
		AreEqual(new DateTime(2000, 01, 15), command.GetArgument<DateTime>());
		AreEqual(5.11f, command.GetArgument<float>());
		AreEqual(164.32, command.GetArgument<double>());
		AreEqual((byte) 4, command.GetArgument<byte>());
		AreEqual(new byte[] { 0, 1, 1, 2, 3, 5, 8, 13 }, command.GetArgumentAsBlob());
		AreEqual(true, command.GetArgument<bool>());
		AreEqual(Guid.Parse("E3966202-40FA-443D-B21F-E1528A1E6DFE"), command.GetArgument<Guid>());
		AreEqual(uint.MaxValue, command.GetArgument<uint>());
		AreEqual(long.MaxValue, command.GetArgument<long>());
		AreEqual(new TimeSpan(12, 34, 56), command.GetArgument<TimeSpan>());
		AreEqual(SerialError.Overrun, command.GetArgument<SerialError>());
		AreEqual(123.4567890123m, command.GetArgument<decimal>());
		AreEqual(decimal.MinValue, command.GetArgument<decimal>());
		AreEqual(decimal.MaxValue, command.GetArgument<decimal>());
		AreEqual((sbyte) 5, command.GetArgument<sbyte>());
	}

	[TestMethod]
	public void SequentialProcessingOfArguments()
	{
		var message = new OscMessage(TestOscCommand.Command, 2, (ulong) 23, "John", 20, new DateTime(2000, 01, 15, 0, 0, 0, DateTimeKind.Utc), 5.11f, 164.32,
			(byte) 4, new byte[] { 0, 1, 1, 2, 3, 5, 8, 13 }, true, Guid.Parse("E3966202-40FA-443D-B21F-E1528A1E6DFE"),
			uint.MaxValue, long.MaxValue, TimeSpan.Parse("12:34:56"), SerialError.Frame
		);

		var command = new TestOscCommand();
		command.Load(message);
		command.StartArgumentProcessing();
		AreEqual(2, command.GetArgumentAsInteger());
		AreEqual((ulong) 23, command.GetArgumentAsUnsignedLong());
		AreEqual("John", command.GetArgumentAsString());
		AreEqual(20, command.GetArgumentAsInteger());
		AreEqual(new DateTime(2000, 01, 15), command.GetArgumentAsDateTime());
		AreEqual(5.11f, command.GetArgumentAsFloat());
		AreEqual(164.32, command.GetArgumentAsDouble());
		AreEqual((byte) 4, command.GetArgumentAsByte());
		AreEqual(new byte[] { 0, 1, 1, 2, 3, 5, 8, 13 }, command.GetArgumentAsBlob());
		AreEqual(true, command.GetArgumentAsBoolean());
		AreEqual(Guid.Parse("E3966202-40FA-443D-B21F-E1528A1E6DFE"), command.GetArgumentAsGuid());
		AreEqual(uint.MaxValue, command.GetArgumentAsUnsignedInteger());
		AreEqual(long.MaxValue, command.GetArgumentAsLong());
		AreEqual(new TimeSpan(12, 34, 56), command.GetArgumentAsTimeSpan());
		AreEqual(8, command.GetArgumentAsInteger());
	}

	[TestMethod]
	public void ToFromByteArray()
	{
		var comparerSettings = new ComparerSettings
		{
			IncludeExcludeOptions = new Dictionary<Type, IncludeExcludeSettings>
			{
				{ typeof(TestOscCommand), new IncludeExcludeSettings(null, [nameof(OscCommand.HasBeenRead), nameof(OscCommand.HasBeenUpdated)]) }
			}
		};

		var command = GetTestCommand();
		var expected = new byte[] { 0x2F, 0x74, 0x65, 0x73, 0x74, 0x00, 0x00, 0x00, 0x2C, 0x69, 0x48, 0x73, 0x69, 0x74, 0x66, 0x64, 0x63, 0x62, 0x54, 0x73, 0x75, 0x68, 0x70, 0x69, 0x69, 0x69, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x17, 0x4A, 0x6F, 0x68, 0x6E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x14, 0xBC, 0x2A, 0x37, 0x00, 0x00, 0x00, 0x00, 0x00, 0x40, 0xA3, 0x85, 0x1F, 0x40, 0x64, 0x8A, 0x3D, 0x70, 0xA3, 0xD7, 0x0A, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x08, 0x00, 0x01, 0x01, 0x02, 0x03, 0x05, 0x08, 0x0D, 0x65, 0x33, 0x39, 0x36, 0x36, 0x32, 0x30, 0x32, 0x2D, 0x34, 0x30, 0x66, 0x61, 0x2D, 0x34, 0x34, 0x33, 0x64, 0x2D, 0x62, 0x32, 0x31, 0x66, 0x2D, 0x65, 0x31, 0x35, 0x32, 0x38, 0x61, 0x31, 0x65, 0x36, 0x64, 0x66, 0x65, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x69, 0x76, 0x85, 0x18, 0x00, 0x00, 0x00, 0x00, 0x02, 0xFF, 0xFF, 0x80, 0x00, 0x00, 0x00, 0xFF, 0xFF };
		var actual = command.ToMessage().ToByteArray();
		actual.Dump();
		AreEqual(expected, actual, settings: comparerSettings);

		var actualMessage = OscPacket.Parse(command.Time, actual) as OscMessage;
		Assert.IsNotNull(actualMessage, "Failed to parse the byte data.");

		var actualCommand = new TestOscCommand();
		actualCommand.Load(actualMessage);

		AreEqual(command, actualCommand, settings: comparerSettings);
	}

	[TestMethod]
	public void ToFromString()
	{
		var command = GetTestCommand();
		command.Version = 1;
		// Defaults for newer versions
		command.Elapsed = TimeSpan.FromSeconds(3);
		command.Error = SerialError.Overrun;
		command.ShortId = 0;
		command.UShortId = 0;
		var actual = command.ToString();
		//actual.Escape().Dump();
		AreEqual("/test,1,23U,\"John\",20,{ Time: 2000-01-15T00:00:00.0000000Z },5.11f,164.32d,'',{ Blob: 0x000101020305080D },True,\"e3966202-40fa-443d-b21f-e1528a1e6dfe\",4294967295u,9223372036854775807L", actual);
		var actualMessage = OscPacket.Parse(command.Time, actual) as OscMessage;
		Assert.IsNotNull(actualMessage);
		var actualCommand = new TestOscCommand();
		actualCommand.Load(actualMessage);
		AreEqual(command, actualCommand, settings: ComparerSettings);

		command = GetTestCommand();
		command.Version = 2;
		// Defaults for newer versions
		command.ShortId = 0;
		command.UShortId = 0;
		actual = command.ToString();
		//actual.Escape().Dump();
		AreEqual("/test,2,23U,\"John\",20,{ Time: 2000-01-15T00:00:00.0000000Z },5.11f,164.32d,\'\u0004\',{ Blob: 0x000101020305080D },True,\"e3966202-40fa-443d-b21f-e1528a1e6dfe\",4294967295u,9223372036854775807L,{ TimeSpan: 12:34:56 },2", actual);
		actualMessage = OscPacket.Parse(command.Time, actual) as OscMessage;
		Assert.IsNotNull(actualMessage);
		actualCommand = new TestOscCommand();
		actualCommand.Load(actualMessage);
		AreEqual(command, actualCommand, settings: ComparerSettings);

		command = GetTestCommand();
		command.Version = 3;
		actual = command.ToString();
		//actual.Escape().Dump();
		AreEqual("/test,3,23U,\"John\",20,{ Time: 2000-01-15T00:00:00.0000000Z },5.11f,164.32d,\'\u0004\',{ Blob: 0x000101020305080D },True,\"e3966202-40fa-443d-b21f-e1528a1e6dfe\",4294967295u,9223372036854775807L,{ TimeSpan: 12:34:56 },2,-32768,65535", actual);
		actualMessage = OscPacket.Parse(command.Time, actual) as OscMessage;
		Assert.IsNotNull(actualMessage);
		actualCommand = new TestOscCommand();
		actualCommand.Load(actualMessage);
		AreEqual(command, actualCommand, settings: ComparerSettings);
	}

	[TestMethod]
	public void ToMessageTimeShouldBeTimeOfInstantiation()
	{
		var command = GetTestCommand();
		var expected = command.Time;
		IncrementTime(seconds: 1);
		var actual = command.ToMessage().Time;
		expected.Dump();
		actual.Dump();
		AreEqual(expected, actual);
	}

	[TestMethod]
	public void ToStringShouldUpdate()
	{
		var command = GetTestCommand(x =>
		{
			x.Version = 1;
			x.Name = "Johnny";
			x.Age = 21;
		});

		var expectedTime = command.Time;
		var expected = "/test,1,23U,\"Johnny\",21,{ Time: 2000-01-15T00:00:00.0000000Z },5.11f,164.32d,'',{ Blob: 0x000101020305080D },True,\"e3966202-40fa-443d-b21f-e1528a1e6dfe\",4294967295u,9223372036854775807L";
		var actual = command.ToString();
		actual.Dump();
		AreEqual(expectedTime, command.Time);
		AreEqual(expected, actual);

		command = GetTestCommand(x =>
		{
			x.Version = 2;
			x.Name = "Jon";
			x.Age = 23;
		});

		expected = "/test,2,23U,\"Jon\",23,{ Time: 2000-01-15T00:00:00.0000000Z },5.11f,164.32d,'',{ Blob: 0x000101020305080D },True,\"e3966202-40fa-443d-b21f-e1528a1e6dfe\",4294967295u,9223372036854775807L,{ TimeSpan: 12:34:56 },2";
		actual = command.ToString();
		actual.Dump();
		AreEqual(expectedTime, command.Time);
		AreEqual(expected, actual);

		expected = "/test";
		actual = command.ToMessage(false).ToString();
		AreEqual(expectedTime, command.Time);
		AreEqual(expected, actual);
	}

	private static TestOscCommand GetTestCommand(Action<TestOscCommand> update = null)
	{
		var response = new TestOscCommand
		{
			Name = "John",
			Age = 20,
			BirthDate = new DateTime(2000, 01, 15, 0, 0, 0, DateTimeKind.Utc),
			Elapsed = new TimeSpan(12, 34, 56),
			Enable = true,
			Error = SerialError.Overrun,
			Height = 5.11f,
			Id = 23,
			SyncId = Guid.Parse("E3966202-40FA-443D-B21F-E1528A1E6DFE"),
			Time = new DateTime(2000, 01, 15, 11, 15, 56, DateTimeKind.Utc),
			Rating = 4,
			Values = [0, 1, 1, 2, 3, 5, 8, 13],
			Visits = uint.MaxValue,
			VoteId = long.MaxValue,
			Weight = 164.32,
			ShortId = short.MinValue,
			UShortId = ushort.MaxValue
		};

		update?.Invoke(response);

		return response;
	}

	#endregion
}