#region References

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests;

[TestClass]
public class BasicTests : XamlCompletionTestBase
{
	#region Methods

	[TestMethod]
	public void AttachedPropertyClassShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl ", "Gri", "Grid.");
	}

	[TestMethod]
	public void AttachedPropertyShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl Grid.", "Ro", "Row=\"\"");
	}

	[TestMethod]
	public void AttachedPropertyShouldBeRenamed()
	{
		AssertSingleCompletionInMiddleOfText("<UserControl Grid.", "=\"2\"", "Ro", "Row");
	}

	[TestMethod]
	public void ClosingTagShouldBeProperlyCompleted()
	{
		AssertSingleCompletion("<UserControl><Button><Button.Styles><Style/></Button.Styles><", "/", "/Button>");
	}

	[TestMethod]
	public void ClrNameSpacesShouldBeCompleted()
	{
		var compl = GetCompletionsFor("<UserControl xmlns:t=\"clr-namespace:Ava");

		Assert.IsTrue(compl.Completions.Count > 0);
		Assert.IsTrue(compl.Completions.Any(v => v.InsertText == "clr-namespace:Avalonia.Data;assembly=Avalonia.Base"));
		Assert.IsTrue(compl.Completions.Any(v => v.InsertText == "clr-namespace:Avalonia.Controls;assembly=Avalonia.Controls"));
	}

	[TestMethod]
	public void CompletationEventHandlerWithoutxmlsn()
	{
		var comp = GetCompletionsFor("<local:MyButton Click=\"");

		Assert.IsNotNull(comp);
		Assert.AreEqual(1, comp.Completions?.Count);
		Assert.AreEqual("MyButton_Click", comp.Completions[0].InsertText);
	}

	[TestMethod]
	public void CompletionsShouldBeSorted()
	{
		var compl = GetCompletionsFor("<DataTemplate");

		Assert.AreEqual(2, compl.Completions.Count);
		Assert.AreEqual("DataTemplate", compl.Completions[0].DisplayText);
		Assert.AreEqual("DataTemplates", compl.Completions[1].DisplayText);
	}

	[TestMethod]
	public void CompletionsWithMultipleKindsShouldBeSorted()
	{
		var compl = GetCompletionsFor("<Style Se");

		Assert.AreEqual(4, compl.Completions.Count);
		Assert.AreEqual("Selector", compl.Completions[0].DisplayText);
		Assert.AreEqual("SelectableTextBlock", compl.Completions[1].DisplayText);
		Assert.AreEqual("SelectingItemsControl", compl.Completions[2].DisplayText);
		Assert.AreEqual("SelectingMultiPage", compl.Completions[3].DisplayText);
	}

	[TestMethod]
	public void BooleanValueShouldBeCompleted()
	{
		AssertSingleCompletion("<Border ClipToBounds=\"", "Fal", "False");
	}

	[TestMethod]
	public void EnumValueShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl HorizontalAlignment=\"", "Le", "Left");
	}

	[TestMethod]
	public void ExtensionDataTypeShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl ", "x:Data", "x:DataType=\"\"");
	}

	[TestMethod]
	public void ExtensionPropertyEnumShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl Content=\"{Binding Mode=", "One", "OneWay");
	}

	[TestMethod]
	public void ExtensionPropertyShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl Content=\"{Binding ", "Pa", "Path=");
	}

	[TestMethod]
	public void ExtensionShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl Content=\"{", "Bind", "Binding");
	}

	[TestMethod]
	public void GenericTypeShouldTransformTypeArguments()
	{
		var compl = GetCompletionsFor("<FuncDataTemplate");

		Assert.AreEqual(2, compl.Completions.Count);
		Assert.AreEqual("FuncDataTemplate", compl.Completions[0].DisplayText);
		// Non-generic (unknown leaf vs container): FuncDataTemplate is not in the leaf list → paired tags.
		Assert.AreEqual("FuncDataTemplate></FuncDataTemplate>", compl.Completions[0].InsertText);
		Assert.AreEqual("FuncDataTemplate>".Length, compl.Completions[0].RecommendedCursorOffset);
		Assert.AreEqual("FuncDataTemplate<T>", compl.Completions[1].DisplayText);
		Assert.AreEqual("FuncDataTemplate x:TypeArguments=\"\"", compl.Completions[1].InsertText);
	}

	[TestMethod]
	public void GetOnlyPropertyShouldNotBeCompleted()
	{
		var compl = GetCompletionsFor("<UserControl P");

		foreach (var c in compl.Completions)
		{
			Assert.AreNotEqual("Parent", c.DisplayText);
		}
	}

	[TestMethod]
	public void PropertyCompletionsShouldBeUnique()
	{
		var compl = GetCompletionsFor("<UserControl P");
		foreach (var v in compl.Completions.GroupBy(g => g.DisplayText))
		{
			Assert.AreEqual(1, v.Count());
		}
	}

	[TestMethod]
	public void PropertyShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl ", "HorizontalAlign", "HorizontalAlignment=\"\"");
	}

	[TestMethod]
	public void PropertyShouldBeRenamed()
	{
		AssertSingleCompletionInMiddleOfText("<UserControl ", "=\"Top\"", "HorizontalAlign", "HorizontalAlignment");
	}

	[TestMethod]
	public void UsingNameSpacesShouldBeCompleted()
	{
		var compl = GetCompletionsFor("<UserControl xmlns:t=\"using:Ava");

		Assert.IsTrue(compl.Completions.Count > 0);
		Assert.IsTrue(compl.Completions.Any(v => v.InsertText == "using:Avalonia.Data"));
		Assert.IsTrue(compl.Completions.Any(v => v.InsertText == "using:Avalonia.Controls"));
	}

	[TestMethod]
	public void WellKnownUrlNameSpacesShouldBeCompleted()
	{
		var compl = GetCompletionsFor("<UserControl xmlns:t=\"http");

		Assert.IsTrue(compl.Completions.Count > 0);
		Assert.IsTrue(compl.Completions.Any(v => v.InsertText == "https://github.com/avaloniaui"));
		Assert.IsTrue(compl.Completions.Any(v => v.InsertText == "http://schemas.microsoft.com/winfx/2006/xaml"));
	}

	[TestMethod]
	public void XmlContentAttachedPropertyClassShouldBeCompleted()
	{
		// Grid is a container → paired tags with caret between.
		AssertSingleCompletion("<UserControl><", "Gri", "Grid></Grid>");
	}

	[TestMethod]
	public void XmlContentAttachedPropertyShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl><Grid.", "Ro", "Row");
	}

	[TestMethod]
	public void XmlContentPropertyShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl><UserControl.", "HorizontalAlign", "HorizontalAlignment");
	}

	#endregion
}
