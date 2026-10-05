#region References

using System.Reflection;
using Cornerstone.VisualStudio.Core;
using Cornerstone.VisualStudio.Core.AssemblyMetadata;
using Cornerstone.VisualStudio.Core.Completion;
using Cornerstone.VisualStudio.Core.DnlibMetadataProvider;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests;

[TestClass]
public class XamlGoToDefinitionResolverTests : XamlCompletionTestBase
{
	#region Fields

	private static readonly Core.AssemblyMetadata.Metadata SharedMetadata = new MetadataReader(new DnlibMetadataProvider())
		.GetForTargetAssembly(new FolderAssemblyProvider(typeof(XamlCompletionTestBase).Assembly.GetModules()[0].FullyQualifiedName));

	#endregion

	#region Methods

	[TestMethod]
	public void ResolvesAttachedPropertyOwnerTypeWhenCaretOnClass()
	{
		var xml = Wrap("<UserControl Grid.Row=\"1\" />");
		var caret = xml.IndexOf("Grid") + 2;
		var target = Resolve(xml, caret);
		Assert.AreEqual(XamlGoToDefinitionKind.Type, target.Kind);
		StringAssert.Contains(target.TypeFullName, "Grid");
	}

	[TestMethod]
	public void ResolvesAttachedPropertyWhenCaretOnMember()
	{
		var xml = Wrap("<UserControl Grid.Row=\"1\" />");
		var caret = xml.IndexOf("Row");
		var target = Resolve(xml, caret);
		Assert.AreEqual(XamlGoToDefinitionKind.Member, target.Kind);
		Assert.AreEqual("Row", target.MemberName);
		StringAssert.Contains(target.TypeFullName, "Grid");
	}

	[TestMethod]
	public void ResolvesElementType()
	{
		var xml = Wrap("<Button Content=\"Hi\" />");
		var caret = xml.IndexOf("Button") + 2;
		var target = Resolve(xml, caret);
		Assert.AreEqual(XamlGoToDefinitionKind.Type, target.Kind);
		StringAssert.Contains(target.TypeFullName, "Button");
	}

	[TestMethod]
	public void ResolvesEventHandlerMethod()
	{
		var xml = Wrap("<Button Click=\"OnClick\" />");
		var caret = xml.IndexOf("OnClick") + 1;
		var target = Resolve(xml, caret);
		Assert.AreEqual(XamlGoToDefinitionKind.MethodName, target.Kind);
		Assert.AreEqual("OnClick", target.MethodName);
	}

	[TestMethod]
	public void ResolvesMarkupExtensionType()
	{
		var xml = Wrap("<TextBlock Text=\"{Binding Name}\" />");
		var caret = xml.IndexOf("Binding") + 2;
		var target = Resolve(xml, caret);
		Assert.AreEqual(XamlGoToDefinitionKind.Type, target.Kind);
		StringAssert.Contains(target.TypeFullName, "Binding");
	}

	[TestMethod]
	public void ResolvesPropertyOnElement()
	{
		var xml = Wrap("<Button Content=\"Hi\" />");
		var caret = xml.IndexOf("Content") + 2;
		var target = Resolve(xml, caret);
		Assert.AreEqual(XamlGoToDefinitionKind.Member, target.Kind);
		Assert.AreEqual("Content", target.MemberName);
	}

	[TestMethod]
	public void ResolvesSelectorChildType()
	{
		var xml = Wrap("<Style Selector=\"Border.IconBadge > Path\" />");
		var caret = xml.IndexOf("Path") + 2;
		var target = Resolve(xml, caret);
		Assert.AreEqual(XamlGoToDefinitionKind.Type, target.Kind);
		StringAssert.Contains(target.TypeFullName, "Path");
	}

	[TestMethod]
	public void ResolvesSelectorProperty()
	{
		var xml = Wrap("<Style Selector=\"Button[Content=Hi]\" />");
		var caret = xml.IndexOf("Content") + 2;
		var target = Resolve(xml, caret);
		Assert.AreEqual(XamlGoToDefinitionKind.Member, target.Kind);
		Assert.AreEqual("Content", target.MemberName);
		StringAssert.Contains(target.TypeFullName, "Button");
	}

	[TestMethod]
	public void ResolvesSelectorStyleClassWhenOnlySelfSearchesElsewhere()
	{
		var xml = Wrap("<Style Selector=\"Button.ControlCard\" />");
		var caret = xml.IndexOf("ControlCard") + 2;
		var target = Resolve(xml, caret);
		Assert.AreEqual(XamlGoToDefinitionKind.StyleClass, target.Kind);
		Assert.AreEqual("ControlCard", target.ClassName);
		Assert.AreEqual(-1, target.DocumentOffset);
	}

	[TestMethod]
	public void ResolvesSelectorStyleClassFromClassesAttribute()
	{
		var xml = Wrap(
			"<Style Selector=\"Button.ControlCard\" />" +
			"<Button Classes=\"ControlCard\" />");
		var definition = xml.IndexOf("ControlCard");
		var caret = xml.LastIndexOf("ControlCard") + 2;
		var target = Resolve(xml, caret);
		Assert.AreEqual(XamlGoToDefinitionKind.StyleClass, target.Kind);
		Assert.AreEqual("ControlCard", target.ClassName);
		Assert.AreEqual(definition, target.DocumentOffset);
	}

	[TestMethod]
	public void ResolvesSelectorStyleClassToOtherSelector()
	{
		var xml = Wrap(
			"<Style Selector=\"Button.ControlCard\" />" +
			"<Style Selector=\"Button.ControlCard:pointerover\" />");
		var definition = xml.IndexOf("ControlCard");
		var caret = xml.LastIndexOf("ControlCard") + 2;
		var target = Resolve(xml, caret);
		Assert.AreEqual(XamlGoToDefinitionKind.StyleClass, target.Kind);
		Assert.AreEqual("ControlCard", target.ClassName);
		Assert.AreEqual(definition, target.DocumentOffset);
	}

	[TestMethod]
	public void ResolvesSelectorType()
	{
		var xml = Wrap("<Style Selector=\"Button.ControlCard\" />");
		var caret = xml.IndexOf("Button") + 2;
		var target = Resolve(xml, caret);
		Assert.AreEqual(XamlGoToDefinitionKind.Type, target.Kind);
		StringAssert.Contains(target.TypeFullName, "Button");
	}

	[TestMethod]
	public void ResolvesSelectorTypeWithoutMetadata()
	{
		var xml = Wrap("<Style Selector=\"Button.ControlCard\" />");
		var caret = xml.IndexOf("Button") + 2;
		var engine = new CompletionEngine();
		var target = XamlGoToDefinitionResolver.Resolve(engine, null, xml, caret, null);
		Assert.AreEqual(XamlGoToDefinitionKind.ClassName, target.Kind);
		Assert.AreEqual("Button", target.ClassName);
	}

	[TestMethod]
	public void ResolvesSimpleTypeNameWithoutMetadata()
	{
		var xml = Wrap("<local:PageNavigator />");
		var caret = xml.IndexOf("PageNavigator") + 4;
		var engine = new CompletionEngine();
		var target = XamlGoToDefinitionResolver.Resolve(engine, null, xml, caret, null);
		Assert.AreEqual(XamlGoToDefinitionKind.ClassName, target.Kind);
		Assert.AreEqual("PageNavigator", target.ClassName);
	}

	[TestMethod]
	public void ResolvesXamlClassAttribute()
	{
		var xml = Wrap("");
		xml = xml.Replace("<UserControl ", "<UserControl x:Class=\"CompletionEngineTests.Models.MyButton\" ");
		var caret = xml.IndexOf("MyButton") + 2;
		var target = Resolve(xml, caret);
		Assert.AreEqual(XamlGoToDefinitionKind.ClassName, target.Kind);
		Assert.AreEqual("CompletionEngineTests.Models.MyButton", target.ClassName);
	}

	[TestMethod]
	public void ResolvesBindingPathToDataTypeProperty()
	{
		var viewModel = new MetadataType("MainViewModel")
		{
			FullName = "CornerstoneApplication.ViewModels.MainViewModel"
		};
		viewModel.Properties.Add(new MetadataProperty("Greeting", null, viewModel, false, false, true, true));
		var metadata = new Core.AssemblyMetadata.Metadata();
		metadata.AddType("using:CornerstoneApplication.ViewModels", viewModel);
		var xml = @"<UserControl xmlns=""https://github.com/BobbyCannon/Cornerstone""
        xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
        xmlns:vm=""using:CornerstoneApplication.ViewModels""
        x:DataType=""vm:MainViewModel"">
        <TextBlock Text=""{Binding Greeting}"" />
        </UserControl>";
		var caret = xml.IndexOf("Greeting") + 4;
		var engine = new CompletionEngine();
		var target = XamlGoToDefinitionResolver.Resolve(engine, metadata, xml, caret, "CornerstoneApplication");
		Assert.AreEqual(XamlGoToDefinitionKind.Member, target.Kind);
		Assert.AreEqual("Greeting", target.MemberName);
		Assert.AreEqual("CornerstoneApplication.ViewModels.MainViewModel", target.TypeFullName);
	}

	[TestMethod]
	public void ResolvesEnumAttributeValue()
	{
		var alignment = new MetadataType("HorizontalAlignment")
		{
			FullName = "Cornerstone.Presentation.Layout.HorizontalAlignment",
			IsEnum = true,
			HasHintValues = true,
			HintValues = new[] { "Stretch", "Left", "Center", "Right" }
		};
		alignment.Properties.Add(new MetadataProperty("Center", alignment, alignment, false, true, true, false));
		var textBlock = new MetadataType("TextBlock")
		{
			FullName = "Cornerstone.Presentation.Controls.TextBlock"
		};
		textBlock.Properties.Add(new MetadataProperty("HorizontalAlignment", alignment, textBlock, false, false, true, true));
		var metadata = new Core.AssemblyMetadata.Metadata();
		metadata.AddType(Utils.AvaloniaNamespace, alignment);
		metadata.AddType(Utils.AvaloniaNamespace, textBlock);
		var xml = Wrap("<TextBlock HorizontalAlignment=\"Center\" />");
		var caret = xml.IndexOf("Center") + 2;
		var engine = new CompletionEngine();
		var target = XamlGoToDefinitionResolver.Resolve(engine, metadata, xml, caret, null);
		Assert.AreEqual(XamlGoToDefinitionKind.Member, target.Kind);
		Assert.AreEqual("Center", target.MemberName);
		Assert.AreEqual("Cornerstone.Presentation.Layout.HorizontalAlignment", target.TypeFullName);
	}

	[TestMethod]
	public void ResolvesBrushAttributeValue()
	{
		var brush = new MetadataType("IImmutableSolidColorBrush")
		{
			FullName = "Cornerstone.Presentation.Media.IImmutableSolidColorBrush"
		};
		var brushes = new MetadataType("Brushes")
		{
			FullName = "Cornerstone.Presentation.Media.Brushes",
			IsStatic = true,
			HasStaticGetProperties = true
		};
		brushes.Properties.Add(new MetadataProperty("Red", brush, brushes, false, true, true, false));
		var color = new MetadataType("Color")
		{
			FullName = "Cornerstone.Presentation.Media.Color"
		};
		var colors = new MetadataType("Colors")
		{
			FullName = "Cornerstone.Presentation.Media.Colors",
			IsStatic = true,
			HasStaticGetProperties = true
		};
		colors.Properties.Add(new MetadataProperty("Red", color, colors, false, true, true, false));
		var iface = new MetadataType("IBrush")
		{
			FullName = "Cornerstone.Presentation.Media.IBrush",
			HasHintValues = true,
			HintValues = new[] { "Red" }
		};
		var textBlock = new MetadataType("TextBlock")
		{
			FullName = "Cornerstone.Presentation.Controls.TextBlock"
		};
		textBlock.Properties.Add(new MetadataProperty("Foreground", iface, textBlock, false, false, true, true));
		var metadata = new Core.AssemblyMetadata.Metadata();
		metadata.AddType(Utils.AvaloniaNamespace, brush);
		metadata.AddType(Utils.AvaloniaNamespace, brushes);
		metadata.AddType(Utils.AvaloniaNamespace, color);
		metadata.AddType(Utils.AvaloniaNamespace, colors);
		metadata.AddType(Utils.AvaloniaNamespace, iface);
		metadata.AddType(Utils.AvaloniaNamespace, textBlock);
		var xml = Wrap("<TextBlock Foreground=\"Red\" />");
		var caret = xml.IndexOf("Red") + 1;
		var engine = new CompletionEngine();
		var target = XamlGoToDefinitionResolver.Resolve(engine, metadata, xml, caret, null);
		Assert.AreEqual(XamlGoToDefinitionKind.Member, target.Kind);
		Assert.AreEqual("Red", target.MemberName);
		Assert.AreEqual("Cornerstone.Presentation.Media.Brushes", target.TypeFullName);
	}

	private static string Wrap(string inner)
	{
		return @"<UserControl xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>".Replace("'", "\"") + inner + "</UserControl>";
	}

	private XamlGoToDefinitionTarget Resolve(string xml, int caret)
	{
		var engine = new CompletionEngine();
		return XamlGoToDefinitionResolver.Resolve(
			engine,
			SharedMetadata,
			xml,
			caret,
			Assembly.GetExecutingAssembly().GetName().Name);
	}

	#endregion
}
