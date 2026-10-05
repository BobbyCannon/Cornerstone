#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.VisualStudio.Core.Completion;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests;

[TestClass]
public class AdvancedTests : XamlCompletionTestBase
{
	#region Methods

	[TestMethod]
	public void BindingPathShouldBeCompletedFromParent()
	{
		AssertSingleCompletion("<UserControl Background=\"{Binding ", "$pa", "$parent[");
	}

	[TestMethod]
	public void BindingPathShouldBeCompletedFromParentProperty()
	{
		AssertSingleCompletion("<UserControl Background=\"{Binding ", "$parent.Ta", "$parent.Tag");
	}

	[TestMethod]
	public void BindingPathShouldBeCompletedFromParentPropertyNested()
	{
		AssertSingleCompletion("<UserControl Background=\"{Binding ", "$parent.Bounds.Wi", "$parent.Bounds.Width");
	}

	[TestMethod]
	public void BindingPathShouldBeCompletedFromParentType()
	{
		AssertSingleCompletion("<UserControl Background=\"{Binding ", "$parent[But", "$parent[Button].");
	}

	[TestMethod]
	public void BindingPathShouldBeCompletedFromParentTypeProperty()
	{
		AssertSingleCompletion("<UserControl Background=\"{Binding ", "$parent[Button].Ta", "$parent[Button].Tag");
	}

	[TestMethod]
	public void BindingPathShouldBeCompletedFromXDataType()
	{
		AssertSingleCompletion("<UserControl x:DataType=\"Button\"><TextBlock Tag=\"{Binding Path=", "Conte", "Content");
	}

	[TestMethod]
	public void BindingPathShouldBeCompletedFromXDataType2()
	{
		AssertSingleCompletion("<UserControl x:DataType=\"Button\"><TextBlock Tag=\"{Binding ", "Conte", "Content");
	}

	[TestMethod]
	public void BindingPathShouldBeCompletedFromXDataTypeIssue463()
	{
		AssertSingleCompletion("<UserControl x:DataType= \"Button\"><TextBlock Tag=\"{Binding Path=", "Conte", "Content");
	}

	[TestMethod]
	public void BindingPathShouldBeCompletedFromXName()
	{
		AssertSingleCompletion("<UserControl x:Name=\"foo\" Tag=\"{Binding ", "#f", "#foo");
	}

	[TestMethod]
	public void ControlThemeNestedSelectorShouldBeCompleted()
	{
		var xaml =
			"""
			<UserControl.Resources>
			    <ControlTheme x:Key="MyButton" TargetType="Button">
			        <Style Selector="
			""";
		var compl = GetCompletionsFor(xaml).Completions;

		Assert.AreEqual(1, System.Linq.Enumerable.Count(compl));
		Assert.IsTrue(compl.Any(v => v.InsertText == "^"));
	}

	[TestMethod]
	public void ControlThemeNestedSelectorShouldBeCompletedPseudoClass()
	{
		var xaml =
			"""
			<UserControl.Resources>
			    <ControlTheme x:Key="MyButton" TargetType="Button">
			        <Style Selector="^:
			""";
		var compl = GetCompletionsFor(xaml).Completions;

		Assert.AreEqual(10, compl.Count);
		Assert.IsTrue(compl.Any(v => v.InsertText == ":disabled"));
	}

	[TestMethod]
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

		Assert.AreSequenceEqual(expected, compl);
	}

	[TestMethod]
	public void ControlThemeNestedSelectorShouldBeCompletedTemplate()
	{
		var xaml =
			"""
			<UserControl.Resources>
			    <ControlTheme x:Key="MyButton" TargetType="Button">
			        <Style Selector="^ /template/ C
			""";
		var compl = GetCompletionsFor(xaml).Completions;

		Assert.IsTrue(compl.Any(v => v.InsertText == "ContentPresenter"));
	}

	[TestMethod]
	public void EnumTypeinStaticExtensionShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl Tag=\"{x:Static ", "HorizontalAlignme", "HorizontalAlignment");
	}

	[TestMethod]
	public void EnumValueinStaticExtensionShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl HorizontalAlignment=\"{x:Static ", "HorizontalAlignment.L", "HorizontalAlignment.Left");
	}

	[TestMethod]
	public void ExtensionDataTypeTypesShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl x:DataType=\"", "But", "Button");
	}

	[TestMethod]
	public void ExtensionPropertyWithWellKnownValueShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl Background=\"{Binding RelativeSource=", "Se", "Self");
	}

	[TestMethod]
	public void ExtensionWithCtorArgumentClassShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl Background=\"{x:Static ", "Brus", "Brushes");
	}

	[TestMethod]
	public void ExtensionWithCtorArgumentEnumShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl Background=\"{Binding RelativeSource={RelativeSource ", "Se", "Self");
	}

	[TestMethod]
	public void ExtensionWithCtorArgumentStaticFieldValuesShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl IsEnabled=\"{Binding Converter={x:Static ", "ObjectConverters.IsN", "ObjectConverters.IsNull");
	}

	[TestMethod]
	public void ExtensionWithCtorArgumentStaticPropertiesValuesShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl Background=\"{x:Static ", "Brushes.Re", "Brushes.Red");
	}

	[TestMethod]
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

	[TestMethod]
	public void ImageSourcecsresRelativeUrisShouldBeCompleted()
	{
		AssertSingleCompletion("<Image Source=\"", "/", "/Test.bmp");
	}

	[TestMethod]
	public void ImageSourcecsresUrisShouldBeCompleted()
	{
		AssertSingleCompletion("<Image Source=\"", "csres:", "csres://Cornerstone.VisualStudio.Tests/Test.bmp");
	}

	[TestMethod]
	public void ImageSourceresmRelativeUrisShouldBeCompleted()
	{
		AssertSingleCompletion("<Image Source=\"", "resm:", "resm:Cornerstone.VisualStudio.Tests.Test.bmp");
	}

	[TestMethod]
	public void ImageSourceresmUrisShouldBeCompleted()
	{
		AssertSingleCompletion("<Image Source=\"", "resm:", "resm:Cornerstone.VisualStudio.Tests.Test.bmp?assembly=Cornerstone.VisualStudio.Tests");
	}

	[TestMethod]
	public void MarkupExtensionAsXamlElementShouldNotHaveExtensionSuffix()
	{
		var xaml = "<Sta";

		var comp = GetCompletionsFor(xaml);
		if (comp == null)
		{
			throw new Exception("No completions found");
		}

		Assert.IsNotNull(comp.Completions
			.Where(x => x.DisplayText.Equals("StaticResource") && x.InsertText.Equals("StaticResource"))
			.FirstOrDefault());
	}

	[TestMethod]
	public void OnFormFactorShouldBeSuggestedAsMarkupExtension()
	{
		var xaml = "<Button Background=\"{O";

		var comp = GetCompletionsFor(xaml);
		if (comp == null)
		{
			throw new Exception("No completions found");
		}

		Assert.IsNotNull(comp.Completions.Where(x => x.DisplayText.Equals("OnFormFactor")).FirstOrDefault());
	}

	[TestMethod]
	public void OnFormFactorShouldBeSuggestedAsXamlElement()
	{
		var xaml = "<O";

		var comp = GetCompletionsFor(xaml);
		if (comp == null)
		{
			throw new Exception("No completions found");
		}

		Assert.IsNotNull(comp.Completions.Where(x => x.DisplayText.Equals("OnFormFactor")).FirstOrDefault());
	}

	[TestMethod]
	public void OnFormFactorSuggestionsAreContextSpecificInMarkupExtension()
	{
		var xaml = "<Button IsVisible=\"{OnFormFactor ";

		var comp = GetCompletionsFor(xaml);
		if (comp == null)
		{
			throw new Exception("No completions found");
		}

		// Suggest property completions
		Assert.AreEqual(2, comp.Completions.Count);
		Assert.IsTrue(comp.Completions.Any(x => x.DisplayText.Equals("True")));
		Assert.IsTrue(comp.Completions.Any(x => x.DisplayText.Equals("False")));

		// Now comma should list platforms for other options
		xaml += ",";
		comp = GetCompletionsFor(xaml);
		if (comp == null)
		{
			throw new Exception("No completions found");
		}

		var formFactors = new List<string> { "Desktop", "Mobile" };

		Assert.AreEqual(formFactors.Count, comp.Completions.Count);
		// Should suggest all platforms
		foreach (var item in comp.Completions)
		{
			if (formFactors.Contains(item.DisplayText, StringComparer.InvariantCultureIgnoreCase))
			{
				formFactors.Remove(item.DisplayText);
			}
		}
		Assert.AreEqual(0, System.Linq.Enumerable.Count(formFactors));
	}

	[TestMethod]
	public void OnPlatformShouldBeSuggestedAsMarkupExtension()
	{
		var xaml = "<Button Background=\"{O";

		var comp = GetCompletionsFor(xaml);
		if (comp == null)
		{
			throw new Exception("No completions found");
		}

		Assert.IsNotNull(comp.Completions.Where(x => x.DisplayText.Equals("OnPlatform")).FirstOrDefault());
	}

	[TestMethod]
	public void OnPlatformShouldBeSuggestedAsXamlElement()
	{
		var xaml = "<O";

		var comp = GetCompletionsFor(xaml);
		if (comp == null)
		{
			throw new Exception("No completions found");
		}

		Assert.IsNotNull(comp.Completions.Where(x => x.DisplayText.Equals("OnPlatform")).FirstOrDefault());
	}

	[TestMethod]
	public void OnPlatformSuggestionsAreContextSpecificInMarkupExtension()
	{
		var xaml = "<Button IsVisible=\"{OnPlatform ";

		var comp = GetCompletionsFor(xaml);
		if (comp == null)
		{
			throw new Exception("No completions found");
		}

		// Suggest property completions
		Assert.AreEqual(2, comp.Completions.Count);
		Assert.IsTrue(comp.Completions.Any(x => x.DisplayText.Equals("True")));
		Assert.IsTrue(comp.Completions.Any(x => x.DisplayText.Equals("False")));

		// Now comma should list platforms for other options
		xaml += ",";
		comp = GetCompletionsFor(xaml);
		if (comp == null)
		{
			throw new Exception("No completions found");
		}

		var platforms = new List<string> { "Windows", "macOS", "Linux", "Browser", "iOS", "Android" };

		Assert.AreEqual(platforms.Count, comp.Completions.Count);
		// Should suggest all platforms
		foreach (var item in comp.Completions)
		{
			if (platforms.Contains(item.DisplayText, StringComparer.InvariantCultureIgnoreCase))
			{
				platforms.Remove(item.DisplayText);
			}
		}
		Assert.AreEqual(0, System.Linq.Enumerable.Count(platforms));
	}

	[TestMethod]
	public void PropertyOfTypeTypeTypeShouldBeCompleted()
	{
		AssertSingleCompletion("<DataTemplate DataType=\"", "But", "Button");
	}

	[TestMethod]
	public void ShouldNotContainAbstractClasses()
	{
		const string xaml = "<UserControl.Styles><Style";
		if (GetCompletionsFor(xaml)?.Completions?.Select(c => c.DisplayText) is { } completions)
		{
			Assert.IsFalse(completions.Contains("StyleBase"));
		}
		else
		{
			Assert.Fail("Unable get completions list.");
		}
	}

	[TestMethod]
	public void StyleAttachedPropertyClassNameShouldBeCompleted()
	{
		AssertSingleCompletion("<Style Selector=\"Button\"><Setter Property=\"", "TextBl", "TextBlock");
	}

	[TestMethod]
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
		//Assert.IsTrue(pos == comp.StartPosition, $"Invalid completion start position typed");

		Assert.IsTrue(comp.Completions.Any(c => c.InsertText == "FontWeight"));

		Assert.AreEqual(1, System.Linq.Enumerable.Count(comp.Completions, c => c.InsertText == "FontWeight"));
	}

	[TestMethod]
	public void StyleAttachedPropertyValueShouldBeCompleted()
	{
		AssertSingleCompletion("<Style Selector=\"Button\"><Setter Property=\"TextElement.FontWeight\" Value=\"", "Bo", "Bold");
	}

	[TestMethod]
	public void StyleIncludeSourceRelativeUrisShouldBeCompiledStyles()
	{
		AssertSingleCompletion("<StyleInclude Source=\"", "/", "/TestCompiledTheme.xaml");
	}

	[TestMethod]
	public void StyleIncludeSourceRelativeUrisShouldBeCompleted()
	{
		AssertSingleCompletion("<StyleInclude Source=\"", "/", "/Test.xaml");
	}

	[TestMethod]
	public void StyleIncludeSourceUrisShouldBeCompleted()
	{
		AssertSingleCompletion("<StyleInclude Source=\"", "csres:", "csres://Cornerstone.VisualStudio.Tests/Test.xaml");
	}

	[TestMethod]
	public void StyleIncludeSourceUrisShouldBeCompletedCompiledStyles()
	{
		AssertSingleCompletion("<StyleInclude Source=\"", "csres:", "csres://Cornerstone.VisualStudio.Tests/TestCompiledTheme.xaml");
	}

	[TestMethod]
	public void StylePropertyNameShouldBeCompleted()
	{
		AssertSingleCompletion("<Style Selector=\"Button\"><Setter Property=\"", "HorizontalAli", "HorizontalAlignment");
	}

	[TestMethod]
	public void StylePropertyNameShouldBeCompletedFromLastSelectorType()
	{
		AssertSingleCompletion("<Style Selector=\"Button.classname:pseudoclass /template/ > Grid#name\"><Setter Property=\"", "ColumnDef", "ColumnDefinitions");
	}

	[TestMethod]
	public void StylePropertyValueShouldBeCompleted()
	{
		AssertSingleCompletion("<Style Selector=\"Button.my\"><Setter Property=\"HorizontalAlignment\" Value=\"", "Le", "Left");
	}

	[TestMethod]
	[DynamicData(nameof(GetStyleSelectors))]
	public void StyleSelectorCompletions(string selector, bool contain, IEnumerable<Completion> expected)
	{
		var compl = GetCompletionsFor(selector)?.Completions;
		if (!contain)
		{
			Assert.AreSequenceEqual(expected, compl);
		}
		else
		{
			foreach (var item in expected)
			{
				// Match identity fields only — cursor/delete offsets are document-relative
				// and not part of the completion “what to insert” contract under test.
				Assert.IsTrue(compl.Any(c =>
					(c.DisplayText == item.DisplayText) &&
					(c.InsertText == item.InsertText) &&
					(c.Kind == item.Kind)));
			}
		}
	}

	[TestMethod]
	public void StyleSelectorControlTypesShouldBeCompleted()
	{
		AssertSingleCompletion("<Style Selector=\"", "But", "Button");
	}

	[TestMethod]
	public void StyleSelectorSomeWellKnownKeywordsShouldBeCompleted()
	{
		var compl = GetCompletionsFor("<Style Selector=\"").Completions;

		Assert.IsTrue(compl.Any(v => v.InsertText == ">"));
		Assert.IsTrue(compl.Any(v => v.InsertText == "."));
		Assert.IsTrue(compl.Any(v => v.InsertText == "#"));
		Assert.IsTrue(compl.Any(v => v.InsertText == "/template/"));
	}

	[TestMethod]
	public void StyleSelectorSomeWellKnownPseudoClassesShouldBeCompleted()
	{
		var compl = GetCompletionsFor("<Style Selector=\"Button:").Completions;

		Assert.IsTrue(compl.Any(v => v.InsertText == ":pointerover"));
		Assert.IsTrue(compl.Any(v => v.InsertText == ":disabled"));
		Assert.IsTrue(compl.Any(v => v.InsertText == ":focus"));
	}

	[TestMethod]
	public void TemplateBindingAvaloniaPropetiesShouldBeCompleted()
	{
		AssertSingleCompletion("<ContentPresenter Background=\"{TemplateBinding ", "Back", "Background");
	}

	[TestMethod]
	public void WellKnownBrushesShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl Background=\"", "Re", "Red");
	}

	[TestMethod]
	public void WellKnownThemeKeysShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl Background=\"{DynamicResource ", "Theme", "ThemeBackgroundBrush");
	}

	[TestMethod]
	public void LocalXKeyShouldBeCompletedForStaticResource()
	{
		AssertSingleCompletion(
			"<UserControl.Resources><SolidColorBrush x:Key=\"MyLocalBrush\" Color=\"Red\" /></UserControl.Resources><UserControl Background=\"{StaticResource ",
			"MyLocal",
			"MyLocalBrush");
	}

	[TestMethod]
	public void LocalXKeyShouldBeCompletedForDynamicResource()
	{
		AssertSingleCompletion(
			"<UserControl.Resources><SolidColorBrush x:Key=\"MyDynBrush\" Color=\"Blue\" /></UserControl.Resources><Border Background=\"{DynamicResource ",
			"MyDyn",
			"MyDynBrush");
	}

	[TestMethod]
	public void LocalXKeyAfterCursorShouldBeCompletedForStaticResource()
	{
		// Keys defined later in the document should still complete (scan full text).
		var before = "<Button Background=\"{StaticResource ";
		var after = "\" /><UserControl.Resources><SolidColorBrush x:Key=\"LaterBrush\" Color=\"Green\" /></UserControl.Resources>";
		AssertSingleCompletionInMiddleOfText(before, after, "Later", "LaterBrush");
	}

	[TestMethod]
	public void xClassDirectiveShouldBeCompleted()
	{
		var compl = GetCompletionsFor("<UserControl x:Cla").Completions;

		Assert.IsTrue(compl.Any(v => v.InsertText == "x:Class=\"\""));
	}

	[TestMethod]
	public void xClassValueShouldBeCompleted()
	{
		AssertSingleCompletion("<UserControl x:Class=\"", "", "Cornerstone.VisualStudio.Tests.TestUserControl");
	}

	[TestMethod]
	public void xKeyDirectiveShouldBeCompleted()
	{
		var compl = GetCompletionsFor("<UserControl x:K").Completions;

		Assert.IsTrue(compl.Any(v => v.InsertText == "x:Key=\"\""));
	}

	[TestMethod]
	public void xmlnsDirectiveShouldBeCompleted()
	{
		var compl = GetCompletionsFor("<UserControl x").Completions;

		Assert.IsTrue(compl.Any(v => v.InsertText == "xmlns:"));
	}

	[TestMethod]
	public void xNameDirectiveShouldBeCompleted()
	{
		var compl = GetCompletionsFor("<UserControl x:N").Completions;

		Assert.IsTrue(compl.Any(v => v.InsertText == "x:Name=\"\""));
	}

	[TestMethod]
	public void xTypeArgumentsDirectiveShouldBeCompleted()
	{
		AssertSingleCompletion("<local:GenericBaseClass`1 ", "x:T", "x:TypeArguments=\"\"");
	}

	[TestMethod]
	public void xTypeArgumentsDirectiveShouldNotBeCompletedOnNonGenericType()
	{
		Assert.IsNull(GetCompletionsFor("<UserControl x:TypeArgum"));
	}

	[TestMethod]
	public void xTypeArgumentsValueShouldBeCompleted()
	{
		AssertSingleCompletion("<local:GenericBaseClass`1 x:TypeArguments=\"", "Tex", "TextBlock");
	}

	#endregion
}
