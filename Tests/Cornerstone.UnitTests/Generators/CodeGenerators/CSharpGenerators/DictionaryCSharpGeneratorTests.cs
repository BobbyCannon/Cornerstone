#region References

using System.Collections.Generic;
using Cornerstone.Generators.CodeGenerators;
using Cornerstone.Generators.CodeGenerators.CSharpGenerators;
using Cornerstone.Testing;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeGenerator = Cornerstone.Generators.CodeGenerator;

#endregion

namespace Cornerstone.UnitTests.Generators.CodeGenerators.CSharpGenerators;

[TestClass]
public class DictionaryCSharpGeneratorTests : CodeGeneratorTests<DictionaryCSharpGenerator>
{
	#region Methods

	[TestMethod]
	public void GenerateScenarios()
	{
		GenerateNewScenarios(
			$@"CSharpGenerators\{nameof(DictionaryCSharpGeneratorTests)}.cs",
			EnableFileUpdates || IsDebugging,
			new Dictionary<string, int> { { "a", 1 }, { "b", 2 } }
		);
	}

	[TestMethod]
	public void RunScenarios()
	{
		var scenarios = GetScenarios();
		var settings = CodeGenerator.DefaultWriterOptions;

		foreach (var scenario in scenarios)
		{
			var actual = Generator.GenerateCode(scenario.From.Type, scenario.From.Value, settings);
			AreEqual(scenario.To.Value, actual, scenario.Name);
		}
	}

	[TestMethod]
	public void RunSingleScenario()
	{
	}

	[TestMethod]
	public void SimpleExamples()
	{
		var generator = new DictionaryCSharpGenerator();
		var dictionary = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };
		var actual = generator.GenerateCode(dictionary.GetType(), dictionary, null);
		var expected = @"new Dictionary<string, int>
{
	{ ""a"", 1 },
	{ ""b"", 2 }
}";
		AreEqual(expected, actual);

		var settings = new CodeWriterOptions { TextFormat = TextFormat.Spaced };
		actual = generator.GenerateCode(dictionary.GetType(), dictionary, settings);
		expected = "new Dictionary<string, int> { { \"a\", 1 }, { \"b\", 2 } }";
		AreEqual(expected, actual);
	}

	private TestScenario[] GetScenarios()
	{
		var scenarios = new TestScenario[]
		{
			// <Scenarios>
			new("0: Dictionary<string, int>", new Dictionary<string, int>
			{
				{ "a", 1 },
				{ "b", 2 }
			}, typeof(Dictionary<string, int>), "new Dictionary<string, int>\r\n{\r\n\t{ \"a\", 1 },\r\n\t{ \"b\", 2 }\r\n}", typeof(string)),
			// </Scenarios>
		};

		return scenarios;
	}

	#endregion
}