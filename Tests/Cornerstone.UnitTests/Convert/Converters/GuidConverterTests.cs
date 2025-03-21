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
			new("11: Guid -> JsonString", Guid.Parse("00000000-0000-0000-0000-000000000012"), new JsonString("00000000-0000-0000-0000-000000000012")),
			new("12: Guid? -> Guid", Guid.Parse("00000000-0000-0000-0000-000000000013"), Guid.Parse("00000000-0000-0000-0000-000000000013")),
			new("13: Guid -> Guid?", Guid.Parse("00000000-0000-0000-0000-000000000014"), Guid.Parse("00000000-0000-0000-0000-000000000014")),
			new("14: Guid? -> Guid?", null, typeof(Guid?), null, typeof(Guid?)),
			new("15: Guid? -> ShortGuid", Guid.Parse("00000000-0000-0000-0000-000000000015"), ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAFQ")),
			new("16: ShortGuid -> Guid?", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAFg"), Guid.Parse("00000000-0000-0000-0000-000000000016")),
			new("17: Guid? -> ShortGuid?", null, typeof(Guid?), null, typeof(ShortGuid?)),
			new("18: ShortGuid? -> Guid?", null, typeof(ShortGuid?), null, typeof(Guid?)),
			new("19: Guid? -> string", null, typeof(Guid?), null, typeof(string)),
			new("20: Guid? -> StringBuilder", null, typeof(Guid?), null, typeof(StringBuilder)),
			new("21: Guid? -> TextBuilder", null, typeof(Guid?), null, typeof(TextBuilder)),
			new("22: Guid? -> GapBuffer<char>", null, typeof(Guid?), null, typeof(GapBuffer<char>)),
			new("23: Guid? -> JsonString", null, typeof(Guid?), null, typeof(JsonString)),
			new("24: ShortGuid -> Guid", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAFw"), Guid.Parse("00000000-0000-0000-0000-000000000017")),
			new("25: Guid -> ShortGuid", Guid.Parse("00000000-0000-0000-0000-000000000018"), ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAGA")),
			new("26: ShortGuid -> Guid?", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAGQ"), Guid.Parse("00000000-0000-0000-0000-000000000019")),
			new("27: Guid? -> ShortGuid", Guid.Parse("00000000-0000-0000-0000-000000000020"), ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAIA")),
			new("28: ShortGuid -> ShortGuid", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAIQ"), ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAIQ")),
			new("29: ShortGuid -> ShortGuid?", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAIg"), ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAIg")),
			new("30: ShortGuid? -> ShortGuid", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAIw"), ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAIw")),
			new("31: ShortGuid -> string", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAJA"), "00000000-0000-0000-0000-000000000024"),
			new("32: ShortGuid -> StringBuilder", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAJQ"), new StringBuilder("00000000-0000-0000-0000-000000000025")),
			new("33: ShortGuid -> TextBuilder", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAJg"), new TextBuilder("00000000-0000-0000-0000-000000000026")),
			new("34: ShortGuid -> GapBuffer<char>", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAJw"), new GapBuffer<char>("00000000-0000-0000-0000-000000000027")),
			new("35: ShortGuid -> JsonString", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAKA"), new JsonString("00000000-0000-0000-0000-000000000028")),
			new("36: ShortGuid? -> Guid", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAKQ"), Guid.Parse("00000000-0000-0000-0000-000000000029")),
			new("37: Guid -> ShortGuid?", Guid.Parse("00000000-0000-0000-0000-000000000030"), ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAMA")),
			new("38: ShortGuid? -> Guid?", null, typeof(ShortGuid?), null, typeof(Guid?)),
			new("39: Guid? -> ShortGuid?", null, typeof(Guid?), null, typeof(ShortGuid?)),
			new("40: ShortGuid? -> ShortGuid", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAMQ"), ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAMQ")),
			new("41: ShortGuid -> ShortGuid?", ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAMg"), ShortGuid.Parse("AAAAAAAAAAAAAAAAAAAAMg")),
			new("42: ShortGuid? -> ShortGuid?", null, typeof(ShortGuid?), null, typeof(ShortGuid?)),
			new("43: ShortGuid? -> string", null, typeof(ShortGuid?), null, typeof(string)),
			new("44: ShortGuid? -> StringBuilder", null, typeof(ShortGuid?), null, typeof(StringBuilder)),
			new("45: ShortGuid? -> TextBuilder", null, typeof(ShortGuid?), null, typeof(TextBuilder)),
			new("46: ShortGuid? -> GapBuffer<char>", null, typeof(ShortGuid?), null, typeof(GapBuffer<char>)),
			new("47: ShortGuid? -> JsonString", null, typeof(ShortGuid?), null, typeof(JsonString)),
			#if (!NET48)
			#endif
			// </Scenarios>
		};
	}

	#endregion
}