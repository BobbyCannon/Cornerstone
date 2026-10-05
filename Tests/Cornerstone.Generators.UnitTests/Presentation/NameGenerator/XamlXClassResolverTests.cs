#nullable enable
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Generators.Presentation.Common;
using Cornerstone.Generators.Presentation.Compiler;
using Cornerstone.Generators.UnitTests.Presentation.NameGenerator.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cornerstone.Generators.UnitTests.Presentation.NameGenerator;

[TestClass]
public class XamlXClassResolverTests
{
    [TestMethod]
    [DataRow("Sample.App", "NamedControl", View.NamedControl)]
    [DataRow("Sample.App", "AttachedProps", View.AttachedProps)]
    [DataRow("Sample.App", "CustomControls", View.CustomControls)]
    [DataRow("Sample.App", "DataTemplates", View.DataTemplates)]
    [DataRow("Sample.App", "FieldModifier", View.FieldModifier)]
    [DataRow("Sample.App", "NamedControls", View.NamedControls)]
    [DataRow("Sample.App", "NoNamedControls", View.NoNamedControls)]
    [DataRow("Sample.App", "SignUpView", View.SignUpView)]
    [DataRow("Sample.App", "xNamedControl", View.XNamedControl)]
    [DataRow("Sample.App", "xNamedControls", View.XNamedControls)]
    [DataRow("Sample.App", "ViewWithGenericBaseView", View.ViewWithGenericBaseView)]
    public async Task ShouldResolveBaseClassFromXamlFile(string nameSpace, string className, string markup)
    {
        var xaml = await View.Load(markup);
        var resolver = new XamlXViewResolver(MiniCompiler.CreateNoop());

        var resolvedClass = resolver.ResolveView(xaml, CancellationToken.None);
        Assert.IsNotNull(resolvedClass);
        Assert.AreEqual(className, resolvedClass.ClassName);
        Assert.AreEqual(nameSpace, resolvedClass.Namespace);
    }
}
