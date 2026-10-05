#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Cornerstone.Presentation;
using Cornerstone.Generators.Presentation.PropertyGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cornerstone.Generators.UnitTests.Presentation.PropertyGenerator;

internal static class PropertyGeneratorTestHelper
{
    public static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.CSharp14);

    private static readonly Lazy<IReadOnlyList<MetadataReference>> s_references = new(() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Select(static MetadataReference (path) => MetadataReference.CreateFromFile(path))
        .Append(MetadataReference.CreateFromFile(typeof(PresentationObject).Assembly.Location))
        .ToList());

    public static CSharpCompilation CreateCompilation(
        string source,
        CSharpParseOptions? parseOptions = null,
        NullableContextOptions nullableContextOptions = NullableContextOptions.Enable) =>
        CSharpCompilation.Create(
            "PropertyGeneratorTests",
            [CSharpSyntaxTree.ParseText(source, parseOptions ?? ParseOptions)],
            s_references.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: nullableContextOptions));

    public static (GeneratorDriverRunResult Result, Compilation Output) RunGenerator(
        CSharpCompilation compilation,
        CSharpParseOptions? parseOptions = null)
    {
        var driver = CreateDriver(parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        return (driver.GetRunResult(), output);
    }

    public static GeneratorDriver CreateDriver(CSharpParseOptions? parseOptions = null) =>
        CSharpGeneratorDriver.Create(
            [new PresentationPropertyIncrementalGenerator().AsSourceGenerator()],
            parseOptions: parseOptions ?? ParseOptions,
            driverOptions: new GeneratorDriverOptions(
                IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));

    /// <summary>
    /// Runs the generator, asserts it reports no diagnostics and the updated compilation has no
    /// errors, then compares the generated source against GeneratedCode/{sampleName}.txt.
    /// </summary>
    public static void AssertGeneratedCode(
        string sampleName,
        [StringSyntax("csharp")] string source,
        string? expectedHintName = null,
        LanguageVersion languageVersion = LanguageVersion.CSharp14,
        NullableContextOptions nullableContextOptions = NullableContextOptions.Enable,
        [CallerFilePath] string callerFilePath = "")
    {
        source = """
                 using Cornerstone.Presentation;
                 using Cornerstone.Presentation.Data;

                 """ + source;
        var parseOptions = new CSharpParseOptions(languageVersion);
        var (result, output) = RunGenerator(CreateCompilation(source, parseOptions, nullableContextOptions), parseOptions);

        Assert.AreEqual(0, result.Diagnostics.Length);

        var errors = output.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Count > 0)
        {
            var generatedText = string.Join(
                "\n\n",
                result.GeneratedTrees.Select(static tree => $"// {tree.FilePath}\n{tree.GetText()}"));
            Assert.Fail(
                $"Generated compilation has errors:\n{string.Join("\n", errors)}\n\nGenerated source:\n{generatedText}");
        }

        SyntaxTree tree;
        if (expectedHintName is not null)
        {
            var matching = result.GeneratedTrees.Where(t => t.FilePath.Replace('\\', '/').EndsWith(expectedHintName, StringComparison.Ordinal)).ToList();
            Assert.AreEqual(1, matching.Count);
            tree = matching[0];
        }
        else
        {
            Assert.AreEqual(1, result.GeneratedTrees.Length);
            tree = result.GeneratedTrees[0];
        }

        var actual = Normalize(tree.GetText().ToString());
        var expectedPath = Path.Combine(Path.GetDirectoryName(callerFilePath)!, "GeneratedCode", sampleName + ".txt");

        if (!File.Exists(expectedPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(expectedPath)!);
            File.WriteAllText(expectedPath, actual);
            Assert.Fail($"Expected file did not exist and was created from actual output; review it: {expectedPath}");
        }

        var expected = Normalize(File.ReadAllText(expectedPath));
        if (expected != actual)
        {
            File.WriteAllText(expectedPath + ".received", actual);
            Assert.AreEqual(expected, actual);
        }
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n").TrimEnd('\n');
}
