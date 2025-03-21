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
			new("15: JsonBoolean -> JsonArray", null, typeof(JsonBoolean), null, typeof(JsonArray)),
			new("16: JsonArray -> JsonBoolean", null, typeof(JsonArray), null, typeof(JsonBoolean)),
			new("17: JsonBoolean -> JsonBoolean", null, typeof(JsonBoolean), null, typeof(JsonBoolean)),
			new("18: JsonBoolean -> JsonNull", null, typeof(JsonBoolean), null, typeof(JsonNull)),
			new("19: JsonNull -> JsonBoolean", null, typeof(JsonNull), null, typeof(JsonBoolean)),
			new("20: JsonBoolean -> JsonNumber", null, typeof(JsonBoolean), null, typeof(JsonNumber)),
			new("21: JsonNumber -> JsonBoolean", null, typeof(JsonNumber), null, typeof(JsonBoolean)),
			new("22: JsonBoolean -> JsonObject", null, typeof(JsonBoolean), null, typeof(JsonObject)),
			new("23: JsonObject -> JsonBoolean", null, typeof(JsonObject), null, typeof(JsonBoolean)),
			new("24: JsonBoolean -> JsonString", null, typeof(JsonBoolean), null, typeof(JsonString)),
			new("25: JsonString -> JsonBoolean", null, typeof(JsonString), null, typeof(JsonBoolean)),
			new("26: JsonBoolean -> string", null, typeof(JsonBoolean), null, typeof(string)),
			new("27: JsonBoolean -> StringBuilder", null, typeof(JsonBoolean), null, typeof(StringBuilder)),
			new("28: JsonBoolean -> TextBuilder", null, typeof(JsonBoolean), null, typeof(TextBuilder)),
			new("29: JsonBoolean -> GapBuffer<char>", null, typeof(JsonBoolean), null, typeof(GapBuffer<char>)),
			new("30: JsonNull -> JsonArray", null, typeof(JsonNull), null, typeof(JsonArray)),
			new("31: JsonArray -> JsonNull", null, typeof(JsonArray), null, typeof(JsonNull)),
			new("32: JsonNull -> JsonBoolean", null, typeof(JsonNull), null, typeof(JsonBoolean)),
			new("33: JsonBoolean -> JsonNull", null, typeof(JsonBoolean), null, typeof(JsonNull)),
			new("34: JsonNull -> JsonNull", null, typeof(JsonNull), null, typeof(JsonNull)),
			new("35: JsonNull -> JsonNumber", null, typeof(JsonNull), null, typeof(JsonNumber)),
			new("36: JsonNumber -> JsonNull", null, typeof(JsonNumber), null, typeof(JsonNull)),
			new("37: JsonNull -> JsonObject", null, typeof(JsonNull), null, typeof(JsonObject)),
			new("38: JsonObject -> JsonNull", null, typeof(JsonObject), null, typeof(JsonNull)),
			new("39: JsonNull -> JsonString", null, typeof(JsonNull), null, typeof(JsonString)),
			new("40: JsonString -> JsonNull", null, typeof(JsonString), null, typeof(JsonNull)),
			new("41: JsonNull -> string", null, typeof(JsonNull), null, typeof(string)),
			new("42: JsonNull -> StringBuilder", null, typeof(JsonNull), null, typeof(StringBuilder)),
			new("43: JsonNull -> TextBuilder", null, typeof(JsonNull), null, typeof(TextBuilder)),
			new("44: JsonNull -> GapBuffer<char>", null, typeof(JsonNull), null, typeof(GapBuffer<char>)),
			new("45: JsonNumber -> JsonArray", null, typeof(JsonNumber), null, typeof(JsonArray)),
			new("46: JsonArray -> JsonNumber", null, typeof(JsonArray), null, typeof(JsonNumber)),
			new("47: JsonNumber -> JsonBoolean", null, typeof(JsonNumber), null, typeof(JsonBoolean)),
			new("48: JsonBoolean -> JsonNumber", null, typeof(JsonBoolean), null, typeof(JsonNumber)),
			new("49: JsonNumber -> JsonNull", null, typeof(JsonNumber), null, typeof(JsonNull)),
			new("50: JsonNull -> JsonNumber", null, typeof(JsonNull), null, typeof(JsonNumber)),
			new("51: JsonNumber -> JsonNumber", null, typeof(JsonNumber), null, typeof(JsonNumber)),
			new("52: JsonNumber -> JsonObject", null, typeof(JsonNumber), null, typeof(JsonObject)),
			new("53: JsonObject -> JsonNumber", null, typeof(JsonObject), null, typeof(JsonNumber)),
			new("54: JsonNumber -> JsonString", null, typeof(JsonNumber), null, typeof(JsonString)),
			new("55: JsonString -> JsonNumber", null, typeof(JsonString), null, typeof(JsonNumber)),
			new("56: JsonNumber -> string", null, typeof(JsonNumber), null, typeof(string)),
			new("57: JsonNumber -> StringBuilder", null, typeof(JsonNumber), null, typeof(StringBuilder)),
			new("58: JsonNumber -> TextBuilder", null, typeof(JsonNumber), null, typeof(TextBuilder)),
			new("59: JsonNumber -> GapBuffer<char>", null, typeof(JsonNumber), null, typeof(GapBuffer<char>)),
			new("60: JsonObject -> JsonArray", null, typeof(JsonObject), null, typeof(JsonArray)),
			new("61: JsonArray -> JsonObject", null, typeof(JsonArray), null, typeof(JsonObject)),
			new("62: JsonObject -> JsonBoolean", null, typeof(JsonObject), null, typeof(JsonBoolean)),
			new("63: JsonBoolean -> JsonObject", null, typeof(JsonBoolean), null, typeof(JsonObject)),
			new("64: JsonObject -> JsonNull", null, typeof(JsonObject), null, typeof(JsonNull)),
			new("65: JsonNull -> JsonObject", null, typeof(JsonNull), null, typeof(JsonObject)),
			new("66: JsonObject -> JsonNumber", null, typeof(JsonObject), null, typeof(JsonNumber)),
			new("67: JsonNumber -> JsonObject", null, typeof(JsonNumber), null, typeof(JsonObject)),
			new("68: JsonObject -> JsonObject", null, typeof(JsonObject), null, typeof(JsonObject)),
			new("69: JsonObject -> JsonString", null, typeof(JsonObject), null, typeof(JsonString)),
			new("70: JsonString -> JsonObject", null, typeof(JsonString), null, typeof(JsonObject)),
			new("71: JsonObject -> string", null, typeof(JsonObject), null, typeof(string)),
			new("72: JsonObject -> StringBuilder", null, typeof(JsonObject), null, typeof(StringBuilder)),
			new("73: JsonObject -> TextBuilder", null, typeof(JsonObject), null, typeof(TextBuilder)),
			new("74: JsonObject -> GapBuffer<char>", null, typeof(JsonObject), null, typeof(GapBuffer<char>)),
			new("75: JsonString -> JsonArray", null, typeof(JsonString), null, typeof(JsonArray)),
			new("76: JsonArray -> JsonString", null, typeof(JsonArray), null, typeof(JsonString)),
			new("77: JsonString -> JsonBoolean", null, typeof(JsonString), null, typeof(JsonBoolean)),
			new("78: JsonBoolean -> JsonString", null, typeof(JsonBoolean), null, typeof(JsonString)),
			new("79: JsonString -> JsonNull", null, typeof(JsonString), null, typeof(JsonNull)),
			new("80: JsonNull -> JsonString", null, typeof(JsonNull), null, typeof(JsonString)),
			new("81: JsonString -> JsonNumber", null, typeof(JsonString), null, typeof(JsonNumber)),
			new("82: JsonNumber -> JsonString", null, typeof(JsonNumber), null, typeof(JsonString)),
			new("83: JsonString -> JsonObject", null, typeof(JsonString), null, typeof(JsonObject)),
			new("84: JsonObject -> JsonString", null, typeof(JsonObject), null, typeof(JsonString)),
			new("85: JsonString -> JsonString", null, typeof(JsonString), null, typeof(JsonString)),
			new("86: JsonString -> string", null, typeof(JsonString), null, typeof(string)),
			new("87: JsonString -> StringBuilder", null, typeof(JsonString), null, typeof(StringBuilder)),
			new("88: JsonString -> TextBuilder", null, typeof(JsonString), null, typeof(TextBuilder)),
			new("89: JsonString -> GapBuffer<char>", null, typeof(JsonString), null, typeof(GapBuffer<char>)),
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