#nullable enable
using System;
using System.ComponentModel;
using Cornerstone.Generators.Presentation.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Cornerstone.Generators.UnitTests.Presentation.NameGenerator.Views;
using XamlX;
using XamlX.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cornerstone.Generators.UnitTests.Presentation.NameGenerator;

[TestClass]
public class MiniCompilerTests
{
    private const string PresentationXaml = "<TextBlock xmlns='clr-namespace:Cornerstone.Presentation.Controls;assembly=Cornerstone.Presentation' />";
    private const string MiniClass = "namespace Example { public class Valid { public int Foo() => 21; } }";
    private const string MiniValidXaml = "<Valid xmlns='clr-namespace:Example;assembly=Example' />";

    [TestMethod]
    public void ShouldResolveTypesFromSimpleValidXamlMarkup()
    {
        var xaml = XDocumentXamlParser.Parse(MiniValidXaml);
        var compilation = CreateBasicCompilation(MiniClass);
        MiniCompiler.CreateRoslyn(new RoslynTypeSystem(compilation)).Transform(xaml);

        Assert.IsNotNull(xaml.Root);
    }

    [TestMethod]
    public void ShouldResolveTypesFromSimplePresentationMarkup()
    {
        var xaml = XDocumentXamlParser.Parse(PresentationXaml);
        var compilation = View.CreatePresentationCompilation();
        MiniCompiler.CreateRoslyn(new RoslynTypeSystem(compilation)).Transform(xaml);

        Assert.IsNotNull(xaml.Root);
    }

    private static CSharpCompilation CreateBasicCompilation(string source) =>
        CSharpCompilation
            .Create("BasicLib", options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddReferences(MetadataReference.CreateFromFile(typeof(string).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(IServiceProvider).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(ITypeDescriptorContext).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(ISupportInitialize).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(TypeConverterAttribute).Assembly.Location))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(source));
}
