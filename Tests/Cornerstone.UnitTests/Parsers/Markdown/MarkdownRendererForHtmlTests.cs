#region References

using Cornerstone.Parsers.Markdown;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Parsers.Markdown;

[TestClass]
public class MarkdownRendererForHtmlTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void BoldAndItalic()
	{
		Process("*Italic*", "<em>Italic</em>");
		Process("_Italic_", "<em>Italic</em>");
		Process("**Bold**", "<strong>Bold</strong>");
		Process("__Bold__", "<strong>Bold</strong>");
		Process("***BoldItalic***", "<em><strong>BoldItalic</strong></em>");
		Process("___BoldItalic___", "<em><strong>BoldItalic</strong></em>");
	}
	
	[TestMethod]
	public void Headers()
	{
		Process("# Header 1", "<h1>Header 1</h1>");
		Process("## Header 2", "<h2>Header 2</h2>");
		Process("### Header 3", "<h3>Header 3</h3>");
		Process("#### Header 4", "<h4>Header 4</h4>");
		Process("##### Header 5", "<h5>Header 5</h5>");
		Process("###### Header 6", "<h6>Header 6</h6>");
	}
	
	[TestMethod]
	public void SampleWithEdgeCases()
	{
		Process(
			"""
			# Header 1
			---
			This #header should not header.
			
			1. Item one
			1. Item two
			
			```blah
			foo bar
			```
			""",
			"""
			<h1>Header 1</h1>
			---
			This #header should not header.
			
			1. Item one
			1. Item two
			
			<pre><code>foo bar</code></pre>
			"""
		);
	}

	private void Process(string markdown, string expected)
	{
		var parser = new MarkdownRendererForHtml();
		var actual = parser.ToHtml(markdown);
		AreEqual(expected, actual);
	}

	#endregion
}