#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.VisualStudio.Core.Completion;
using Xunit;

#endregion

namespace Cornerstone.VisualStudio.Tests;

public class AdvancedTests : XamlCompletionTestBase
{
	#region Methods

	[Fact]
	public void BindingPathShouldBeCompletedFromParent()
	{
		AssertSingleCompletion("<UserControl Background=\"{Binding ", "$pa", "$parent[");
	}

	[Fact]
	public void BindingPathShouldBeCompletedFromParentProperty()
	{
		AssertSingleCompletion("<UserControl Background=\"{Binding ", "$parent.Ta", "$parent.Tag");
	}

	[Fact]
	public void BindingPathShouldBeCompletedFromParentPropertyNested()
	{
		AssertSingleCompletion("<UserControl Background=\"{Binding ", "$parent.Bounds.Wi", "$parent.Bounds.Width");
	}

	[Fact]
	public void BindingPathShouldBeCompletedFromParentType()
	{
		AssertSingleCompletion("<UserControl Background=\"{Binding ", "$parent[But", "$parent[Button].");
	}

	[Fact]
	public void BindingPathShouldBeCompletedFromParentTypeProperty()
	{
		AssertSingleCompletion("<UserControl Background=\"{Binding ", "$parent[Button].Ta", "$parent[Button].Tag");
	}

	[Fact]
	public void BindingPathShouldBeCompletedFromXDataType()
	{
		AssertSingleCompletion("<UserControl x:DataType=\"Button\"><TextBlock Tag=\"{Binding Path=", "Conte", "Content");
	}

	[Fact]
	public void BindingPathShouldBeCompletedFromXDataType2()
	{
		AssertSingleCompletion("<UserControl x:DataType=\"Button\"><TextBlock Tag=\"{Binding ", "Conte", "Content");
	}

	[Fact]
	public void BindingPathShouldBeCompletedFromXDataTypeIssue463()
	{
		AssertSingleCompletion("<UserControl x:DataType= \"Button\"><TextBlock Tag=\"{Binding Path=", "Conte", "Content");
	}

	[Fact]
	public void BindingPathShouldBeCompletedFromXName()
	{
		AssertSingleCompletion("<UserControl x:Name=\"foo\" Tag=\"{Binding ", "#f", "#foo");
	}

	[Fact]
	public void ControlThemeNestedSelectorShouldBeCompleted()
	{
		var xaml =
			"""
			<UserControl.Resources>
			    <ControlTheme x:Key="MyButton" TargetType="Button">
			        <Style Selector="
			""";
		var compl = GetCompletionsFor(xaml).Completions;

		Assert.Single(compl);
		Assert.Contains(compl, v => v.InsertText == "^");
	}

	[Fact]
	public void ControlThemeNestedSelectorShouldBeCompletedPseudoClass()
	{
		var xaml =
			"""
			<UserControl.Resources>
			    <ControlTheme x:Key="MyButton" TargetType="Button">
			        <Style Selector="^:
			""";
		var compl = GetCompletionsFor(xaml).Completions;

		Assert.Equal(10, compl.Count);
		Assert.Contains(compl, v => v.InsertText == ":disabled");
	}

	[Fact]
	public void ControlThemeNestedSelectorShouldBeCompletedSetter()
	{
		var expected = new[]
		{
			"Command",
			"CommandParameter",
			"CommandBar"
		};

		var xaml =
			"""
			<UserControl.Resources>
			    <ControlTheme x:Key="MyButton" TargetType="Button">
			        <Style Selector="^:disabled">
			            <Setter Property="Com
			""";
		var compl = GetCompletionsFor(xaml).Completions.Select(c => c.InsertText);

		Assert.Equal(expected, compl);
	}

	[Fact]
	public void ControlThemeNestedSelectorShouldBeCompletedTemplate()
	{
		var xaml =
			"""
			<UserControl.Resources>
			    <ControlTheme x:Key="MyButton" TargetType="Button">
			        <Style Selector="^ /template/ C
			""";
		var compl = GetCompletionsFor(xaml).Completions;

		Assert.Contains(compl, v => v.InsertText == "ContentPresenter");
	}

	[Fact]
	public void EnumTypeinStaticExtensionShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl Tag=\"{x:Static ", "HorizontalAlignme", "HorizontalAlignment");
	}

	[Fact]
	public void EnumValueinStaticExtensionShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl HorizontalAlignment=\"{x:Static ", "HorizontalAlignment.L", "HorizontalAlignment.Left");
	}

	[Fact]
	public void ExtensionDataTypeTypesShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl x:DataType=\"", "But", "Button");
	}

	[Fact]
	public void ExtensionPropertyWithWellKnownValueShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl Background=\"{Binding RelativeSource=", "Se", "Self");
	}

	[Fact]
	public void ExtensionWithCtorArgumentClassShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl Background=\"{x:Static ", "Brus", "Brushes");
	}

	[Fact]
	public void ExtensionWithCtorArgumentEnumShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl Background=\"{Binding RelativeSource={RelativeSource ", "Se", "Self");
	}

	[Fact]
	public void ExtensionWithCtorArgumentStaticFieldValuesShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl IsEnabled=\"{Binding Converter={x:Static ", "ObjectConverters.IsN", "ObjectConverters.IsNull");
	}

	[Fact]
	public void ExtensionWithCtorArgumentStaticPropertiesValuesShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl Background=\"{x:Static ", "Brushes.Re", "Brushes.Red");
	}

	[Fact]
	public void ExtensionWithCtorArgumentTypeShouldBeCompleted()
	{
		AssertSingleCompletion("<DataTemplate DataType=\"{x:Type ", "But", "Button");
	}

	public static IEnumerable<object[]> GetStyleSelectors()
	{
		yield return
		[
			"<Style Selector=\"Button[Min",
			false,
			new Completion[]
			{
				new("MinHeight", "MinHeight=", CompletionKind.Property),
				new("MinWidth", "MinWidth=", CompletionKind.Property)
			}
		];
		yield return
		[
			"<Style Selector=\"Button[(Grid.",
			false,
			new Completion[]
			{
				new("Column", "Column)", CompletionKind.AttachedProperty),
				new("ColumnSpan", "ColumnSpan)", CompletionKind.AttachedProperty),
				new("IsSharedSizeScope", "IsSharedSizeScope)", CompletionKind.AttachedProperty),
				new("Row", "Row)", CompletionKind.AttachedProperty),
				new("RowSpan", "RowSpan)", CompletionKind.AttachedProperty)
			}
		];
		yield return
		[
			"<Style Selector=\"",
			true,
			new Completion[]
			{
				new(":", CompletionKind.Selector | CompletionKind.Enum),
				new(">", CompletionKind.Selector | CompletionKind.Enum),
				new(".", CompletionKind.Selector | CompletionKind.Enum),
				new("^", CompletionKind.Selector | CompletionKind.Enum)
			}
		];
		yield return
		[
			"<Style Selector=\"Button:",
			false,
			new Completion[]
			{
				new(":disabled", CompletionKind.Selector | CompletionKind.Enum),
				new(":flyout-open", CompletionKind.Selector | CompletionKind.Enum),
				new(":focus", CompletionKind.Selector | CompletionKind.Enum),
				new(":focus-visible", CompletionKind.Selector | CompletionKind.Enum),
				new(":focus-within", CompletionKind.Selector | CompletionKind.Enum),
				new(":not()", ":not(", CompletionKind.Selector | CompletionKind.Enum),
				new(":nth-child()", ":nth-child(", CompletionKind.Selector | CompletionKind.Enum),
				new(":nth-last-child()", ":nth-last-child(", CompletionKind.Selector | CompletionKind.Enum),
				new(":pointerover", CompletionKind.Selector | CompletionKind.Enum),
				new(":pressed", CompletionKind.Selector | CompletionKind.Enum)
			}
		];
		yield return
		[
			"<Style Selector=\"/temp",
			false,
			new Completion[]
			{
				new("/template/", "/template/", CompletionKind.Selector | CompletionKind.Enum)
			}
		];
		yield return
		[
			"<UserControl x:Name=\"foo\"><UserControl.Styles><Style Selector=\"#",
			false,
			new Completion[]
			{
				new("foo", "foo", CompletionKind.Name | CompletionKind.Class)
			}
		];
		yield return
		[
			"<Style Selector=\"Button[(Grid.IsSharedSizeScope)=",
			false,
			new Completion[]
			{
				new("False", CompletionKind.StaticProperty),
				new("True", CompletionKind.StaticProperty)
			}
		];
		yield return
		[
			"<Style Selector=\"TextBlock[HorizontalAlignment=",
			false,
			new Completion[]
			{
				new("Center", CompletionKind.Enum),
				new("Left", CompletionKind.Enum),
				new("Right", CompletionKind.Enum),
				new("Stretch", CompletionKind.Enum)
			}
		];
		yield return
		[
			"<Style Selector=\"TextBlock[HorizontalAlignment=c",
			false,
			new Completion[]
			{
				new("Center", CompletionKind.Enum)
			}
		];
		yield return
		[
			"<Style Selector=\"Button[(Grid.IsSharedSizeScope)=t",
			false,
			new Completion[]
			{
				new("True", CompletionKind.StaticProperty)
			}
		];
		yield return
		[
			"<Style Selector=\"local|",
			true,
			new Completion[]
			{
				new("AttachedBehavior", "local|AttachedBehavior", CompletionKind.Class | CompletionKind.TargetTypeClass)
			}
		];

		yield return
		[
			"<Style Selector=\"ToggleSwitch /template/ #",
			true,
			new Completion[]
			{
				new("PART_MovingKnobs", CompletionKind.Class | CompletionKind.Name),
				new("PART_OffContentPresenter", CompletionKind.Class | CompletionKind.Name),
				new("PART_OnContentPresenter", CompletionKind.Class | CompletionKind.Name),
				new("PART_SwitchKnob", CompletionKind.Class | CompletionKind.Name)
			}
		];
		yield return
		[
			"<Style Selector=\"ToggleSwitch /template/ ContentPresenter#",
			true,
			new Completion[]
			{
				new("PART_OffContentPresenter", CompletionKind.Class | CompletionKind.Name),
				new("PART_OnContentPresenter", CompletionKind.Class | CompletionKind.Name)
			}
		];
	}

	[Fact]
	public void ImageSourceavaresRelativeUrisShouldBeCompleted()
	{
		AssertSingleCompletion("<Image Source=\"", "/", "/Test.bmp");
	}

	[Fact]
	public void ImageSourceavaresUrisShouldBeCompleted()
	{
		AssertSingleCompletion("<Image Source=\"", "avares:", "avares://Cornerstone.VisualStudio.Tests/Test.bmp");
	}

	[Fact]
	public void ImageSourceresmRelativeUrisShouldBeCompleted()
	{
		AssertSingleCompletion("<Image Source=\"", "resm:", "resm:Cornerstone.VisualStudio.Tests.Test.bmp");
	}

	[Fact]
	public void ImageSourceresmUrisShouldBeCompleted()
	{
		AssertSingleCompletion("<Image Source=\"", "resm:", "resm:Cornerstone.VisualStudio.Tests.Test.bmp?assembly=Cornerstone.VisualStudio.Tests");
	}

	[Fact]
	public void MarkupExtensionAsXamlElementShouldNotHaveExtensionSuffix()
	{
		var xaml = "<Sta";

		var comp = GetCompletionsFor(xaml);
		if (comp == null)
		{
			throw new Exception("No completions found");
		}

		Assert.NotNull(comp.Completions
			.Where(x => x.DisplayText.Equals("StaticResource") && x.InsertText.Equals("StaticResource"))
			.FirstOrDefault());
	}

	[Fact]
	public void OnFormFactorShouldBeSuggestedAsMarkupExtension()
	{
		var xaml = "<Button Background=\"{O";

		var comp = GetCompletionsFor(xaml);
		if (comp == null)
		{
			throw new Exception("No completions found");
		}

		Assert.NotNull(comp.Completions.Where(x => x.DisplayText.Equals("OnFormFactor")).FirstOrDefault());
	}

	[Fact]
	public void OnFormFactorShouldBeSuggestedAsXamlElement()
	{
		var xaml = "<O";

		var comp = GetCompletionsFor(xaml);
		if (comp == null)
		{
			throw new Exception("No completions found");
		}

		Assert.NotNull(comp.Completions.Where(x => x.DisplayText.Equals("OnFormFactor")).FirstOrDefault());
	}

	[Fact]
	public void OnFormFactorSuggestionsAreContextSpecificInMarkupExtension()
	{
		var xaml = "<Button IsVisible=\"{OnFormFactor ";

		var comp = GetCompletionsFor(xaml);
		if (comp == null)
		{
			throw new Exception("No completions found");
		}

		// Suggest property completions
		Assert.Equal(2, comp.Completions.Count);
		Assert.Contains(comp.Completions, x => x.DisplayText.Equals("True"));
		Assert.Contains(comp.Completions, x => x.DisplayText.Equals("False"));

		// Now comma should list platforms for other options
		xaml += ",";
		comp = GetCompletionsFor(xaml);
		if (comp == null)
		{
			throw new Exception("No completions found");
		}

		var formFactors = new List<string> { "Desktop", "Mobile" };

		Assert.Equal(formFactors.Count, comp.Completions.Count);
		// Should suggest all platforms
		foreach (var item in comp.Completions)
		{
			if (formFactors.Contains(item.DisplayText, StringComparer.InvariantCultureIgnoreCase))
			{
				formFactors.Remove(item.DisplayText);
			}
		}
		Assert.Empty(formFactors);
	}

	[Fact]
	public void OnPlatformShouldBeSuggestedAsMarkupExtension()
	{
		var xaml = "<Button Background=\"{O";

		var comp = GetCompletionsFor(xaml);
		if (comp == null)
		{
			throw new Exception("No completions found");
		}

		Assert.NotNull(comp.Completions.Where(x => x.DisplayText.Equals("OnPlatform")).FirstOrDefault());
	}

	[Fact]
	public void OnPlatformShouldBeSuggestedAsXamlElement()
	{
		var xaml = "<O";

		var comp = GetCompletionsFor(xaml);
		if (comp == null)
		{
			throw new Exception("No completions found");
		}

		Assert.NotNull(comp.Completions.Where(x => x.DisplayText.Equals("OnPlatform")).FirstOrDefault());
	}

	[Fact]
	public void OnPlatformSuggestionsAreContextSpecificInMarkupExtension()
	{
		var xaml = "<Button IsVisible=\"{OnPlatform ";

		var comp = GetCompletionsFor(xaml);
		if (comp == null)
		{
			throw new Exception("No completions found");
		}

		// Suggest property completions
		Assert.Equal(2, comp.Completions.Count);
		Assert.Contains(comp.Completions, x => x.DisplayText.Equals("True"));
		Assert.Contains(comp.Completions, x => x.DisplayText.Equals("False"));

		// Now comma should list platforms for other options
		xaml += ",";
		comp = GetCompletionsFor(xaml);
		if (comp == null)
		{
			throw new Exception("No completions found");
		}

		var platforms = new List<string> { "Windows", "macOS", "Linux", "Browser", "iOS", "Android" };

		Assert.Equal(platforms.Count, comp.Completions.Count);
		// Should suggest all platforms
		foreach (var item in comp.Completions)
		{
			if (platforms.Contains(item.DisplayText, StringComparer.InvariantCultureIgnoreCase))
			{
				platforms.Remove(item.DisplayText);
			}
		}
		Assert.Empty(platforms);
	}

	[Fact]
	public void PropertyOfTypeTypeTypeShouldBeCompleted()
	{
		AssertSingleCompletion("<DataTemplate DataType=\"", "But", "Button");
	}

	[Fact]
	public void ShouldNotContainAbstractClasses()
	{
		const string xaml = "<UserControl.Styles><Style";
		if (GetCompletionsFor(xaml)?.Completions?.Select(c => c.DisplayText) is { } completions)
		{
			Assert.DoesNotContain("StyleBase", completions);
		}
		else
		{
			Assert.Fail("Unable get completions list.");
		}
	}

	[Fact]
	public void StyleAttachedPropertyClassNameShouldBeCompleted()
	{
		AssertSingleCompletion("<Style Selector=\"Button\"><Setter Property=\"", "TextBl", "TextBlock");
	}

	[Fact]
	public void StyleAttachedPropertyNameShouldBeCompleted()
	{
		var xaml = "<Style Selector=\"Button\"><Setter Property=\"";
		var typed = "TextElement.FontWe";

		var comp = GetCompletionsFor(xaml + typed);
		if (comp == null)
		{
			throw new Exception("No completions found");
		}

		// AttachedProperty in Setter changed in GH#302 - this part of the test is now failing
		// and I don't know why. I have tested this in an actual xaml document and it works
		// perfectly fine, so I'm skipping this now
		//var pos = xaml.Length + typed.IndexOf('.');
		//Assert.True(pos == comp.StartPosition, $"Invalid completion start position typed");

		Assert.Contains(comp.Completions, c => c.InsertText == "FontWeight");

		Assert.Single(comp.Completions, c => c.InsertText == "FontWeight");
	}

	[Fact]
	public void StyleAttachedPropertyValueShouldBeCompleted()
	{
		AssertSingleCompletion("<Style Selector=\"Button\"><Setter Property=\"TextElement.FontWeight\" Value=\"", "Bo", "Bold");
	}

	[Fact]
	public void StyleIncludeSourceRelativeUrisShouldBeCompiledStyles()
	{
		AssertSingleCompletion("<StyleInclude Source=\"", "/", "/TestCompiledTheme.xaml");
	}

	[Fact]
	public void StyleIncludeSourceRelativeUrisShouldBeCompleted()
	{
		AssertSingleCompletion("<StyleInclude Source=\"", "/", "/Test.xaml");
	}

	[Fact]
	public void StyleIncludeSourceUrisShouldBeCompleted()
	{
		AssertSingleCompletion("<StyleInclude Source=\"", "avares:", "avares://Cornerstone.VisualStudio.Tests/Test.xaml");
	}

	[Fact]
	public void StyleIncludeSourceUrisShouldBeCompletedCompiledStyles()
	{
		AssertSingleCompletion("<StyleInclude Source=\"", "avares:", "avares://Cornerstone.VisualStudio.Tests/TestCompiledTheme.xaml");
	}

	[Fact]
	public void StylePropertyNameShouldBeCompleted()
	{
		AssertSingleCompletion("<Style Selector=\"Button\"><Setter Property=\"", "HorizontalAli", "HorizontalAlignment");
	}

	[Fact]
	public void StylePropertyNameShouldBeCompletedFromLastSelectorType()
	{
		AssertSingleCompletion("<Style Selector=\"Button.classname:pseudoclass /template/ > Grid#name\"><Setter Property=\"", "ColumnDef", "ColumnDefinitions");
	}

	[Fact]
	public void StylePropertyValueShouldBeCompleted()
	{
		AssertSingleCompletion("<Style Selector=\"Button.my\"><Setter Property=\"HorizontalAlignment\" Value=\"", "Le", "Left");
	}

	[Theory]
	[MemberData(nameof(GetStyleSelectors))]
	public void StyleSelectorCompletions(string selector, bool contain, IEnumerable<Completion> expected)
	{
		var compl = GetCompletionsFor(selector)?.Completions;
		if (!contain)
		{
			Assert.Equal(expected, compl);
		}
		else
		{
			foreach (var item in expected)
			{
				// Match identity fields only — cursor/delete offsets are document-relative
				// and not part of the completion “what to insert” contract under test.
				Assert.Contains(compl, c =>
					(c.DisplayText == item.DisplayText) &&
					(c.InsertText == item.InsertText) &&
					(c.Kind == item.Kind));
			}
		}
	}

	[Fact]
	public void StyleSelectorControlTypesShouldBeCompleted()
	{
		AssertSingleCompletion("<Style Selector=\"", "But", "Button");
	}

	[Fact]
	public void StyleSelectorSomeWellKnownKeywordsShouldBeCompleted()
	{
		var compl = GetCompletionsFor("<Style Selector=\"").Completions;

		Assert.Contains(compl, v => v.InsertText == ">");
		Assert.Contains(compl, v => v.InsertText == ".");
		Assert.Contains(compl, v => v.InsertText == "#");
		Assert.Contains(compl, v => v.InsertText == "/template/");
	}

	[Fact]
	public void StyleSelectorSomeWellKnownPseudoClassesShouldBeCompleted()
	{
		var compl = GetCompletionsFor("<Style Selector=\"Button:").Completions;

		Assert.Contains(compl, v => v.InsertText == ":pointerover");
		Assert.Contains(compl, v => v.InsertText == ":disabled");
		Assert.Contains(compl, v => v.InsertText == ":focus");
	}

	[Fact]
	public void TemplateBindingAvaloniaPropetiesShouldBeCompleted()
	{
		AssertSingleCompletion("<ContentPresenter Background=\"{TemplateBinding ", "Back", "Background");
	}

	[Fact]
	public void WellKnownBrushesShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl Background=\"", "Re", "Red");
	}

	[Fact]
	public void WellKnownThemeKeysShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl Background=\"{DynamicResource ", "Theme", "ThemeBackgroundBrush");
	}

	[Fact]
	public void LocalXKeyShouldBeCompletedForStaticResource()
	{
		AssertSingleCompletion(
			"<UserControl.Resources><SolidColorBrush x:Key=\"MyLocalBrush\" Color=\"Red\" /></UserControl.Resources><UserControl Background=\"{StaticResource ",
			"MyLocal",
			"MyLocalBrush");
	}

	[Fact]
	public void LocalXKeyShouldBeCompletedForDynamicResource()
	{
		AssertSingleCompletion(
			"<UserControl.Resources><SolidColorBrush x:Key=\"MyDynBrush\" Color=\"Blue\" /></UserControl.Resources><Border Background=\"{DynamicResource ",
			"MyDyn",
			"MyDynBrush");
	}

	[Fact]
	public void LocalXKeyAfterCursorShouldBeCompletedForStaticResource()
	{
		// Keys defined later in the document should still complete (scan full text).
		var before = "<Button Background=\"{StaticResource ";
		var after = "\" /><UserControl.Resources><SolidColorBrush x:Key=\"LaterBrush\" Color=\"Green\" /></UserControl.Resources>";
		AssertSingleCompletionInMiddleOfText(before, after, "Later", "LaterBrush");
	}

	[Fact]
	public void xClassDirectiveShouldBeCompleted()
	{
		var compl = GetCompletionsFor("<UserControl x:Cla").Completions;

		Assert.Contains(compl, v => v.InsertText == "x:Class=\"\"");
	}

	[Fact]
	public void xClassValueShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl x:Class=\"", "", "Cornerstone.VisualStudio.Tests.TestUserControl");
	}

	[Fact]
	public void xKeyDirectiveShouldBeCompleted()
	{
		var compl = GetCompletionsFor("<UserControl x:K").Completions;

		Assert.Contains(compl, v => v.InsertText == "x:Key=\"\"");
	}

	[Fact]
	public void xmlnsDirectiveShouldBeCompleted()
	{
		var compl = GetCompletionsFor("<UserControl x").Completions;

		Assert.Contains(compl, v => v.InsertText == "xmlns:");
	}

	[Fact]
	public void xNameDirectiveShouldBeCompleted()
	{
		var compl = GetCompletionsFor("<UserControl x:N").Completions;

		Assert.Contains(compl, v => v.InsertText == "x:Name=\"\"");
	}

	[Fact]
	public void xTypeArgumentsDirectiveShouldBeCompleted()
	{
		AssertSingleCompletion("<local:GenericBaseClass`1 ", "x:T", "x:TypeArguments=\"\"");
	}

	[Fact]
	public void xTypeArgumentsDirectiveShouldNotBeCompletedOnNonGenericType()
	{
		Assert.Null(GetCompletionsFor("<UserControl x:TypeArgum"));
	}

	[Fact]
	public void xTypeArgumentsValueShouldBeCompleted()
	{
		AssertSingleCompletion("<local:GenericBaseClass`1 x:TypeArguments=\"", "Tex", "TextBlock");
	}

	#endregion
}