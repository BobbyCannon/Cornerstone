#region References

using Cornerstone.Generators;
using Cornerstone.Generators.CodeGenerators.CSharpGenerators;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Generators.CodeGenerators.CSharpGenerators;

[TestClass]
public class DateCSharpGeneratorTests : CodeGeneratorTests<DateCSharpGenerator>
{
	#region Methods

	[TestMethod]
	public void GenerateScenarios()
	{
		GenerateNewScenarios($@"CSharpGenerators\{nameof(DateCSharpGeneratorTests)}.cs", EnableFileUpdates);
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

	private TestScenario[] GetScenarios()
	{
		var scenarios = new TestScenario[]
		{
			// <Scenarios>
			
			// </Scenarios>
		};

		return scenarios;
	}

	#endregion
}