#region References

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests;

[TestClass]
public class StyleClassCompletionTests : XamlCompletionTestBase
{
	#region Methods

	[TestMethod]
	public void TextBlockPrefixClaOffersClassesAttribute()
	{
		var set = GetCompletionsFor("<TextBlock Cla");
		Assert.IsNotNull(set);
		var classes = set.Completions.Single(c => c.DisplayText == "Classes");
		Assert.AreEqual("Classes=\"\"", classes.InsertText);
	}

	[TestMethod]
	public void TextBlockOtherPrefixDoesNotOfferClassesAttribute()
	{
		var set = GetCompletionsFor("<TextBlock Fore");
		Assert.IsNotNull(set);
		Assert.IsFalse(set.Completions.Any(c => c.DisplayText == "Classes"));
	}

	[TestMethod]
	public void ClassesAttributeCompletesSelectorClassNames()
	{
		var set = GetCompletionsFor(
			"<Style Selector=\"Button.ControlCard\" /><Button Classes=\"");
		Assert.IsNotNull(set);
		Assert.IsTrue(set.Completions.Any(c => c.InsertText == "ControlCard"));
	}

	[TestMethod]
	public void ClassesAttributeCompletesBetweenExistingQuotes()
	{
		var set = GetCompletionsFor(
			"<Style Selector=\"Button.ControlCard\" /><Button Classes=\"",
			"\" />");
		Assert.IsNotNull(set);
		Assert.IsTrue(set.Completions.Any(c => c.InsertText == "ControlCard"));
	}

	[TestMethod]
	public void ClassesAttributeFiltersByTypedPrefix()
	{
		var set = GetCompletionsFor(
			"<Style Selector=\"Button.ControlCard\" /><Style Selector=\"Border.FeatureCard\" /><Button Classes=\"Con");
		Assert.IsNotNull(set);
		Assert.IsTrue(set.Completions.Any(c => c.InsertText == "ControlCard"));
		Assert.IsFalse(set.Completions.Any(c => c.InsertText == "FeatureCard"));
	}

	[TestMethod]
	public void SelectorDotCompletesDocumentStyleClasses()
	{
		var set = GetCompletionsFor(
			"<Style Selector=\"Button.ControlCard\" /><Style Selector=\"Button.");
		Assert.IsNotNull(set);
		Assert.IsTrue(set.Completions.Any(c => c.InsertText == "ControlCard"));
	}

	[TestMethod]
	public void ClassesAttributeCompletesAdditionalStyleClassNames()
	{
		var set = GetCompletionsFor(
			"<TextBox Classes=\"",
			"\" />",
			["Search", "ClearButton"]);
		Assert.IsNotNull(set);
		Assert.IsTrue(set.Completions.Any(c => c.InsertText == "Search"));
		Assert.IsTrue(set.Completions.Any(c => c.InsertText == "ClearButton"));
	}

	#endregion
}
