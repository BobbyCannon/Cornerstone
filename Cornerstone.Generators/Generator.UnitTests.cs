#region References

using System;
using System.Collections.Generic;
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

	public const string MsTestTestAssemblyInitializeAttributeFullName = "Microsoft.VisualStudio.TestTools.UnitTesting.AssemblyInitializeAttribute";
	public const string MsTestTestClassAttributeFullName = "Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute";
	public const string MsTestTestClassCleanupAttributeFullName = "Microsoft.VisualStudio.TestTools.UnitTesting.ClassCleanupAttribute";
	public const string MsTestTestClassInitializeAttributeFullName = "Microsoft.VisualStudio.TestTools.UnitTesting.ClassInitializeAttribute";
	public const string MsTestTestCleanupAttributeFullName = "Microsoft.VisualStudio.TestTools.UnitTesting.TestCleanupAttribute";
	public const string MsTestTestInitializeAttributeFullName = "Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute";
	public const string MsTestTestMethodAttributeFullName = "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute";
	public const string SkipInAotAttributeAttributeFullName = "Cornerstone.Testing.SkipInAotAttribute";

	#endregion

	#region Methods

	private static bool IsMsTestMethodAttribute(SourceAttributeInfo attribute)
	{
		if (attribute.FullyQualifiedName is MsTestTestMethodAttributeFullName)
		{
			return true;
		}

		var type = attribute.TypeSymbol;
		while (type != null)
		{
			if (type.ToDisplayString() == MsTestTestMethodAttributeFullName)
			{
				return true;
			}

			type = type.BaseType;
		}

		return false;
	}

	private static List<SourceMethodInfo> CollectTestMethods(SourceTypeInfo type)
	{
		var methods = new List<SourceMethodInfo>();

		for (var symbol = type.TypeSymbol; symbol is { SpecialType: not SpecialType.System_Object }; symbol = symbol.BaseType)
		{
			foreach (var method in ProcessTypeSymbol(symbol).Methods)
			{
				if (!method.Attributes.Any(IsMsTestMethodAttribute))
				{
					continue;
				}

				if (methods.Any(x => x.Name == method.Name))
				{
					continue;
				}

				methods.Add(method);
			}
		}

		return methods;
	}

	private static SourceAttributeInfo FindSkipInAotAttribute(SourceTypeInfo testClass)
	{
		for (var symbol = testClass.TypeSymbol; symbol is { SpecialType: not SpecialType.System_Object }; symbol = symbol.BaseType)
		{
			var attribute = ProcessTypeSymbol(symbol).Attributes
				.FirstOrDefault(a => a.FullyQualifiedName == SkipInAotAttributeAttributeFullName);
			if (attribute != null)
			{
				return attribute;
			}
		}

		return null;
	}

	private static SourceMethodInfo FindTestMethodUsingAttribute(
		SourceTypeInfo type,
		string attributeFullName,
		Dictionary<string, SourceTypeInfo> typesLookup)
	{
		var current = type;

		while (current != null)
		{
			var candidate = current.Methods.FirstOrDefault(x =>
				x.Attributes.Any(a => a.FullyQualifiedName == attributeFullName)
			);

			if (candidate != null)
			{
				return candidate;
			}

			current = typesLookup.TryGetValue(current.BaseFullyGlobalQualifiedTypeName, out var value) ? value : null;
		}

		return null;
	}

	private void GenerateUnitTestMain(
		SourceProductionContext spc,
		Compilation compilation,
		ImmutableArray<SourceTypeInfo> typesToProcess,
		Dictionary<string, SourceTypeInfo> typesLookup)
	{
		var hasValidReferences = compilation
			.ReferencedAssemblyNames.Any(a =>
				string.Equals(a.Name, "Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(a.Name, "MSTest.TestFramework", StringComparison.OrdinalIgnoreCase)
			);

		var isExecutable = compilation.Options.OutputKind
			is OutputKind.ConsoleApplication
			or OutputKind.WindowsApplication
			or OutputKind.WindowsRuntimeApplication;

		if (!hasValidReferences || !isExecutable)
		{
			return;
		}

		var testClasses = typesToProcess
			.Where(x => !x.IsAbstract)
			.Where(x => x.Attributes.Any(a => a.FullyQualifiedName is MsTestTestClassAttributeFullName))
			.Where(x => CollectTestMethods(x).Count > 0)
			.OrderBy(x => x.Name)
			.ToArray();

		if (testClasses.Length <= 0)
		{
			return;
		}

		foreach (var type in typesToProcess)
		{
			if (type.IsAbstract)
			{
				continue;
			}

			var isMsTest = CollectTestMethods(type).Count > 0;
			var hasTestClassAttribute = type.Attributes.Any(a => a.FullyQualifiedName is MsTestTestClassAttributeFullName);
			if (isMsTest && !hasTestClassAttribute)
			{
				DiagnosticReporter.ReportMissingTestClassAttribute(type.TypeSymbol);
			}
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
			var aotSkipClassAttribute = FindSkipInAotAttribute(testClass);

			builder.IndentWriteLine("runner.AddTest(");
			builder.IncreaseIndent();
			builder.IndentWriteLine($"new {nameof(TestClassInfo)} {{");
			builder.IncreaseIndent();
			builder.WriteAssignment(nameof(TestClassInfo.ClassName), testClass.Name);
			builder.IndentWriteLine($"{nameof(TestClassInfo.ConstructorInfo)} = typeof({testClass.FullyGlobalQualifiedName}).GetConstructor([]),");
			builder.WriteAssignment(nameof(TestClassInfo.SkipInAot), aotSkipClassAttribute != null);
			builder.WriteAssignment(nameof(TestClassInfo.SkipInAotReason), aotSkipClassAttribute?.ConstructorArguments.FirstOrDefault()?.ToString() ?? "Not compatible with AOT");

			var initializeMethod = FindTestMethodUsingAttribute(testClass, MsTestTestInitializeAttributeFullName, typesLookup);

			// todo: support base implementations?
			builder.IndentWrite($"{nameof(TestClassInfo.InitializeMethod)} = ");
			if (initializeMethod != null)
			{
				builder.WriteLine($"new {nameof(TestMethodInfo)} {{");
				builder.IncreaseIndent();
				builder.WriteAssignment(nameof(TestMethodInfo.Name), initializeMethod.Name);
				builder.IndentWriteLine($"{nameof(TestMethodInfo.MethodInfo)} = {nameof(TestRunner)}.{nameof(TestRunner.GetTestMethod)}(typeof({testClass.FullyGlobalQualifiedName}), \"{initializeMethod.Name}\"),");
				builder.DecreaseIndent();
				builder.IndentWriteLine("},");
			}
			else
			{
				builder.WriteLine("null,");
			}

			var cleanupMethod = testClass.Methods.FirstOrDefault(x =>
				x.Attributes.Any(a => a.FullyQualifiedName is MsTestTestCleanupAttributeFullName)
			);

			builder.IndentWrite($"{nameof(TestClassInfo.CleanupMethod)} = ");
			if (cleanupMethod != null)
			{
				builder.WriteLine($"new {nameof(TestMethodInfo)} {{");
				builder.IncreaseIndent();
				builder.WriteAssignment(nameof(TestMethodInfo.Name), cleanupMethod.Name);
				builder.IndentWriteLine($"{nameof(TestMethodInfo.MethodInfo)} = {nameof(TestRunner)}.{nameof(TestRunner.GetTestMethod)}(typeof({testClass.FullyGlobalQualifiedName}), \"{cleanupMethod.Name}\"),");
				builder.DecreaseIndent();
				builder.IndentWriteLine("},");
			}
			else
			{
				builder.WriteLine("null,");
			}

			var classInitializeMethod = FindTestMethodUsingAttribute(testClass, MsTestTestClassInitializeAttributeFullName, typesLookup);
			builder.IndentWrite($"{nameof(TestClassInfo.ClassInitializeMethod)} = ");
			if (classInitializeMethod != null)
			{
				builder.WriteLine($"new {nameof(TestMethodInfo)} {{");
				builder.IncreaseIndent();
				builder.WriteAssignment(nameof(TestMethodInfo.Name), classInitializeMethod.Name);
				builder.IndentWriteLine($"{nameof(TestMethodInfo.MethodInfo)} = {nameof(TestRunner)}.{nameof(TestRunner.GetTestMethod)}(typeof({testClass.FullyGlobalQualifiedName}), \"{classInitializeMethod.Name}\"),");
				builder.DecreaseIndent();
				builder.IndentWriteLine("},");
			}
			else
			{
				builder.WriteLine("null,");
			}

			var classCleanupMethod = FindTestMethodUsingAttribute(testClass, MsTestTestClassCleanupAttributeFullName, typesLookup);
			builder.IndentWrite($"{nameof(TestClassInfo.ClassCleanupMethod)} = ");
			if (classCleanupMethod != null)
			{
				builder.WriteLine($"new {nameof(TestMethodInfo)} {{");
				builder.IncreaseIndent();
				builder.WriteAssignment(nameof(TestMethodInfo.Name), classCleanupMethod.Name);
				builder.IndentWriteLine($"{nameof(TestMethodInfo.MethodInfo)} = {nameof(TestRunner)}.{nameof(TestRunner.GetTestMethod)}(typeof({testClass.FullyGlobalQualifiedName}), \"{classCleanupMethod.Name}\"),");
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
				var orderedMethods = CollectTestMethods(testClass).OrderBy(x => x.Name);
				foreach (var method in orderedMethods)
				{

					var aotSkipMethodAttribute = method.Attributes.FirstOrDefault(a => a.FullyQualifiedName == SkipInAotAttributeAttributeFullName);

					if (!first)
					{
						builder.WriteLine(",");
					}

					builder.IndentWriteLine($"new {nameof(TestMethodInfo)} {{");
					builder.IncreaseIndent();
					builder.WriteAssignment(nameof(TestMethodInfo.Name), method.Name);
					builder.IndentWriteLine($"{nameof(TestMethodInfo.MethodInfo)} = {nameof(TestRunner)}.{nameof(TestRunner.GetTestMethod)}(typeof({testClass.FullyGlobalQualifiedName}), \"{method.Name}\"),");
					builder.WriteAssignment(nameof(TestMethodInfo.SkipInAot), aotSkipMethodAttribute != null);
					builder.WriteAssignment(nameof(TestMethodInfo.SkipInAotReason), aotSkipMethodAttribute?.ConstructorArguments.FirstOrDefault()?.ToString() ?? "Not compatible with AOT");
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