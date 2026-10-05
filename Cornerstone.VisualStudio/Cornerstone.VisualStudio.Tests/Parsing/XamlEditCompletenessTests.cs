using Cornerstone.VisualStudio.Core.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cornerstone.VisualStudio.Tests.Parsing;

[TestClass]
public class XamlEditCompletenessTests
{
	[TestMethod]
	[DataRow("<")]
	[DataRow("  <  ")]
	[DataRow("<Button")]
	[DataRow("<Button ")]
	[DataRow("<Button Width=\"")]
	[DataRow("<Button Width='")]
	[DataRow("<Button Width=\"100")]
	[DataRow("</")]
	[DataRow("</Grid")]
	[DataRow("<!-- comment")]
	[DataRow("<![CDATA[ stuff")]
	public void IsClearlyIncompleteTrueForMidEdit(string xaml)
	{
		Assert.IsTrue(XamlEditCompleteness.IsClearlyIncomplete(xaml));
	}

	[TestMethod]
	[DataRow("")]
	[DataRow("   ")]
	[DataRow("<Button />")]
	[DataRow("<Button Width=\"100\" />")]
	[DataRow("<Button Width='100' />")]
	[DataRow("<Button></Button>")]
	[DataRow("<Grid>\n  <TextBlock Text=\"Hi\" />\n</Grid>")]
	[DataRow("<!-- done -->")]
	[DataRow("<![CDATA[x]]>")]
	// Unclosed *elements* after a finished tag are not "mid-edit tag" — still send to host.
	[DataRow("<Grid>")]
	[DataRow("<Grid><Button />")]
	public void IsClearlyIncompleteFalseWhenLastTagLooksFinished(string xaml)
	{
		Assert.IsFalse(XamlEditCompleteness.IsClearlyIncomplete(xaml));
	}

	[TestMethod]
	[DataRow("<Button")]
	[DataRow("<Button Width=\"100\"")]
	[DataRow("<Grid>\n  <TextBlock")]
	[DataRow("</Text")]
	public void IsInsideOpenTagTrueInsideTag(string textBeforeCaret)
	{
		Assert.IsTrue(XamlEditCompleteness.IsInsideOpenTag(textBeforeCaret));
	}

	[TestMethod]
	[DataRow("")]
	[DataRow("<Button />")]
	[DataRow("<Button Width=\"100\">")]
	[DataRow("<TextBlock>Hello")]
	[DataRow("<Grid>\n  <TextBlock>Hi")]
	public void IsInsideOpenTagFalseInContentOrAfterTag(string textBeforeCaret)
	{
		Assert.IsFalse(XamlEditCompleteness.IsInsideOpenTag(textBeforeCaret));
	}

	[TestMethod]
	[DataRow("<TextBlock Text=\"Ab")]
	[DataRow("<TextBlock Text=")]
	[DataRow("<Button Content=\"Save")]
	[DataRow("<Window Title=\"Hi")]
	[DataRow("<TextBox Watermark=\"Search")]
	[DataRow("<TextBlock AutomationProperties.Name=\"Submit")]
	public void FreeTextAttributeValueDoesNotComplete(string textBeforeCaret)
	{
		Assert.IsTrue(XamlEditCompleteness.IsFreeTextAttributeValue(textBeforeCaret));
	}

	[TestMethod]
	[DataRow("<TextBlock Text=\"{Bind")]
	[DataRow("<TextBlock Foreground=\"R")]
	[DataRow("<TextBlock HorizontalAlignment=\"C")]
	[DataRow("<TextBlock Classes=\"Di")]
	[DataRow("<TextBlock Text")]
	[DataRow("<TextBlock Text=\"Hi\" F")]
	[DataRow("<TextBlock>Hello")]
	public void NonTextContextsStillComplete(string textBeforeCaret)
	{
		Assert.IsFalse(XamlEditCompleteness.IsFreeTextAttributeValue(textBeforeCaret));
	}

	[TestMethod]
	public void ChangeLooksLikeMarkupDetectsTagChars()
	{
		Assert.IsTrue(XamlEditCompleteness.ChangeLooksLikeMarkup("", ">"));
		Assert.IsTrue(XamlEditCompleteness.ChangeLooksLikeMarkup("<", ""));
		Assert.IsTrue(XamlEditCompleteness.ChangeLooksLikeMarkup("", "/"));
		Assert.IsFalse(XamlEditCompleteness.ChangeLooksLikeMarkup("", "x"));
		Assert.IsFalse(XamlEditCompleteness.ChangeLooksLikeMarkup("H", "i"));
	}
}
