﻿#region References

using System;
using System.Collections.Generic;
using Cornerstone.Compare;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Protocols.Osc;
using Cornerstone.Testing;
using Cornerstone.UnitTests.Protocols.Samples;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Protocols.Osc;

[TestClass]
public class SampleOscCommandTests : CornerstoneUnitTest
{
	#region Constructors

	public SampleOscCommandTests()
	{
		ComparerSettings = new ComparerSettings
		{
			TypeIncludeExcludeSettings = new Dictionary<Type, IncludeExcludeSettings>
			{
				{ typeof(SampleOscCommand), new IncludeExcludeSettings(null, [nameof(SampleOscCommand.Time), nameof(SampleOscCommand.HasBeenRead), nameof(SampleOscCommand.HasBeenUpdated)]) }
			}
		};
	}

	#endregion

	#region Properties

	private ComparerSettings ComparerSettings { get; }

	#endregion

	#region Methods

	[TestMethod]
	public void DateTimeSwitch()
	{
		var command = new SampleOscCommand { BirthDate = new DateTime(2021, 02, 17, 08, 56, 32, 123, DateTimeKind.Local) };
		var expected = "/sample,3,null,{ Time: 2021-02-17T13:56:32.1230000Z },False,{ SampleValue: 0,0,0 },{ Time: 1900-01-01T00:00:00.0000000Z }";
		var actual = command.ToString();

		AreEqual(expected, actual);

		command.Timestamp = command.BirthDate.ToOscTimeTag();
		expected = "/sample,3,null,{ Time: 2021-02-17T13:56:32.1230000Z },False,{ SampleValue: 0,0,0 },{ Time: 2021-02-17T13:56:32.1230000Z }";
		actual = command.ToString();

		AreEqual(expected, actual);

		command = OscCommand.FromMessage<SampleOscCommand>(expected, new OscArgumentParser<SampleCustomValue>());
		AreEqual(new DateTime(2021, 02, 17, 13, 56, 32, 123, DateTimeKind.Utc), command.BirthDate);
		AreEqual(new OscTimeTag(new DateTime(2021, 02, 17, 13, 56, 32, 123, DateTimeKind.Utc)), command.Timestamp);

		command.Timestamp = new OscTimeTag(new DateTime(2021, 02, 17, 01, 13, 12, 762, DateTimeKind.Utc));
		expected = "/sample,3,null,{ Time: 2021-02-17T13:56:32.1230000Z },False,{ SampleValue: 0,0,0 },{ Time: 2021-02-17T01:13:12.7620000Z }";
		actual = command.ToString();

		AreEqual(expected, actual);

		command = OscCommand.FromMessage<SampleOscCommand>(expected, new OscArgumentParser<SampleCustomValue>());
		AreEqual(new DateTime(2021, 02, 17, 13, 56, 32, 123, DateTimeKind.Utc), command.BirthDate);
		AreEqual(new OscTimeTag(new DateTime(2021, 02, 17, 01, 13, 12, 762, DateTimeKind.Utc)), command.Timestamp);

		command.BirthDate = command.Timestamp.ToDateTime();
		expected = "/sample,3,null,{ Time: 2021-02-17T01:13:12.7620000Z },False,{ SampleValue: 0,0,0 },{ Time: 2021-02-17T01:13:12.7620000Z }";
		actual = command.ToString();

		AreEqual(expected, actual);

		command = OscCommand.FromMessage<SampleOscCommand>(expected, new OscArgumentParser<SampleCustomValue>());
		AreEqual(new DateTime(2021, 02, 17, 01, 13, 12, 762, DateTimeKind.Utc), command.BirthDate);
		AreEqual(new OscTimeTag(new DateTime(2021, 02, 17, 01, 13, 12, 762, DateTimeKind.Utc)), command.Timestamp);
	}

	[TestMethod]
	public void DowngradeCommand()
	{
		var data = "/sample,3,\"Bob\",{ Time: 2020-02-14T11:36:15.0000000Z },True,{ SampleValue: 1,2,3 },{ Time: 2020-02-14T11:36:16.123Z }";
		var parser = new OscArgumentParser<SampleCustomValue>();
		var message = OscPacket.Parse(data, parser) as OscMessage;
		IsNotNull(message);

		var command = OscCommand.FromMessage<SampleOscCommand>(message);
		command.Version = 2;
		var expected = "/sample,2,\"Bob\",{ Time: 2020-02-14T11:36:15.0000000Z },True";
		var actual = command.ToString();
		AreEqual(expected, actual);

		command = OscCommand.FromMessage<SampleOscCommand>(message);
		command.Version = 1;
		expected = "/sample,1,\"Bob\"";
		actual = command.ToString();
		AreEqual(expected, actual);
	}

	[TestMethod]
	public void MinimumTime()
	{
		var expected = new SampleOscCommand
		{
			BirthDate = DateTime.MinValue + TimeSpan.FromTicks(1),
			Timestamp = OscTimeTag.MinValue
		};
		var value = expected.ToMessage().ToString();
		value.Dump();
		var parser = new OscArgumentParser<SampleCustomValue>();
		var actual = OscCommand.FromMessage<SampleOscCommand>(value, parser);
		AreEqual(expected.BirthDate, actual.BirthDate);
	}

	[TestMethod]
	public void ParseWithArgumentParser()
	{
		var data = "/sample,3,\"Bob\",{ Time: 2020-02-14T11:36:15.0000000Z },True,{ SampleValue: 1,2,3 }";
		var message = OscPacket.Parse(data, new OscArgumentParser<SampleCustomValue>()) as OscMessage;
		IsNotNull(message);

		var actual = OscCommand.FromMessage<SampleOscCommand>(message);
		AreEqual(new DateTime(2020, 02, 14, 11, 36, 15, DateTimeKind.Utc), actual.BirthDate);
		AreEqual(new SampleCustomValue(1, 2, 3), actual.Value);
	}

	[TestMethod]
	public void ParseWithoutArgumentParser()
	{
		var data = "/sample,3,\"Bob\",{ Time: 2020-02-14T11:36:15.0000000Z },True,{ SampleValue: 1,2,3 }";
		var message = OscPacket.Parse(data) as OscMessage;
		IsNotNull(message);

		ExpectedException<InvalidCastException>(() => OscCommand.FromMessage<SampleOscCommand>(message),
			"Unable to cast object of type 'System.String' to type 'Cornerstone.UnitTests.Protocols.Samples.SampleCustomValue'.",
			"Specified cast is not valid."
		);
	}

	[TestMethod]
	public void Version1()
	{
		var command = new SampleOscCommand { Version = 1, Name = "Bob" };
		var expected = "/sample,1,\"Bob\"";
		var actual = command.ToString();
		AreEqual(expected, actual);

		var expected2 = new byte[] { 0x2F, 0x73, 0x61, 0x6D, 0x70, 0x6C, 0x65, 0x00, 0x2C, 0x69, 0x73, 0x00, 0x00, 0x00, 0x00, 0x01, 0x42, 0x6F, 0x62, 0x00 };
		var actual2 = command.ToMessage().ToByteArray();
		actual2.Dump();
		AreEqual(expected2, actual2);
		AreEqual(0, actual2.Length % 4);

		var actualCommand = OscCommand.FromMessage<SampleOscCommand>((OscMessage) OscPacket.Parse(expected));
		AreEqual(command, actualCommand, settings: ComparerSettings);
		actualCommand = OscCommand.FromMessage<SampleOscCommand>((OscMessage) OscPacket.Parse(expected2));
		AreEqual(command, actualCommand, settings: ComparerSettings);
	}

	[TestMethod]
	public void Version2()
	{
		var command = new SampleOscCommand { Version = 2, Name = "Bob", BirthDate = new DateTime(2020, 02, 14, 11, 36, 15, DateTimeKind.Utc), Enabled = true };
		var expected = "/sample,2,\"Bob\",{ Time: 2020-02-14T11:36:15.0000000Z },True";
		var actual = command.ToString();
		AreEqual(expected, actual);

		var expected2 = new byte[] { 0x2F, 0x73, 0x61, 0x6D, 0x70, 0x6C, 0x65, 0x00, 0x2C, 0x69, 0x73, 0x74, 0x54, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x42, 0x6F, 0x62, 0x00, 0xE1, 0xF1, 0x04, 0xAF, 0x00, 0x00, 0x00, 0x00 };
		var actual2 = command.ToMessage().ToByteArray();
		actual2.Dump();
		AreEqual(expected2, actual2);
		AreEqual(0, actual2.Length % 4);

		var actualCommand = OscCommand.FromMessage<SampleOscCommand>((OscMessage) OscPacket.Parse(expected));
		AreEqual(command, actualCommand, settings: ComparerSettings);
		actualCommand = OscCommand.FromMessage<SampleOscCommand>((OscMessage) OscPacket.Parse(expected2));
		AreEqual(command, actualCommand, settings: ComparerSettings);
	}

	[TestMethod]
	public void Version3()
	{
		var birthdate = "2020-02-14T11:36:15.3214567Z".ToUtcDateTime();
		var command = new SampleOscCommand { Version = 3, Name = "Bob", BirthDate = birthdate, Enabled = true, Timestamp = new OscTimeTag(new DateTime(2020, 02, 14, 11, 36, 15, 456, DateTimeKind.Utc)), Value = new SampleCustomValue(1, 2, 3) };
		var expected = "/sample,3,\"Bob\",{ Time: 2020-02-14T11:36:15.3214567Z },True,{ SampleValue: 1,2,3 },{ Time: 2020-02-14T11:36:15.4560000Z }";
		var actual = command.ToString();
		AreEqual(expected, actual);

		var expected2 = new byte[] { 0x2F, 0x73, 0x61, 0x6D, 0x70, 0x6C, 0x65, 0x00, 0x2C, 0x69, 0x73, 0x74, 0x54, 0x61, 0x74, 0x00, 0x00, 0x00, 0x00, 0x03, 0x42, 0x6F, 0x62, 0x00, 0xE1, 0xF1, 0x04, 0xAF, 0x52, 0x4A, 0xFC, 0x7D, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x03, 0xE1, 0xF1, 0x04, 0xAF, 0x74, 0xBC, 0x6A, 0x7E };
		var actual2 = command.ToMessage().ToByteArray();
		actual2.ToHexString(prefix: "0x", delimiter: ", ").Dump();
		AreEqual(expected2, actual2);
		AreEqual(0, actual2.Length % 4);

		var parser = new OscArgumentParser<SampleCustomValue>();
		var actualCommand = OscCommand.FromMessage<SampleOscCommand>((OscMessage) OscPacket.Parse(expected, parser));
		AreEqual(command, actualCommand, settings: ComparerSettings);
		actualCommand = OscCommand.FromMessage<SampleOscCommand>((OscMessage) OscPacket.Parse(expected2, parser));
		AreEqual(command, actualCommand, settings: ComparerSettings);
	}

	#endregion
}