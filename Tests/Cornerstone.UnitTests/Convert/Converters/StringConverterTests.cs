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
public class StringConverterTests : ConverterTests<StringConverter>
{
	#region Methods

	[TestMethod]
	public void Basic()
	{
		AreEqual(null, Converter.TryConvertTo(null, typeof(string), typeof(bool?), out var value) ? value : 1);
		AreEqual(true, Converter.TryConvertTo("true", typeof(string), typeof(bool), out value) ? value : 1);
		AreEqual(true, Converter.TryConvertTo("True", typeof(string), typeof(bool), out value) ? value : 1);
		AreEqual(true, Converter.TryConvertTo("TRUE", typeof(string), typeof(bool), out value) ? value : 1);
		AreEqual(false, Converter.TryConvertTo("false", typeof(string), typeof(bool), out value) ? value : 1);
		AreEqual(false, Converter.TryConvertTo("False", typeof(string), typeof(bool), out value) ? value : 1);
		AreEqual(false, Converter.TryConvertTo("FALSE", typeof(string), typeof(bool), out value) ? value : 1);
	}

	[TestMethod]
	public void CanConvert()
	{
		ValidateCanConvert();

		IsTrue(Converter.CanConvert(typeof(StringBuilder), typeof(TextBuilder)));
		IsTrue(Converter.CanConvert(typeof(StringBuilder), typeof(int)));
		IsTrue(Converter.CanConvert(typeof(TextBuilder), typeof(Guid)));
		IsTrue(Converter.CanConvert(typeof(JsonString), typeof(ShortGuid)));
		IsTrue(Converter.CanConvert(typeof(JsonString), typeof(bool)));
		IsTrue(Converter.CanConvert(typeof(JsonString), typeof(bool?)));
		IsTrue(Converter.CanConvert(typeof(string), typeof(Version)));

		IsFalse(Converter.CanConvert(typeof(DateTime), typeof(string)));
	}

	[TestMethod]
	public void GenerateScenarios()
	{
		GenerateNewScenarios($"{nameof(StringConverterTests)}.cs", EnableFileUpdates || IsDebugging);
	}

	[TestMethod]
	public void RunAllTests()
	{
		TestScenarios(GetTestScenarios());
	}

	[TestMethod]
	public void RunSingleTest()
	{
		TestScenarios(GetTestScenarios()[11]);
	}

	[SuppressMessage("ReSharper", "StringLiteralTypo")]
	[SuppressMessage("ReSharper", "RedundantCast")]
	private TestScenario[] GetTestScenarios()
	{
		var response = new List<TestScenario>();

		response.AddRange(new TestScenario[]
		{
			// <Scenarios>
			new("0: string -> string", null, typeof(string), null, typeof(string)),
			new("1: string -> StringBuilder", null, typeof(string), null, typeof(StringBuilder)),
			new("2: StringBuilder -> string", null, typeof(StringBuilder), null, typeof(string)),
			new("3: string -> TextBuilder", null, typeof(string), null, typeof(TextBuilder)),
			new("4: TextBuilder -> string", null, typeof(TextBuilder), null, typeof(string)),
			new("5: string -> GapBuffer<char>", null, typeof(string), null, typeof(GapBuffer<char>)),
			new("6: GapBuffer<char> -> string", null, typeof(GapBuffer<char>), null, typeof(string)),
			new("7: string -> JsonString", null, typeof(string), null, typeof(JsonString)),
			new("8: JsonString -> string", null, typeof(JsonString), null, typeof(string)),
			new("9: string -> char", "a", 'a'),
			new("10: string -> char?", null, typeof(string), null, typeof(char?)),
			new("11: string -> DateTime", "2000-01-02T03:04:00.0000000Z", new DateTime(2000, 1, 2, 3, 4, 0, DateTimeKind.Utc)),
			new("12: string -> DateTime?", null, typeof(string), null, typeof(DateTime?)),
			new("13: string -> DateTimeOffset", "2000-01-02T03:04:01.0000000Z", new DateTimeOffset(2000, 1, 2, 3, 4, 1, 0, 0, new TimeSpan(0, 0, 0))),
			new("14: string -> DateTimeOffset?", null, typeof(string), null, typeof(DateTimeOffset?)),
			new("15: string -> IsoDateTime", "2000-01-02T03:04:02.0000000Z", new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 2, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("16: string -> IsoDateTime?", null, typeof(string), null, typeof(IsoDateTime?)),
			new("17: string -> OscTimeTag", "2000-01-02T03:04:03.0000000Z", new OscTimeTag(new DateTime(2000, 1, 2, 3, 4, 3, DateTimeKind.Utc))),
			new("18: string -> OscTimeTag?", null, typeof(string), null, typeof(OscTimeTag?)),
			new("19: string -> TimeSpan", "0:03:04:04.0000000", new TimeSpan(3, 4, 4)),
			new("20: string -> TimeSpan?", null, typeof(string), null, typeof(TimeSpan?)),
			new("21: string -> byte", "6", 6),
			new("22: string -> byte?", null, typeof(string), null, typeof(byte?)),
			new("23: string -> sbyte", "7", 7),
			new("24: string -> sbyte?", null, typeof(string), null, typeof(sbyte?)),
			new("25: string -> short", "8", 8),
			new("26: string -> short?", null, typeof(string), null, typeof(short?)),
			new("27: string -> ushort", "9", 9),
			new("28: string -> ushort?", null, typeof(string), null, typeof(ushort?)),
			new("29: string -> int", "10", 10),
			new("30: string -> int?", null, typeof(string), null, typeof(int?)),
			new("31: string -> uint", "11", 11),
			new("32: string -> uint?", null, typeof(string), null, typeof(uint?)),
			new("33: string -> long", "12", 12),
			new("34: string -> long?", null, typeof(string), null, typeof(long?)),
			new("35: string -> ulong", "13", 13),
			new("36: string -> ulong?", null, typeof(string), null, typeof(ulong?)),
			new("37: string -> IntPtr", "14", (IntPtr) 14),
			new("38: string -> IntPtr?", null, typeof(string), null, typeof(IntPtr?)),
			new("39: string -> UIntPtr", "15", (UIntPtr) 15),
			new("40: string -> UIntPtr?", null, typeof(string), null, typeof(UIntPtr?)),
			new("41: string -> Int128", "16", 16),
			new("42: string -> Int128?", null, typeof(string), null, typeof(Int128?)),
			new("43: string -> UInt128", "17", 17),
			new("44: string -> UInt128?", null, typeof(string), null, typeof(UInt128?)),
			new("45: string -> decimal", "18.01", (decimal) 18.01m),
			new("46: string -> decimal?", null, typeof(string), null, typeof(decimal?)),
			new("47: string -> double", "19.02", (double) 19.02d),
			new("48: string -> double?", null, typeof(string), null, typeof(double?)),
			new("49: string -> float", "20.03", (float) 20.03f),
			new("50: string -> float?", null, typeof(string), null, typeof(float?)),
			new("51: string -> Guid", "00000000-0000-0000-0000-000000000021", Guid.Parse("00000000-0000-0000-0000-000000000021")),
			new("52: string -> Guid?", null, typeof(string), null, typeof(Guid?)),
			new("53: string -> ShortGuid", "00000000-0000-0000-0000-000000000022", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAIg")),
			new("54: string -> ShortGuid?", null, typeof(string), null, typeof(ShortGuid?)),
			new("55: string -> bool", "True", true),
			new("56: string -> bool?", null, typeof(string), null, typeof(bool?)),
			new("57: string -> Version", null, typeof(string), null, typeof(Version)),
			new("58: StringBuilder -> string", null, typeof(StringBuilder), null, typeof(string)),
			new("59: string -> StringBuilder", null, typeof(string), null, typeof(StringBuilder)),
			new("60: StringBuilder -> StringBuilder", null, typeof(StringBuilder), null, typeof(StringBuilder)),
			new("61: StringBuilder -> TextBuilder", null, typeof(StringBuilder), null, typeof(TextBuilder)),
			new("62: TextBuilder -> StringBuilder", null, typeof(TextBuilder), null, typeof(StringBuilder)),
			new("63: StringBuilder -> GapBuffer<char>", null, typeof(StringBuilder), null, typeof(GapBuffer<char>)),
			new("64: GapBuffer<char> -> StringBuilder", null, typeof(GapBuffer<char>), null, typeof(StringBuilder)),
			new("65: StringBuilder -> JsonString", null, typeof(StringBuilder), null, typeof(JsonString)),
			new("66: JsonString -> StringBuilder", null, typeof(JsonString), null, typeof(StringBuilder)),
			new("67: StringBuilder -> char", new StringBuilder("w"), 'w'),
			new("68: StringBuilder -> char?", null, typeof(StringBuilder), null, typeof(char?)),
			new("69: StringBuilder -> DateTime", "2000-01-02T03:04:22.0000000Z", new DateTime(2000, 1, 2, 3, 4, 22, DateTimeKind.Utc)),
			new("70: StringBuilder -> DateTime?", null, typeof(StringBuilder), null, typeof(DateTime?)),
			new("71: StringBuilder -> DateTimeOffset", "2000-01-02T03:04:23.0000000Z", new DateTimeOffset(2000, 1, 2, 3, 4, 23, 0, 0, new TimeSpan(0, 0, 0))),
			new("72: StringBuilder -> DateTimeOffset?", null, typeof(StringBuilder), null, typeof(DateTimeOffset?)),
			new("73: StringBuilder -> IsoDateTime", "2000-01-02T03:04:24.0000000Z", new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 24, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("74: StringBuilder -> IsoDateTime?", null, typeof(StringBuilder), null, typeof(IsoDateTime?)),
			new("75: StringBuilder -> OscTimeTag", "2000-01-02T03:04:25.0000000Z", new OscTimeTag(new DateTime(2000, 1, 2, 3, 4, 25, DateTimeKind.Utc))),
			new("76: StringBuilder -> OscTimeTag?", null, typeof(StringBuilder), null, typeof(OscTimeTag?)),
			new("77: StringBuilder -> TimeSpan", "0:03:04:26.0000000", new TimeSpan(3, 4, 26)),
			new("78: StringBuilder -> TimeSpan?", null, typeof(StringBuilder), null, typeof(TimeSpan?)),
			new("79: StringBuilder -> byte", new StringBuilder("28"), 28),
			new("80: StringBuilder -> byte?", null, typeof(StringBuilder), null, typeof(byte?)),
			new("81: StringBuilder -> sbyte", new StringBuilder("29"), 29),
			new("82: StringBuilder -> sbyte?", null, typeof(StringBuilder), null, typeof(sbyte?)),
			new("83: StringBuilder -> short", new StringBuilder("30"), 30),
			new("84: StringBuilder -> short?", null, typeof(StringBuilder), null, typeof(short?)),
			new("85: StringBuilder -> ushort", new StringBuilder("31"), 31),
			new("86: StringBuilder -> ushort?", null, typeof(StringBuilder), null, typeof(ushort?)),
			new("87: StringBuilder -> int", new StringBuilder("32"), 32),
			new("88: StringBuilder -> int?", null, typeof(StringBuilder), null, typeof(int?)),
			new("89: StringBuilder -> uint", new StringBuilder("33"), 33),
			new("90: StringBuilder -> uint?", null, typeof(StringBuilder), null, typeof(uint?)),
			new("91: StringBuilder -> long", new StringBuilder("34"), 34),
			new("92: StringBuilder -> long?", null, typeof(StringBuilder), null, typeof(long?)),
			new("93: StringBuilder -> ulong", new StringBuilder("35"), 35),
			new("94: StringBuilder -> ulong?", null, typeof(StringBuilder), null, typeof(ulong?)),
			new("95: StringBuilder -> IntPtr", new StringBuilder("36"), (IntPtr) 36),
			new("96: StringBuilder -> IntPtr?", null, typeof(StringBuilder), null, typeof(IntPtr?)),
			new("97: StringBuilder -> UIntPtr", new StringBuilder("37"), (UIntPtr) 37),
			new("98: StringBuilder -> UIntPtr?", null, typeof(StringBuilder), null, typeof(UIntPtr?)),
			new("99: StringBuilder -> Int128", new StringBuilder("38"), 38),
			new("100: StringBuilder -> Int128?", null, typeof(StringBuilder), null, typeof(Int128?)),
			new("101: StringBuilder -> UInt128", new StringBuilder("39"), 39),
			new("102: StringBuilder -> UInt128?", null, typeof(StringBuilder), null, typeof(UInt128?)),
			new("103: StringBuilder -> decimal", new StringBuilder("40.04"), (decimal) 40.04m),
			new("104: StringBuilder -> decimal?", null, typeof(StringBuilder), null, typeof(decimal?)),
			new("105: StringBuilder -> double", new StringBuilder("41.05"), (double) 41.05d),
			new("106: StringBuilder -> double?", null, typeof(StringBuilder), null, typeof(double?)),
			new("107: StringBuilder -> float", new StringBuilder("42.06"), (float) 42.06f),
			new("108: StringBuilder -> float?", null, typeof(StringBuilder), null, typeof(float?)),
			new("109: StringBuilder -> Guid", new StringBuilder("00000000-0000-0000-0000-000000000043"), Guid.Parse("00000000-0000-0000-0000-000000000043")),
			new("110: StringBuilder -> Guid?", null, typeof(StringBuilder), null, typeof(Guid?)),
			new("111: StringBuilder -> ShortGuid", new StringBuilder("00000000-0000-0000-0000-000000000044"), ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAARA")),
			new("112: StringBuilder -> ShortGuid?", null, typeof(StringBuilder), null, typeof(ShortGuid?)),
			new("113: StringBuilder -> bool", new StringBuilder("True"), true),
			new("114: StringBuilder -> bool?", null, typeof(StringBuilder), null, typeof(bool?)),
			new("115: StringBuilder -> Version", null, typeof(StringBuilder), null, typeof(Version)),
			new("116: TextBuilder -> string", null, typeof(TextBuilder), null, typeof(string)),
			new("117: string -> TextBuilder", null, typeof(string), null, typeof(TextBuilder)),
			new("118: TextBuilder -> StringBuilder", null, typeof(TextBuilder), null, typeof(StringBuilder)),
			new("119: StringBuilder -> TextBuilder", null, typeof(StringBuilder), null, typeof(TextBuilder)),
			new("120: TextBuilder -> TextBuilder", null, typeof(TextBuilder), null, typeof(TextBuilder)),
			new("121: TextBuilder -> GapBuffer<char>", null, typeof(TextBuilder), null, typeof(GapBuffer<char>)),
			new("122: GapBuffer<char> -> TextBuilder", null, typeof(GapBuffer<char>), null, typeof(TextBuilder)),
			new("123: TextBuilder -> JsonString", null, typeof(TextBuilder), null, typeof(JsonString)),
			new("124: JsonString -> TextBuilder", null, typeof(JsonString), null, typeof(TextBuilder)),
			new("125: TextBuilder -> char", new TextBuilder("S"), 'S'),
			new("126: TextBuilder -> char?", null, typeof(TextBuilder), null, typeof(char?)),
			new("127: TextBuilder -> DateTime", "2000-01-02T03:04:44.0000000Z", new DateTime(2000, 1, 2, 3, 4, 44, DateTimeKind.Utc)),
			new("128: TextBuilder -> DateTime?", null, typeof(TextBuilder), null, typeof(DateTime?)),
			new("129: TextBuilder -> DateTimeOffset", "2000-01-02T03:04:45.0000000Z", new DateTimeOffset(2000, 1, 2, 3, 4, 45, 0, 0, new TimeSpan(0, 0, 0))),
			new("130: TextBuilder -> DateTimeOffset?", null, typeof(TextBuilder), null, typeof(DateTimeOffset?)),
			new("131: TextBuilder -> IsoDateTime", "2000-01-02T03:04:46.0000000Z", new IsoDateTime(new DateTime(2000, 1, 2, 3, 4, 46, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("132: TextBuilder -> IsoDateTime?", null, typeof(TextBuilder), null, typeof(IsoDateTime?)),
			new("133: TextBuilder -> OscTimeTag", "2000-01-02T03:04:47.0000000Z", new OscTimeTag(new DateTime(2000, 1, 2, 3, 4, 47, DateTimeKind.Utc))),
			new("134: TextBuilder -> OscTimeTag?", null, typeof(TextBuilder), null, typeof(OscTimeTag?)),
			new("135: TextBuilder -> TimeSpan", "0:03:04:48.0000000", new TimeSpan(3, 4, 48)),
			new("136: TextBuilder -> TimeSpan?", null, typeof(TextBuilder), null, typeof(TimeSpan?)),
			new("137: TextBuilder -> byte", new TextBuilder("50"), 50),
			new("138: TextBuilder -> byte?", null, typeof(TextBuilder), null, typeof(byte?)),
			new("139: TextBuilder -> sbyte", new TextBuilder("51"), 51),
			new("140: TextBuilder -> sbyte?", null, typeof(TextBuilder), null, typeof(sbyte?)),
			new("141: TextBuilder -> short", new TextBuilder("52"), 52),
			new("142: TextBuilder -> short?", null, typeof(TextBuilder), null, typeof(short?)),
			new("143: TextBuilder -> ushort", new TextBuilder("53"), 53),
			new("144: TextBuilder -> ushort?", null, typeof(TextBuilder), null, typeof(ushort?)),
			new("145: TextBuilder -> int", new TextBuilder("54"), 54),
			new("146: TextBuilder -> int?", null, typeof(TextBuilder), null, typeof(int?)),
			new("147: TextBuilder -> uint", new TextBuilder("55"), 55),
			new("148: TextBuilder -> uint?", null, typeof(TextBuilder), null, typeof(uint?)),
			new("149: TextBuilder -> long", new TextBuilder("56"), 56),
			new("150: TextBuilder -> long?", null, typeof(TextBuilder), null, typeof(long?)),
			new("151: TextBuilder -> ulong", new TextBuilder("57"), 57),
			new("152: TextBuilder -> ulong?", null, typeof(TextBuilder), null, typeof(ulong?)),
			new("153: TextBuilder -> IntPtr", new TextBuilder("58"), (IntPtr) 58),
			new("154: TextBuilder -> IntPtr?", null, typeof(TextBuilder), null, typeof(IntPtr?)),
			new("155: TextBuilder -> UIntPtr", new TextBuilder("59"), (UIntPtr) 59),
			new("156: TextBuilder -> UIntPtr?", null, typeof(TextBuilder), null, typeof(UIntPtr?)),
			new("157: TextBuilder -> Int128", new TextBuilder("60"), 60),
			new("158: TextBuilder -> Int128?", null, typeof(TextBuilder), null, typeof(Int128?)),
			new("159: TextBuilder -> UInt128", new TextBuilder("61"), 61),
			new("160: TextBuilder -> UInt128?", null, typeof(TextBuilder), null, typeof(UInt128?)),
			new("161: TextBuilder -> decimal", new TextBuilder("62.07"), (decimal) 62.07m),
			new("162: TextBuilder -> decimal?", null, typeof(TextBuilder), null, typeof(decimal?)),
			new("163: TextBuilder -> double", new TextBuilder("63.08"), (double) 63.08d),
			new("164: TextBuilder -> double?", null, typeof(TextBuilder), null, typeof(double?)),
			new("165: TextBuilder -> float", new TextBuilder("64.09"), (float) 64.09f),
			new("166: TextBuilder -> float?", null, typeof(TextBuilder), null, typeof(float?)),
			new("167: TextBuilder -> Guid", new TextBuilder("00000000-0000-0000-0000-000000000065"), Guid.Parse("00000000-0000-0000-0000-000000000065")),
			new("168: TextBuilder -> Guid?", null, typeof(TextBuilder), null, typeof(Guid?)),
			new("169: TextBuilder -> ShortGuid", new TextBuilder("00000000-0000-0000-0000-000000000066"), ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAZg")),
			new("170: TextBuilder -> ShortGuid?", null, typeof(TextBuilder), null, typeof(ShortGuid?)),
			new("171: TextBuilder -> bool", new TextBuilder("True"), true),
			new("172: TextBuilder -> bool?", null, typeof(TextBuilder), null, typeof(bool?)),
			new("173: TextBuilder -> Version", null, typeof(TextBuilder), null, typeof(Version)),
			new("174: GapBuffer<char> -> string", null, typeof(GapBuffer<char>), null, typeof(string)),
			new("175: string -> GapBuffer<char>", null, typeof(string), null, typeof(GapBuffer<char>)),
			new("176: GapBuffer<char> -> StringBuilder", null, typeof(GapBuffer<char>), null, typeof(StringBuilder)),
			new("177: StringBuilder -> GapBuffer<char>", null, typeof(StringBuilder), null, typeof(GapBuffer<char>)),
			new("178: GapBuffer<char> -> TextBuilder", null, typeof(GapBuffer<char>), null, typeof(TextBuilder)),
			new("179: TextBuilder -> GapBuffer<char>", null, typeof(TextBuilder), null, typeof(GapBuffer<char>)),
			new("180: GapBuffer<char> -> GapBuffer<char>", null, typeof(GapBuffer<char>), null, typeof(GapBuffer<char>)),
			new("181: GapBuffer<char> -> JsonString", null, typeof(GapBuffer<char>), null, typeof(JsonString)),
			new("182: JsonString -> GapBuffer<char>", null, typeof(JsonString), null, typeof(GapBuffer<char>)),
			new("183: GapBuffer<char> -> char", new GapBuffer<char>("%"), '%'),
			new("184: GapBuffer<char> -> char?", null, typeof(GapBuffer<char>), null, typeof(char?)),
			new("185: GapBuffer<char> -> DateTime", "2000-01-02T03:05:06.0000000Z", new DateTime(2000, 1, 2, 3, 5, 6, DateTimeKind.Utc)),
			new("186: GapBuffer<char> -> DateTime?", null, typeof(GapBuffer<char>), null, typeof(DateTime?)),
			new("187: GapBuffer<char> -> DateTimeOffset", "2000-01-02T03:05:07.0000000Z", new DateTimeOffset(2000, 1, 2, 3, 5, 7, 0, 0, new TimeSpan(0, 0, 0))),
			new("188: GapBuffer<char> -> DateTimeOffset?", null, typeof(GapBuffer<char>), null, typeof(DateTimeOffset?)),
			new("189: GapBuffer<char> -> IsoDateTime", "2000-01-02T03:05:08.0000000Z", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 8, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("190: GapBuffer<char> -> IsoDateTime?", null, typeof(GapBuffer<char>), null, typeof(IsoDateTime?)),
			new("191: GapBuffer<char> -> OscTimeTag", "2000-01-02T03:05:09.0000000Z", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 9, DateTimeKind.Utc))),
			new("192: GapBuffer<char> -> OscTimeTag?", null, typeof(GapBuffer<char>), null, typeof(OscTimeTag?)),
			new("193: GapBuffer<char> -> TimeSpan", "0:03:05:10.0000000", new TimeSpan(3, 5, 10)),
			new("194: GapBuffer<char> -> TimeSpan?", null, typeof(GapBuffer<char>), null, typeof(TimeSpan?)),
			new("195: GapBuffer<char> -> byte", new GapBuffer<char>("72"), 72),
			new("196: GapBuffer<char> -> byte?", null, typeof(GapBuffer<char>), null, typeof(byte?)),
			new("197: GapBuffer<char> -> sbyte", new GapBuffer<char>("73"), 73),
			new("198: GapBuffer<char> -> sbyte?", null, typeof(GapBuffer<char>), null, typeof(sbyte?)),
			new("199: GapBuffer<char> -> short", new GapBuffer<char>("74"), 74),
			new("200: GapBuffer<char> -> short?", null, typeof(GapBuffer<char>), null, typeof(short?)),
			new("201: GapBuffer<char> -> ushort", new GapBuffer<char>("75"), 75),
			new("202: GapBuffer<char> -> ushort?", null, typeof(GapBuffer<char>), null, typeof(ushort?)),
			new("203: GapBuffer<char> -> int", new GapBuffer<char>("76"), 76),
			new("204: GapBuffer<char> -> int?", null, typeof(GapBuffer<char>), null, typeof(int?)),
			new("205: GapBuffer<char> -> uint", new GapBuffer<char>("77"), 77),
			new("206: GapBuffer<char> -> uint?", null, typeof(GapBuffer<char>), null, typeof(uint?)),
			new("207: GapBuffer<char> -> long", new GapBuffer<char>("78"), 78),
			new("208: GapBuffer<char> -> long?", null, typeof(GapBuffer<char>), null, typeof(long?)),
			new("209: GapBuffer<char> -> ulong", new GapBuffer<char>("79"), 79),
			new("210: GapBuffer<char> -> ulong?", null, typeof(GapBuffer<char>), null, typeof(ulong?)),
			new("211: GapBuffer<char> -> IntPtr", new GapBuffer<char>("80"), (IntPtr) 80),
			new("212: GapBuffer<char> -> IntPtr?", null, typeof(GapBuffer<char>), null, typeof(IntPtr?)),
			new("213: GapBuffer<char> -> UIntPtr", new GapBuffer<char>("81"), (UIntPtr) 81),
			new("214: GapBuffer<char> -> UIntPtr?", null, typeof(GapBuffer<char>), null, typeof(UIntPtr?)),
			new("215: GapBuffer<char> -> Int128", new GapBuffer<char>("82"), 82),
			new("216: GapBuffer<char> -> Int128?", null, typeof(GapBuffer<char>), null, typeof(Int128?)),
			new("217: GapBuffer<char> -> UInt128", new GapBuffer<char>("83"), 83),
			new("218: GapBuffer<char> -> UInt128?", null, typeof(GapBuffer<char>), null, typeof(UInt128?)),
			new("219: GapBuffer<char> -> decimal", new GapBuffer<char>("84.10"), (decimal) 84.10m),
			new("220: GapBuffer<char> -> decimal?", null, typeof(GapBuffer<char>), null, typeof(decimal?)),
			new("221: GapBuffer<char> -> double", new GapBuffer<char>("85.11"), (double) 85.11d),
			new("222: GapBuffer<char> -> double?", null, typeof(GapBuffer<char>), null, typeof(double?)),
			new("223: GapBuffer<char> -> float", new GapBuffer<char>("86.12"), (float) 86.12f),
			new("224: GapBuffer<char> -> float?", null, typeof(GapBuffer<char>), null, typeof(float?)),
			new("225: GapBuffer<char> -> Guid", new GapBuffer<char>("00000000-0000-0000-0000-000000000087"), Guid.Parse("00000000-0000-0000-0000-000000000087")),
			new("226: GapBuffer<char> -> Guid?", null, typeof(GapBuffer<char>), null, typeof(Guid?)),
			new("227: GapBuffer<char> -> ShortGuid", new GapBuffer<char>("00000000-0000-0000-0000-000000000088"), ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAiA")),
			new("228: GapBuffer<char> -> ShortGuid?", null, typeof(GapBuffer<char>), null, typeof(ShortGuid?)),
			new("229: GapBuffer<char> -> bool", new GapBuffer<char>("True"), true),
			new("230: GapBuffer<char> -> bool?", null, typeof(GapBuffer<char>), null, typeof(bool?)),
			new("231: GapBuffer<char> -> Version", null, typeof(GapBuffer<char>), null, typeof(Version)),
			new("232: JsonString -> string", null, typeof(JsonString), null, typeof(string)),
			new("233: string -> JsonString", null, typeof(string), null, typeof(JsonString)),
			new("234: JsonString -> StringBuilder", null, typeof(JsonString), null, typeof(StringBuilder)),
			new("235: StringBuilder -> JsonString", null, typeof(StringBuilder), null, typeof(JsonString)),
			new("236: JsonString -> TextBuilder", null, typeof(JsonString), null, typeof(TextBuilder)),
			new("237: TextBuilder -> JsonString", null, typeof(TextBuilder), null, typeof(JsonString)),
			new("238: JsonString -> GapBuffer<char>", null, typeof(JsonString), null, typeof(GapBuffer<char>)),
			new("239: GapBuffer<char> -> JsonString", null, typeof(GapBuffer<char>), null, typeof(JsonString)),
			new("240: JsonString -> JsonString", null, typeof(JsonString), null, typeof(JsonString)),
			new("241: JsonString -> char", new JsonString("`"), '`'),
			new("242: JsonString -> char?", null, typeof(JsonString), null, typeof(char?)),
			new("243: JsonString -> DateTime", "2000-01-02T03:05:28.0000000Z", new DateTime(2000, 1, 2, 3, 5, 28, DateTimeKind.Utc)),
			new("244: JsonString -> DateTime?", null, typeof(JsonString), null, typeof(DateTime?)),
			new("245: JsonString -> DateTimeOffset", "2000-01-02T03:05:29.0000000Z", new DateTimeOffset(2000, 1, 2, 3, 5, 29, 0, 0, new TimeSpan(0, 0, 0))),
			new("246: JsonString -> DateTimeOffset?", null, typeof(JsonString), null, typeof(DateTimeOffset?)),
			new("247: JsonString -> IsoDateTime", "2000-01-02T03:05:30.0000000Z", new IsoDateTime(new DateTime(2000, 1, 2, 3, 5, 30, DateTimeKind.Utc), new TimeSpan(0, 0, 0))),
			new("248: JsonString -> IsoDateTime?", null, typeof(JsonString), null, typeof(IsoDateTime?)),
			new("249: JsonString -> OscTimeTag", "2000-01-02T03:05:31.0000000Z", new OscTimeTag(new DateTime(2000, 1, 2, 3, 5, 31, DateTimeKind.Utc))),
			new("250: JsonString -> OscTimeTag?", null, typeof(JsonString), null, typeof(OscTimeTag?)),
			new("251: JsonString -> TimeSpan", "0:03:05:32.0000000", new TimeSpan(3, 5, 32)),
			new("252: JsonString -> TimeSpan?", null, typeof(JsonString), null, typeof(TimeSpan?)),
			new("253: JsonString -> byte", new JsonString("94"), 94),
			new("254: JsonString -> byte?", null, typeof(JsonString), null, typeof(byte?)),
			new("255: JsonString -> sbyte", new JsonString("95"), 95),
			new("256: JsonString -> sbyte?", null, typeof(JsonString), null, typeof(sbyte?)),
			new("257: JsonString -> short", new JsonString("96"), 96),
			new("258: JsonString -> short?", null, typeof(JsonString), null, typeof(short?)),
			new("259: JsonString -> ushort", new JsonString("97"), 97),
			new("260: JsonString -> ushort?", null, typeof(JsonString), null, typeof(ushort?)),
			new("261: JsonString -> int", new JsonString("98"), 98),
			new("262: JsonString -> int?", null, typeof(JsonString), null, typeof(int?)),
			new("263: JsonString -> uint", new JsonString("99"), 99),
			new("264: JsonString -> uint?", null, typeof(JsonString), null, typeof(uint?)),
			new("265: JsonString -> long", new JsonString("100"), 100),
			new("266: JsonString -> long?", null, typeof(JsonString), null, typeof(long?)),
			new("267: JsonString -> ulong", new JsonString("101"), 101),
			new("268: JsonString -> ulong?", null, typeof(JsonString), null, typeof(ulong?)),
			new("269: JsonString -> IntPtr", new JsonString("102"), (IntPtr) 102),
			new("270: JsonString -> IntPtr?", null, typeof(JsonString), null, typeof(IntPtr?)),
			new("271: JsonString -> UIntPtr", new JsonString("103"), (UIntPtr) 103),
			new("272: JsonString -> UIntPtr?", null, typeof(JsonString), null, typeof(UIntPtr?)),
			new("273: JsonString -> Int128", new JsonString("104"), 104),
			new("274: JsonString -> Int128?", null, typeof(JsonString), null, typeof(Int128?)),
			new("275: JsonString -> UInt128", new JsonString("105"), 105),
			new("276: JsonString -> UInt128?", null, typeof(JsonString), null, typeof(UInt128?)),
			new("277: JsonString -> decimal", new JsonString("106.13"), (decimal) 106.13m),
			new("278: JsonString -> decimal?", null, typeof(JsonString), null, typeof(decimal?)),
			new("279: JsonString -> double", new JsonString("107.14"), (double) 107.14d),
			new("280: JsonString -> double?", null, typeof(JsonString), null, typeof(double?)),
			new("281: JsonString -> float", new JsonString("108.15"), (float) 108.15f),
			new("282: JsonString -> float?", null, typeof(JsonString), null, typeof(float?)),
			new("283: JsonString -> Guid", new JsonString("00000000-0000-0000-0000-000000000109"), Guid.Parse("00000000-0000-0000-0000-000000000109")),
			new("284: JsonString -> Guid?", null, typeof(JsonString), null, typeof(Guid?)),
			new("285: JsonString -> ShortGuid", new JsonString("00000000-0000-0000-0000-000000000110"), ShortGuid.Parse("AAAAAAAAAAAAAAAAAAABEA")),
			new("286: JsonString -> ShortGuid?", null, typeof(JsonString), null, typeof(ShortGuid?)),
			new("287: JsonString -> bool", new JsonString("True"), true),
			new("288: JsonString -> bool?", null, typeof(JsonString), null, typeof(bool?)),
			new("289: JsonString -> Version", null, typeof(JsonString), null, typeof(Version)),
			#if (!NET48)
			new("290: string -> DateOnly", "2000-01-02", new DateOnly(2000, 1, 2)),
			new("291: string -> DateOnly?", null, typeof(string), null, typeof(DateOnly?)),
			new("292: string -> TimeOnly", "03:05:51.0000000", TimeOnly.Parse("03:05:51.0000000")),
			new("293: string -> TimeOnly?", null, typeof(string), null, typeof(TimeOnly?)),
			new("294: StringBuilder -> DateOnly", "2000-01-02", new DateOnly(2000, 1, 2)),
			new("295: StringBuilder -> DateOnly?", null, typeof(StringBuilder), null, typeof(DateOnly?)),
			new("296: StringBuilder -> TimeOnly", "03:05:53.0000000", TimeOnly.Parse("03:05:53.0000000")),
			new("297: StringBuilder -> TimeOnly?", null, typeof(StringBuilder), null, typeof(TimeOnly?)),
			new("298: TextBuilder -> DateOnly", "2000-01-02", new DateOnly(2000, 1, 2)),
			new("299: TextBuilder -> DateOnly?", null, typeof(TextBuilder), null, typeof(DateOnly?)),
			new("300: TextBuilder -> TimeOnly", "03:05:55.0000000", TimeOnly.Parse("03:05:55.0000000")),
			new("301: TextBuilder -> TimeOnly?", null, typeof(TextBuilder), null, typeof(TimeOnly?)),
			new("302: GapBuffer<char> -> DateOnly", "2000-01-02", new DateOnly(2000, 1, 2)),
			new("303: GapBuffer<char> -> DateOnly?", null, typeof(GapBuffer<char>), null, typeof(DateOnly?)),
			new("304: GapBuffer<char> -> TimeOnly", "03:05:57.0000000", TimeOnly.Parse("03:05:57.0000000")),
			new("305: GapBuffer<char> -> TimeOnly?", null, typeof(GapBuffer<char>), null, typeof(TimeOnly?)),
			new("306: JsonString -> DateOnly", "2000-01-02", new DateOnly(2000, 1, 2)),
			new("307: JsonString -> DateOnly?", null, typeof(JsonString), null, typeof(DateOnly?)),
			new("308: JsonString -> TimeOnly", "03:05:59.0000000", TimeOnly.Parse("03:05:59.0000000")),
			new("309: JsonString -> TimeOnly?", null, typeof(JsonString), null, typeof(TimeOnly?)),
			#endif
			// </Scenarios>
		});

		return response.ToArray();
	}

	#endregion
}