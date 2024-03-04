#region References

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Cornerstone.Collections;
using Cornerstone.Convert.Converters;
using Cornerstone.Serialization.Json.Values;
using Cornerstone.Testing;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Convert.Converters;

[TestClass]
public class GuidConverterTests : ConverterTests<GuidConverter>
{
	#region Methods

	[TestMethod]
	public void Basic()
	{
		IsTrue(Converter.TryConvertTo(null, typeof(Guid?), typeof(Guid?), out var guid));
		AreEqual(null, guid);
	}

	[TestMethod]
	public void CanConvert()
	{
		ValidateCanConvert();

		IsTrue(Converter.CanConvert(typeof(Guid), typeof(ShortGuid)));
		IsTrue(Converter.CanConvert(typeof(Guid), typeof(string)));
		IsTrue(Converter.CanConvert(typeof(Guid?), typeof(Guid?)));

		IsFalse(Converter.CanConvert(typeof(string), typeof(Guid)));
		IsFalse(Converter.CanConvert(typeof(string), typeof(ShortGuid)));
		IsFalse(Converter.CanConvert(typeof(DateTime), typeof(Guid)));
		IsFalse(Converter.CanConvert(typeof(Guid), typeof(DateTime)));
		IsFalse(Converter.CanConvert(typeof(string), typeof(string)));
	}

	[TestMethod]
	public void GenerateScenarios()
	{
		GenerateNewScenarios($"{nameof(GuidConverterTests)}.cs", EnableFileUpdates || IsDebugging);
	}

	[TestMethod]
	public void RunAllTests()
	{
		TestScenarios(GetTestScenarios());
	}

	[TestMethod]
	public void RunSingleTest()
	{
		TestScenarios(GetTestScenarios()[0]);
	}

	[SuppressMessage("ReSharper", "StringLiteralTypo")]
	[SuppressMessage("ReSharper", "RedundantCast")]
	private TestScenario[] GetTestScenarios()
	{
		return new TestScenario[]
		{
			// <Scenarios>
			new("0: Guid -> Guid", Guid.Parse("00000000-0000-0000-0000-000000000001"), Guid.Parse("00000000-0000-0000-0000-000000000001")),
			new("1: Guid -> Guid?", Guid.Parse("00000000-0000-0000-0000-000000000002"), Guid.Parse("00000000-0000-0000-0000-000000000002")),
			new("2: Guid? -> Guid", Guid.Parse("00000000-0000-0000-0000-000000000003"), Guid.Parse("00000000-0000-0000-0000-000000000003")),
			new("3: Guid -> ShortGuid", Guid.Parse("00000000-0000-0000-0000-000000000004"), ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAABA")),
			new("4: ShortGuid -> Guid", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAABQ"), Guid.Parse("00000000-0000-0000-0000-000000000005")),
			new("5: Guid -> ShortGuid?", Guid.Parse("00000000-0000-0000-0000-000000000006"), ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAABg")),
			new("6: ShortGuid? -> Guid", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAABw"), Guid.Parse("00000000-0000-0000-0000-000000000007")),
			new("7: Guid -> string", Guid.Parse("00000000-0000-0000-0000-000000000008"), "00000000-0000-0000-0000-000000000008"),
			new("8: Guid -> StringBuilder", Guid.Parse("00000000-0000-0000-0000-000000000009"), new StringBuilder("00000000-0000-0000-0000-000000000009")),
			new("9: Guid -> TextBuilder", Guid.Parse("00000000-0000-0000-0000-000000000010"), new TextBuilder("00000000-0000-0000-0000-000000000010")),
			new("10: Guid -> GapBuffer<char>", Guid.Parse("00000000-0000-0000-0000-000000000011"), new GapBuffer<char>("00000000-0000-0000-0000-000000000011")),
			new("11: Guid -> RopeBuffer<char>", Guid.Parse("00000000-0000-0000-0000-000000000012"), new RopeBuffer<char>("00000000-0000-0000-0000-000000000012")),
			new("12: Guid -> JsonString", Guid.Parse("00000000-0000-0000-0000-000000000013"), new JsonString("00000000-0000-0000-0000-000000000013")),
			new("13: Guid? -> Guid", Guid.Parse("00000000-0000-0000-0000-000000000014"), Guid.Parse("00000000-0000-0000-0000-000000000014")),
			new("14: Guid -> Guid?", Guid.Parse("00000000-0000-0000-0000-000000000015"), Guid.Parse("00000000-0000-0000-0000-000000000015")),
			new("15: Guid? -> Guid?", null, typeof(Guid?), null, typeof(Guid?)),
			new("16: Guid? -> ShortGuid", Guid.Parse("00000000-0000-0000-0000-000000000016"), ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAFg")),
			new("17: ShortGuid -> Guid?", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAFw"), Guid.Parse("00000000-0000-0000-0000-000000000017")),
			new("18: Guid? -> ShortGuid?", null, typeof(Guid?), null, typeof(ShortGuid?)),
			new("19: ShortGuid? -> Guid?", null, typeof(ShortGuid?), null, typeof(Guid?)),
			new("20: Guid? -> string", null, typeof(Guid?), null, typeof(string)),
			new("21: Guid? -> StringBuilder", null, typeof(Guid?), null, typeof(StringBuilder)),
			new("22: Guid? -> TextBuilder", null, typeof(Guid?), null, typeof(TextBuilder)),
			new("23: Guid? -> GapBuffer<char>", null, typeof(Guid?), null, typeof(GapBuffer<char>)),
			new("24: Guid? -> RopeBuffer<char>", null, typeof(Guid?), null, typeof(RopeBuffer<char>)),
			new("25: Guid? -> JsonString", null, typeof(Guid?), null, typeof(JsonString)),
			new("26: ShortGuid -> Guid", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAGA"), Guid.Parse("00000000-0000-0000-0000-000000000018")),
			new("27: Guid -> ShortGuid", Guid.Parse("00000000-0000-0000-0000-000000000019"), ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAGQ")),
			new("28: ShortGuid -> Guid?", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAIA"), Guid.Parse("00000000-0000-0000-0000-000000000020")),
			new("29: Guid? -> ShortGuid", Guid.Parse("00000000-0000-0000-0000-000000000021"), ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAIQ")),
			new("30: ShortGuid -> ShortGuid", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAIg"), ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAIg")),
			new("31: ShortGuid -> ShortGuid?", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAIw"), ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAIw")),
			new("32: ShortGuid? -> ShortGuid", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAJA"), ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAJA")),
			new("33: ShortGuid -> string", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAJQ"), "00000000-0000-0000-0000-000000000025"),
			new("34: ShortGuid -> StringBuilder", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAJg"), new StringBuilder("00000000-0000-0000-0000-000000000026")),
			new("35: ShortGuid -> TextBuilder", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAJw"), new TextBuilder("00000000-0000-0000-0000-000000000027")),
			new("36: ShortGuid -> GapBuffer<char>", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAKA"), new GapBuffer<char>("00000000-0000-0000-0000-000000000028")),
			new("37: ShortGuid -> RopeBuffer<char>", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAKQ"), new RopeBuffer<char>("00000000-0000-0000-0000-000000000029")),
			new("38: ShortGuid -> JsonString", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAMA"), new JsonString("00000000-0000-0000-0000-000000000030")),
			new("39: ShortGuid? -> Guid", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAMQ"), Guid.Parse("00000000-0000-0000-0000-000000000031")),
			new("40: Guid -> ShortGuid?", Guid.Parse("00000000-0000-0000-0000-000000000032"), ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAMg")),
			new("41: ShortGuid? -> Guid?", null, typeof(ShortGuid?), null, typeof(Guid?)),
			new("42: Guid? -> ShortGuid?", null, typeof(Guid?), null, typeof(ShortGuid?)),
			new("43: ShortGuid? -> ShortGuid", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAMw"), ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAMw")),
			new("44: ShortGuid -> ShortGuid?", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAANA"), ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAANA")),
			new("45: ShortGuid? -> ShortGuid?", null, typeof(ShortGuid?), null, typeof(ShortGuid?)),
			new("46: ShortGuid? -> string", null, typeof(ShortGuid?), null, typeof(string)),
			new("47: ShortGuid? -> StringBuilder", null, typeof(ShortGuid?), null, typeof(StringBuilder)),
			new("48: ShortGuid? -> TextBuilder", null, typeof(ShortGuid?), null, typeof(TextBuilder)),
			new("49: ShortGuid? -> GapBuffer<char>", null, typeof(ShortGuid?), null, typeof(GapBuffer<char>)),
			new("50: ShortGuid? -> RopeBuffer<char>", null, typeof(ShortGuid?), null, typeof(RopeBuffer<char>)),
			new("51: ShortGuid? -> JsonString", null, typeof(ShortGuid?), null, typeof(JsonString)),
			#if (!NET48)
			#endif
			// </Scenarios>
		};
	}

	#endregion
}