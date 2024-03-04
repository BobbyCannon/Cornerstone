#region References

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cornerstone.Extensions;
using Cornerstone.Generators.CodeGenerators;
using Cornerstone.Testing;
using Cornerstone.Text.Human;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Text;

[TestClass]
public class StringFormatterTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	[SuppressMessage("ReSharper", "StringLiteralTypo")]
	public void ToCamelCase()
	{
		var scenarios = new Dictionary<string, string>
		{
			{ "HelloWorld", "helloWorld" },
			{ "Hello-World", "helloWorld" },
			{ "hello-world", "helloWorld" },
			{ "foo%_$_#bar", "fooBar" },
			{ "foo_____bar", "fooBar" },
			{ "foo-_-_-bar", "fooBar" },
			{ "Hello_World", "helloWorld" },
			{ "hello_world", "helloWorld" },
			{ "Hello World", "helloWorld" },
			{ "hello world", "helloWorld" },
			{ "foo _ _ bar", "fooBar" },
			{ "helloworld", "helloworld" },
			{ "foo__bar", "fooBar" }
		};

		//CSharpCodeWriter.ToCodeString(GetTestScenarios().ToDictionary(x => x, StringFormatter.ToCamelCase)).Dump();

		var keys = scenarios.Keys.ToList();

		for (var i = 0; i < keys.Count; i++)
		{
			var key = keys[i];

			//$"{i} : {key} >> {scenarios[key]}".Dump();

			AreEqual(scenarios[key], key.ToCamelCase());
		}
	}

	[TestMethod]
	[SuppressMessage("ReSharper", "StringLiteralTypo")]
	public void ToPascalCase()
	{
		var scenarios = new Dictionary<string, string>
		{
			{ "HelloWorld", "HelloWorld" },
			{ "Hello-World", "HelloWorld" },
			{ "hello-world", "HelloWorld" },
			{ "foo%_$_#bar", "FooBar" },
			{ "foo_____bar", "FooBar" },
			{ "foo-_-_-bar", "FooBar" },
			{ "Hello_World", "HelloWorld" },
			{ "hello_world", "HelloWorld" },
			{ "Hello World", "HelloWorld" },
			{ "hello world", "HelloWorld" },
			{ "foo _ _ bar", "FooBar" },
			{ "helloworld", "Helloworld" },
			{ "foo__bar", "FooBar" }
		};

		//CSharpCodeWriter.GenerateCode(GetTestScenarios().ToDictionary(x => x, StringFormatter.ToPascalCase)).Dump();

		var keys = scenarios.Keys.ToList();

		for (var i = 0; i < keys.Count; i++)
		{
			var key = keys[i];

			//$"{i} : {key} >> {scenarios[key]}".Dump();

			AreEqual(scenarios[key], key.ToPascalCase());
		}
	}

	[TestMethod]
	public void ToSentenceCase()
	{
		var scenarios = new Dictionary<string, string>
		{
			{ "HelloWorld", "Hello World" },
			{
				"Hello-World",
				"Hello World"
			},
			{
				"hello-world",
				"Hello World"
			},
			{
				"foo%_$_#bar",
				"Foo Bar"
			},
			{
				"foo_____bar",
				"Foo Bar"
			},
			{
				"foo-_-_-bar",
				"Foo Bar"
			},
			{
				"Hello_World",
				"Hello World"
			},
			{
				"hello_world",
				"Hello World"
			},
			{
				"Hello World",
				"Hello World"
			},
			{
				"hello world",
				"Hello World"
			},
			{
				"foo _ _ bar",
				"Foo Bar"
			},
			{
				"helloworld",
				"Helloworld"
			},
			{
				"foo__bar",
				"Foo Bar"
			}
		};

		CSharpCodeWriter.GenerateCode(GetTestScenarios().ToDictionary(x => x, x => x.ToSentenceCase())).Dump();

		var keys = scenarios.Keys.ToList();

		for (var i = 0; i < keys.Count; i++)
		{
			var key = keys[i];

			//$"{i} : {key} >> {scenarios[key]}".Dump();

			AreEqual(scenarios[key], key.ToSentenceCase());
		}
	}

	protected string[] GetTestScenarios()
	{
		var core = new[]
		{
			"HelloWorld", "Hello-World", "hello-world",
			"foo%_$_#bar", "foo_____bar", "foo-_-_-bar"
		};

		return ArrayExtensions.CombineArrays(core,
				core.Select(x => x.Replace("-", "_")).ToArray(),
				core.Select(x => x.Replace("-", " ")).ToArray(),
				core.Select(x => x.Replace("-", "")).ToArray(),
				core.Select(x => x.Replace(" ", "  ")).ToArray(),
				core.Select(x => $" {x} ").ToArray()
			)
			.Distinct()
			.ToArray();
	}

	#endregion
}