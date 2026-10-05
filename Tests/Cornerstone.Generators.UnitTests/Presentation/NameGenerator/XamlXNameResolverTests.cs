#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Generators.Presentation.Common;
using Cornerstone.Generators.Presentation.Common.Domain;
using Cornerstone.Generators.Presentation.Compiler;
using Cornerstone.Generators.UnitTests.Presentation.NameGenerator.Views;
using Cornerstone.Presentation.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cornerstone.Generators.UnitTests.Presentation.NameGenerator;

[TestClass]
public class XamlXNameResolverTests
{
    [TestMethod]
    [DataRow(View.NamedControl)]
    [DataRow(View.XNamedControl)]
    [DataRow(View.AttachedProps)]
    public async Task ShouldResolveTypesFromMarkupFileWithNamedControl(string resource)
    {
        var xaml = await View.Load(resource);
        var controls = ResolveNames(xaml);

        Assert.AreEqual(1, controls.Count);
        var control = controls[0];
        Assert.AreEqual("UserNameTextBox", control.Name);
        Assert.Contains(typeof(TextBox).FullName!, control.TypeName);
    }

    [TestMethod]
    [DataRow(View.NamedControls)]
    [DataRow(View.XNamedControls)]
    public async Task ShouldResolveTypesFromMarkupFileWithNamedControls(string resource)
    {
        var xaml = await View.Load(resource);
        var controls = ResolveNames(xaml);

        Assert.IsTrue(controls.Count > 0);
        Assert.AreEqual(3, controls.Count);
        Assert.AreEqual("UserNameTextBox", controls[0].Name);
        Assert.AreEqual("PasswordTextBox", controls[1].Name);
        Assert.AreEqual("SignUpButton", controls[2].Name);
        Assert.Contains(typeof(TextBox).FullName!, controls[0].TypeName);
        Assert.Contains(typeof(TextBox).FullName!, controls[1].TypeName);
        Assert.Contains(typeof(Button).FullName!, controls[2].TypeName);
    }

    [TestMethod]
    public async Task ShouldResolveTypesFromMarkupFileWithCustomControls()
    {
        var xaml = await View.Load(View.CustomControls);
        var controls = ResolveNames(xaml);

        Assert.IsTrue(controls.Count > 0);
        Assert.AreEqual(3, controls.Count);
        Assert.AreEqual("ClrNamespaceColorPicker", controls[0].Name);
        Assert.AreEqual("UriColorPicker", controls[1].Name);
        Assert.AreEqual("UserNameTextBox", controls[2].Name);
        Assert.Contains(typeof(ColorPicker).FullName!, controls[0].TypeName);
        Assert.Contains(typeof(ColorPicker).FullName!, controls[1].TypeName);
        Assert.Contains("Controls.CustomTextBox", controls[2].TypeName);
    }

    [TestMethod]
    public async Task ShouldResolveTypesFromMarkupFileWhenTypesContainsGenericArguments()
    {
        var xaml = await View.Load(View.ViewWithGenericBaseView);
        var controls = ResolveNames(xaml);
        Assert.AreEqual(2, controls.Count);
        
        var currentControl = controls[0];
        Assert.AreEqual("Root", currentControl.Name);
        Assert.AreEqual("global::Sample.App.BaseView<global::System.String>", currentControl.TypeName);

        currentControl = controls[1];
        Assert.AreEqual("NotAsRootNode", currentControl.Name);
        Assert.Contains("Sample.App.BaseView", currentControl.TypeName);
        Assert.AreEqual("global::Sample.App.BaseView<global::System.Int32>", currentControl.TypeName);
    }

    [TestMethod]
    public async Task ShouldNotResolveNamedControlsFromMarkupFileWithoutNamedControls()
    {
        var xaml = await View.Load(View.NoNamedControls);
        var controls = ResolveNames(xaml);

        Assert.AreEqual(0, controls.Count);
    }

    [TestMethod]
    public async Task ShouldNotResolveElementsFromDataTemplates()
    {
        var xaml = await View.Load(View.DataTemplates);
        var controls = ResolveNames(xaml);

        Assert.IsTrue(controls.Count > 0);
        Assert.AreEqual(2, controls.Count);
        Assert.AreEqual("UserNameTextBox", controls[0].Name);
        Assert.AreEqual("NamedListBox", controls[1].Name);
        Assert.Contains(typeof(TextBox).FullName!, controls[0].TypeName);
        Assert.Contains(typeof(ListBox).FullName!, controls[1].TypeName);
    }

    [TestMethod]
    public async Task ShouldResolveNamesFromComplexViews()
    {
        var xaml = await View.Load(View.SignUpView);
        var controls = ResolveNames(xaml);

        Assert.IsTrue(controls.Count > 0);
        Assert.AreEqual(10, controls.Count);
        Assert.AreEqual("UserNameTextBox", controls[0].Name);
        Assert.AreEqual("UserNameValidation", controls[1].Name);
        Assert.AreEqual("PasswordTextBox", controls[2].Name);
        Assert.AreEqual("PasswordValidation", controls[3].Name);
        Assert.AreEqual("AwesomeListView", controls[4].Name);
        Assert.AreEqual("ConfirmPasswordTextBox", controls[5].Name);
        Assert.AreEqual("ConfirmPasswordValidation", controls[6].Name);
        Assert.AreEqual("SignUpButtonDescription", controls[7].Name);
        Assert.AreEqual("SignUpButton", controls[8].Name);
        Assert.AreEqual("CompoundValidation", controls[9].Name);
    }

    private static IReadOnlyList<ResolvedName> ResolveNames(string xaml)
    {
        var nameResolver = new XamlXNameResolver();

        // Step 1: parse XAML as xml nodes, without any type information.
        var classResolver = new XamlXViewResolver(MiniCompiler.CreateNoop());
        var classInfo = classResolver.ResolveView(xaml, CancellationToken.None);
        Assert.IsNotNull(classInfo);
        var names = nameResolver.ResolveXmlNames(classInfo.Xaml, CancellationToken.None);

        // Step 2: use compilation context to resolve types
        var compilation =
            View.CreatePresentationCompilation()
                .WithCustomTextBox()
                .WithBaseView();
        return names.ResolveNames(compilation, nameResolver).ToArray();
    }
}
