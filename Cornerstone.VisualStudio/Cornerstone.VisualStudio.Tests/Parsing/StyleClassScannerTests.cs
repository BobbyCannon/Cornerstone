#region References

using System.Linq;
using Cornerstone.VisualStudio.Core.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests.Parsing;

[TestClass]
public class StyleClassScannerTests
{
	#region Methods

	[TestMethod]
	public void FindSelectorClassOffsetSkipsCaretMatch()
	{
		var xml = "<Style Selector=\"Button.ControlCard\" /><Button Classes=\"ControlCard\" />";
		var definition = xml.IndexOf("ControlCard");
		var usage = xml.LastIndexOf("ControlCard");
		Assert.AreEqual(definition, StyleClassScanner.FindSelectorClassOffset(xml, "ControlCard", usage + 2));
		Assert.AreEqual(-1, StyleClassScanner.FindSelectorClassOffset(xml, "ControlCard", definition + 2));
	}

	[TestMethod]
	public void EnumerateSelectorClassOffsetsFindsDottedClasses()
	{
		var xml = "<Style Selector=\"Button.ControlCard\" /><Style Selector='Border.FeatureCard' />";
		var controlCard = StyleClassScanner.EnumerateSelectorClassOffsets(xml, "ControlCard").ToArray();
		var featureCard = StyleClassScanner.EnumerateSelectorClassOffsets(xml, "FeatureCard").ToArray();
		Assert.AreEqual(1, controlCard.Length);
		Assert.AreEqual(xml.IndexOf("ControlCard"), controlCard[0]);
		Assert.AreEqual(1, featureCard.Length);
		Assert.AreEqual(xml.IndexOf("FeatureCard"), featureCard[0]);
	}

	[TestMethod]
	public void FindClassNamesReturnsDistinctSelectorClasses()
	{
		var xml = "<Style Selector=\"Button.ControlCard\" /><Style Selector=\"Button.ControlCard:pointerover\" /><Style Selector='Border.FeatureCard' />";
		var names = StyleClassScanner.FindClassNames(xml);
		Assert.AreEqual(2, names.Count);
		Assert.AreEqual("ControlCard", names[0]);
		Assert.AreEqual("FeatureCard", names[1]);
	}

	[TestMethod]
	public void FindClassNamesFindsCaretClassSelectors()
	{
		var names = StyleClassScanner.FindClassNames("<Style Selector=\"^.Search\" /><Style Selector=\"^.ClearButton[AcceptsReturn=False]\" />");
		Assert.IsTrue(names.Contains("Search"));
		Assert.IsTrue(names.Contains("ClearButton"));
	}

	[TestMethod]
	public void FindClassNamesIgnoresAttachedPropertyDots()
	{
		var names = StyleClassScanner.FindClassNames("<Style Selector=\"^[(Theme.Color)=None]\" />");
		Assert.AreEqual(0, names.Count);
	}

	[TestMethod]
	public void FindClassNamesIncludesClassesAttributeUsages()
	{
		var names = StyleClassScanner.FindClassNames("<TextBox Classes=\"Search ClearButton\" />");
		Assert.IsTrue(names.Contains("Search"));
		Assert.IsTrue(names.Contains("ClearButton"));
	}

	[TestMethod]
	public void FindSelectorClassOffsetIgnoresUnknownClass()
	{
		Assert.AreEqual(-1, StyleClassScanner.FindSelectorClassOffset("<Style Selector=\"Button\" />", "ControlCard", 0));
	}

	#endregion
}
