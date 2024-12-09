#region References

using Cornerstone.Automation.Web.Browsers;
using Cornerstone.Extensions;
using Cornerstone.Parsers.Html;
using Cornerstone.Parsers.Xml;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Parsers.Html;

[TestClass]
public class SyntaxColorExtensionsTests : CornerstoneUnitTest
{
	[TestMethod]
	public void Name()
	{
		var writer = new HtmlWriter();
		writer.Start();

		var details = EnumExtensions.GetAllEnumDetails<SyntaxColor>();
		foreach (var detail in details)
		{
			writer.WriteElement("div",
				detail.Value.Name,
				new XmlAttribute("class", $"syntax{detail.Key}")
				);
		}

		writer.Stop();

		var actual = writer.ToString();
		actual.Dump();

		if (EnableBrowserSamples)
		{
			using var browser = Chrome.AttachOrCreate();
			browser.AutoClose = false;
			browser.SetHtml(actual);
		}
	}
}