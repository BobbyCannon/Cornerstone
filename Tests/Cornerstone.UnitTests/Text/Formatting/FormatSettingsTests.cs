#region References

using Cornerstone.Text;
using Cornerstone.Text.Formatting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Text.Formatting;

[TestClass]
public class FormatSettingsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void LineEndingModeCrlfIgnoresDocument()
	{
		var options = new DocumentFormatOptions { LineEndingMode = LineEndingMode.Crlf };
		var buffer = new StringGapBuffer("{\n}");
		AreEqual("\r\n", options.ResolveNewLine(buffer));
	}

	[TestMethod]
	public void LineEndingModeLfIgnoresDocument()
	{
		var options = new DocumentFormatOptions { LineEndingMode = LineEndingMode.Lf };
		var buffer = new StringGapBuffer("{\r\n}");
		AreEqual("\n", options.ResolveNewLine(buffer));
	}

	[TestMethod]
	public void ResolveMapsExtensionsToProfiles()
	{
		var settings = new FormatSettings();
		AreEqual(settings.Json, settings.Resolve("json"));
		AreEqual(settings.Json, settings.Resolve(".JSON"));
		AreEqual(settings.CSharp, settings.Resolve("cs"));
		AreEqual(settings.Xml, settings.Resolve("xml"));
		AreEqual(settings.Xml, settings.Resolve("csproj"));
		AreEqual(settings.Xaml, settings.Resolve("cxaml"));
		AreEqual(settings.Xaml, settings.Resolve("xaml"));
		AreEqual(settings.Html, settings.Resolve("html"));
		AreEqual(settings.Html, settings.Resolve("htm"));
		AreEqual(settings.Markdown, settings.Resolve("md"));
		AreEqual(settings.PowerShell, settings.Resolve("ps1"));
		IsNull(settings.Resolve("unknown"));
		IsNull(settings.Resolve(null));
	}

	[TestMethod]
	public void XamlAndHtmlProfilesDifferFromXml()
	{
		var settings = new FormatSettings();
		IsTrue(settings.Xaml.AttributesOnNewLine);
		IsFalse(settings.Xml.AttributesOnNewLine);
		AreEqual(XmlFormatDialect.Html, settings.Html.Dialect);
		AreEqual(XmlFormatDialect.Xml, settings.Xml.Dialect);
		AreEqual(XmlFormatDialect.Xml, settings.Xaml.Dialect);
	}

	#endregion
}