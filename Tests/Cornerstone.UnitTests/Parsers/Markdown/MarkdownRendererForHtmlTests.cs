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
		Process("*Italic*", "<p><em>Italic</em></p>");
		Process("_Italic_", "<p><em>Italic</em></p>");
		Process("**Bold**", "<p><strong>Bold</strong></p>");
		Process("__Bold__", "<p><strong>Bold</strong></p>");
		Process("***BoldItalic***", "<p><em><strong>BoldItalic</strong></em></p>");
		Process("___BoldItalic___", "<p><em><strong>BoldItalic</strong></em></p>");
	}

	[TestMethod]
	public void BlockQuoteStripsMarkerAndKeepsInlines()
	{
		Process(
			"> hello **world**",
			"<blockquote>hello <strong>world</strong></blockquote>");
	}

	[TestMethod]
	public void CodeBlockEscapesAndIncludesLanguage()
	{
		Process(
			"""
			```csharp
			var x = 1 < 2;
			```
			""",
			"<div class=\"code-block\"><div class=\"code-block-header\">csharp</div><pre><code class=\"language-csharp\">var x = 1 &lt; 2;</code></pre></div>");
	}

	[TestMethod]
	public void EscapesHtmlInParagraphs()
	{
		Process("use <script> & tags", "<p>use &lt;script&gt; &amp; tags</p>");
	}

	[TestMethod]
	public void Headers()
	{
		Process("# Header 1", "<h1 id=\"header-1\">Header 1</h1>");
		Process("## Header 2", "<h2 id=\"header-2\">Header 2</h2>");
		Process("### Header 3", "<h3 id=\"header-3\">Header 3</h3>");
		Process("#### Header 4", "<h4 id=\"header-4\">Header 4</h4>");
		Process("##### Header 5", "<h5 id=\"header-5\">Header 5</h5>");
		Process("###### Header 6", "<h6 id=\"header-6\">Header 6</h6>");
	}

	[TestMethod]
	public void InlineCodeAndStrikethrough()
	{
		Process("use `foo` here", "<p>use <code>foo</code> here</p>");
		Process("~~gone~~", "<p><del>gone</del></p>");
	}

	[TestMethod]
	public void Links()
	{
		Process(
			"See [Other](Other.md#heading).",
			"<p>See <a href=\"Other.md#heading\">Other</a>.</p>");
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
			<h1 id="header-1">Header 1</h1>
			<hr />
			<p>This #header should not header.<br /><br />1. Item one<br />1. Item two</p>
			<div class="code-block"><div class="code-block-header">blah</div><pre><code class="language-blah">foo bar</code></pre></div>
			"""
		);
	}

	[TestMethod]
	public void TableWithHeaderAndLink()
	{
		Process(
			"""
			| Name | Link |
			|------|------|
			| A | [Go](Keystone.md) |
			""",
			"<table><thead><tr><th><strong>Name</strong></th><th><strong>Link</strong></th></tr></thead><tbody><tr><td>A</td><td><a href=\"Keystone.md\">Go</a></td></tr></tbody></table>");
	}

	[TestMethod]
	public void UnorderedListProjectsItems()
	{
		Process(
			"""
			- one
			- **two**
			""",
			"<ul><li>one</li><li><strong>two</strong></li></ul>");
	}

	private void Process(string markdown, string expected)
	{
		var parser = new MarkdownRendererForHtml();
		var actual = parser.ToHtml(markdown);
		AreEqual(expected, actual);
	}

	#endregion
}
