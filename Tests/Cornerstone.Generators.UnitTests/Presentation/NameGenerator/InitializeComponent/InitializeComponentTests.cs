#nullable enable
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Generators.Presentation.Common;
using Cornerstone.Generators.Presentation.Common.Domain;
using Cornerstone.Generators.Presentation.Compiler;
using Cornerstone.Generators.Presentation.NameGenerator;
using Cornerstone.Generators.UnitTests.Presentation.NameGenerator.InitializeComponent.GeneratedInitializeComponent;
using Cornerstone.Generators.UnitTests.Presentation.NameGenerator.Views;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cornerstone.Generators.UnitTests.Presentation.NameGenerator.InitializeComponent;

[TestClass]
public class InitializeComponentTests
{
    [TestMethod]
    [DataRow(InitializeComponentCode.NamedControl, View.NamedControl, false)]
    [DataRow(InitializeComponentCode.NamedControls, View.NamedControls, false)]
    [DataRow(InitializeComponentCode.XNamedControl, View.XNamedControl, false)]
    [DataRow(InitializeComponentCode.XNamedControls, View.XNamedControls, false)]
    [DataRow(InitializeComponentCode.NoNamedControls, View.NoNamedControls, false)]
    [DataRow(InitializeComponentCode.CustomControls, View.CustomControls, false)]
    [DataRow(InitializeComponentCode.DataTemplates, View.DataTemplates, false)]
    [DataRow(InitializeComponentCode.SignUpView, View.SignUpView, false)]
    [DataRow(InitializeComponentCode.FieldModifier, View.FieldModifier, false)]
    [DataRow(InitializeComponentCode.AttachedPropsWithDevTools, View.AttachedProps, true)]
    [DataRow(InitializeComponentCode.AttachedProps, View.AttachedProps, false)]
    [DataRow(InitializeComponentCode.ControlWithoutWindow, View.ControlWithoutWindow, false)]
    public async Task ShouldGenerateFindControlRefsFromMarkupFile(
        string expectation,
        string markup,
        bool devToolsMode)
    {
        var excluded = devToolsMode ? null : "Cornerstone.Presentation.Diagnostics";

        // Step 1: parse XAML as xml nodes, without any type information.
        var classResolver = new XamlXViewResolver(MiniCompiler.CreateNoop());

        var xaml = await View.Load(markup);
        var classInfo = classResolver.ResolveView(xaml, CancellationToken.None);
        Assert.IsNotNull(classInfo);
        var nameResolver = new XamlXNameResolver();
        var names = nameResolver.ResolveXmlNames(classInfo.Xaml, CancellationToken.None);

        // Step 2: use compilation context to resolve types
        var compilation =
            View.CreatePresentationCompilation(excluded)
                .WithCustomTextBox();
        var resolvedNames = names.ResolveNames(compilation, nameResolver).ToArray();

        // Step 3: run generator
        var generator = new InitializeComponentCodeGenerator(devToolsMode);
        var generatorVersion = typeof(InitializeComponentCodeGenerator).Assembly.GetName().Version?.ToString();

        var code = generator
            .GenerateCode("SampleView", "Sample.App",  resolvedNames)
            .Replace("\r", string.Empty);

        var expected = (await InitializeComponentCode.Load(expectation))
            .Replace("\r", string.Empty)
            .Replace("$GeneratorVersion", generatorVersion);
            
        CSharpSyntaxTree.ParseText(code);
        Assert.AreEqual(expected, code);
    }
}
