#region References

using System;
using System.Collections.Generic;
using Cornerstone.Protocols.Osc;
using Cornerstone.Serialization;
using Cornerstone.Serialization.Json;
using Cornerstone.Serialization.Json.Converters;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Serialization.Json.Converters;

[TestClass]
public class DateJsonConverterTests : JsonConverterTest<DateJsonConverter>
{
	#region Methods

	[TestMethod]
	public void Basic()
	{
		var scenarios = new List<(string, DateTime)>
		{
			("\"2023-10-31T12:01:02Z\"", new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Unspecified)),
			("\"2023-10-31T16:01:02Z\"", new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local)),
			("\"2023-10-31T12:01:02Z\"", new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Utc))
		};

		var settings = Serializer.DefaultSettings;
		var type = typeof(DateTime);

		for (var index = 0; index < scenarios.Count; index++)
		{
			var scenario = scenarios[index];
			$"Scenario {index}".Dump();
			AreEqual(scenario.Item1, Converter.GetJsonString(scenario.Item2, settings));
			AreEqual(scenario.Item2, Converter.ConvertTo(type, JsonSerializer.Parse(scenario.Item1)));
		}
	}

	[TestMethod]
	public void GenerateScenarios()
	{
		GenerateNewScenarios(
			$"{nameof(DateJsonConverterTests)}.cs",
			EnableFileUpdates | IsDebugging
		);
	}

	[TestMethod]
	public void RunScenarios()
	{
		var scenarios = GetScenarios();
		var settings = Serializer.DefaultSettings;

		foreach (var scenario in scenarios)
		{
			var actual = Converter.GetJsonString(scenario.Value, settings);
			AreEqual(scenario.Expected, actual, scenario.Name);
		}
	}

	[TestMethod]
	public void RunSingleScenario()
	{
		var scenario = GetScenarios()[13];
		var settings = Serializer.DefaultSettings;
		var actual = Converter.GetJsonString(scenario.Value, settings);
		AreEqual(scenario.Expected, actual, scenario.Name);
	}

	private SerializationScenario[] GetScenarios()
	{
		var scenarios = new SerializationScenario[]
		{
			// <Scenarios>
			new("0 DateOnly", DateOnly.MinValue, typeof(DateOnly), "\"0001-01-01\""),
			new("1 DateOnly", DateOnly.MaxValue, typeof(DateOnly), "\"9999-12-31\""),
			new("2 DateOnly", new DateOnly(2023, 10, 31), typeof(DateOnly), "\"2023-10-31\""),
			new("3 DateOnly?", null, typeof(DateOnly?), "\"null\""),
			new("4 DateOnly?", DateOnly.MinValue, typeof(DateOnly?), "\"0001-01-01\""),
			new("5 DateOnly?", DateOnly.MaxValue, typeof(DateOnly?), "\"9999-12-31\""),
			new("6 DateOnly?", new DateOnly(2023, 10, 31), typeof(DateOnly?), "\"2023-10-31\""),
			new("7 DateOnly?", null, typeof(DateOnly?), "\"null\""),
			new("8 DateOnly?", DateOnly.MinValue, typeof(DateOnly?), "\"0001-01-01\""),
			new("9 DateOnly?", DateOnly.MaxValue, typeof(DateOnly?), "\"9999-12-31\""),
			new("10 DateOnly?", new DateOnly(2023, 10, 31), typeof(DateOnly?), "\"2023-10-31\""),
			new("11 DateTime", DateTime.MinValue, typeof(DateTime), "\"0001-01-01T00:00:00\""),
			new("12 DateTime", DateTime.MaxValue, typeof(DateTime), "\"9999-12-31T23:59:59.9999999\""),
			new("13 DateTime", new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), typeof(DateTime), "\"2023-10-31T16:01:02Z\""),
			new("14 DateTime", new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), typeof(DateTime), "\"2023-10-31T12:01:03Z\""),
			new("15 DateTime", new DateTime(2023, 10, 31, 12, 1, 4, DateTimeKind.Unspecified), typeof(DateTime), "\"2023-10-31T12:01:04Z\""),
			new("16 DateTime?", null, typeof(DateTime?), "\"null\""),
			new("17 DateTime?", DateTime.MinValue, typeof(DateTime?), "\"0001-01-01T00:00:00\""),
			new("18 DateTime?", DateTime.MaxValue, typeof(DateTime?), "\"9999-12-31T23:59:59.9999999\""),
			new("19 DateTime?", new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), typeof(DateTime?), "\"2023-10-31T16:01:02Z\""),
			new("20 DateTime?", new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), typeof(DateTime?), "\"2023-10-31T12:01:03Z\""),
			new("21 DateTime?", new DateTime(2023, 10, 31, 12, 1, 4, DateTimeKind.Unspecified), typeof(DateTime?), "\"2023-10-31T12:01:04Z\""),
			new("22 DateTime?", null, typeof(DateTime?), "\"null\""),
			new("23 DateTime?", DateTime.MinValue, typeof(DateTime?), "\"0001-01-01T00:00:00\""),
			new("24 DateTime?", DateTime.MaxValue, typeof(DateTime?), "\"9999-12-31T23:59:59.9999999\""),
			new("25 DateTime?", new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), typeof(DateTime?), "\"2023-10-31T16:01:02Z\""),
			new("26 DateTime?", new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), typeof(DateTime?), "\"2023-10-31T12:01:03Z\""),
			new("27 DateTime?", new DateTime(2023, 10, 31, 12, 1, 4, DateTimeKind.Unspecified), typeof(DateTime?), "\"2023-10-31T12:01:04Z\""),
			new("28 DateTimeOffset", DateTimeOffset.MinValue, typeof(DateTimeOffset), "\"0001-01-01T00:00:00+00:00\""),
			new("29 DateTimeOffset", DateTimeOffset.MaxValue, typeof(DateTimeOffset), "\"9999-12-31T23:59:59.9999999+00:00\""),
			new("30 DateTimeOffset", new DateTimeOffset(2023, 10, 31, 12, 1, 2, 0, 0, new TimeSpan(1, 2, 0)), typeof(DateTimeOffset), "\"2023-10-31T12:01:02+01:02\""),
			new("31 DateTimeOffset?", null, typeof(DateTimeOffset?), "\"null\""),
			new("32 DateTimeOffset?", DateTimeOffset.MinValue, typeof(DateTimeOffset?), "\"0001-01-01T00:00:00+00:00\""),
			new("33 DateTimeOffset?", DateTimeOffset.MaxValue, typeof(DateTimeOffset?), "\"9999-12-31T23:59:59.9999999+00:00\""),
			new("34 DateTimeOffset?", new DateTimeOffset(2023, 10, 31, 12, 1, 2, 0, 0, new TimeSpan(1, 2, 0)), typeof(DateTimeOffset?), "\"2023-10-31T12:01:02+01:02\""),
			new("35 DateTimeOffset?", null, typeof(DateTimeOffset?), "\"null\""),
			new("36 DateTimeOffset?", DateTimeOffset.MinValue, typeof(DateTimeOffset?), "\"0001-01-01T00:00:00+00:00\""),
			new("37 DateTimeOffset?", DateTimeOffset.MaxValue, typeof(DateTimeOffset?), "\"9999-12-31T23:59:59.9999999+00:00\""),
			new("38 DateTimeOffset?", new DateTimeOffset(2023, 10, 31, 12, 1, 2, 0, 0, new TimeSpan(1, 2, 0)), typeof(DateTimeOffset?), "\"2023-10-31T12:01:02+01:02\""),
			new("39 IsoDateTime", IsoDateTime.MinValue, typeof(IsoDateTime), "\"0001-01-01T00:00:00.0000000Z\""),
			new("40 IsoDateTime", IsoDateTime.MaxValue, typeof(IsoDateTime), "\"9999-12-31T23:59:59.9999999Z\""),
			new("41 IsoDateTime", new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), new TimeSpan(1, 2, 3)), typeof(IsoDateTime), "\"2023-10-31T12:01:02.0000000-04:00/PT1H2M3.000S\""),
			new("42 IsoDateTime", new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), new TimeSpan(4, 5, 6)), typeof(IsoDateTime), "\"2023-10-31T12:01:03.0000000Z/PT4H5M6.000S\""),
			new("43 IsoDateTime?", null, typeof(IsoDateTime?), "\"null\""),
			new("44 IsoDateTime?", IsoDateTime.MinValue, typeof(IsoDateTime?), "\"0001-01-01T00:00:00.0000000Z\""),
			new("45 IsoDateTime?", IsoDateTime.MaxValue, typeof(IsoDateTime?), "\"9999-12-31T23:59:59.9999999Z\""),
			new("46 IsoDateTime?", new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), new TimeSpan(1, 2, 3)), typeof(IsoDateTime?), "\"2023-10-31T12:01:02.0000000-04:00/PT1H2M3.000S\""),
			new("47 IsoDateTime?", new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), new TimeSpan(4, 5, 6)), typeof(IsoDateTime?), "\"2023-10-31T12:01:03.0000000Z/PT4H5M6.000S\""),
			new("48 IsoDateTime?", null, typeof(IsoDateTime?), "\"null\""),
			new("49 IsoDateTime?", IsoDateTime.MinValue, typeof(IsoDateTime?), "\"0001-01-01T00:00:00.0000000Z\""),
			new("50 IsoDateTime?", IsoDateTime.MaxValue, typeof(IsoDateTime?), "\"9999-12-31T23:59:59.9999999Z\""),
			new("51 IsoDateTime?", new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Local), new TimeSpan(1, 2, 3)), typeof(IsoDateTime?), "\"2023-10-31T12:01:02.0000000-04:00/PT1H2M3.000S\""),
			new("52 IsoDateTime?", new IsoDateTime(new DateTime(2023, 10, 31, 12, 1, 3, DateTimeKind.Utc), new TimeSpan(4, 5, 6)), typeof(IsoDateTime?), "\"2023-10-31T12:01:03.0000000Z/PT4H5M6.000S\""),
			new("53 OscTimeTag", OscTimeTag.MinValue, typeof(OscTimeTag), "\"1900-01-01T00:00:00.0000000Z\""),
			new("54 OscTimeTag", OscTimeTag.MaxValue, typeof(OscTimeTag), "\"2036-02-07T06:28:16.0000000Z\""),
			new("55 OscTimeTag", new OscTimeTag(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Utc)), typeof(OscTimeTag), "\"2023-10-31T12:01:02.0000000Z\""),
			new("56 OscTimeTag?", null, typeof(OscTimeTag?), "\"null\""),
			new("57 OscTimeTag?", OscTimeTag.MinValue, typeof(OscTimeTag?), "\"1900-01-01T00:00:00.0000000Z\""),
			new("58 OscTimeTag?", OscTimeTag.MaxValue, typeof(OscTimeTag?), "\"2036-02-07T06:28:16.0000000Z\""),
			new("59 OscTimeTag?", new OscTimeTag(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Utc)), typeof(OscTimeTag?), "\"2023-10-31T12:01:02.0000000Z\""),
			new("60 OscTimeTag?", null, typeof(OscTimeTag?), "\"null\""),
			new("61 OscTimeTag?", OscTimeTag.MinValue, typeof(OscTimeTag?), "\"1900-01-01T00:00:00.0000000Z\""),
			new("62 OscTimeTag?", OscTimeTag.MaxValue, typeof(OscTimeTag?), "\"2036-02-07T06:28:16.0000000Z\""),
			new("63 OscTimeTag?", new OscTimeTag(new DateTime(2023, 10, 31, 12, 1, 2, DateTimeKind.Utc)), typeof(OscTimeTag?), "\"2023-10-31T12:01:02.0000000Z\""),
			// </Scenarios>
		};

		return scenarios;
	}

	#endregion
}