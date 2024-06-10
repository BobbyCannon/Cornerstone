#region References

using System.Collections;
using System.Collections.Generic;
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
public class JsonValueConverterTests : ConverterTests<JsonValueConverter>
{
	#region Methods

	[TestMethod]
	public void CanConvert()
	{
		ValidateCanConvert();

		IsTrue(Converter.CanConvert(typeof(JsonObject), typeof(IDictionary)));
		IsTrue(Converter.CanConvert(typeof(JsonObject), typeof(Dictionary<,>)));
	}

	[TestMethod]
	public void GenerateScenarios()
	{
		GenerateNewScenarios($"{nameof(JsonValueConverterTests)}.cs",
			EnableFileUpdates || IsDebugging
		);
	}

	[TestMethod]
	public void RunScenarios()
	{
		var scenarios = GetTestScenarios();
		
		foreach (var scenario in scenarios)
		{
			var result = Converter.TryConvertTo(
				scenario.From.Value, scenario.From.Type,
				scenario.To.Type, out var value
			);

			IsTrue(result);
			AreEqual(scenario.To.Value, value);
		}
	}

	[SuppressMessage("ReSharper", "StringLiteralTypo")]
	[SuppressMessage("ReSharper", "RedundantCast")]
	private TestScenario[] GetTestScenarios()
	{
		var response = new List<TestScenario>();

		response.AddRange(new TestScenario[]
		{
			// <Scenarios>
			new("0: JsonArray -> JsonArray", null, typeof(JsonArray), null, typeof(JsonArray)),
			new("1: JsonArray -> JsonBoolean", null, typeof(JsonArray), null, typeof(JsonBoolean)),
			new("2: JsonBoolean -> JsonArray", null, typeof(JsonBoolean), null, typeof(JsonArray)),
			new("3: JsonArray -> JsonNull", null, typeof(JsonArray), null, typeof(JsonNull)),
			new("4: JsonNull -> JsonArray", null, typeof(JsonNull), null, typeof(JsonArray)),
			new("5: JsonArray -> JsonNumber", null, typeof(JsonArray), null, typeof(JsonNumber)),
			new("6: JsonNumber -> JsonArray", null, typeof(JsonNumber), null, typeof(JsonArray)),
			new("7: JsonArray -> JsonObject", null, typeof(JsonArray), null, typeof(JsonObject)),
			new("8: JsonObject -> JsonArray", null, typeof(JsonObject), null, typeof(JsonArray)),
			new("9: JsonArray -> JsonString", null, typeof(JsonArray), null, typeof(JsonString)),
			new("10: JsonString -> JsonArray", null, typeof(JsonString), null, typeof(JsonArray)),
			new("11: JsonArray -> string", null, typeof(JsonArray), null, typeof(string)),
			new("12: JsonArray -> StringBuilder", null, typeof(JsonArray), null, typeof(StringBuilder)),
			new("13: JsonArray -> TextBuilder", null, typeof(JsonArray), null, typeof(TextBuilder)),
			new("14: JsonArray -> GapBuffer<char>", null, typeof(JsonArray), null, typeof(GapBuffer<char>)),
			new("15: JsonArray -> RopeBuffer<char>", null, typeof(JsonArray), null, typeof(RopeBuffer<char>)),
			new("16: JsonBoolean -> JsonArray", null, typeof(JsonBoolean), null, typeof(JsonArray)),
			new("17: JsonArray -> JsonBoolean", null, typeof(JsonArray), null, typeof(JsonBoolean)),
			new("18: JsonBoolean -> JsonBoolean", null, typeof(JsonBoolean), null, typeof(JsonBoolean)),
			new("19: JsonBoolean -> JsonNull", null, typeof(JsonBoolean), null, typeof(JsonNull)),
			new("20: JsonNull -> JsonBoolean", null, typeof(JsonNull), null, typeof(JsonBoolean)),
			new("21: JsonBoolean -> JsonNumber", null, typeof(JsonBoolean), null, typeof(JsonNumber)),
			new("22: JsonNumber -> JsonBoolean", null, typeof(JsonNumber), null, typeof(JsonBoolean)),
			new("23: JsonBoolean -> JsonObject", null, typeof(JsonBoolean), null, typeof(JsonObject)),
			new("24: JsonObject -> JsonBoolean", null, typeof(JsonObject), null, typeof(JsonBoolean)),
			new("25: JsonBoolean -> JsonString", null, typeof(JsonBoolean), null, typeof(JsonString)),
			new("26: JsonString -> JsonBoolean", null, typeof(JsonString), null, typeof(JsonBoolean)),
			new("27: JsonBoolean -> string", null, typeof(JsonBoolean), null, typeof(string)),
			new("28: JsonBoolean -> StringBuilder", null, typeof(JsonBoolean), null, typeof(StringBuilder)),
			new("29: JsonBoolean -> TextBuilder", null, typeof(JsonBoolean), null, typeof(TextBuilder)),
			new("30: JsonBoolean -> GapBuffer<char>", null, typeof(JsonBoolean), null, typeof(GapBuffer<char>)),
			new("31: JsonBoolean -> RopeBuffer<char>", null, typeof(JsonBoolean), null, typeof(RopeBuffer<char>)),
			new("32: JsonNull -> JsonArray", null, typeof(JsonNull), null, typeof(JsonArray)),
			new("33: JsonArray -> JsonNull", null, typeof(JsonArray), null, typeof(JsonNull)),
			new("34: JsonNull -> JsonBoolean", null, typeof(JsonNull), null, typeof(JsonBoolean)),
			new("35: JsonBoolean -> JsonNull", null, typeof(JsonBoolean), null, typeof(JsonNull)),
			new("36: JsonNull -> JsonNull", null, typeof(JsonNull), null, typeof(JsonNull)),
			new("37: JsonNull -> JsonNumber", null, typeof(JsonNull), null, typeof(JsonNumber)),
			new("38: JsonNumber -> JsonNull", null, typeof(JsonNumber), null, typeof(JsonNull)),
			new("39: JsonNull -> JsonObject", null, typeof(JsonNull), null, typeof(JsonObject)),
			new("40: JsonObject -> JsonNull", null, typeof(JsonObject), null, typeof(JsonNull)),
			new("41: JsonNull -> JsonString", null, typeof(JsonNull), null, typeof(JsonString)),
			new("42: JsonString -> JsonNull", null, typeof(JsonString), null, typeof(JsonNull)),
			new("43: JsonNull -> string", null, typeof(JsonNull), null, typeof(string)),
			new("44: JsonNull -> StringBuilder", null, typeof(JsonNull), null, typeof(StringBuilder)),
			new("45: JsonNull -> TextBuilder", null, typeof(JsonNull), null, typeof(TextBuilder)),
			new("46: JsonNull -> GapBuffer<char>", null, typeof(JsonNull), null, typeof(GapBuffer<char>)),
			new("47: JsonNull -> RopeBuffer<char>", null, typeof(JsonNull), null, typeof(RopeBuffer<char>)),
			new("48: JsonNumber -> JsonArray", null, typeof(JsonNumber), null, typeof(JsonArray)),
			new("49: JsonArray -> JsonNumber", null, typeof(JsonArray), null, typeof(JsonNumber)),
			new("50: JsonNumber -> JsonBoolean", null, typeof(JsonNumber), null, typeof(JsonBoolean)),
			new("51: JsonBoolean -> JsonNumber", null, typeof(JsonBoolean), null, typeof(JsonNumber)),
			new("52: JsonNumber -> JsonNull", null, typeof(JsonNumber), null, typeof(JsonNull)),
			new("53: JsonNull -> JsonNumber", null, typeof(JsonNull), null, typeof(JsonNumber)),
			new("54: JsonNumber -> JsonNumber", null, typeof(JsonNumber), null, typeof(JsonNumber)),
			new("55: JsonNumber -> JsonObject", null, typeof(JsonNumber), null, typeof(JsonObject)),
			new("56: JsonObject -> JsonNumber", null, typeof(JsonObject), null, typeof(JsonNumber)),
			new("57: JsonNumber -> JsonString", null, typeof(JsonNumber), null, typeof(JsonString)),
			new("58: JsonString -> JsonNumber", null, typeof(JsonString), null, typeof(JsonNumber)),
			new("59: JsonNumber -> string", null, typeof(JsonNumber), null, typeof(string)),
			new("60: JsonNumber -> StringBuilder", null, typeof(JsonNumber), null, typeof(StringBuilder)),
			new("61: JsonNumber -> TextBuilder", null, typeof(JsonNumber), null, typeof(TextBuilder)),
			new("62: JsonNumber -> GapBuffer<char>", null, typeof(JsonNumber), null, typeof(GapBuffer<char>)),
			new("63: JsonNumber -> RopeBuffer<char>", null, typeof(JsonNumber), null, typeof(RopeBuffer<char>)),
			new("64: JsonObject -> JsonArray", null, typeof(JsonObject), null, typeof(JsonArray)),
			new("65: JsonArray -> JsonObject", null, typeof(JsonArray), null, typeof(JsonObject)),
			new("66: JsonObject -> JsonBoolean", null, typeof(JsonObject), null, typeof(JsonBoolean)),
			new("67: JsonBoolean -> JsonObject", null, typeof(JsonBoolean), null, typeof(JsonObject)),
			new("68: JsonObject -> JsonNull", null, typeof(JsonObject), null, typeof(JsonNull)),
			new("69: JsonNull -> JsonObject", null, typeof(JsonNull), null, typeof(JsonObject)),
			new("70: JsonObject -> JsonNumber", null, typeof(JsonObject), null, typeof(JsonNumber)),
			new("71: JsonNumber -> JsonObject", null, typeof(JsonNumber), null, typeof(JsonObject)),
			new("72: JsonObject -> JsonObject", null, typeof(JsonObject), null, typeof(JsonObject)),
			new("73: JsonObject -> JsonString", null, typeof(JsonObject), null, typeof(JsonString)),
			new("74: JsonString -> JsonObject", null, typeof(JsonString), null, typeof(JsonObject)),
			new("75: JsonObject -> string", null, typeof(JsonObject), null, typeof(string)),
			new("76: JsonObject -> StringBuilder", null, typeof(JsonObject), null, typeof(StringBuilder)),
			new("77: JsonObject -> TextBuilder", null, typeof(JsonObject), null, typeof(TextBuilder)),
			new("78: JsonObject -> GapBuffer<char>", null, typeof(JsonObject), null, typeof(GapBuffer<char>)),
			new("79: JsonObject -> RopeBuffer<char>", null, typeof(JsonObject), null, typeof(RopeBuffer<char>)),
			new("80: JsonString -> JsonArray", null, typeof(JsonString), null, typeof(JsonArray)),
			new("81: JsonArray -> JsonString", null, typeof(JsonArray), null, typeof(JsonString)),
			new("82: JsonString -> JsonBoolean", null, typeof(JsonString), null, typeof(JsonBoolean)),
			new("83: JsonBoolean -> JsonString", null, typeof(JsonBoolean), null, typeof(JsonString)),
			new("84: JsonString -> JsonNull", null, typeof(JsonString), null, typeof(JsonNull)),
			new("85: JsonNull -> JsonString", null, typeof(JsonNull), null, typeof(JsonString)),
			new("86: JsonString -> JsonNumber", null, typeof(JsonString), null, typeof(JsonNumber)),
			new("87: JsonNumber -> JsonString", null, typeof(JsonNumber), null, typeof(JsonString)),
			new("88: JsonString -> JsonObject", null, typeof(JsonString), null, typeof(JsonObject)),
			new("89: JsonObject -> JsonString", null, typeof(JsonObject), null, typeof(JsonString)),
			new("90: JsonString -> JsonString", null, typeof(JsonString), null, typeof(JsonString)),
			new("91: JsonString -> string", null, typeof(JsonString), null, typeof(string)),
			new("92: JsonString -> StringBuilder", null, typeof(JsonString), null, typeof(StringBuilder)),
			new("93: JsonString -> TextBuilder", null, typeof(JsonString), null, typeof(TextBuilder)),
			new("94: JsonString -> GapBuffer<char>", null, typeof(JsonString), null, typeof(GapBuffer<char>)),
			new("95: JsonString -> RopeBuffer<char>", null, typeof(JsonString), null, typeof(RopeBuffer<char>)),
			#if (!NET48)
			#endif
			// </Scenarios>
		});

		response.Add(new TestScenario($"{response.Count} Array To Interface",
				new JsonArray(new JsonNumber(1), new JsonNumber(2), new JsonNumber(3), new JsonNumber(4)),
				typeof(JsonArray),
				new List<int> { 1, 2, 3, 4 },
				typeof(IList<int>)
			)
		);

		return response.ToArray();
	}

	#endregion
}