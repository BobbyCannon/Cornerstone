#region References

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests;

[TestClass]
public class KnownBugTests : XamlCompletionTestBase
{
	#region Methods

	[TestMethod]
	public void CompletionShouldRecognizeDoubleTransition()
	{
		// Non-leaf types complete as paired tags (caret between open/close).
		AssertSingleCompletion("<", "DoubleTra", "DoubleTransition></DoubleTransition>");
	}

	[TestMethod]
	public void CompletionShouldShowPropertiesFromBaseClasses()
	{
		AssertSingleCompletion("<local:EmptyClassDerivedFromGenericClassWithDouble ", "Generic", "GenericProperty=\"\"");
	}

	[TestMethod]
	public void InterfacePropertiesShouldNotBeShown()
	{
		Assert.IsFalse(GetCompletionsFor("<Button ").Completions.Any(c => c.InsertText.Contains("IStyleable")));
	}

	[TestMethod]
	[DataRow("Item")]
	public void NonStylePropertiesShouldNotBeShownOnStyle(string propertyName)
	{
		var comp = GetCompletionsFor("<UserControl><UserControl.Styles><Style><Style." +
			propertyName.Substring(0, 1));
		if (comp == null)
		{
			return;
		}
		Assert.IsFalse(comp.Completions.Any(c => c.InsertText.StartsWith(propertyName)));
	}

	[TestMethod]
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
			Assert.IsTrue(gridAttachedProperties.Contains(c.DisplayText), "Non-attached property " + c.DisplayText);
		}

		foreach (var a in gridAttachedProperties)
		{
			Assert.IsTrue(completions.Any(c => c.DisplayText == a), "Attached property " + a + " is not shown");
		}
	}

	[TestMethod]
	public void RowDefinitionsDirtyShouldNotBeShown()
	{
		AssertSingleCompletion("<UserControl><Grid ", "Row", "RowDefinitions=\"\"");
	}

	[TestMethod]
	[DataRow("Animations")]
	public void StylePropertiesShouldBeShown(string propertyName)
	{
		AssertSingleCompletion("<UserControl><UserControl.Styles><Style><Style.", propertyName.Substring(0, 1),
			propertyName);
	}

	#endregion
}
