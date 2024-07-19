#region References

using System;
using System.Collections.Generic;
using Cornerstone.Compare;
using Cornerstone.Data;
using Cornerstone.Protocols.Osc;
using Cornerstone.Testing;
using Cornerstone.UnitTests;
using Cornerstone.UnitTests.Protocols;
using Cornerstone.UnitTests.Protocols.Samples;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.IntegrationTests.Protocols.Osc;

[TestClass]
public class SampleOscCommandTests : CornerstoneUnitTest
{
	#region Constructors

	public SampleOscCommandTests()
	{
		ComparerOptions = new ComparerOptions
		{
			IncludeExcludeOptions = new Dictionary<Type, IncludeExcludeOptions>
			{
				{ typeof(SampleOscCommand), new IncludeExcludeOptions(null, [nameof(SampleOscCommand.Time), nameof(SampleOscCommand.HasBeenRead), nameof(SampleOscCommand.HasBeenUpdated)]) }
			}
		};
	}

	#endregion

	#region Properties

	private ComparerOptions ComparerOptions { get; }

	#endregion

	#region Methods

	[TestMethod]
	public void DateTimeMinMaxTests()
	{
		var originalZone = TimeZoneHelper.GetSystemTimeZone();

		try
		{
			var timeZones = new[] { "Pacific Standard Time", "Central Standard Time", "Eastern Standard Time" };

			foreach (var zone in timeZones)
			{
				zone.Dump();

				TimeZoneHelper.SetSystemTimeZone(zone);

				var command = new SampleOscCommand { BirthDate = DateTime.MinValue, Timestamp = OscTimeTag.MinValue };
				var expected = "/sample,3,null,{ Time: 0001-01-01T00:00:00.0000000Z },False,{ SampleValue: 0,0,0 },{ Time: 1900-01-01T00:00:00.0000000Z }";
				var actual = command.ToMessage().ToString();
				Assert.AreEqual(expected, actual);

				var actualMessage = OscPacket.Parse(expected, new OscArgumentParser<SampleCustomValue>()) as OscMessage;
				var actualCommand = OscCommand.FromMessage<SampleOscCommand>(actualMessage);
				Assert.AreEqual(DateTime.MinValue, actualCommand.BirthDate);

				command = new SampleOscCommand { BirthDate = DateTime.MaxValue, Timestamp = OscTimeTag.MaxValue };
				expected = "/sample,3,null,{ Time: 9999-12-31T23:59:59.9999999Z },False,{ SampleValue: 0,0,0 },{ Time: 2036-02-07T06:28:16.0000000Z }";
				actual = command.ToMessage().ToString();
				Assert.AreEqual(expected, actual);

				actualMessage = OscPacket.Parse(expected, new OscArgumentParser<SampleCustomValue>()) as OscMessage;
				actualCommand = OscCommand.FromMessage<SampleOscCommand>(actualMessage);
				Assert.AreEqual(DateTime.MaxValue, actualCommand.BirthDate);
			}
		}
		finally
		{
			TimeZoneHelper.SetSystemTimeZone(originalZone);
		}
	}

	[TestMethod]
	public void DateTimeZoneTest()
	{
		SetTime(new DateTime(2021, 02, 18, 01, 54, 00, DateTimeKind.Utc));

		var originalZone = TimeZoneHelper.GetSystemTimeZone();

		try
		{
			TimeZoneHelper.SetSystemTimeZone("Pacific Standard Time");

			var command = new SampleOscCommand
			{
				BirthDate = new DateTime(1970, 01, 02, 0, 0, 0, DateTimeKind.Local),
				Timestamp = new OscTimeTag(new DateTime(1970, 01, 02, 0, 0, 0, DateTimeKind.Local))
			};
			var expected = "/sample,3,null,{ Time: 1970-01-02T08:00:00.0000000Z },False,{ SampleValue: 0,0,0 },{ Time: 1970-01-02T08:00:00.0000000Z }";
			var actual = command.ToMessage().ToString();
			Assert.AreEqual(expected, actual);

			TimeZoneHelper.SetSystemTimeZone("Central Standard Time");
			var value = TimeService.CurrentTime.UtcNow.IsDaylightSavingTime() ? 3 : 2;

			command = new SampleOscCommand
			{
				BirthDate = new DateTime(1970, 01, 02, value, 0, 0, DateTimeKind.Local),
				Timestamp = new OscTimeTag(new DateTime(1970, 01, 02, value, 0, 0, DateTimeKind.Local))
			};
			actual = command.ToMessage().ToString();
			Assert.AreEqual(expected, actual);

			TimeZoneHelper.SetSystemTimeZone("Eastern Standard Time");
			value = TimeService.CurrentTime.UtcNow.IsDaylightSavingTime() ? 4 : 3;

			command = new SampleOscCommand
			{
				BirthDate = new DateTime(1970, 01, 02, value, 0, 0, DateTimeKind.Local),
				Timestamp = new OscTimeTag(new DateTime(1970, 01, 02, value, 0, 0, DateTimeKind.Local))
			};
			actual = command.ToMessage().ToString();
			Assert.AreEqual(expected, actual);
		}
		finally
		{
			TimeZoneHelper.SetSystemTimeZone(originalZone);
		}
	}

	#endregion
}