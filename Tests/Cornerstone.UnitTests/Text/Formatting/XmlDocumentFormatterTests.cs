#region References

using Cornerstone.Collections;
using Cornerstone.Text;
using Cornerstone.Text.Formatting;
using Cornerstone.Text.Parsing;
using Cornerstone.Text.Parsing.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Text.Formatting;

[TestClass]
public class XmlDocumentFormatterTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AttributesOnNewLine()
	{
		var expected = "<Person\n\tName=\"John\"\n\tAge=\"21\" />";
		AreEqual(expected, Format("<Person Name=\"John\" Age=\"21\"/>", new XmlFormatOptions
		{
			NewLine = "\n",
			AttributesOnNewLine = true
		}));
	}

	[TestMethod]
	public void AttributesStayOnOneLineByDefault()
	{
		AreEqual("<Person Name=\"John\" Age=\"21\" />", Format("<Person Name=\"John\" Age=\"21\"/>", new XmlFormatOptions { NewLine = "\n" }));
	}

	[TestMethod]
	public void CommentsAndCDataArePreserved()
	{
		var xml = "<root><!-- keep --><![CDATA[ a < b ]]></root>";
		var formatted = Format(xml, new XmlFormatOptions { NewLine = "\n" });
		IsTrue(formatted.Contains("<!-- keep -->"));
		IsTrue(formatted.Contains("<![CDATA[ a < b ]]>"));
	}

	[TestMethod]
	public void DetectsWindowsNewLineFromDocument()
	{
		var formatted = Format("<root>\r\n<a/>\r\n</root>", new XmlFormatOptions());
		IsTrue(formatted.Contains("\r\n"));
	}

	[TestMethod]
	public void HtmlPreKeepsInnerWhitespace()
	{
		var html = "<pre>\n  keep  me\n</pre>";
		var formatted = Format(html, new XmlFormatOptions
		{
			NewLine = "\n",
			Dialect = XmlFormatDialect.Html
		});
		IsTrue(formatted.Contains("  keep  me"));
	}

	[TestMethod]
	public void HtmlVoidBrDoesNotGetClosingTag()
	{
		AreEqual("<div>\n\t<br />\n</div>", Format("<div><br/></div>", new XmlFormatOptions
		{
			NewLine = "\n",
			Dialect = XmlFormatDialect.Html
		}));
	}

	[TestMethod]
	public void InvalidFragmentStillReturnsString()
	{
		var formatted = Format("<root>", new XmlFormatOptions { NewLine = "\n" });
		IsFalse(string.IsNullOrEmpty(formatted));
	}

	[TestMethod]
	public void MinifyRemovesIgnorableWhitespace()
	{
		AreEqual("<root><a/></root>", Format("<root>\n  <a/>\n</root>", new XmlFormatOptions
		{
			Minify = true,
			NewLine = "\n"
		}));
	}

	[TestMethod]
	public void MixedContentIndentsChildElements()
	{
		var formatted = Format("<p>Hello<b>x</b></p>", new XmlFormatOptions { NewLine = "\n" });
		IsTrue(formatted.Contains("Hello"));
		IsTrue(formatted.Contains("<b>x</b>"));
		IsTrue(formatted.Contains("\n"));
	}

	[TestMethod]
	public void NestedElementsPrettyPrint()
	{
		var expected = "<root>\n\t<child />\n</root>";
		AreEqual(expected, Format("<root><child/></root>", new XmlFormatOptions { NewLine = "\n" }));
	}

	[TestMethod]
	public void PrettyPrintIsIdempotent()
	{
		var options = new XmlFormatOptions { NewLine = "\n" };
		var first = Format("<root><a/><b>x</b></root>", options);
		AreEqual(first, Format(first, options));
	}

	[TestMethod]
	public void SelfClosingHonorsSpaceOption()
	{
		AreEqual("<a />", Format("<a/>", new XmlFormatOptions { NewLine = "\n" }));
		AreEqual("<a/>", Format("<a/>", new XmlFormatOptions { NewLine = "\n", SpaceBeforeEmptyClose = false }));
	}

	[TestMethod]
	public void TextOnlyStaysOnOneLine()
	{
		AreEqual("<title>Hi</title>", Format("<title>Hi</title>", new XmlFormatOptions { NewLine = "\n" }));
	}

	[TestMethod]
	public void XmlSpacePreserveKeepsInnerWhitespace()
	{
		var xml = "<p xml:space=\"preserve\">\n  Hi\n</p>";
		var formatted = Format(xml, new XmlFormatOptions { NewLine = "\n" });
		IsTrue(formatted.Contains("\n  Hi\n"));
	}

	private string Format(string xml, XmlFormatOptions options)
	{
		var buffer = new StringGapBuffer(xml);
		var tokenizer = new XmlTokenizer(buffer, new SpeedyQueue<Token>());
		IsTrue(DocumentFormatter.TryFormat(tokenizer, buffer, options, out var formatted));
		return formatted;
	}

	#endregion
}