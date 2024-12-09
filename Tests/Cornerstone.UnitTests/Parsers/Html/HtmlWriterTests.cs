#region References

using System;
using Cornerstone.Parsers.Html;
using Cornerstone.Parsers.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Parsers.Html;

[TestClass]
public class HtmlWriterTests : CornerstoneUnitTest
{
	[TestMethod]
	public void IndividualMethods()
	{
		var scenarios = new (Action<HtmlWriter> setup, string expected)[]
		{
			(x => x.Start(), "<html>\r\n\t<head>\r\n\t\t<style>\r\n\t\t\t*, *::before, *::after {\r\n\t\t\t\t-webkit-box-sizing: border-box;\r\n\t\t\t\t-moz-box-sizing: border-box;\r\n\t\t\t\tbox-sizing: border-box;\r\n\t\t\t}\r\n\t\t\thtml, body {\r\n\t\t\t\tbackground: #191919;\r\n\t\t\t\tcolor: #D8D8D8;\r\n\t\t\t\tfont-style: normal;\r\n\t\t\t\tfont-size: 16px;\r\n\t\t\t\tfont-size: 1rem;\r\n\t\t\t\tline-height: 1.5;\r\n\t\t\t}\r\n\t\t\th1 {\r\n\t\t\t\tfont-size: 25px;\r\n\t\t\t\tline-height: 1;\r\n\t\t\t\tmargin-block-start: 0.25em;\r\n\t\t\t\tmargin-block-end: 0.5em;\r\n\t\t\t\tmargin-inline-start: 0px;\r\n\t\t\t\tmargin-inline-end: 0px;\r\n\t\t\t}\r\n\t\t\th2 {\r\n\t\t\t\tfont-size: 24px;\r\n\t\t\t\tline-height: 1;\r\n\t\t\t\tmargin-block-start: 0.25em;\r\n\t\t\t\tmargin-block-end: 0.5em;\r\n\t\t\t\tmargin-inline-start: 0px;\r\n\t\t\t\tmargin-inline-end: 0px;\r\n\t\t\t}\r\n\t\t\th3 {\r\n\t\t\t\tfont-size: 23px;\r\n\t\t\t\tline-height: 1;\r\n\t\t\t\tmargin-block-start: 0.25em;\r\n\t\t\t\tmargin-block-end: 0.5em;\r\n\t\t\t\tmargin-inline-start: 0px;\r\n\t\t\t\tmargin-inline-end: 0px;\r\n\t\t\t}\r\n\t\t\th4 {\r\n\t\t\t\tfont-size: 22px;\r\n\t\t\t\tline-height: 1;\r\n\t\t\t\tmargin-block-start: 0.25em;\r\n\t\t\t\tmargin-block-end: 0.5em;\r\n\t\t\t\tmargin-inline-start: 0px;\r\n\t\t\t\tmargin-inline-end: 0px;\r\n\t\t\t}\r\n\t\t\th5 {\r\n\t\t\t\tfont-size: 21px;\r\n\t\t\t\tline-height: 1;\r\n\t\t\t\tmargin-block-start: 0.25em;\r\n\t\t\t\tmargin-block-end: 0.5em;\r\n\t\t\t\tmargin-inline-start: 0px;\r\n\t\t\t\tmargin-inline-end: 0px;\r\n\t\t\t}\r\n\t\t\th6 {\r\n\t\t\t\tfont-size: 20px;\r\n\t\t\t\tline-height: 1;\r\n\t\t\t\tmargin-block-start: 0.25em;\r\n\t\t\t\tmargin-block-end: 0.5em;\r\n\t\t\t\tmargin-inline-start: 0px;\r\n\t\t\t\tmargin-inline-end: 0px;\r\n\t\t\t}\r\n\t\t\t.syntaxDefault {\r\n\t\t\t\tcolor: #D8D8D8;\r\n\t\t\t}\r\n\t\t\t.syntaxArgument {\r\n\t\t\t\tcolor: #9CDCFE;\r\n\t\t\t}\r\n\t\t\t.syntaxCommand {\r\n\t\t\t\tcolor: #00FFFF;\r\n\t\t\t}\r\n\t\t\t.syntaxComment {\r\n\t\t\t\tcolor: #55B030;\r\n\t\t\t}\r\n\t\t\t.syntaxIdentifier {\r\n\t\t\t\tcolor: #44C8B0;\r\n\t\t\t}\r\n\t\t\t.syntaxKeyword {\r\n\t\t\t\tcolor: #4E9AD3;\r\n\t\t\t}\r\n\t\t\t.syntaxKeywordControl {\r\n\t\t\t\tcolor: #D8A0DF;\r\n\t\t\t}\r\n\t\t\t.syntaxKeywordMuted {\r\n\t\t\t\tcolor: #808080;\r\n\t\t\t}\r\n\t\t\t.syntaxLink {\r\n\t\t\t\tcolor: #0066FF;\r\n\t\t\t}\r\n\t\t\t.syntaxMember {\r\n\t\t\t\tcolor: #EE82EE;\r\n\t\t\t}\r\n\t\t\t.syntaxMethod {\r\n\t\t\t\tcolor: #DCDCAA;\r\n\t\t\t}\r\n\t\t\t.syntaxNumber {\r\n\t\t\t\tcolor: #B5CEA8;\r\n\t\t\t}\r\n\t\t\t.syntaxParameter {\r\n\t\t\t\tcolor: #78C4CF;\r\n\t\t\t}\r\n\t\t\t.syntaxParameterName {\r\n\t\t\t\tcolor: #78C4CF;\r\n\t\t\t}\r\n\t\t\t.syntaxString {\r\n\t\t\t\tcolor: #D69D85;\r\n\t\t\t}\r\n\t\t\t.syntaxVariable {\r\n\t\t\t\tcolor: #9CDCFE;\r\n\t\t\t}\r\n\t\t\ta {\r\n\t\t\t\tcolor: #0066FF\r\n\t\t\t}\r\n\t\t\tpre {\r\n\t\t\t\tbackground: #212121;\r\n\t\t\t\tpadding: 10px;\r\n\t\t\t\tfont-family: monospace, monospace;\r\n\t\t\t}\r\n\t\t\tblockquote {\r\n\t\t\t\tbackground: #252525;\r\n\t\t\t\tborder-left: 10px solid #323232;\r\n\t\t\t\tmargin: 10px;\r\n\t\t\t\tpadding: 10px;\r\n\t\t\t}\r\n\t\t</style>\r\n\t</head>\r\n\t<body>"),
			(x => x.Stop(), "</body>\r\n</html>\r\n"),
			(x => x.WriteElement("a", "Test", new XmlAttribute("href", "https://test.com")), "<a href=\"https://test.com\">Test</a>"),
			(x => x.WriteElement("span", "Blue", new XmlAttribute("class", "blue")), "<span class=\"blue\">Blue</span>")
		};

		foreach (var scenario in scenarios)
		{
			var writer = new HtmlWriter();
			scenario.setup(writer);
			var actual = writer.ToString();
			AreEqual(scenario.expected, actual, () => CopyToClipboard(actual));
		}
	}
}