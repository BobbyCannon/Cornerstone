#nullable enable
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Generators.Presentation.Common;
using Cornerstone.Generators.Presentation.Common.Domain;
using Cornerstone.Generators.Presentation.Compiler;
using Cornerstone.Generators.Presentation.NameGenerator;
using Cornerstone.Generators.UnitTests.Presentation.NameGenerator.OnlyProperties.GeneratedCode;
using Cornerstone.Generators.UnitTests.Presentation.NameGenerator.Views;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cornerstone.Generators.UnitTests.Presentation.NameGenerator.OnlyProperties;

[TestClass]
public class OnlyPropertiesTests
{
    [TestMethod]
    [DataRow(OnlyPropertiesCode.NamedControl, View.NamedControl)]
    [DataRow(OnlyPropertiesCode.NamedControls, View.NamedControls)]
    [DataRow(OnlyPropertiesCode.XNamedControl, View.XNamedControl)]
    [DataRow(OnlyPropertiesCode.XNamedControls, View.XNamedControls)]
    [DataRow(OnlyPropertiesCode.NoNamedControls, View.NoNamedControls)]
    [DataRow(OnlyPropertiesCode.CustomControls, View.CustomControls)]
    [DataRow(OnlyPropertiesCode.DataTemplates, View.DataTemplates)]
    [DataRow(OnlyPropertiesCode.SignUpView, View.SignUpView)]
    [DataRow(OnlyPropertiesCode.AttachedProps, View.AttachedProps)]
    [DataRow(OnlyPropertiesCode.FieldModifier, View.FieldModifier)]
    [DataRow(OnlyPropertiesCode.ControlWithoutWindow, View.ControlWithoutWindow)]
    public async Task ShouldGenerateFindControlRefsFromMarkupFile(string expectation, string markup)
    {
        // Step 1: parse XAML as xml nodes, without any type information.
        var classResolver = new XamlXViewResolver(MiniCompiler.CreateNoop());

        var xaml = await View.Load(markup);
        var classInfo = classResolver.ResolveView(xaml, CancellationToken.None);
        Assert.IsNotNull(classInfo);
        var nameResolver = new XamlXNameResolver();
        var names = nameResolver.ResolveXmlNames(classInfo.Xaml, CancellationToken.None);

        // Step 2: use compilation context to resolve types
        var compilation =
            View.CreatePresentationCompilation()
                .WithCustomTextBox();
        var resolvedNames = names.ResolveNames(compilation, nameResolver).ToArray();

        // Step 3: run generator
        var generator = new OnlyPropertiesCodeGenerator();
        var generatorVersion = typeof(OnlyPropertiesCodeGenerator).Assembly.GetName().Version?.ToString();

        var code = generator
            .GenerateCode("SampleView", "Sample.App", resolvedNames)
            .Replace("\r", string.Empty);

        var expected = (await OnlyPropertiesCode.Load(expectation))
            .Replace("\r", string.Empty)
            .Replace("$GeneratorVersion", generatorVersion);

        CSharpSyntaxTree.ParseText(code);
        Assert.AreEqual(expected, code);
    }
}
