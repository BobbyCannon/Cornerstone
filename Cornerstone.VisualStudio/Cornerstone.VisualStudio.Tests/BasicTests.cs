#region References

using System.Linq;
using Xunit;

#endregion

namespace Cornerstone.VisualStudio.Tests;

public class BasicTests : XamlCompletionTestBase
{
	#region Methods

	[Fact]
	public void AttachedPropertyClassShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl ", "Gri", "Grid.");
	}

	[Fact]
	public void AttachedPropertyShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl Grid.", "Ro", "Row=\"\"");
	}

	[Fact]
	public void AttachedPropertyShouldBeRenamed()
	{
		AssertSingleCompletionInMiddleOfText("<UserControl Grid.", "=\"2\"", "Ro", "Row");
	}

	[Fact]
	public void ClosingTagShouldBeProperlyCompleted()
	{
		AssertSingleCompletion("<UserControl><Button><Button.Styles><Style/></Button.Styles><", "/", "/Button>");
	}

	[Fact]
	public void ClrNameSpacesShouldBeCompleted()
	{
		var compl = GetCompletionsFor("<UserControl xmlns:t=\"clr-namespace:Ava");

		Assert.NotEmpty(compl.Completions);
		Assert.Contains(compl.Completions, v => v.InsertText == "clr-namespace:Avalonia.Data;assembly=Avalonia.Base");
		Assert.Contains(compl.Completions, v => v.InsertText == "clr-namespace:Avalonia.Controls;assembly=Avalonia.Controls");
	}

	[Fact]
	public void CompletationEventHandlerWithoutxmlsn()
	{
		var comp = GetCompletionsFor("<local:MyButton Click=\"");

		Assert.NotNull(comp);
		Assert.Equal(1, comp.Completions?.Count);
		Assert.Equal("MyButton_Click", comp.Completions[0].InsertText);
	}

	[Fact]
	public void CompletionsShouldBeSorted()
	{
		var compl = GetCompletionsFor("<DataTemplate");

		Assert.Equal(2, compl.Completions.Count);
		Assert.Equal("DataTemplate", compl.Completions[0].DisplayText);
		Assert.Equal("DataTemplates", compl.Completions[1].DisplayText);
	}

	[Fact]
	public void CompletionsWithMultipleKindsShouldBeSorted()
	{
		var compl = GetCompletionsFor("<Style Se");

		Assert.Equal(4, compl.Completions.Count);
		Assert.Equal("Selector", compl.Completions[0].DisplayText);
		Assert.Equal("SelectableTextBlock", compl.Completions[1].DisplayText);
		Assert.Equal("SelectingItemsControl", compl.Completions[2].DisplayText);
		Assert.Equal("SelectingMultiPage", compl.Completions[3].DisplayText);
	}

	[Fact]
	public void EnumValueShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl HorizontalAlignment=\"", "Le", "Left");
	}

	[Fact]
	public void ExtensionDataTypeShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl ", "x:Data", "x:DataType=\"\"");
	}

	[Fact]
	public void ExtensionPropertyEnumShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl Content=\"{Binding Mode=", "One", "OneWay");
	}

	[Fact]
	public void ExtensionPropertyShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl Content=\"{Binding ", "Pa", "Path=");
	}

	[Fact]
	public void ExtensionShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl Content=\"{", "Bind", "Binding");
	}

	[Fact]
	public void GenericTypeShouldTransformTypeArguments()
	{
		var compl = GetCompletionsFor("<FuncDataTemplate");

		Assert.Equal(2, compl.Completions.Count);
		Assert.Equal("FuncDataTemplate", compl.Completions[0].DisplayText);
		// Non-generic (unknown leaf vs container): FuncDataTemplate is not in the leaf list → paired tags.
		Assert.Equal("FuncDataTemplate></FuncDataTemplate>", compl.Completions[0].InsertText);
		Assert.Equal("FuncDataTemplate>".Length, compl.Completions[0].RecommendedCursorOffset);
		Assert.Equal("FuncDataTemplate<T>", compl.Completions[1].DisplayText);
		Assert.Equal("FuncDataTemplate x:TypeArguments=\"\"", compl.Completions[1].InsertText);
	}

	[Fact]
	public void GetOnlyPropertyShouldNotBeCompleted()
	{
		var compl = GetCompletionsFor("<UserControl P");

		Assert.All(compl.Completions, c => Assert.NotEqual("Parent", c.DisplayText));
	}

	[Fact]
	public void PropertyCompletionsShouldBeUnique()
	{
		var compl = GetCompletionsFor("<UserControl P");
		Assert.All(compl.Completions.GroupBy(v => v.DisplayText), v => Assert.Single(v));
	}

	[Fact]
	public void PropertyShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl ", "HorizontalAlign", "HorizontalAlignment=\"\"");
	}

	[Fact]
	public void PropertyShouldBeRenamed()
	{
		AssertSingleCompletionInMiddleOfText("<UserControl ", "=\"Top\"", "HorizontalAlign", "HorizontalAlignment");
	}

	[Fact]
	public void UsingNameSpacesShouldBeCompleted()
	{
		var compl = GetCompletionsFor("<UserControl xmlns:t=\"using:Ava");

		Assert.NotEmpty(compl.Completions);
		Assert.Contains(compl.Completions, v => v.InsertText == "using:Avalonia.Data");
		Assert.Contains(compl.Completions, v => v.InsertText == "using:Avalonia.Controls");
	}

	[Fact]
	public void WellKnownUrlNameSpacesShouldBeCompleted()
	{
		var compl = GetCompletionsFor("<UserControl xmlns:t=\"http");

		Assert.NotEmpty(compl.Completions);
		Assert.Contains(compl.Completions, v => v.InsertText == "https://github.com/avaloniaui");
		Assert.Contains(compl.Completions, v => v.InsertText == "http://schemas.microsoft.com/winfx/2006/xaml");
	}

	[Fact]
	public void XmlContentAttachedPropertyClassShouldBeCompleted()
	{
		// Grid is a container → paired tags with caret between.
		AssertSingleCompletion("<UserControl><", "Gri", "Grid></Grid>");
	}

	[Fact]
	public void XmlContentAttachedPropertyShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl><Grid.", "Ro", "Row");
	}

	[Fact]
	public void XmlContentPropertyShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl><UserControl.", "HorizontalAlign", "HorizontalAlignment");
	}

	#endregion
}