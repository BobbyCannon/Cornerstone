#region References

using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Generators.UnitTests.SourceReflection;

[TestClass]
public class TestRunnerTests : GeneratorUnitTest
{
	#region Methods

	[TestMethod]
	public void TestRunnerBuildForMsTest()
	{
		var source = """
					using Microsoft.VisualStudio.TestTools.UnitTesting;

					namespace Cornerstone.Test.Models;

					[TestClass]
					public class UnitTests
					{
						[TestMethod]
						public void Test()
						{
						}
					}
					""";

		var testRunnerSource = File.ReadAllText(GetTestRunnerSourcePath()).Trim('\uFEFF').TrimEnd('\r', '\n');
		var result = Run(source, OutputKind.ConsoleApplication);
		var actuals = result.RunResult.GeneratedTrees
			.Select(tree => tree.ToString().Trim('\uFEFF').Trim())
			.ToArray();

		Assert.IsTrue(actuals.Any(actual => actual.Contains("class Program") && actual.Contains("new TestRunner(args)")),
			"Generated sources should include TestRunner Program.");
		Assert.IsTrue(actuals.Any(actual => actual.Contains(testRunnerSource)),
			"Generated sources should include TestRunner.cs.");
	}

	private static string GetTestRunnerSourcePath([CallerFilePath] string callerFilePath = null)
	{
		return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(callerFilePath), "..", "..", "..", "Cornerstone.Generators", "TestRunner.cs"));
	}

	#endregion
}