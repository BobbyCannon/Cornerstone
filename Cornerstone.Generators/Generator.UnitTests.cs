#region References

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Cornerstone.Generators.Models;
using Microsoft.CodeAnalysis;

#endregion

namespace Cornerstone.Generators;

public partial class Generator
{
	#region Constants

	public const string MSTestTestCleanupAttributeFullName = "Microsoft.VisualStudio.TestTools.UnitTesting.TestCleanupAttribute";
	public const string MSTestTestInitializeAttributeFullName = "Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute";
	public const string MSTestTestMethodAttributeFullName = "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute";
	public const string NUnitSetupAttributeFullName = "NUnit.Framework.SetUpAttribute";
	public const string NUnitTearDownAttributeFullName = "NUnit.Framework.TearDownAttribute";
	public const string NUnitTestAttributeFullName = "NUnit.Framework.TestAttribute";

	#endregion

	#region Methods

	private SourceMemberInfo FindTestInitializeMethod(SourceTypeInfo type)
	{
		var current = type;

		while (current != null)
		{
			var candidate = current
				.Methods.FirstOrDefault(x =>
					x.Attributes.Any(a =>
						a.FullyQualifiedName
							is MSTestTestInitializeAttributeFullName
							or NUnitSetupAttributeFullName
					)
				);

			if (candidate != null)
			{
				return candidate;
			}

			current = _typesLookup.TryGetValue(current.BaseFullyGlobalQualifiedTypeName, out var value) ? value : null;
		}

		return null;
	}

	private void GenerateUnitTestMain(
		SourceProductionContext spc,
		Compilation compilation,
		ImmutableArray<SourceTypeInfo> typesToProcess)
	{
		var hasValidReferences = compilation
			.ReferencedAssemblyNames.Any(a =>
				string.Equals(a.Name, "Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(a.Name, "MSTest.TestFramework", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(a.Name, "NUnit.Framework", StringComparison.OrdinalIgnoreCase)
			);

		var isExecutable = compilation.Options.OutputKind
			is OutputKind.ConsoleApplication
			or OutputKind.WindowsApplication
			or OutputKind.WindowsRuntimeApplication;

		if (!hasValidReferences || !isExecutable)
		{
			return;
		}

		var testClasses = typesToProcess.Where(x => x.Methods.Any(m =>
				m.Attributes.Any(a => a.FullyQualifiedName
					is MSTestTestMethodAttributeFullName
					or NUnitTestAttributeFullName
				)
			)
		).OrderBy(x => x.Name).ToArray();

		if (testClasses.Length <= 0)
		{
			return;
		}

		var testRunnerFile = GetEmbeddedText("TestRunner.cs");
		var builder = new CSharpCodeBuilder();

		builder.WriteLine(testRunnerFile);
		builder.WriteLine();

		builder.IndentWriteLine("class Program");
		builder.IndentWriteLine("{");
		builder.IncreaseIndent();
		builder.IndentWriteLine("public static int Main(string[] args)");
		builder.IndentWriteLine("{");
		builder.IncreaseIndent();
		builder.IndentWriteLine("var runner = new TestRunner(args);");

		foreach (var testClass in testClasses)
		{
			builder.IndentWriteLine("runner.AddTest(");
			builder.IncreaseIndent();
			builder.IndentWriteLine($"new {nameof(TestClassInfo)} {{");
			builder.IncreaseIndent();
			builder.WriteAssignment(nameof(TestClassInfo.ClassName), testClass.Name);
			builder.IndentWriteLine($"{nameof(TestClassInfo.ConstructorInfo)} = typeof({testClass.FullyGlobalQualifiedName}).GetConstructor([]),");

			var initializeMethod = FindTestInitializeMethod(testClass);

			// todo: support base implementations?
			builder.IndentWrite($"{nameof(TestClassInfo.InitializeMethod)} = ");
			if (initializeMethod != null)
			{
				builder.WriteLine($"new {nameof(TestMethodInfo)} {{");
				builder.IncreaseIndent();
				builder.WriteAssignment(nameof(TestMethodInfo.Name), initializeMethod.Name);
				builder.IndentWriteLine($"{nameof(TestMethodInfo.MethodInfo)} = typeof({testClass.FullyGlobalQualifiedName}).GetMethod(\"{initializeMethod.Name}\"),");
				builder.DecreaseIndent();
				builder.IndentWriteLine("},");
			}
			else
			{
				builder.WriteLine("null,");
			}

			var cleanupMethod = testClass.Methods.FirstOrDefault(x => x.Attributes.Any(a =>
				a.FullyQualifiedName
					is MSTestTestCleanupAttributeFullName
					or NUnitTearDownAttributeFullName
			));

			builder.IndentWrite($"{nameof(TestClassInfo.CleanupMethod)} = ");
			if (cleanupMethod != null)
			{
				builder.WriteLine($"new {nameof(TestMethodInfo)} {{");
				builder.IncreaseIndent();
				builder.WriteAssignment(nameof(TestMethodInfo.Name), cleanupMethod.Name);
				builder.IndentWriteLine($"{nameof(TestMethodInfo.MethodInfo)} = typeof({testClass.FullyGlobalQualifiedName}).GetMethod(\"{cleanupMethod.Name}\"),");
				builder.DecreaseIndent();
				builder.IndentWriteLine("},");
			}
			else
			{
				builder.WriteLine("null,");
			}

			builder.WriteArray($"{nameof(TestClassInfo.TestMethods)} =", () =>
			{
				var first = true;
				var orderedMethods = testClass.Methods.OrderBy(x => x.Name);
				foreach (var method in orderedMethods)
				{
					if (!method.Attributes.Any(a =>
							a.FullyQualifiedName
								is MSTestTestMethodAttributeFullName
								or NUnitTestAttributeFullName
						))
					{
						continue;
					}

					if (!first)
					{
						builder.WriteLine(",");
					}

					builder.IndentWriteLine($"new {nameof(TestMethodInfo)} {{");
					builder.IncreaseIndent();
					builder.WriteAssignment(nameof(TestMethodInfo.Name), method.Name);
					builder.IndentWriteLine($"{nameof(TestMethodInfo.MethodInfo)} = typeof({testClass.FullyGlobalQualifiedName}).GetMethod(\"{method.Name}\"),");
					builder.DecreaseIndent();
					builder.IndentWrite("}");
					first = false;
				}

				builder.WriteLine();
			});

			builder.DecreaseIndent();
			builder.IndentWriteLine("}");
			builder.DecreaseIndent();
			builder.IndentWriteLine(");");
		}

		builder.IndentWriteLine("runner.Process();");
		builder.IndentWriteLine("return 0;");
		builder.DecreaseIndent();
		builder.IndentWriteLine("}");
		builder.DecreaseIndent();
		builder.IndentWriteLine("}");

		var source = builder.ToString();
		Trace.WriteLine(source);
		spc.AddSource("__Cornerstone.GeneratedTests.g.cs", source);
	}

	private static string GetEmbeddedText(string simpleFileName)
	{
		var assembly = Assembly.GetExecutingAssembly();
		var resourceName = assembly.GetName().Name + "." + simpleFileName.Replace('/', '.');
		using var stream = assembly.GetManifestResourceStream(resourceName)
			?? throw new Exception($"Embedded resource not found: {resourceName}");
		using var reader = new StreamReader(stream, Encoding.UTF8);
		return reader.ReadToEnd();
	}

	#endregion
}