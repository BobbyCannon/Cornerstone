#region References

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
public class BooleanConverterTests : ConverterTests<BooleanConverter>
{
	#region Methods

	[TestMethod]
	public void CanConvert()
	{
		ValidateCanConvert();

		IsTrue(Converter.CanConvert(typeof(bool), typeof(bool)));
		IsTrue(Converter.CanConvert(typeof(bool), typeof(string)));

		IsFalse(Converter.CanConvert(typeof(string), typeof(bool)));
		IsFalse(Converter.CanConvert(typeof(string), typeof(string)));
	}

	[TestMethod]
	public void GenerateScenarios()
	{
		GenerateNewScenarios(
			$"{nameof(BooleanConverterTests)}.cs",
			EnableFileUpdates || IsDebugging
		);
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
			new("0: bool -> bool", true, true),
			new("1: bool -> bool?", false, false),
			new("2: bool? -> bool", true, true),
			new("3: bool -> string", false, "False"),
			new("4: bool -> StringBuilder", true, new StringBuilder("True")),
			new("5: bool -> TextBuilder", false, new TextBuilder("False")),
			new("6: bool -> GapBuffer<char>", true, new GapBuffer<char>("True")),
			new("7: bool -> JsonString", false, new JsonString("False")),
			new("8: bool? -> bool", true, true),
			new("9: bool -> bool?", false, false),
			new("10: bool? -> bool?", null, typeof(bool?), null, typeof(bool?)),
			new("11: bool? -> string", null, typeof(bool?), null, typeof(string)),
			new("12: bool? -> StringBuilder", null, typeof(bool?), null, typeof(StringBuilder)),
			new("13: bool? -> TextBuilder", null, typeof(bool?), null, typeof(TextBuilder)),
			new("14: bool? -> GapBuffer<char>", null, typeof(bool?), null, typeof(GapBuffer<char>)),
			new("15: bool? -> JsonString", null, typeof(bool?), null, typeof(JsonString)),
			#if (!NET48)
			#endif
			// </Scenarios>
		});

		return response.ToArray();
	}

	#endregion
}