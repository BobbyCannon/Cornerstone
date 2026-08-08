#region References

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Xunit;

#endregion

namespace Cornerstone.VisualStudio.Tests;

public class KnownBugTests : XamlCompletionTestBase
{
	#region Methods

	[Fact]
	public void CompletionShouldRecognizeDoubleTransition()
	{
		// Non-leaf types complete as paired tags (caret between open/close).
		AssertSingleCompletion("<", "DoubleTra", "DoubleTransition></DoubleTransition>");
	}

	[Fact]
	public void CompletionShouldShowPropertiesFromBaseClasses()
	{
		AssertSingleCompletion("<local:EmptyClassDerivedFromGenericClassWithDouble ", "Generic", "GenericProperty=\"\"");
	}

	[Fact]
	public void InterfacePropertiesShouldNotBeShown()
	{
		Assert.DoesNotContain(GetCompletionsFor("<Button ").Completions, c => c.InsertText.Contains("IStyleable"));
	}

	[Theory]
	[InlineData("Item")]
	public void NonStylePropertiesShouldNotBeShownOnStyle(string propertyName)
	{
		var comp = GetCompletionsFor("<UserControl><UserControl.Styles><Style><Style." +
			propertyName.Substring(0, 1));
		if (comp == null)
		{
			return;
		}
		Assert.DoesNotContain(comp.Completions, c => c.InsertText.StartsWith(propertyName));
	}

	[Fact]
	public void OnlyAttachedPropertiesShouldBeShownInDottedXamlTag()
	{
		var gridAttachedProperties = new HashSet<string>(typeof(Grid)
			.GetFields(BindingFlags.Public | BindingFlags.Static).Where(p =>
				p.FieldType.IsConstructedGenericType
				&& (p.FieldType.GetGenericTypeDefinition() == typeof(AttachedProperty<>)))
			.Select(p => p.Name.Replace("Property", "")));
		var completions = GetCompletionsFor("<UserControl><Grid.").Completions;
		foreach (var c in completions)
		{
			Assert.True(gridAttachedProperties.Contains(c.DisplayText), "Non-attached property " + c.DisplayText);
		}

		foreach (var a in gridAttachedProperties)
		{
			Assert.True(completions.Any(c => c.DisplayText == a), "Attached property " + a + " is not shown");
		}
	}

	[Fact]
	public void RowDefinitionsDirtyShouldNotBeShown()
	{
		AssertSingleCompletion("<UserControl><Grid ", "Row", "RowDefinitions=\"\"");
	}

	[Theory]
	[InlineData("Animations")]
	public void StylePropertiesShouldBeShown(string propertyName)
	{
		AssertSingleCompletion("<UserControl><UserControl.Styles><Style><Style.", propertyName.Substring(0, 1),
			propertyName);
	}

	#endregion
}