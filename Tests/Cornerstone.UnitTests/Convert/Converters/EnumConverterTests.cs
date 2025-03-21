#region References

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Cornerstone.Collections;
using Cornerstone.Convert.Converters;
using Cornerstone.Extensions;
using Cornerstone.Serialization.Json.Values;
using Cornerstone.Testing;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Convert.Converters;

[TestClass]
public class EnumConverterTests : ConverterTests<EnumConverter>
{
	#region Methods

	[TestMethod]
	public void CanConvert()
	{
		ValidateCanConvert();

		IsTrue(Converter.CanConvert(typeof(SampleEnum), typeof(string)));
		IsTrue(Converter.CanConvert(typeof(string), typeof(SampleEnum)));
		IsTrue(Converter.CanConvert(typeof(string), typeof(SampleEnum?)));
		IsTrue(Converter.CanConvert(typeof(int), typeof(SampleEnum)));
		IsTrue(Converter.CanConvert(typeof(int), typeof(SampleEnum?)));
		IsTrue(Converter.CanConvert(typeof(SampleEnum), typeof(SampleEnum)));

		IsFalse(Converter.CanConvert(typeof(string), typeof(string)));
	}

	[TestMethod]
	public void GenerateScenarios()
	{
		GenerateNewScenarios(
			$"{nameof(EnumConverterTests)}.cs",
			EnableFileUpdates || IsDebugging,
			[typeof(SampleEnum)],
			ArrayExtensions.CombineArrays(
				Activator.StringTypes
			)
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
		TestScenarios(GetTestScenarios()[12]);
	}

	[SuppressMessage("ReSharper", "StringLiteralTypo")]
	[SuppressMessage("ReSharper", "RedundantCast")]
	private TestScenario[] GetTestScenarios()
	{
		var response = new List<TestScenario>
		{
			// <Scenarios>
			new("0: SampleEnum -> string", SampleEnum.Zero, "Zero"),
			new("1: string -> SampleEnum", "One", SampleEnum.One),
			new("2: SampleEnum -> StringBuilder", SampleEnum.Two, new StringBuilder("Two")),
			new("3: StringBuilder -> SampleEnum", new StringBuilder("Three"), SampleEnum.Three),
			new("4: SampleEnum -> TextBuilder", SampleEnum.Four, new TextBuilder("Four")),
			new("5: TextBuilder -> SampleEnum", new TextBuilder("Five"), SampleEnum.Five),
			new("6: SampleEnum -> GapBuffer<char>", SampleEnum.Six, new GapBuffer<char>("Six")),
			new("7: GapBuffer<char> -> SampleEnum", new GapBuffer<char>("Seven"), SampleEnum.Seven),
			new("8: SampleEnum -> JsonString", SampleEnum.Eight, new JsonString("Eight")),
			new("9: JsonString -> SampleEnum", new JsonString("Nine"), SampleEnum.Nine),
			#if (!NET48)
			#endif
			// </Scenarios>
		};

		response.Add(new TestScenario<byte, SampleEnum>($"{response.Count} byte -> SampleEnum?", 9, SampleEnum.Nine));
		response.Add(new TestScenario<ulong, SampleEnum>($"{response.Count} ulong -> SampleEnum?", 10, SampleEnum.Ten));
		response.Add(new TestScenario<string, SampleEnum>($"{response.Count} string -> SampleEnum?", "8", SampleEnum.Eight));
		response.Add(new TestScenario<string, SampleEnum>($"{response.Count} string -> SampleEnum?", "Eight", SampleEnum.Eight));

		return response.ToArray();
	}

	#endregion

	#region Enumerations

	public enum SampleEnum
	{
		Zero = 0,
		One = 1,
		Two = 2,
		Three = 3,
		Four = 4,
		Five = 5,
		Six = 6,
		Seven = 7,
		Eight = 8,
		Nine = 9,
		Ten = 10,
		Eleven = 11,
		Twelve = 12,
		Thirteen = 13,
		Fourteen = 14,
		Fifteen = 15,
		Sixteen = 16,
		Seventeen = 17,
		Eighteen = 18,
		Nineteen = 19,
		Twenty = 20
	}

	#endregion
}