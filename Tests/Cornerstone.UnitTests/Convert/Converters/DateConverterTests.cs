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
			new("19: DateTime -> JsonString", new DateTime(2000, 1, 2, 3, 4, 20, 40, DateTimeKind.Utc), "2000-01-02T03:04:20.0400000Z"),
			new("20: DateTime? -> DateTime", new DateTime(2000, 1, 2, 3, 4, 21, 42, DateTimeKind.Utc), new DateTime(2000, 1, 2, 3, 4, 21, 42, DateTimeKind.Utc)),
			new("21: DateTime -> DateTime?", new DateTime(2000, 1, 2, 3, 4, 22, 44, DateTimeKind.Utc), new DateTime(2000, 1, 2, 3, 4, 22, 44, DateTimeKind.Utc)),
			new("22: DateTime? -> DateTime?", null, typeof(DateTime?), null, typeof(DateTime?)),
			new("23: DateTime? -> DateTimeOffset", new DateTime(2000, 1, 2, 3, 4, 23, 46, DateTimeKind.Utc), new DateTimeOffset(2000, 1, 2, 3, 4, 23, 46, 0, new TimeSpan(0, 0, 0))),
			new("24: DateTimeOffset -> DateTime?", new DateTimeOffset(2000, 1, 2, 3, 4, 24, 48, 0, new TimeSpan(0, 0, 0)), new DateTime(2000, 1, 2, 3, 4, 24, 48, DateTimeKind.Utc)),
			new("25: DateTime? -> DateTimeOffset?", null, typeof(DateTime?), null, typeof(DateTimeOffset?)),
			new("26: DateTimeOffset? -> DateTime?", null, typeof(DateTimeOffset?), null, typeof(DateTime?)),
			new("27: DateTime? -> IsoDateTime", new DateTime(2000, 1, 2, 3, 4, 25, 50, DateTimeKind.Utc), new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 25, 50, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("28: IsoDateTime -> DateTime?", new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 26, 52, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateTime(2000, 1, 2, 3, 4, 26, 52, DateTimeKind.Utc)),
			new("29: DateTime? -> IsoDateTime?", null, typeof(DateTime?), null, typeof(IsoDateTime?)),
			new("30: IsoDateTime? -> DateTime?", null, typeof(IsoDateTime?), null, typeof(DateTime?)),
			new("31: DateTime? -> OscTimeTag", new DateTime(2000, 1, 2, 3, 4, 27, 54, DateTimeKind.Utc), new OscTimeTag(new DateTime(2000, 1, 2, 3, 4, 27, 54, DateTimeKind.Utc))),
			new("32: OscTimeTag -> DateTime?", new OscTimeTag(new DateTime(2000, 1, 2, 3, 4, 28, 56, DateTimeKind.Utc)), new DateTime(2000, 1, 2, 3, 4, 28, 56, DateTimeKind.Utc)),
			new("33: DateTime? -> OscTimeTag?", null, typeof(DateTime?), null, typeof(OscTimeTag?)),
			new("34: OscTimeTag? -> DateTime?", null, typeof(OscTimeTag?), null, typeof(DateTime?)),
			new("35: DateTime? -> string", null, typeof(DateTime?), null, typeof(string)),
			new("36: DateTime? -> StringBuilder", null, typeof(DateTime?), null, typeof(StringBuilder)),
			new("37: DateTime? -> TextBuilder", null, typeof(DateTime?), null, typeof(TextBuilder)),
			new("38: DateTime? -> GapBuffer<char>", null, typeof(DateTime?), null, typeof(GapBuffer<char>)),
			new("39: DateTime? -> JsonString", null, typeof(DateTime?), null, typeof(JsonString)),
			new("40: DateTimeOffset -> DateTime", new DateTimeOffset(2000, 1, 2, 3, 4, 29, 58, 0, new TimeSpan(0, 0, 0)), new DateTime(2000, 1, 2, 3, 4, 29, 58, DateTimeKind.Utc)),
			new("41: DateTime -> DateTimeOffset", new DateTime(2000, 1, 2, 3, 4, 30, 60, DateTimeKind.Utc), new DateTimeOffset(2000, 1, 2, 3, 4, 30, 60, 0, new TimeSpan(0, 0, 0))),
			new("42: DateTimeOffset -> DateTime?", new DateTimeOffset(2000, 1, 2, 3, 4, 31, 62, 0, new TimeSpan(0, 0, 0)), new DateTime(2000, 1, 2, 3, 4, 31, 62, DateTimeKind.Utc)),
			new("43: DateTime? -> DateTimeOffset", new DateTime(2000, 1, 2, 3, 4, 32, 64, DateTimeKind.Utc), new DateTimeOffset(2000, 1, 2, 3, 4, 32, 64, 0, new TimeSpan(0, 0, 0))),
			new("44: DateTimeOffset -> DateTimeOffset", new DateTimeOffset(2000, 1, 2, 3, 4, 33, 66, 0, new TimeSpan(0, 0, 0)), new DateTimeOffset(2000, 1, 2, 3, 4, 33, 66, 0, new TimeSpan(0, 0, 0))),
			new("45: DateTimeOffset -> DateTimeOffset?", new DateTimeOffset(2000, 1, 2, 3, 4, 34, 68, 0, new TimeSpan(0, 0, 0)), new DateTimeOffset(2000, 1, 2, 3, 4, 34, 68, 0, new TimeSpan(0, 0, 0))),
			new("46: DateTimeOffset? -> DateTimeOffset", new DateTimeOffset(2000, 1, 2, 3, 4, 35, 70, 0, new TimeSpan(0, 0, 0)), new DateTimeOffset(2000, 1, 2, 3, 4, 35, 70, 0, new TimeSpan(0, 0, 0))),
			new("47: DateTimeOffset -> IsoDateTime", new DateTimeOffset(2000, 1, 2, 3, 4, 36, 72, 0, new TimeSpan(0, 0, 0)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 36, 72, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("48: IsoDateTime -> DateTimeOffset", new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 37, 74, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateTimeOffset(2000, 1, 2, 3, 4, 37, 74, 0, new TimeSpan(0, 0, 0))),
			new("49: DateTimeOffset -> IsoDateTime?", new DateTimeOffset(2000, 1, 2, 3, 4, 38, 76, 0, new TimeSpan(0, 0, 0)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 38, 76, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("50: IsoDateTime? -> DateTimeOffset", new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 39, 78, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateTimeOffset(2000, 1, 2, 3, 4, 39, 78, 0, new TimeSpan(0, 0, 0))),
			new("51: DateTimeOffset -> OscTimeTag", new DateTimeOffset(2000, 1, 2, 3, 4, 40, 80, 0, new TimeSpan(0, 0, 0)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 4, 40, 80, DateTimeKind.Utc))),
			new("52: OscTimeTag -> DateTimeOffset", new OscTimeTag(new DateTime(2000, 1, 2, 3, 4, 41, 82, DateTimeKind.Utc)), new DateTimeOffset(2000, 1, 2, 3, 4, 41, 82, 0, new TimeSpan(0, 0, 0))),
			new("53: DateTimeOffset -> OscTimeTag?", new DateTimeOffset(2000, 1, 2, 3, 4, 42, 84, 0, new TimeSpan(0, 0, 0)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 4, 42, 84, DateTimeKind.Utc))),
			new("54: OscTimeTag? -> DateTimeOffset", new OscTimeTag(new DateTime(2000, 1, 2, 3, 4, 43, 86, DateTimeKind.Utc)), new DateTimeOffset(2000, 1, 2, 3, 4, 43, 86, 0, new TimeSpan(0, 0, 0))),
			new("55: DateTimeOffset -> string", new DateTimeOffset(2000, 1, 2, 3, 4, 44, 88, 0, new TimeSpan(0, 0, 0)), "2000-01-02T03:04:44.0880000+00:00"),
			new("56: DateTimeOffset -> StringBuilder", new DateTimeOffset(2000, 1, 2, 3, 4, 45, 90, 0, new TimeSpan(0, 0, 0)), "2000-01-02T03:04:45.0900000+00:00"),
			new("57: DateTimeOffset -> TextBuilder", new DateTimeOffset(2000, 1, 2, 3, 4, 46, 92, 0, new TimeSpan(0, 0, 0)), "2000-01-02T03:04:46.0920000+00:00"),
			new("58: DateTimeOffset -> GapBuffer<char>", new DateTimeOffset(2000, 1, 2, 3, 4, 47, 94, 0, new TimeSpan(0, 0, 0)), "2000-01-02T03:04:47.0940000+00:00"),
			new("59: DateTimeOffset -> JsonString", new DateTimeOffset(2000, 1, 2, 3, 4, 48, 96, 0, new TimeSpan(0, 0, 0)), "2000-01-02T03:04:48.0960000+00:00"),
			new("60: DateTimeOffset? -> DateTime", new DateTimeOffset(2000, 1, 2, 3, 4, 49, 98, 0, new TimeSpan(0, 0, 0)), new DateTime(2000, 1, 2, 3, 4, 49, 98, DateTimeKind.Utc)),
			new("61: DateTime -> DateTimeOffset?", new DateTime(2000, 1, 2, 3, 4, 50, 100, DateTimeKind.Utc), new DateTimeOffset(2000, 1, 2, 3, 4, 50, 100, 0, new TimeSpan(0, 0, 0))),
			new("62: DateTimeOffset? -> DateTime?", null, typeof(DateTimeOffset?), null, typeof(DateTime?)),
			new("63: DateTime? -> DateTimeOffset?", null, typeof(DateTime?), null, typeof(DateTimeOffset?)),
			new("64: DateTimeOffset? -> DateTimeOffset", new DateTimeOffset(2000, 1, 2, 3, 4, 51, 102, 0, new TimeSpan(0, 0, 0)), new DateTimeOffset(2000, 1, 2, 3, 4, 51, 102, 0, new TimeSpan(0, 0, 0))),
			new("65: DateTimeOffset -> DateTimeOffset?", new DateTimeOffset(2000, 1, 2, 3, 4, 52, 104, 0, new TimeSpan(0, 0, 0)), new DateTimeOffset(2000, 1, 2, 3, 4, 52, 104, 0, new TimeSpan(0, 0, 0))),
			new("66: DateTimeOffset? -> DateTimeOffset?", null, typeof(DateTimeOffset?), null, typeof(DateTimeOffset?)),
			new("67: DateTimeOffset? -> IsoDateTime", new DateTimeOffset(2000, 1, 2, 3, 4, 53, 106, 0, new TimeSpan(0, 0, 0)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 53, 106, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("68: IsoDateTime -> DateTimeOffset?", new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 54, 108, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateTimeOffset(2000, 1, 2, 3, 4, 54, 108, 0, new TimeSpan(0, 0, 0))),
			new("69: DateTimeOffset? -> IsoDateTime?", null, typeof(DateTimeOffset?), null, typeof(IsoDateTime?)),
			new("70: IsoDateTime? -> DateTimeOffset?", null, typeof(IsoDateTime?), null, typeof(DateTimeOffset?)),
			new("71: DateTimeOffset? -> OscTimeTag", new DateTimeOffset(2000, 1, 2, 3, 4, 55, 110, 0, new TimeSpan(0, 0, 0)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 4, 55, 110, DateTimeKind.Utc))),
			new("72: OscTimeTag -> DateTimeOffset?", new OscTimeTag(new DateTime(2000, 1, 2, 3, 4, 56, 112, DateTimeKind.Utc)), new DateTimeOffset(2000, 1, 2, 3, 4, 56, 112, 0, new TimeSpan(0, 0, 0))),
			new("73: DateTimeOffset? -> OscTimeTag?", null, typeof(DateTimeOffset?), null, typeof(OscTimeTag?)),
			new("74: OscTimeTag? -> DateTimeOffset?", null, typeof(OscTimeTag?), null, typeof(DateTimeOffset?)),
			new("75: DateTimeOffset? -> string", null, typeof(DateTimeOffset?), null, typeof(string)),
			new("76: DateTimeOffset? -> StringBuilder", null, typeof(DateTimeOffset?), null, typeof(StringBuilder)),
			new("77: DateTimeOffset? -> TextBuilder", null, typeof(DateTimeOffset?), null, typeof(TextBuilder)),
			new("78: DateTimeOffset? -> GapBuffer<char>", null, typeof(DateTimeOffset?), null, typeof(GapBuffer<char>)),
			new("79: DateTimeOffset? -> JsonString", null, typeof(DateTimeOffset?), null, typeof(JsonString)),
			new("80: IsoDateTime -> DateTime", new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 57, 114, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateTime(2000, 1, 2, 3, 4, 57, 114, DateTimeKind.Utc)),
			new("81: DateTime -> IsoDateTime", new DateTime(2000, 1, 2, 3, 4, 58, 116, DateTimeKind.Utc), new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 58, 116, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("82: IsoDateTime -> DateTime?", new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 59, 118, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateTime(2000, 1, 2, 3, 4, 59, 118, DateTimeKind.Utc)),
			new("83: DateTime? -> IsoDateTime", new DateTime(2000, 1, 2, 3, 5, 0, 120, DateTimeKind.Utc), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 0, 120, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("84: IsoDateTime -> DateTimeOffset", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 1, 122, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateTimeOffset(2000, 1, 2, 3, 5, 1, 122, 0, new TimeSpan(0, 0, 0))),
			new("85: DateTimeOffset -> IsoDateTime", new DateTimeOffset(2000, 1, 2, 3, 5, 2, 124, 0, new TimeSpan(0, 0, 0)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 2, 124, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("86: IsoDateTime -> DateTimeOffset?", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 3, 126, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateTimeOffset(2000, 1, 2, 3, 5, 3, 126, 0, new TimeSpan(0, 0, 0))),
			new("87: DateTimeOffset? -> IsoDateTime", new DateTimeOffset(2000, 1, 2, 3, 5, 4, 128, 0, new TimeSpan(0, 0, 0)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 4, 128, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("88: IsoDateTime -> IsoDateTime", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 5, 130, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 5, 130, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("89: IsoDateTime -> IsoDateTime?", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 6, 132, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 6, 132, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("90: IsoDateTime? -> IsoDateTime", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 7, 134, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 7, 134, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("91: IsoDateTime -> OscTimeTag", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 8, 136, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 8, 136, DateTimeKind.Utc))),
			new("92: OscTimeTag -> IsoDateTime", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 9, 138, DateTimeKind.Utc)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 9, 138, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("93: IsoDateTime -> OscTimeTag?", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 10, 140, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 10, 140, DateTimeKind.Utc))),
			new("94: OscTimeTag? -> IsoDateTime", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 11, 142, DateTimeKind.Utc)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 11, 142, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("95: IsoDateTime -> string", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 12, 144, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), "2000-01-02T03:05:12.1440000Z"),
			new("96: IsoDateTime -> StringBuilder", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 13, 146, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), "2000-01-02T03:05:13.1460000Z"),
			new("97: IsoDateTime -> TextBuilder", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 14, 148, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), "2000-01-02T03:05:14.1480000Z"),
			new("98: IsoDateTime -> GapBuffer<char>", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 15, 150, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), "2000-01-02T03:05:15.1500000Z"),
			new("99: IsoDateTime -> JsonString", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 16, 152, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), "2000-01-02T03:05:16.1520000Z"),
			new("100: IsoDateTime? -> DateTime", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 17, 154, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateTime(2000, 1, 2, 3, 5, 17, 154, DateTimeKind.Utc)),
			new("101: DateTime -> IsoDateTime?", new DateTime(2000, 1, 2, 3, 5, 18, 156, DateTimeKind.Utc), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 18, 156, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("102: IsoDateTime? -> DateTime?", null, typeof(IsoDateTime?), null, typeof(DateTime?)),
			new("103: DateTime? -> IsoDateTime?", null, typeof(DateTime?), null, typeof(IsoDateTime?)),
			new("104: IsoDateTime? -> DateTimeOffset", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 19, 158, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateTimeOffset(2000, 1, 2, 3, 5, 19, 158, 0, new TimeSpan(0, 0, 0))),
			new("105: DateTimeOffset -> IsoDateTime?", new DateTimeOffset(2000, 1, 2, 3, 5, 20, 160, 0, new TimeSpan(0, 0, 0)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 20, 160, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("106: IsoDateTime? -> DateTimeOffset?", null, typeof(IsoDateTime?), null, typeof(DateTimeOffset?)),
			new("107: DateTimeOffset? -> IsoDateTime?", null, typeof(DateTimeOffset?), null, typeof(IsoDateTime?)),
			new("108: IsoDateTime? -> IsoDateTime", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 21, 162, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 21, 162, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("109: IsoDateTime -> IsoDateTime?", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 22, 164, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 22, 164, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("110: IsoDateTime? -> IsoDateTime?", null, typeof(IsoDateTime?), null, typeof(IsoDateTime?)),
			new("111: IsoDateTime? -> OscTimeTag", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 23, 166, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 23, 166, DateTimeKind.Utc))),
			new("112: OscTimeTag -> IsoDateTime?", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 24, 168, DateTimeKind.Utc)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 24, 168, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("113: IsoDateTime? -> OscTimeTag?", null, typeof(IsoDateTime?), null, typeof(OscTimeTag?)),
			new("114: OscTimeTag? -> IsoDateTime?", null, typeof(OscTimeTag?), null, typeof(IsoDateTime?)),
			new("115: IsoDateTime? -> string", null, typeof(IsoDateTime?), null, typeof(string)),
			new("116: IsoDateTime? -> StringBuilder", null, typeof(IsoDateTime?), null, typeof(StringBuilder)),
			new("117: IsoDateTime? -> TextBuilder", null, typeof(IsoDateTime?), null, typeof(TextBuilder)),
			new("118: IsoDateTime? -> GapBuffer<char>", null, typeof(IsoDateTime?), null, typeof(GapBuffer<char>)),
			new("119: IsoDateTime? -> JsonString", null, typeof(IsoDateTime?), null, typeof(JsonString)),
			new("120: OscTimeTag -> DateTime", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 25, 170, DateTimeKind.Utc)), new DateTime(2000, 1, 2, 3, 5, 25, 170, DateTimeKind.Utc)),
			new("121: DateTime -> OscTimeTag", new DateTime(2000, 1, 2, 3, 5, 26, 172, DateTimeKind.Utc), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 26, 172, DateTimeKind.Utc))),
			new("122: OscTimeTag -> DateTime?", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 27, 174, DateTimeKind.Utc)), new DateTime(2000, 1, 2, 3, 5, 27, 174, DateTimeKind.Utc)),
			new("123: DateTime? -> OscTimeTag", new DateTime(2000, 1, 2, 3, 5, 28, 176, DateTimeKind.Utc), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 28, 176, DateTimeKind.Utc))),
			new("124: OscTimeTag -> DateTimeOffset", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 29, 178, DateTimeKind.Utc)), new DateTimeOffset(2000, 1, 2, 3, 5, 29, 178, 0, new TimeSpan(0, 0, 0))),
			new("125: DateTimeOffset -> OscTimeTag", new DateTimeOffset(2000, 1, 2, 3, 5, 30, 180, 0, new TimeSpan(0, 0, 0)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 30, 180, DateTimeKind.Utc))),
			new("126: OscTimeTag -> DateTimeOffset?", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 31, 182, DateTimeKind.Utc)), new DateTimeOffset(2000, 1, 2, 3, 5, 31, 182, 0, new TimeSpan(0, 0, 0))),
			new("127: DateTimeOffset? -> OscTimeTag", new DateTimeOffset(2000, 1, 2, 3, 5, 32, 184, 0, new TimeSpan(0, 0, 0)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 32, 184, DateTimeKind.Utc))),
			new("128: OscTimeTag -> IsoDateTime", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 33, 186, DateTimeKind.Utc)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 33, 186, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("129: IsoDateTime -> OscTimeTag", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 34, 188, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 34, 188, DateTimeKind.Utc))),
			new("130: OscTimeTag -> IsoDateTime?", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 35, 190, DateTimeKind.Utc)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 35, 190, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("131: IsoDateTime? -> OscTimeTag", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 36, 192, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 36, 192, DateTimeKind.Utc))),
			new("132: OscTimeTag -> OscTimeTag", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 37, 194, DateTimeKind.Utc)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 37, 194, DateTimeKind.Utc))),
			new("133: OscTimeTag -> OscTimeTag?", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 38, 196, DateTimeKind.Utc)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 38, 196, DateTimeKind.Utc))),
			new("134: OscTimeTag? -> OscTimeTag", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 39, 198, DateTimeKind.Utc)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 39, 198, DateTimeKind.Utc))),
			new("135: OscTimeTag -> string", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 40, 200, DateTimeKind.Utc)), "2000-01-02T03:05:40.2000000Z"),
			new("136: OscTimeTag -> StringBuilder", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 41, 202, DateTimeKind.Utc)), "2000-01-02T03:05:41.2020000Z"),
			new("137: OscTimeTag -> TextBuilder", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 42, 204, DateTimeKind.Utc)), "2000-01-02T03:05:42.2040000Z"),
			new("138: OscTimeTag -> GapBuffer<char>", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 43, 206, DateTimeKind.Utc)), "2000-01-02T03:05:43.2060000Z"),
			new("139: OscTimeTag -> JsonString", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 44, 208, DateTimeKind.Utc)), "2000-01-02T03:05:44.2080000Z"),
			new("140: OscTimeTag? -> DateTime", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 45, 210, DateTimeKind.Utc)), new DateTime(2000, 1, 2, 3, 5, 45, 210, DateTimeKind.Utc)),
			new("141: DateTime -> OscTimeTag?", new DateTime(2000, 1, 2, 3, 5, 46, 212, DateTimeKind.Utc), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 46, 212, DateTimeKind.Utc))),
			new("142: OscTimeTag? -> DateTime?", null, typeof(OscTimeTag?), null, typeof(DateTime?)),
			new("143: DateTime? -> OscTimeTag?", null, typeof(DateTime?), null, typeof(OscTimeTag?)),
			new("144: OscTimeTag? -> DateTimeOffset", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 47, 214, DateTimeKind.Utc)), new DateTimeOffset(2000, 1, 2, 3, 5, 47, 214, 0, new TimeSpan(0, 0, 0))),
			new("145: DateTimeOffset -> OscTimeTag?", new DateTimeOffset(2000, 1, 2, 3, 5, 48, 216, 0, new TimeSpan(0, 0, 0)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 48, 216, DateTimeKind.Utc))),
			new("146: OscTimeTag? -> DateTimeOffset?", null, typeof(OscTimeTag?), null, typeof(DateTimeOffset?)),
			new("147: DateTimeOffset? -> OscTimeTag?", null, typeof(DateTimeOffset?), null, typeof(OscTimeTag?)),
			new("148: OscTimeTag? -> IsoDateTime", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 49, 218, DateTimeKind.Utc)), new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 49, 218, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("149: IsoDateTime -> OscTimeTag?", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 50, 220, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 50, 220, DateTimeKind.Utc))),
			new("150: OscTimeTag? -> IsoDateTime?", null, typeof(OscTimeTag?), null, typeof(IsoDateTime?)),
			new("151: IsoDateTime? -> OscTimeTag?", null, typeof(IsoDateTime?), null, typeof(OscTimeTag?)),
			new("152: OscTimeTag? -> OscTimeTag", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 51, 222, DateTimeKind.Utc)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 51, 222, DateTimeKind.Utc))),
			new("153: OscTimeTag -> OscTimeTag?", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 52, 224, DateTimeKind.Utc)), new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 52, 224, DateTimeKind.Utc))),
			new("154: OscTimeTag? -> OscTimeTag?", null, typeof(OscTimeTag?), null, typeof(OscTimeTag?)),
			new("155: OscTimeTag? -> string", null, typeof(OscTimeTag?), null, typeof(string)),
			new("156: OscTimeTag? -> StringBuilder", null, typeof(OscTimeTag?), null, typeof(StringBuilder)),
			new("157: OscTimeTag? -> TextBuilder", null, typeof(OscTimeTag?), null, typeof(TextBuilder)),
			new("158: OscTimeTag? -> GapBuffer<char>", null, typeof(OscTimeTag?), null, typeof(GapBuffer<char>)),
			new("159: OscTimeTag? -> JsonString", null, typeof(OscTimeTag?), null, typeof(JsonString)),
			#if (!NET48)
			new("160: DateTime -> DateOnly", new DateTime(2113, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), new DateOnly(2113, 1, 1)),
			new("161: DateOnly -> DateTime", new DateOnly(2114, 1, 1), new DateTime(2114, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)),
			new("162: DateTime -> DateOnly?", new DateTime(2115, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), new DateOnly(2115, 1, 1)),
			new("163: DateOnly? -> DateTime", new DateOnly(2116, 1, 1), new DateTime(2116, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)),
			new("164: DateTime? -> DateOnly", new DateTime(2117, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), new DateOnly(2117, 1, 1)),
			new("165: DateOnly -> DateTime?", new DateOnly(2118, 1, 1), new DateTime(2118, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)),
			new("166: DateTime? -> DateOnly?", null, typeof(DateTime?), null, typeof(DateOnly?)),
			new("167: DateOnly? -> DateTime?", null, typeof(DateOnly?), null, typeof(DateTime?)),
			new("168: DateTimeOffset -> DateOnly", new DateTimeOffset(2119, 1, 1, 0, 0, 0, 0, 0, new TimeSpan(0, 0, 0)), new DateOnly(2119, 1, 1)),
			new("169: DateOnly -> DateTimeOffset", new DateOnly(2120, 1, 1), new DateTimeOffset(2120, 1, 1, 0, 0, 0, 0, 0, new TimeSpan(0, 0, 0))),
			new("170: DateTimeOffset -> DateOnly?", new DateTimeOffset(2121, 1, 1, 0, 0, 0, 0, 0, new TimeSpan(0, 0, 0)), new DateOnly(2121, 1, 1)),
			new("171: DateOnly? -> DateTimeOffset", new DateOnly(2122, 1, 1), new DateTimeOffset(2122, 1, 1, 0, 0, 0, 0, 0, new TimeSpan(0, 0, 0))),
			new("172: DateTimeOffset? -> DateOnly", new DateTimeOffset(2123, 1, 1, 0, 0, 0, 0, 0, new TimeSpan(0, 0, 0)), new DateOnly(2123, 1, 1)),
			new("173: DateOnly -> DateTimeOffset?", new DateOnly(2124, 1, 1), new DateTimeOffset(2124, 1, 1, 0, 0, 0, 0, 0, new TimeSpan(0, 0, 0))),
			new("174: DateTimeOffset? -> DateOnly?", null, typeof(DateTimeOffset?), null, typeof(DateOnly?)),
			new("175: DateOnly? -> DateTimeOffset?", null, typeof(DateOnly?), null, typeof(DateTimeOffset?)),
			new("176: IsoDateTime -> DateOnly", new IsoDateTime(new DateTime(2125, 1, 1, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateOnly(2125, 1, 1)),
			new("177: DateOnly -> IsoDateTime", new DateOnly(2126, 1, 1), new IsoDateTime(new DateTime(2126, 1, 1, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("178: IsoDateTime -> DateOnly?", new IsoDateTime(new DateTime(2127, 1, 1, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateOnly(2127, 1, 1)),
			new("179: DateOnly? -> IsoDateTime", new DateOnly(2002, 1, 1), new IsoDateTime(new DateTime(2002, 1, 1, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("180: IsoDateTime? -> DateOnly", new IsoDateTime(new DateTime(2003, 1, 1, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateOnly(2003, 1, 1)),
			new("181: DateOnly -> IsoDateTime?", new DateOnly(2004, 1, 1), new IsoDateTime(new DateTime(2004, 1, 1, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("182: IsoDateTime? -> DateOnly?", null, typeof(IsoDateTime?), null, typeof(DateOnly?)),
			new("183: DateOnly? -> IsoDateTime?", null, typeof(DateOnly?), null, typeof(IsoDateTime?)),
			new("184: OscTimeTag -> DateOnly", new OscTimeTag(new DateTime(2005, 1, 1, 5, 0, 0, DateTimeKind.Utc)), new DateOnly(2005, 1, 1)),
			new("185: DateOnly -> OscTimeTag", new DateOnly(2006, 1, 1), new OscTimeTag(new DateTime(2006, 1, 1, 5, 0, 0, DateTimeKind.Utc))),
			new("186: OscTimeTag -> DateOnly?", new OscTimeTag(new DateTime(2007, 1, 1, 5, 0, 0, DateTimeKind.Utc)), new DateOnly(2007, 1, 1)),
			new("187: DateOnly? -> OscTimeTag", new DateOnly(2008, 1, 1), new OscTimeTag(new DateTime(2008, 1, 1, 5, 0, 0, DateTimeKind.Utc))),
			new("188: OscTimeTag? -> DateOnly", new OscTimeTag(new DateTime(2009, 1, 1, 5, 0, 0, DateTimeKind.Utc)), new DateOnly(2009, 1, 1)),
			new("189: DateOnly -> OscTimeTag?", new DateOnly(2010, 1, 1), new OscTimeTag(new DateTime(2010, 1, 1, 5, 0, 0, DateTimeKind.Utc))),
			new("190: OscTimeTag? -> DateOnly?", null, typeof(OscTimeTag?), null, typeof(DateOnly?)),
			new("191: DateOnly? -> OscTimeTag?", null, typeof(DateOnly?), null, typeof(OscTimeTag?)),
			new("192: DateOnly -> DateTime", new DateOnly(2011, 1, 1), new DateTime(2011, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)),
			new("193: DateTime -> DateOnly", new DateTime(2012, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), new DateOnly(2012, 1, 1)),
			new("194: DateOnly -> DateTime?", new DateOnly(2013, 1, 1), new DateTime(2013, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)),
			new("195: DateTime? -> DateOnly", new DateTime(2014, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), new DateOnly(2014, 1, 1)),
			new("196: DateOnly -> DateTimeOffset", new DateOnly(2015, 1, 1), new DateTimeOffset(2015, 1, 1, 0, 0, 0, 0, 0, new TimeSpan(0, 0, 0))),
			new("197: DateTimeOffset -> DateOnly", new DateTimeOffset(2016, 1, 1, 0, 0, 0, 0, 0, new TimeSpan(0, 0, 0)), new DateOnly(2016, 1, 1)),
			new("198: DateOnly -> DateTimeOffset?", new DateOnly(2017, 1, 1), new DateTimeOffset(2017, 1, 1, 0, 0, 0, 0, 0, new TimeSpan(0, 0, 0))),
			new("199: DateTimeOffset? -> DateOnly", new DateTimeOffset(2018, 1, 1, 0, 0, 0, 0, 0, new TimeSpan(0, 0, 0)), new DateOnly(2018, 1, 1)),
			new("200: DateOnly -> IsoDateTime", new DateOnly(2019, 1, 1), new IsoDateTime(new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("201: IsoDateTime -> DateOnly", new IsoDateTime(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateOnly(2020, 1, 1)),
			new("202: DateOnly -> IsoDateTime?", new DateOnly(2021, 1, 1), new IsoDateTime(new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("203: IsoDateTime? -> DateOnly", new IsoDateTime(new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateOnly(2022, 1, 1)),
			new("204: DateOnly -> OscTimeTag", new DateOnly(2023, 1, 1), new OscTimeTag(new DateTime(2023, 1, 1, 5, 0, 0, DateTimeKind.Utc))),
			new("205: OscTimeTag -> DateOnly", new OscTimeTag(new DateTime(2024, 1, 1, 5, 0, 0, DateTimeKind.Utc)), new DateOnly(2024, 1, 1)),
			new("206: DateOnly -> OscTimeTag?", new DateOnly(2025, 1, 1), new OscTimeTag(new DateTime(2025, 1, 1, 5, 0, 0, DateTimeKind.Utc))),
			new("207: OscTimeTag? -> DateOnly", new OscTimeTag(new DateTime(2026, 1, 1, 5, 0, 0, DateTimeKind.Utc)), new DateOnly(2026, 1, 1)),
			new("208: DateOnly -> string", new DateOnly(2027, 1, 1), "2027-01-01"),
			new("209: DateOnly -> StringBuilder", new DateOnly(2028, 1, 1), "2028-01-01"),
			new("210: DateOnly -> TextBuilder", new DateOnly(2029, 1, 1), "2029-01-01"),
			new("211: DateOnly -> GapBuffer<char>", new DateOnly(2030, 1, 1), "2030-01-01"),
			new("212: DateOnly -> JsonString", new DateOnly(2031, 1, 1), "2031-01-01"),
			new("213: DateOnly? -> DateTime", new DateOnly(2032, 1, 1), new DateTime(2032, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)),
			new("214: DateTime -> DateOnly?", new DateTime(2033, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), new DateOnly(2033, 1, 1)),
			new("215: DateOnly? -> DateTime?", null, typeof(DateOnly?), null, typeof(DateTime?)),
			new("216: DateTime? -> DateOnly?", null, typeof(DateTime?), null, typeof(DateOnly?)),
			new("217: DateOnly? -> DateTimeOffset", new DateOnly(2034, 1, 1), new DateTimeOffset(2034, 1, 1, 0, 0, 0, 0, 0, new TimeSpan(0, 0, 0))),
			new("218: DateTimeOffset -> DateOnly?", new DateTimeOffset(2035, 1, 1, 0, 0, 0, 0, 0, new TimeSpan(0, 0, 0)), new DateOnly(2035, 1, 1)),
			new("219: DateOnly? -> DateTimeOffset?", null, typeof(DateOnly?), null, typeof(DateTimeOffset?)),
			new("220: DateTimeOffset? -> DateOnly?", null, typeof(DateTimeOffset?), null, typeof(DateOnly?)),
			new("221: DateOnly? -> IsoDateTime", new DateOnly(2036, 1, 1), new IsoDateTime(new DateTime(2036, 1, 1, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("222: IsoDateTime -> DateOnly?", new IsoDateTime(new DateTime(2037, 1, 1, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0)), new DateOnly(2037, 1, 1)),
			new("223: DateOnly? -> IsoDateTime?", null, typeof(DateOnly?), null, typeof(IsoDateTime?)),
			new("224: IsoDateTime? -> DateOnly?", null, typeof(IsoDateTime?), null, typeof(DateOnly?)),
			new("225: DateOnly? -> OscTimeTag", new DateOnly(2038, 1, 1), OscTimeTag.MaxValue),
			new("226: OscTimeTag -> DateOnly?", OscTimeTag.MaxValue, new DateOnly(2036, 2, 7)),
			new("227: DateOnly? -> OscTimeTag?", null, typeof(DateOnly?), null, typeof(OscTimeTag?)),
			new("228: OscTimeTag? -> DateOnly?", null, typeof(OscTimeTag?), null, typeof(DateOnly?)),
			new("229: DateOnly? -> string", null, typeof(DateOnly?), null, typeof(string)),
			new("230: DateOnly? -> StringBuilder", null, typeof(DateOnly?), null, typeof(StringBuilder)),
			new("231: DateOnly? -> TextBuilder", null, typeof(DateOnly?), null, typeof(TextBuilder)),
			new("232: DateOnly? -> GapBuffer<char>", null, typeof(DateOnly?), null, typeof(GapBuffer<char>)),
			new("233: DateOnly? -> JsonString", null, typeof(DateOnly?), null, typeof(JsonString)),
			new("234: DateOnly -> DateOnly", new DateOnly(2040, 1, 1), new DateOnly(2040, 1, 1)),
			new("235: DateOnly -> DateOnly?", new DateOnly(2041, 1, 1), new DateOnly(2041, 1, 1)),
			new("236: DateOnly? -> DateOnly", new DateOnly(2042, 1, 1), new DateOnly(2042, 1, 1)),
			new("237: DateOnly? -> DateOnly", new DateOnly(2043, 1, 1), new DateOnly(2043, 1, 1)),
			new("238: DateOnly -> DateOnly?", new DateOnly(2044, 1, 1), new DateOnly(2044, 1, 1)),
			new("239: DateOnly? -> DateOnly?", null, typeof(DateOnly?), null, typeof(DateOnly?)),
			#endif
			// </Scenarios>
		});

		return response.ToArray();
	}

	#endregion
}