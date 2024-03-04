#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Cornerstone.Collections;
using Cornerstone.Convert.Converters;
using Cornerstone.Protocols.Osc;
using Cornerstone.Serialization.Json.Values;
using Cornerstone.Testing;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Convert.Converters;

[TestClass]
public class DateConverterTests : ConverterTests<DateConverter>
{
	#region Methods

	[TestMethod]
	public void CanConvert()
	{
		ValidateCanConvert();

		IsTrue(Converter.CanConvert(typeof(DateTime), typeof(IsoDateTime)));

		IsFalse(Converter.CanConvert(typeof(string), typeof(DateTime)));
		IsFalse(Converter.CanConvert(typeof(EnumConverterTests.SampleEnum), typeof(DateTime)));
	}

	[TestMethod]
	public void GenerateScenarios()
	{
		GenerateNewScenarios($"{nameof(DateConverterTests)}.cs", EnableFileUpdates || IsDebugging);
	}

	[TestMethod]
	public void RunAllTests()
	{
		TestScenarios(GetTestScenarios());
	}

	[TestMethod]
	public void RunSingleTest()
	{
		TestScenarios(GetTestScenarios()[7]);
	}

	[SuppressMessage("ReSharper", "StringLiteralTypo")]
	[SuppressMessage("ReSharper", "RedundantCast")]
	private TestScenario[] GetTestScenarios()
	{
		var response = new List<TestScenario>();

		response.AddRange(new TestScenario[]
		{
			// <Scenarios>
			new("0: DateTime -> DateTime", new DateTime(2000, 1, 2, 3, 4, 1, 2, DateTimeKind.Utc), new DateTime(2000, 1, 2, 3, 4, 1, 2, DateTimeKind.Utc)),
			new("1: DateTime -> DateTime?", new DateTime(2000, 1, 2, 3, 4, 2, 4, DateTimeKind.Utc), new DateTime(2000, 1, 2, 3, 4, 2, 4, DateTimeKind.Utc)),
			new("2: DateTime? -> DateTime", new DateTime(2000, 1, 2, 3, 4, 3, 6, DateTimeKind.Utc), new DateTime(2000, 1, 2, 3, 4, 3, 6, DateTimeKind.Utc)),
			new("3: DateTime -> DateTimeOffset", new DateTime(2000, 1, 2, 3, 4, 4, 8, DateTimeKind.Utc), new DateTimeOffset(2000, 1, 2, 3, 4, 4, 8, 0, new TimeSpan(0, 0, 0))),
			new("4: DateTimeOffset -> DateTime", new DateTimeOffset(2000, 1, 2, 3, 4, 5, 10, 0, new TimeSpan(0, 0, 0)), new DateTime(2000, 1, 2, 3, 4, 5, 10, DateTimeKind.Utc)),
			new("5: DateTime -> DateTimeOffset?", new DateTime(2000, 1, 2, 3, 4, 6, 12, DateTimeKind.Utc), new DateTimeOffset(2000, 1, 2, 3, 4, 6, 12, 0, new TimeSpan(0, 0, 0))),
			new("6: DateTimeOffset? -> DateTime", new DateTimeOffset(2000, 1, 2, 3, 4, 7, 14, 0, new TimeSpan(0, 0, 0)), new DateTime(2000, 1, 2, 3, 4, 7, 14, DateTimeKind.Utc)),
			new("7: DateTime -> IsoDateTime", new DateTime(2000, 1, 2, 3, 4, 8, 16, DateTimeKind.Utc), new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 8, 16, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("8: IsoDateTime -> DateTime", new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 9, 18, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateTime(2000, 1, 2, 3, 4, 9, 18, DateTimeKind.Utc)),
			new("9: DateTime -> IsoDateTime?", new DateTime(2000, 1, 2, 3, 4, 10, 20, DateTimeKind.Utc), new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 10, 20, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("10: IsoDateTime? -> DateTime", new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 11, 22, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateTime(2000, 1, 2, 3, 4, 11, 22, DateTimeKind.Utc)),
			new("11: DateTime -> OscTimeTag", new DateTime(2000, 1, 2, 3, 4, 12, 24, DateTimeKind.Utc), new OscTimeTag(new DateTime(2000, 1, 2, 3, 4, 12, 24, DateTimeKind.Utc))),
			new("12: OscTimeTag -> DateTime", new OscTimeTag(new DateTime(2000, 1, 2, 3, 4, 13, 26, DateTimeKind.Utc)), new DateTime(2000, 1, 2, 3, 4, 13, 26, DateTimeKind.Utc)),
			new("13: DateTime -> OscTimeTag?", new DateTime(2000, 1, 2, 3, 4, 14, 28, DateTimeKind.Utc), new OscTimeTag(new DateTime(2000, 1, 2, 3, 4, 14, 28, DateTimeKind.Utc))),
			new("14: OscTimeTag? -> DateTime", new OscTimeTag(new DateTime(2000, 1, 2, 3, 4, 15, 30, DateTimeKind.Utc)), new DateTime(2000, 1, 2, 3, 4, 15, 30, DateTimeKind.Utc)),
			new("15: DateTime -> string", new DateTime(2000, 1, 2, 3, 4, 16, 32, DateTimeKind.Utc), "2000-01-02T03:04:16.0320000Z"),
			new("16: DateTime -> StringBuilder", new DateTime(2000, 1, 2, 3, 4, 17, 34, DateTimeKind.Utc), "2000-01-02T03:04:17.0340000Z"),
			new("17: DateTime -> TextBuilder", new DateTime(2000, 1, 2, 3, 4, 18, 36, DateTimeKind.Utc), "2000-01-02T03:04:18.0360000Z"),
			new("18: DateTime -> GapBuffer<char>", new DateTime(2000, 1, 2, 3, 4, 19, 38, DateTimeKind.Utc), "2000-01-02T03:04:19.0380000Z"),
			new("19: DateTime -> RopeBuffer<char>", new DateTime(2000, 1, 2, 3, 4, 20, 40, DateTimeKind.Utc), "2000-01-02T03:04:20.0400000Z"),
			new("20: DateTime -> JsonString", new DateTime(2000, 1, 2, 3, 4, 21, 42, DateTimeKind.Utc), "2000-01-02T03:04:21.0420000Z"),
			new("21: DateTime? -> DateTime", new DateTime(2000, 1, 2, 3, 4, 22, 44, DateTimeKind.Utc), new DateTime(2000, 1, 2, 3, 4, 22, 44, DateTimeKind.Utc)),
			new("22: DateTime -> DateTime?", new DateTime(2000, 1, 2, 3, 4, 23, 46, DateTimeKind.Utc), new DateTime(2000, 1, 2, 3, 4, 23, 46, DateTimeKind.Utc)),
			new("23: DateTime? -> DateTime?", null, typeof(DateTime?), null, typeof(DateTime?)),
			new("24: DateTime? -> DateTimeOffset", new DateTime(2000, 1, 2, 3, 4, 24, 48, DateTimeKind.Utc), new DateTimeOffset(2000, 1, 2, 3, 4, 24, 48, 0, new TimeSpan(0, 0, 0))),
			new("25: DateTimeOffset -> DateTime?", new DateTimeOffset(2000, 1, 2, 3, 4, 25, 50, 0, new TimeSpan(0, 0, 0)), new DateTime(2000, 1, 2, 3, 4, 25, 50, DateTimeKind.Utc)),
			new("26: DateTime? -> DateTimeOffset?", null, typeof(DateTime?), null, typeof(DateTimeOffset?)),
			new("27: DateTimeOffset? -> DateTime?", null, typeof(DateTimeOffset?), null, typeof(DateTime?)),
			new("28: DateTime? -> IsoDateTime", new DateTime(2000, 1, 2, 3, 4, 26, 52, DateTimeKind.Utc), new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 26, 52, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("29: IsoDateTime -> DateTime?", new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 27, 54, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateTime(2000, 1, 2, 3, 4, 27, 54, DateTimeKind.Utc)),
			new("30: DateTime? -> IsoDateTime?", null, typeof(DateTime?), null, typeof(IsoDateTime?)),
			new("31: IsoDateTime? -> DateTime?", null, typeof(IsoDateTime?), null, typeof(DateTime?)),
			new("32: DateTime? -> OscTimeTag", new DateTime(2000, 1, 2, 3, 4, 28, 56, DateTimeKind.Utc), new OscTimeTag(new DateTime(2000, 1, 2, 3, 4, 28, 56, DateTimeKind.Utc))),
			new("33: OscTimeTag -> DateTime?", new OscTimeTag(new DateTime(2000, 1, 2, 3, 4, 29, 58, DateTimeKind.Utc)), new DateTime(2000, 1, 2, 3, 4, 29, 58, DateTimeKind.Utc)),
			new("34: DateTime? -> OscTimeTag?", null, typeof(DateTime?), null, typeof(OscTimeTag?)),
			new("35: OscTimeTag? -> DateTime?", null, typeof(OscTimeTag?), null, typeof(DateTime?)),
			new("36: DateTime? -> string", null, typeof(DateTime?), null, typeof(string)),
			new("37: DateTime? -> StringBuilder", null, typeof(DateTime?), null, typeof(StringBuilder)),
			new("38: DateTime? -> TextBuilder", null, typeof(DateTime?), null, typeof(TextBuilder)),
			new("39: DateTime? -> GapBuffer<char>", null, typeof(DateTime?), null, typeof(GapBuffer<char>)),
			new("40: DateTime? -> RopeBuffer<char>", null, typeof(DateTime?), null, typeof(RopeBuffer<char>)),
			new("41: DateTime? -> JsonString", null, typeof(DateTime?), null, typeof(JsonString)),
			new("42: DateTimeOffset -> DateTime", new DateTimeOffset(2000, 1, 2, 3, 4, 30, 60, 0, new TimeSpan(0, 0, 0)), new DateTime(2000, 1, 2, 3, 4, 30, 60, DateTimeKind.Utc)),
			new("43: DateTime -> DateTimeOffset", new DateTime(2000, 1, 2, 3, 4, 31, 62, DateTimeKind.Utc), new DateTimeOffset(2000, 1, 2, 3, 4, 31, 62, 0, new TimeSpan(0, 0, 0))),
			new("44: DateTimeOffset -> DateTime?", new DateTimeOffset(2000, 1, 2, 3, 4, 32, 64, 0, new TimeSpan(0, 0, 0)), new DateTime(2000, 1, 2, 3, 4, 32, 64, DateTimeKind.Utc)),
			new("45: DateTime? -> DateTimeOffset", new DateTime(2000, 1, 2, 3, 4, 33, 66, DateTimeKind.Utc), new DateTimeOffset(2000, 1, 2, 3, 4, 33, 66, 0, new TimeSpan(0, 0, 0))),
			new("46: DateTimeOffset -> DateTimeOffset", new DateTimeOffset(2000, 1, 2, 3, 4, 34, 68, 0, new TimeSpan(0, 0, 0)), new DateTimeOffset(2000, 1, 2, 3, 4, 34, 68, 0, new TimeSpan(0, 0, 0))),
			new("47: DateTimeOffset -> DateTimeOffset?", new DateTimeOffset(2000, 1, 2, 3, 4, 35, 70, 0, new TimeSpan(0, 0, 0)), new DateTimeOffset(2000, 1, 2, 3, 4, 35, 70, 0, new TimeSpan(0, 0, 0))),
			new("48: DateTimeOffset? -> DateTimeOffset", new DateTimeOffset(2000, 1, 2, 3, 4, 36, 72, 0, new TimeSpan(0, 0, 0)), new DateTimeOffset(2000, 1, 2, 3, 4, 36, 72, 0, new TimeSpan(0, 0, 0))),
			new("49: DateTimeOffset -> IsoDateTime", new DateTimeOffset(2000, 1, 2, 3, 4, 37, 74, 0, new TimeSpan(0, 0, 0)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 37, 74, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("50: IsoDateTime -> DateTimeOffset", new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 38, 76, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateTimeOffset(2000, 1, 2, 3, 4, 38, 76, 0, new TimeSpan(0, 0, 0))),
			new("51: DateTimeOffset -> IsoDateTime?", new DateTimeOffset(2000, 1, 2, 3, 4, 39, 78, 0, new TimeSpan(0, 0, 0)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 39, 78, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("52: IsoDateTime? -> DateTimeOffset", new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 40, 80, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateTimeOffset(2000, 1, 2, 3, 4, 40, 80, 0, new TimeSpan(0, 0, 0))),
			new("53: DateTimeOffset -> OscTimeTag", new DateTimeOffset(2000, 1, 2, 3, 4, 41, 82, 0, new TimeSpan(0, 0, 0)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 4, 41, 82, DateTimeKind.Utc))),
			new("54: OscTimeTag -> DateTimeOffset", new OscTimeTag(new DateTime(2000, 1, 2, 3, 4, 42, 84, DateTimeKind.Utc)), new DateTimeOffset(2000, 1, 2, 3, 4, 42, 84, 0, new TimeSpan(0, 0, 0))),
			new("55: DateTimeOffset -> OscTimeTag?", new DateTimeOffset(2000, 1, 2, 3, 4, 43, 86, 0, new TimeSpan(0, 0, 0)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 4, 43, 86, DateTimeKind.Utc))),
			new("56: OscTimeTag? -> DateTimeOffset", new OscTimeTag(new DateTime(2000, 1, 2, 3, 4, 44, 88, DateTimeKind.Utc)), new DateTimeOffset(2000, 1, 2, 3, 4, 44, 88, 0, new TimeSpan(0, 0, 0))),
			new("57: DateTimeOffset -> string", new DateTimeOffset(2000, 1, 2, 3, 4, 45, 90, 0, new TimeSpan(0, 0, 0)), "2000-01-02T03:04:45.0900000+00:00"),
			new("58: DateTimeOffset -> StringBuilder", new DateTimeOffset(2000, 1, 2, 3, 4, 46, 92, 0, new TimeSpan(0, 0, 0)), "2000-01-02T03:04:46.0920000+00:00"),
			new("59: DateTimeOffset -> TextBuilder", new DateTimeOffset(2000, 1, 2, 3, 4, 47, 94, 0, new TimeSpan(0, 0, 0)), "2000-01-02T03:04:47.0940000+00:00"),
			new("60: DateTimeOffset -> GapBuffer<char>", new DateTimeOffset(2000, 1, 2, 3, 4, 48, 96, 0, new TimeSpan(0, 0, 0)), "2000-01-02T03:04:48.0960000+00:00"),
			new("61: DateTimeOffset -> RopeBuffer<char>", new DateTimeOffset(2000, 1, 2, 3, 4, 49, 98, 0, new TimeSpan(0, 0, 0)), "2000-01-02T03:04:49.0980000+00:00"),
			new("62: DateTimeOffset -> JsonString", new DateTimeOffset(2000, 1, 2, 3, 4, 50, 100, 0, new TimeSpan(0, 0, 0)), "2000-01-02T03:04:50.1000000+00:00"),
			new("63: DateTimeOffset? -> DateTime", new DateTimeOffset(2000, 1, 2, 3, 4, 51, 102, 0, new TimeSpan(0, 0, 0)), new DateTime(2000, 1, 2, 3, 4, 51, 102, DateTimeKind.Utc)),
			new("64: DateTime -> DateTimeOffset?", new DateTime(2000, 1, 2, 3, 4, 52, 104, DateTimeKind.Utc), new DateTimeOffset(2000, 1, 2, 3, 4, 52, 104, 0, new TimeSpan(0, 0, 0))),
			new("65: DateTimeOffset? -> DateTime?", null, typeof(DateTimeOffset?), null, typeof(DateTime?)),
			new("66: DateTime? -> DateTimeOffset?", null, typeof(DateTime?), null, typeof(DateTimeOffset?)),
			new("67: DateTimeOffset? -> DateTimeOffset", new DateTimeOffset(2000, 1, 2, 3, 4, 53, 106, 0, new TimeSpan(0, 0, 0)), new DateTimeOffset(2000, 1, 2, 3, 4, 53, 106, 0, new TimeSpan(0, 0, 0))),
			new("68: DateTimeOffset -> DateTimeOffset?", new DateTimeOffset(2000, 1, 2, 3, 4, 54, 108, 0, new TimeSpan(0, 0, 0)), new DateTimeOffset(2000, 1, 2, 3, 4, 54, 108, 0, new TimeSpan(0, 0, 0))),
			new("69: DateTimeOffset? -> DateTimeOffset?", null, typeof(DateTimeOffset?), null, typeof(DateTimeOffset?)),
			new("70: DateTimeOffset? -> IsoDateTime", new DateTimeOffset(2000, 1, 2, 3, 4, 55, 110, 0, new TimeSpan(0, 0, 0)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 55, 110, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("71: IsoDateTime -> DateTimeOffset?", new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 56, 112, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateTimeOffset(2000, 1, 2, 3, 4, 56, 112, 0, new TimeSpan(0, 0, 0))),
			new("72: DateTimeOffset? -> IsoDateTime?", null, typeof(DateTimeOffset?), null, typeof(IsoDateTime?)),
			new("73: IsoDateTime? -> DateTimeOffset?", null, typeof(IsoDateTime?), null, typeof(DateTimeOffset?)),
			new("74: DateTimeOffset? -> OscTimeTag", new DateTimeOffset(2000, 1, 2, 3, 4, 57, 114, 0, new TimeSpan(0, 0, 0)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 4, 57, 114, DateTimeKind.Utc))),
			new("75: OscTimeTag -> DateTimeOffset?", new OscTimeTag(new DateTime(2000, 1, 2, 3, 4, 58, 116, DateTimeKind.Utc)), new DateTimeOffset(2000, 1, 2, 3, 4, 58, 116, 0, new TimeSpan(0, 0, 0))),
			new("76: DateTimeOffset? -> OscTimeTag?", null, typeof(DateTimeOffset?), null, typeof(OscTimeTag?)),
			new("77: OscTimeTag? -> DateTimeOffset?", null, typeof(OscTimeTag?), null, typeof(DateTimeOffset?)),
			new("78: DateTimeOffset? -> string", null, typeof(DateTimeOffset?), null, typeof(string)),
			new("79: DateTimeOffset? -> StringBuilder", null, typeof(DateTimeOffset?), null, typeof(StringBuilder)),
			new("80: DateTimeOffset? -> TextBuilder", null, typeof(DateTimeOffset?), null, typeof(TextBuilder)),
			new("81: DateTimeOffset? -> GapBuffer<char>", null, typeof(DateTimeOffset?), null, typeof(GapBuffer<char>)),
			new("82: DateTimeOffset? -> RopeBuffer<char>", null, typeof(DateTimeOffset?), null, typeof(RopeBuffer<char>)),
			new("83: DateTimeOffset? -> JsonString", null, typeof(DateTimeOffset?), null, typeof(JsonString)),
			new("84: IsoDateTime -> DateTime", new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 59, 118, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateTime(2000, 1, 2, 3, 4, 59, 118, DateTimeKind.Utc)),
			new("85: DateTime -> IsoDateTime", new DateTime(2000, 1, 2, 3, 5, 0, 120, DateTimeKind.Utc), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 0, 120, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("86: IsoDateTime -> DateTime?", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 1, 122, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateTime(2000, 1, 2, 3, 5, 1, 122, DateTimeKind.Utc)),
			new("87: DateTime? -> IsoDateTime", new DateTime(2000, 1, 2, 3, 5, 2, 124, DateTimeKind.Utc), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 2, 124, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("88: IsoDateTime -> DateTimeOffset", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 3, 126, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateTimeOffset(2000, 1, 2, 3, 5, 3, 126, 0, new TimeSpan(0, 0, 0))),
			new("89: DateTimeOffset -> IsoDateTime", new DateTimeOffset(2000, 1, 2, 3, 5, 4, 128, 0, new TimeSpan(0, 0, 0)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 4, 128, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("90: IsoDateTime -> DateTimeOffset?", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 5, 130, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateTimeOffset(2000, 1, 2, 3, 5, 5, 130, 0, new TimeSpan(0, 0, 0))),
			new("91: DateTimeOffset? -> IsoDateTime", new DateTimeOffset(2000, 1, 2, 3, 5, 6, 132, 0, new TimeSpan(0, 0, 0)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 6, 132, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("92: IsoDateTime -> IsoDateTime", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 7, 134, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 7, 134, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("93: IsoDateTime -> IsoDateTime?", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 8, 136, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 8, 136, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("94: IsoDateTime? -> IsoDateTime", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 9, 138, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 9, 138, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("95: IsoDateTime -> OscTimeTag", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 10, 140, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 10, 140, DateTimeKind.Utc))),
			new("96: OscTimeTag -> IsoDateTime", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 11, 142, DateTimeKind.Utc)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 11, 142, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("97: IsoDateTime -> OscTimeTag?", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 12, 144, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 12, 144, DateTimeKind.Utc))),
			new("98: OscTimeTag? -> IsoDateTime", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 13, 146, DateTimeKind.Utc)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 13, 146, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("99: IsoDateTime -> string", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 14, 148, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), "2000-01-02T03:05:14.1480000Z"),
			new("100: IsoDateTime -> StringBuilder", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 15, 150, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), "2000-01-02T03:05:15.1500000Z"),
			new("101: IsoDateTime -> TextBuilder", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 16, 152, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), "2000-01-02T03:05:16.1520000Z"),
			new("102: IsoDateTime -> GapBuffer<char>", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 17, 154, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), "2000-01-02T03:05:17.1540000Z"),
			new("103: IsoDateTime -> RopeBuffer<char>", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 18, 156, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), "2000-01-02T03:05:18.1560000Z"),
			new("104: IsoDateTime -> JsonString", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 19, 158, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), "2000-01-02T03:05:19.1580000Z"),
			new("105: IsoDateTime? -> DateTime", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 20, 160, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateTime(2000, 1, 2, 3, 5, 20, 160, DateTimeKind.Utc)),
			new("106: DateTime -> IsoDateTime?", new DateTime(2000, 1, 2, 3, 5, 21, 162, DateTimeKind.Utc), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 21, 162, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("107: IsoDateTime? -> DateTime?", null, typeof(IsoDateTime?), null, typeof(DateTime?)),
			new("108: DateTime? -> IsoDateTime?", null, typeof(DateTime?), null, typeof(IsoDateTime?)),
			new("109: IsoDateTime? -> DateTimeOffset", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 22, 164, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateTimeOffset(2000, 1, 2, 3, 5, 22, 164, 0, new TimeSpan(0, 0, 0))),
			new("110: DateTimeOffset -> IsoDateTime?", new DateTimeOffset(2000, 1, 2, 3, 5, 23, 166, 0, new TimeSpan(0, 0, 0)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 23, 166, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("111: IsoDateTime? -> DateTimeOffset?", null, typeof(IsoDateTime?), null, typeof(DateTimeOffset?)),
			new("112: DateTimeOffset? -> IsoDateTime?", null, typeof(DateTimeOffset?), null, typeof(IsoDateTime?)),
			new("113: IsoDateTime? -> IsoDateTime", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 24, 168, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 24, 168, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("114: IsoDateTime -> IsoDateTime?", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 25, 170, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 25, 170, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("115: IsoDateTime? -> IsoDateTime?", null, typeof(IsoDateTime?), null, typeof(IsoDateTime?)),
			new("116: IsoDateTime? -> OscTimeTag", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 26, 172, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 26, 172, DateTimeKind.Utc))),
			new("117: OscTimeTag -> IsoDateTime?", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 27, 174, DateTimeKind.Utc)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 27, 174, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("118: IsoDateTime? -> OscTimeTag?", null, typeof(IsoDateTime?), null, typeof(OscTimeTag?)),
			new("119: OscTimeTag? -> IsoDateTime?", null, typeof(OscTimeTag?), null, typeof(IsoDateTime?)),
			new("120: IsoDateTime? -> string", null, typeof(IsoDateTime?), null, typeof(string)),
			new("121: IsoDateTime? -> StringBuilder", null, typeof(IsoDateTime?), null, typeof(StringBuilder)),
			new("122: IsoDateTime? -> TextBuilder", null, typeof(IsoDateTime?), null, typeof(TextBuilder)),
			new("123: IsoDateTime? -> GapBuffer<char>", null, typeof(IsoDateTime?), null, typeof(GapBuffer<char>)),
			new("124: IsoDateTime? -> RopeBuffer<char>", null, typeof(IsoDateTime?), null, typeof(RopeBuffer<char>)),
			new("125: IsoDateTime? -> JsonString", null, typeof(IsoDateTime?), null, typeof(JsonString)),
			new("126: OscTimeTag -> DateTime", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 28, 176, DateTimeKind.Utc)), new DateTime(2000, 1, 2, 3, 5, 28, 176, DateTimeKind.Utc)),
			new("127: DateTime -> OscTimeTag", new DateTime(2000, 1, 2, 3, 5, 29, 178, DateTimeKind.Utc), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 29, 178, DateTimeKind.Utc))),
			new("128: OscTimeTag -> DateTime?", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 30, 180, DateTimeKind.Utc)), new DateTime(2000, 1, 2, 3, 5, 30, 180, DateTimeKind.Utc)),
			new("129: DateTime? -> OscTimeTag", new DateTime(2000, 1, 2, 3, 5, 31, 182, DateTimeKind.Utc), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 31, 182, DateTimeKind.Utc))),
			new("130: OscTimeTag -> DateTimeOffset", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 32, 184, DateTimeKind.Utc)), new DateTimeOffset(2000, 1, 2, 3, 5, 32, 184, 0, new TimeSpan(0, 0, 0))),
			new("131: DateTimeOffset -> OscTimeTag", new DateTimeOffset(2000, 1, 2, 3, 5, 33, 186, 0, new TimeSpan(0, 0, 0)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 33, 186, DateTimeKind.Utc))),
			new("132: OscTimeTag -> DateTimeOffset?", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 34, 188, DateTimeKind.Utc)), new DateTimeOffset(2000, 1, 2, 3, 5, 34, 188, 0, new TimeSpan(0, 0, 0))),
			new("133: DateTimeOffset? -> OscTimeTag", new DateTimeOffset(2000, 1, 2, 3, 5, 35, 190, 0, new TimeSpan(0, 0, 0)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 35, 190, DateTimeKind.Utc))),
			new("134: OscTimeTag -> IsoDateTime", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 36, 192, DateTimeKind.Utc)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 36, 192, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("135: IsoDateTime -> OscTimeTag", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 37, 194, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 37, 194, DateTimeKind.Utc))),
			new("136: OscTimeTag -> IsoDateTime?", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 38, 196, DateTimeKind.Utc)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 38, 196, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("137: IsoDateTime? -> OscTimeTag", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 39, 198, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 39, 198, DateTimeKind.Utc))),
			new("138: OscTimeTag -> OscTimeTag", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 40, 200, DateTimeKind.Utc)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 40, 200, DateTimeKind.Utc))),
			new("139: OscTimeTag -> OscTimeTag?", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 41, 202, DateTimeKind.Utc)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 41, 202, DateTimeKind.Utc))),
			new("140: OscTimeTag? -> OscTimeTag", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 42, 204, DateTimeKind.Utc)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 42, 204, DateTimeKind.Utc))),
			new("141: OscTimeTag -> string", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 43, 206, DateTimeKind.Utc)), "2000-01-02T03:05:43.2060000Z"),
			new("142: OscTimeTag -> StringBuilder", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 44, 208, DateTimeKind.Utc)), "2000-01-02T03:05:44.2080000Z"),
			new("143: OscTimeTag -> TextBuilder", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 45, 210, DateTimeKind.Utc)), "2000-01-02T03:05:45.2100000Z"),
			new("144: OscTimeTag -> GapBuffer<char>", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 46, 212, DateTimeKind.Utc)), "2000-01-02T03:05:46.2120000Z"),
			new("145: OscTimeTag -> RopeBuffer<char>", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 47, 214, DateTimeKind.Utc)), "2000-01-02T03:05:47.2140000Z"),
			new("146: OscTimeTag -> JsonString", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 48, 216, DateTimeKind.Utc)), "2000-01-02T03:05:48.2160000Z"),
			new("147: OscTimeTag? -> DateTime", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 49, 218, DateTimeKind.Utc)), new DateTime(2000, 1, 2, 3, 5, 49, 218, DateTimeKind.Utc)),
			new("148: DateTime -> OscTimeTag?", new DateTime(2000, 1, 2, 3, 5, 50, 220, DateTimeKind.Utc), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 50, 220, DateTimeKind.Utc))),
			new("149: OscTimeTag? -> DateTime?", null, typeof(OscTimeTag?), null, typeof(DateTime?)),
			new("150: DateTime? -> OscTimeTag?", null, typeof(DateTime?), null, typeof(OscTimeTag?)),
			new("151: OscTimeTag? -> DateTimeOffset", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 51, 222, DateTimeKind.Utc)), new DateTimeOffset(2000, 1, 2, 3, 5, 51, 222, 0, new TimeSpan(0, 0, 0))),
			new("152: DateTimeOffset -> OscTimeTag?", new DateTimeOffset(2000, 1, 2, 3, 5, 52, 224, 0, new TimeSpan(0, 0, 0)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 52, 224, DateTimeKind.Utc))),
			new("153: OscTimeTag? -> DateTimeOffset?", null, typeof(OscTimeTag?), null, typeof(DateTimeOffset?)),
			new("154: DateTimeOffset? -> OscTimeTag?", null, typeof(DateTimeOffset?), null, typeof(OscTimeTag?)),
			new("155: OscTimeTag? -> IsoDateTime", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 53, 226, DateTimeKind.Utc)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 53, 226, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("156: IsoDateTime -> OscTimeTag?", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 54, 228, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 54, 228, DateTimeKind.Utc))),
			new("157: OscTimeTag? -> IsoDateTime?", null, typeof(OscTimeTag?), null, typeof(IsoDateTime?)),
			new("158: IsoDateTime? -> OscTimeTag?", null, typeof(IsoDateTime?), null, typeof(OscTimeTag?)),
			new("159: OscTimeTag? -> OscTimeTag", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 55, 230, DateTimeKind.Utc)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 55, 230, DateTimeKind.Utc))),
			new("160: OscTimeTag -> OscTimeTag?", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 56, 232, DateTimeKind.Utc)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 56, 232, DateTimeKind.Utc))),
			new("161: OscTimeTag? -> OscTimeTag?", null, typeof(OscTimeTag?), null, typeof(OscTimeTag?)),
			new("162: OscTimeTag? -> string", null, typeof(OscTimeTag?), null, typeof(string)),
			new("163: OscTimeTag? -> StringBuilder", null, typeof(OscTimeTag?), null, typeof(StringBuilder)),
			new("164: OscTimeTag? -> TextBuilder", null, typeof(OscTimeTag?), null, typeof(TextBuilder)),
			new("165: OscTimeTag? -> GapBuffer<char>", null, typeof(OscTimeTag?), null, typeof(GapBuffer<char>)),
			new("166: OscTimeTag? -> RopeBuffer<char>", null, typeof(OscTimeTag?), null, typeof(RopeBuffer<char>)),
			new("167: OscTimeTag? -> JsonString", null, typeof(OscTimeTag?), null, typeof(JsonString)),
			#if (!NET48)
			new("168: DateTime -> DateOnly", new DateTime(2117, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), new DateOnly(2117, 1, 1)),
			new("169: DateOnly -> DateTime", new DateOnly(2118, 1, 1), new DateTime(2118, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)),
			new("170: DateTime -> DateOnly?", new DateTime(2119, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), new DateOnly(2119, 1, 1)),
			new("171: DateOnly? -> DateTime", new DateOnly(2120, 1, 1), new DateTime(2120, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)),
			new("172: DateTime? -> DateOnly", new DateTime(2121, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), new DateOnly(2121, 1, 1)),
			new("173: DateOnly -> DateTime?", new DateOnly(2122, 1, 1), new DateTime(2122, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)),
			new("174: DateTime? -> DateOnly?", null, typeof(DateTime?), null, typeof(DateOnly?)),
			new("175: DateOnly? -> DateTime?", null, typeof(DateOnly?), null, typeof(DateTime?)),
			new("176: DateTimeOffset -> DateOnly", new DateTimeOffset(2123, 1, 1, 0, 0, 0, 0, 0, new TimeSpan(0, 0, 0)), new DateOnly(2123, 1, 1)),
			new("177: DateOnly -> DateTimeOffset", new DateOnly(2124, 1, 1), new DateTimeOffset(2124, 1, 1, 0, 0, 0, 0, 0, new TimeSpan(0, 0, 0))),
			new("178: DateTimeOffset -> DateOnly?", new DateTimeOffset(2125, 1, 1, 0, 0, 0, 0, 0, new TimeSpan(0, 0, 0)), new DateOnly(2125, 1, 1)),
			new("179: DateOnly? -> DateTimeOffset", new DateOnly(2126, 1, 1), new DateTimeOffset(2126, 1, 1, 0, 0, 0, 0, 0, new TimeSpan(0, 0, 0))),
			new("180: DateTimeOffset? -> DateOnly", new DateTimeOffset(2127, 1, 1, 0, 0, 0, 0, 0, new TimeSpan(0, 0, 0)), new DateOnly(2127, 1, 1)),
			new("181: DateOnly -> DateTimeOffset?", new DateOnly(2002, 1, 1), new DateTimeOffset(2002, 1, 1, 0, 0, 0, 0, 0, new TimeSpan(0, 0, 0))),
			new("182: DateTimeOffset? -> DateOnly?", null, typeof(DateTimeOffset?), null, typeof(DateOnly?)),
			new("183: DateOnly? -> DateTimeOffset?", null, typeof(DateOnly?), null, typeof(DateTimeOffset?)),
			new("184: IsoDateTime -> DateOnly", new IsoDateTime(new DateTime(2003, 1, 1, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateOnly(2003, 1, 1)),
			new("185: DateOnly -> IsoDateTime", new DateOnly(2004, 1, 1), new IsoDateTime(new DateTime(2004, 1, 1, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("186: IsoDateTime -> DateOnly?", new IsoDateTime(new DateTime(2005, 1, 1, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateOnly(2005, 1, 1)),
			new("187: DateOnly? -> IsoDateTime", new DateOnly(2006, 1, 1), new IsoDateTime(new DateTime(2006, 1, 1, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("188: IsoDateTime? -> DateOnly", new IsoDateTime(new DateTime(2007, 1, 1, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateOnly(2007, 1, 1)),
			new("189: DateOnly -> IsoDateTime?", new DateOnly(2008, 1, 1), new IsoDateTime(new DateTime(2008, 1, 1, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("190: IsoDateTime? -> DateOnly?", null, typeof(IsoDateTime?), null, typeof(DateOnly?)),
			new("191: DateOnly? -> IsoDateTime?", null, typeof(DateOnly?), null, typeof(IsoDateTime?)),
			new("192: OscTimeTag -> DateOnly", new OscTimeTag(new DateTime(2009, 1, 1, 5, 0, 0, DateTimeKind.Utc)), new DateOnly(2009, 1, 1)),
			new("193: DateOnly -> OscTimeTag", new DateOnly(2010, 1, 1), new OscTimeTag(new DateTime(2010, 1, 1, 5, 0, 0, DateTimeKind.Utc))),
			new("194: OscTimeTag -> DateOnly?", new OscTimeTag(new DateTime(2011, 1, 1, 5, 0, 0, DateTimeKind.Utc)), new DateOnly(2011, 1, 1)),
			new("195: DateOnly? -> OscTimeTag", new DateOnly(2012, 1, 1), new OscTimeTag(new DateTime(2012, 1, 1, 5, 0, 0, DateTimeKind.Utc))),
			new("196: OscTimeTag? -> DateOnly", new OscTimeTag(new DateTime(2013, 1, 1, 5, 0, 0, DateTimeKind.Utc)), new DateOnly(2013, 1, 1)),
			new("197: DateOnly -> OscTimeTag?", new DateOnly(2014, 1, 1), new OscTimeTag(new DateTime(2014, 1, 1, 5, 0, 0, DateTimeKind.Utc))),
			new("198: OscTimeTag? -> DateOnly?", null, typeof(OscTimeTag?), null, typeof(DateOnly?)),
			new("199: DateOnly? -> OscTimeTag?", null, typeof(DateOnly?), null, typeof(OscTimeTag?)),
			new("200: DateOnly -> DateTime", new DateOnly(2015, 1, 1), new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)),
			new("201: DateTime -> DateOnly", new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), new DateOnly(2016, 1, 1)),
			new("202: DateOnly -> DateTime?", new DateOnly(2017, 1, 1), new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)),
			new("203: DateTime? -> DateOnly", new DateTime(2018, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), new DateOnly(2018, 1, 1)),
			new("204: DateOnly -> DateTimeOffset", new DateOnly(2019, 1, 1), new DateTimeOffset(2019, 1, 1, 0, 0, 0, 0, 0, new TimeSpan(0, 0, 0))),
			new("205: DateTimeOffset -> DateOnly", new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, 0, new TimeSpan(0, 0, 0)), new DateOnly(2020, 1, 1)),
			new("206: DateOnly -> DateTimeOffset?", new DateOnly(2021, 1, 1), new DateTimeOffset(2021, 1, 1, 0, 0, 0, 0, 0, new TimeSpan(0, 0, 0))),
			new("207: DateTimeOffset? -> DateOnly", new DateTimeOffset(2022, 1, 1, 0, 0, 0, 0, 0, new TimeSpan(0, 0, 0)), new DateOnly(2022, 1, 1)),
			new("208: DateOnly -> IsoDateTime", new DateOnly(2023, 1, 1), new IsoDateTime(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("209: IsoDateTime -> DateOnly", new IsoDateTime(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateOnly(2024, 1, 1)),
			new("210: DateOnly -> IsoDateTime?", new DateOnly(2025, 1, 1), new IsoDateTime(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("211: IsoDateTime? -> DateOnly", new IsoDateTime(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateOnly(2026, 1, 1)),
			new("212: DateOnly -> OscTimeTag", new DateOnly(2027, 1, 1), new OscTimeTag(new DateTime(2027, 1, 1, 5, 0, 0, DateTimeKind.Utc))),
			new("213: OscTimeTag -> DateOnly", new OscTimeTag(new DateTime(2028, 1, 1, 5, 0, 0, DateTimeKind.Utc)), new DateOnly(2028, 1, 1)),
			new("214: DateOnly -> OscTimeTag?", new DateOnly(2029, 1, 1), new OscTimeTag(new DateTime(2029, 1, 1, 5, 0, 0, DateTimeKind.Utc))),
			new("215: OscTimeTag? -> DateOnly", new OscTimeTag(new DateTime(2030, 1, 1, 5, 0, 0, DateTimeKind.Utc)), new DateOnly(2030, 1, 1)),
			new("216: DateOnly -> string", new DateOnly(2031, 1, 1), "2031-01-01"),
			new("217: DateOnly -> StringBuilder", new DateOnly(2032, 1, 1), "2032-01-01"),
			new("218: DateOnly -> TextBuilder", new DateOnly(2033, 1, 1), "2033-01-01"),
			new("219: DateOnly -> GapBuffer<char>", new DateOnly(2034, 1, 1), "2034-01-01"),
			new("220: DateOnly -> RopeBuffer<char>", new DateOnly(2035, 1, 1), "2035-01-01"),
			new("221: DateOnly -> JsonString", new DateOnly(2036, 1, 1), "2036-01-01"),
			new("222: DateOnly? -> DateTime", new DateOnly(2037, 1, 1), new DateTime(2037, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)),
			new("223: DateTime -> DateOnly?", new DateTime(2038, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), new DateOnly(2038, 1, 1)),
			new("224: DateOnly? -> DateTime?", null, typeof(DateOnly?), null, typeof(DateTime?)),
			new("225: DateTime? -> DateOnly?", null, typeof(DateTime?), null, typeof(DateOnly?)),
			new("226: DateOnly? -> DateTimeOffset", new DateOnly(2039, 1, 1), new DateTimeOffset(2039, 1, 1, 0, 0, 0, 0, 0, new TimeSpan(0, 0, 0))),
			new("227: DateTimeOffset -> DateOnly?", new DateTimeOffset(2040, 1, 1, 0, 0, 0, 0, 0, new TimeSpan(0, 0, 0)), new DateOnly(2040, 1, 1)),
			new("228: DateOnly? -> DateTimeOffset?", null, typeof(DateOnly?), null, typeof(DateTimeOffset?)),
			new("229: DateTimeOffset? -> DateOnly?", null, typeof(DateTimeOffset?), null, typeof(DateOnly?)),
			new("230: DateOnly? -> IsoDateTime", new DateOnly(2041, 1, 1), new IsoDateTime(new DateTime(2041, 1, 1, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("231: IsoDateTime -> DateOnly?", new IsoDateTime(new DateTime(2042, 1, 1, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateOnly(2042, 1, 1)),
			new("232: DateOnly? -> IsoDateTime?", null, typeof(DateOnly?), null, typeof(IsoDateTime?)),
			new("233: IsoDateTime? -> DateOnly?", null, typeof(IsoDateTime?), null, typeof(DateOnly?)),
			new("234: DateOnly? -> OscTimeTag", new DateOnly(2043, 1, 1), OscTimeTag.MaxValue),
			new("235: OscTimeTag -> DateOnly?", OscTimeTag.MaxValue, new DateOnly(2036, 2, 7)),
			new("236: DateOnly? -> OscTimeTag?", null, typeof(DateOnly?), null, typeof(OscTimeTag?)),
			new("237: OscTimeTag? -> DateOnly?", null, typeof(OscTimeTag?), null, typeof(DateOnly?)),
			new("238: DateOnly? -> string", null, typeof(DateOnly?), null, typeof(string)),
			new("239: DateOnly? -> StringBuilder", null, typeof(DateOnly?), null, typeof(StringBuilder)),
			new("240: DateOnly? -> TextBuilder", null, typeof(DateOnly?), null, typeof(TextBuilder)),
			new("241: DateOnly? -> GapBuffer<char>", null, typeof(DateOnly?), null, typeof(GapBuffer<char>)),
			new("242: DateOnly? -> RopeBuffer<char>", null, typeof(DateOnly?), null, typeof(RopeBuffer<char>)),
			new("243: DateOnly? -> JsonString", null, typeof(DateOnly?), null, typeof(JsonString)),
			new("244: DateOnly -> DateOnly", new DateOnly(2045, 1, 1), new DateOnly(2045, 1, 1)),
			new("245: DateOnly -> DateOnly?", new DateOnly(2046, 1, 1), new DateOnly(2046, 1, 1)),
			new("246: DateOnly? -> DateOnly", new DateOnly(2047, 1, 1), new DateOnly(2047, 1, 1)),
			new("247: DateOnly? -> DateOnly", new DateOnly(2048, 1, 1), new DateOnly(2048, 1, 1)),
			new("248: DateOnly -> DateOnly?", new DateOnly(2049, 1, 1), new DateOnly(2049, 1, 1)),
			new("249: DateOnly? -> DateOnly?", null, typeof(DateOnly?), null, typeof(DateOnly?)),
			#endif
			// </Scenarios>
		});

		return response.ToArray();
	}

	#endregion
}