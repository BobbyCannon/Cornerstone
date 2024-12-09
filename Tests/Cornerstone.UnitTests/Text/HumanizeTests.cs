#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Convert;
using Cornerstone.Data.Times;
using Cornerstone.Generators.CodeGenerators;
using Cornerstone.Testing;
using Cornerstone.Text;
using Cornerstone.Text.Human;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Text;

[TestClass]
public class HumanizeTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Basic()
	{
		var value = TimeSpan.FromDays(512);
		var options = new HumanizeSettings { MinUnit = TimeUnit.Year, MaxUnit = TimeUnit.Year };
		AreEqual("1 Year", value.Humanize(options));
		options = new HumanizeSettings { MinUnit = TimeUnit.Day, MaxUnit = TimeUnit.Day };
		AreEqual("512 Days", value.Humanize(options));
	}

	[TestMethod]
	public void Precision()
	{
		var scenarios = new (string expected, TimeSpan value, HumanizeSettings settings)[]
		{
			("1.98 Minutes", TimeSpan.FromMinutes(1.98), new HumanizeSettings { MinUnit = TimeUnit.Minute, MaxUnit = TimeUnit.Minute, Precision = 2 }),
			("2 Minutes", TimeSpan.FromMinutes(1.98), new HumanizeSettings { MinUnit = TimeUnit.Minute, MaxUnit = TimeUnit.Minute, Precision = 1 }),
			("1 Minutes", TimeSpan.FromMinutes(1.23), new HumanizeSettings { MinUnit = TimeUnit.Minute, MaxUnit = TimeUnit.Minute, Precision = 0 }),
			("1 Minute", TimeSpan.FromMinutes(0.99), new HumanizeSettings { MinUnit = TimeUnit.Minute, MaxUnit = TimeUnit.Minute, Precision = 0 }),
		};

		foreach (var scenario in scenarios)
		{
			scenario.expected.Dump();
			var actual = scenario.value.Humanize(scenario.settings);
			AreEqual(scenario.expected, actual);
		}
	}

	[TestMethod]
	public void GenerateScenarios()
	{
		GenerateNewScenarios(EnableFileUpdates || IsDebugging);
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

	private void GenerateNewScenarios(bool enableTestScenarioCreation)
	{
		if (GetRuntimeInformation().DotNetRuntimeVersion.Major <= 4)
		{
			// Do NOT generate scenarios on .NET48
			"We do not generate scenarios on .NET48".Dump();
			return;
		}

		if (!enableTestScenarioCreation)
		{
			"Not generating scenarios due to not being enabled".Dump();
			return;
		}

		// \Text\HumanizeTests.cs
		var filePath = $@"{UnitTestsDirectory}\Text\{nameof(HumanizeTests)}.cs";
		var builder = new TextBuilder();
		var scenarioIndex = 0;
		var combinations = new HumanizeSettings[]
		{
			new() { MaxUnit = TimeUnit.Max, MinUnit = TimeUnit.Min, Precision = 3, WordFormat = WordFormat.Full },
			new() { MaxUnit = TimeUnit.Max, MinUnit = TimeUnit.Min, Precision = 3, WordFormat = WordFormat.Abbreviation },
			new() { MaxUnit = TimeUnit.Max, MinUnit = TimeUnit.Min, Precision = 7, WordFormat = WordFormat.Full },
			new() { MaxUnit = TimeUnit.Max, MinUnit = TimeUnit.Min, Precision = 7, WordFormat = WordFormat.Abbreviation },
			new() { MaxUnit = TimeUnit.Hour, MinUnit = TimeUnit.Min, Precision = 7, WordFormat = WordFormat.Full },
			new() { MaxUnit = TimeUnit.Hour, MinUnit = TimeUnit.Min, Precision = 7, WordFormat = WordFormat.Abbreviation }
		};
		var settingsType = typeof(HumanizeSettings);
		var timeSpan = new TimeSpan(366, 12, 35, 59, 987);
		var codeSettings = new CodeWriterSettings
		{
			TextFormat = TextFormat.Spaced,
			OutputMode = CodeWriterMode.Instance,
			EnumFormat = EnumFormat.Name,
			NamingConvention = NamingConvention.PascalCase
		};

		foreach (var settings in combinations)
		{
			// public TestScenario(string name, object from, Type fromType, object to, Type toType)
			var settingsCode = CSharpCodeWriter.GenerateCode(settings, codeSettings);
			var line = string.Format(
				"new(\r\n\t\"{0}: {1}\",\r\n\t{2},\r\n\ttypeof({1}),\r\n\t\"{3}\",\r\n\ttypeof(string)\r\n),",
				scenarioIndex++,
				CSharpCodeWriter.GetCodeTypeName(settingsType),
				settingsCode,
				timeSpan.Humanize(settings)
			);

			builder.AppendLine(line);
		}

		UpdateFileIfNecessary(filePath, builder);
	}

	[SuppressMessage("ReSharper", "RedundantCast")]
	private TestScenario[] GetTestScenarios()
	{
		var response = new TestScenario[]
		{
			// <Scenarios>
			new(
				"0: HumanizeOptions",
				new HumanizeSettings { MaxUnit = TimeUnit.Year, MaxUnitSegments = int.MaxValue, MinUnit = TimeUnit.Min, Precision = 3, WordFormat = WordFormat.Full },
				typeof(HumanizeSettings),
				"1 Year, 1 Day, 12 Hours, 35 Minutes, 59 Seconds, and 987 Milliseconds",
				typeof(string)
			),
			new(
				"1: HumanizeOptions",
				new HumanizeSettings { MaxUnit = TimeUnit.Year, MaxUnitSegments = int.MaxValue, MinUnit = TimeUnit.Min, Precision = 3, WordFormat = WordFormat.Abbreviation },
				typeof(HumanizeSettings),
				"1 yr 1 d 12 h 35 m 59 s 987 ms",
				typeof(string)
			),
			new(
				"2: HumanizeOptions",
				new HumanizeSettings { MaxUnit = TimeUnit.Year, MaxUnitSegments = int.MaxValue, MinUnit = TimeUnit.Min, Precision = 7, WordFormat = WordFormat.Full },
				typeof(HumanizeSettings),
				"1 Year, 1 Day, 12 Hours, 35 Minutes, 59 Seconds, and 987 Milliseconds",
				typeof(string)
			),
			new(
				"3: HumanizeOptions",
				new HumanizeSettings { MaxUnit = TimeUnit.Year, MaxUnitSegments = int.MaxValue, MinUnit = TimeUnit.Min, Precision = 7, WordFormat = WordFormat.Abbreviation },
				typeof(HumanizeSettings),
				"1 yr 1 d 12 h 35 m 59 s 987 ms",
				typeof(string)
			),
			new(
				"4: HumanizeOptions",
				new HumanizeSettings { MaxUnit = TimeUnit.Hour, MaxUnitSegments = int.MaxValue, MinUnit = TimeUnit.Min, Precision = 7, WordFormat = WordFormat.Full },
				typeof(HumanizeSettings),
				"12 Hours, 35 Minutes, 59 Seconds, and 987 Milliseconds",
				typeof(string)
			),
			new(
				"5: HumanizeOptions",
				new HumanizeSettings { MaxUnit = TimeUnit.Hour, MaxUnitSegments = int.MaxValue, MinUnit = TimeUnit.Min, Precision = 7, WordFormat = WordFormat.Abbreviation },
				typeof(HumanizeSettings),
				"12 h 35 m 59 s 987 ms",
				typeof(string)
			),
			// </Scenarios>
		};

		return response;
	}

	private void TestScenarios(params TestScenario[] scenarios)
	{
		var timeSpan = new TimeSpan(366, 12, 35, 59, 987);

		foreach (var scenario in scenarios)
		{
			scenario.Name.Dump();
			var settings = (HumanizeSettings) scenario.From.Value;
			var actual = timeSpan.Humanize(settings);
			AreEqual(scenario.To.Value, actual);
		}
	}

	#endregion
}