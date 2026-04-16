#region References

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
using Cornerstone.Compare;
using Cornerstone.Extensions;
using Cornerstone.Reflection;
using Cornerstone.Testing;
using Cornerstone.UnitTests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

#endregion

#pragma warning disable IL2026
#pragma warning disable IL3000

namespace Cornerstone.Generators.UnitTests;

public abstract class GeneratorUnitTest : CornerstoneUnitTest
{
	#region Fields

	private static readonly string[] _usingStatements;

	#endregion

	#region Constructors

	static GeneratorUnitTest()
	{
		_usingStatements =
		[
			"using Avalonia;",
			"using Avalonia.Data;",
			"using Cornerstone.Avalonia;",
			"using Cornerstone.Collections;",
			"using Cornerstone.Data;",
			"using Cornerstone.Presentation;",
			"using Cornerstone.Profiling;",
			"using Cornerstone.Reflection;",
			"using Cornerstone.Serialization;",
			"using Cornerstone.Storage.Sql;",
			"using Cornerstone.Sync;",
			"using System;",
			"using System.ComponentModel.DataAnnotations;",
			"using System.Linq;",
			"using System.Reflection;"
		];
	}

	#endregion

	#region Methods

	protected (string ConsoleOutput, TimeSpan Duration) ExecuteAndCaptureOutput(RunResults results, string entryTypeName, string entryMethodName, string namespaceName)
	{
		using var peStream = new MemoryStream();
		var emitResult = results
			.OutputCompilation.Emit(
				peStream,
				options: new EmitOptions(

					// You can set instrumentation/debug info if needed
					instrumentationKinds: [InstrumentationKind.None]
				)
			);

		if (!emitResult.Success)
		{
			var errors = string.Join("\n", emitResult.Diagnostics
				.Where(d => d.Severity == DiagnosticSeverity.Error)
				.Select(d => d.ToString()));

			Assert.Fail($"Emit failed:\n{errors}");
		}

		peStream.Position = 0;

		// Use collectible context so we can unload (good practice in tests)
		var loadContext = new AssemblyLoadContext("TestAssemblyContext", true);
		var assembly = loadContext.LoadFromStream(peStream);
		var type = assembly.GetType($"{namespaceName}.{entryTypeName}")
			?? throw new Exception($"Type not found: {namespaceName}.{entryTypeName}");
		var method = type.GetMethod(entryMethodName, BindingFlags.Public | BindingFlags.Static)
			?? throw new Exception($"Static method not found: {entryMethodName}");

		using var stringWriter = new StringWriter();
		var originalOut = Console.Out;
		Console.SetOut(stringWriter);

		var stopwatch = Stopwatch.StartNew();

		try
		{
			method.Invoke(null, null);
		}
		finally
		{
			stopwatch.Stop();
			Console.SetOut(originalOut);
		}

		var output = stringWriter.ToString().Trim();
		loadContext.Unload();

		return (output, stopwatch.Elapsed);
	}

	protected void ExpectErrors(
		string input, string[] expectedErrors,
		OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
		NullableContextOptions nullableContextOptions = NullableContextOptions.Disable,
		[CallerMemberName] string callingMethodName = "")
	{
		var result = Run(input, outputKind, nullableContextOptions);
		var diagnosticErrors = new List<string>();
		var diagnostics = result.RunResult.Diagnostics;
		diagnosticErrors.AddRange(diagnostics
			.Where(x => x.Severity == DiagnosticSeverity.Error)
			.Select(x => $"{x.Severity} {x.Id}: {x.GetMessage()}"));

		diagnostics = result.InputCompilation.GetDiagnostics();
		diagnosticErrors.AddRange(diagnostics
			.Where(x => x.Severity == DiagnosticSeverity.Error)
			.Select(x => $"{x.Severity} {x.Id}: {x.GetMessage()}"));

		diagnostics = result.OutputCompilation.GetDiagnostics();
		diagnosticErrors.AddRange(diagnostics
			.Where(x => x.Severity == DiagnosticSeverity.Error)
			.Select(x => $"{x.Severity} {x.Id}: {x.GetMessage()}"));

		var areEqual = Compare(expectedErrors, diagnosticErrors).Result == CompareResult.AreEqual;

		if (!string.IsNullOrWhiteSpace(SourceFileName)
			&& (EnableFileUpdates || IsDebugging)
			&& !areEqual)
		{
			var sourceFileInfo = new FileInfo(SourceFileName);
			UpdateFile(callingMethodName, sourceFileInfo,
				builder =>
				{
					builder.WriteLine();
					builder.IndentWriteLine("var expected = new[]");
					builder.IndentWriteLine("{");
					builder.IncreaseIndent();

					var first = true;
					foreach (var error in diagnosticErrors)
					{
						if (!first)
						{
							builder.WriteLine(",");
						}

						builder.IndentWrite("\"");
						builder.Write(error.Escape());
						builder.Write("\"");
						first = false;
					}

					builder.WriteLine();
					builder.DecreaseIndent();
					builder.IndentWriteLine("};");
				});
			Assert.Inconclusive("Test updated...");
		}

		if (!areEqual && (diagnosticErrors.Count > 0))
		{
			throw new CornerstoneException(string.Join("\r\n", diagnosticErrors));
		}

		if (!areEqual)
		{
			throw new CornerstoneException(Babel.Tower[BabelKeys.TestingShouldHaveFailed]);
		}
	}

	protected RunResults Run(string input,
		OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
		NullableContextOptions nullableContextOptions = NullableContextOptions.Disable)
	{
		var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
		var inputCompilation = CreateCompilation(input, parseOptions, outputKind, nullableContextOptions);
		var generator = new Generator();

		//var profilingGenerator = new ProfilingGenerator();

		// Example: enabling interceptors in specific namespaces
		var globalBuildProperties = new Dictionary<string, string>
		{
			// bug: this is not working...
			["InterceptorsNamespaces"] = "$(InterceptorsNamespaces);GeneratedInterceptors",
			["InterceptorsPreviewNamespaces"] = "$(InterceptorsPreviewNamespaces);GeneratedInterceptors"

			//["build_property.Nullable"] = "disable", // or "enable", "annotations", "warnings"
			//["build_property.GlobalUsings"] = "System;System.Collections.Generic;",
			//["build_property.MyCustomSetting"] = "SomeValue",       // your own props
		};

		var optionsProvider = new InMemoryAnalyzerConfigOptionsProvider(globalBuildProperties);

		GeneratorDriver driver = CSharpGeneratorDriver.Create(
			[
				generator.AsSourceGenerator()

				//profilingGenerator.AsSourceGenerator()
			],
			driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, true),
			optionsProvider: optionsProvider,
			parseOptions: parseOptions
		);

		driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);
		var runResult = driver.GetRunResult();

		return new RunResults(driver, inputCompilation, outputCompilation, diagnostics, runResult);
	}

	protected RunResults ValidateSource(
		string input, string[] expected,
		OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
		NullableContextOptions nullableContextOptions = NullableContextOptions.Disable,
		[CallerMemberName] string callingMethodName = "")
	{
		var result = Run(input, outputKind, nullableContextOptions);
		var sourceResults = new (string Actual, string Expected)[result.RunResult.GeneratedTrees.Length];

		for (var i = 0; i < result.RunResult.GeneratedTrees.Length; i++)
		{
			var actual = result.RunResult.GeneratedTrees[i].ToString().Trim();
			sourceResults[i] = new ValueTuple<string, string>(actual, i < expected.Length ? expected[i] : null);
		}

		var diagnostics = result.InputCompilation.GetDiagnostics();
		var diagnosticErrors = diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error).ToList();
		if (diagnosticErrors.Count > 0)
		{
			var source = string.Join(Environment.NewLine, sourceResults.Select(x => x.Actual));
			source.Dump();

			//var errors = string.Join(Environment.NewLine, diagnosticErrors.Select(x => x.GetMessage() + x.Location.GetLineSpan()));
			//Assert.Fail(errors);
		}

		diagnostics = result.OutputCompilation.GetDiagnostics();
		diagnosticErrors = diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error).ToList();
		if (diagnosticErrors.Count > 0)
		{
			var source = string.Join(Environment.NewLine, sourceResults.Select(x => x.Actual));
			source.Dump();

			var errors = string.Join(Environment.NewLine, diagnosticErrors.Select(x => x.GetMessage() + x.Location.GetLineSpan()));
			Assert.Fail(errors);
		}

		if (!string.IsNullOrWhiteSpace(SourceFileName)
			&& (EnableFileUpdates || IsDebugging)
			&& sourceResults.Any(r => !string.Equals(r.Expected, r.Actual)))
		{
			var sourceFileInfo = new FileInfo(SourceFileName);
			UpdateFile(callingMethodName, sourceFileInfo,
				builder =>
				{
					builder.WriteLine();
					builder.IndentWriteLine("var expected = new[]");
					builder.IndentWriteLine("{");
					builder.IncreaseIndent();

					var endIndex = sourceResults.Length - 1;

					for (var index = 0; index <= endIndex; index++)
					{
						var sourceResult = sourceResults[index];
						var rawStringDelimiter = CalculateRawStringLength(sourceResult.Actual);
						var lines = sourceResult.Actual.Split("\r\n");
						builder.IndentWriteLine(rawStringDelimiter);
						foreach (var line in lines)
						{
							builder.IndentWriteLine(line);
						}
						builder.IndentWrite(rawStringDelimiter);

						if (index < endIndex)
						{
							builder.WriteLine(",");
						}
					}

					builder.WriteLine();
					builder.DecreaseIndent();
					builder.IndentWriteLine("};");
				});
			Assert.Inconclusive("Test updated...");
		}

		var session = new CompareSession();
		foreach (var sourceResult in sourceResults)
		{
			if (!string.Equals(sourceResult.Expected, sourceResult.Actual))
			{
				session.AddDifference(sourceResult.Expected, sourceResult.Actual, true);
			}
		}

		return session.Differences.Length > 0
			? throw new CompareException(session.Differences.ToString())
			: result;
	}

	private string CalculateRawStringLength(string content)
	{
		var maxQuoteRun = 0;
		var currentRun = 0;

		foreach (var c in content)
		{
			if (c == '"')
			{
				currentRun++;
				if (currentRun > maxQuoteRun)
				{
					maxQuoteRun = currentRun;
				}
			}
			else
			{
				currentRun = 0;
			}
		}

		var delimiterLength = Math.Max(3, maxQuoteRun + 1);
		return new string('"', delimiterLength);
	}

	private Compilation CreateCompilation(string input,
		CSharpParseOptions parseOptions,
		OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
		NullableContextOptions nullableContextOptions = NullableContextOptions.Disable)
	{
		input = $"{string.Join("\r\n", _usingStatements)}\r\n{input}";

		var netCoreAppPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

		var references = new List<PortableExecutableReference>
		{
			MetadataReference.CreateFromFile(Path.Combine(netCoreAppPath, "System.Private.CoreLib.dll")),
			MetadataReference.CreateFromFile(Path.Combine(netCoreAppPath, "System.Console.dll")),
			MetadataReference.CreateFromFile(Path.Combine(netCoreAppPath, "WindowsBase.dll")),
			MetadataReference.CreateFromFile(Path.Combine(netCoreAppPath, "System.Runtime.dll")),
			MetadataReference.CreateFromFile(Path.Combine(netCoreAppPath, "System.Collections.dll")),
			MetadataReference.CreateFromFile(Path.Combine(netCoreAppPath, "System.ComponentModel.dll")),
			MetadataReference.CreateFromFile(Path.Combine(netCoreAppPath, "System.ComponentModel.Annotations.dll")),
			MetadataReference.CreateFromFile(Path.Combine(netCoreAppPath, "System.ComponentModel.DataAnnotations.dll")),
			MetadataReference.CreateFromFile(Path.Combine(netCoreAppPath, "System.Linq.dll")),
			MetadataReference.CreateFromFile(Path.Combine(netCoreAppPath, "System.ObjectModel.dll")),
			MetadataReference.CreateFromFile(Path.Combine(netCoreAppPath, "System.Private.Xml.dll")),
			MetadataReference.CreateFromFile(Path.Combine(netCoreAppPath, "System.Xml.dll")),
			MetadataReference.CreateFromFile(typeof(Control).Assembly.Location), // Avalonia.Controls
			MetadataReference.CreateFromFile(typeof(Interactive).Assembly.Location), // Avalonia.Base
			MetadataReference.CreateFromFile(typeof(TestClassAttribute).Assembly.Location), // MSTest.TestFramework

			// Add more assemblies
			MetadataReference.CreateFromFile(typeof(CornerstoneAttribute).Assembly.Location), // Cornerstone
			MetadataReference.CreateFromFile(typeof(CornerstoneApplication).Assembly.Location) // Cornerstone.Avalonia

			//MetadataReference.CreateFromFile(typeof(global::Avalonia.Application).Assembly.Location),
			//MetadataReference.CreateFromFile(typeof(global::Avalonia.AvaloniaObject).Assembly.Location)
		};

		//foreach (var r in references)
		//{
		//	Console.WriteLine(r.FilePath);
		//}

		var syntaxTrees = new List<SyntaxTree>
		{
			CSharpSyntaxTree.ParseText(input, parseOptions)
		};

		var compilation = CSharpCompilation.Create(
			"TestCompilation",
			syntaxTrees,
			references,
			new CSharpCompilationOptions(
				outputKind,
				nullableContextOptions: nullableContextOptions
			).WithMetadataImportOptions(MetadataImportOptions.All)
		);

		//if (!expectErrors)
		//{
		//	var diagnostics = compilation.GetDiagnostics();
		//	foreach (var error in diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
		//	{
		//		throw new Exception($"Compilation Error: {error.ToString()}");
		//	}
		//}

		return compilation;
	}

	#endregion

	#region Records

	public record RunResults(
		GeneratorDriver Driver,
		Compilation InputCompilation,
		Compilation OutputCompilation,
		ImmutableArray<Diagnostic> Diagnostics,
		GeneratorDriverRunResult RunResult
	);

	#endregion
}