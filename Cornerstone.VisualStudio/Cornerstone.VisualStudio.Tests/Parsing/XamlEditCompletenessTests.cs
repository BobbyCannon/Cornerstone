using Cornerstone.VisualStudio.Core.Parsing;
using Xunit;

namespace Cornerstone.VisualStudio.Tests.Parsing;

public class XamlEditCompletenessTests
{
	[Theory]
	[InlineData("<")]
	[InlineData("  <  ")]
	[InlineData("<Button")]
	[InlineData("<Button ")]
	[InlineData("<Button Width=\"")]
	[InlineData("<Button Width='")]
	[InlineData("<Button Width=\"100")]
	[InlineData("</")]
	[InlineData("</Grid")]
	[InlineData("<!-- comment")]
	[InlineData("<![CDATA[ stuff")]
	public void IsClearlyIncompleteTrueForMidEdit(string xaml)
	{
		Assert.True(XamlEditCompleteness.IsClearlyIncomplete(xaml));
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("<Button />")]
	[InlineData("<Button Width=\"100\" />")]
	[InlineData("<Button Width='100' />")]
	[InlineData("<Button></Button>")]
	[InlineData("<Grid>\n  <TextBlock Text=\"Hi\" />\n</Grid>")]
	[InlineData("<!-- done -->")]
	[InlineData("<![CDATA[x]]>")]
	// Unclosed *elements* after a finished tag are not "mid-edit tag" — still send to host.
	[InlineData("<Grid>")]
	[InlineData("<Grid><Button />")]
	public void IsClearlyIncompleteFalseWhenLastTagLooksFinished(string xaml)
	{
		Assert.False(XamlEditCompleteness.IsClearlyIncomplete(xaml));
	}
}
