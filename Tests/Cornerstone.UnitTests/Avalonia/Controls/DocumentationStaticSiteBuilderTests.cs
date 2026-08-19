#region References

using System.IO;
using Cornerstone.Avalonia.Documentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Controls;

[TestClass]
public class DocumentationStaticSiteBuilderTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ExportWritesThemeSiteAndLinkedPages()
	{
		var root = Path.Combine(Path.GetTempPath(), "CornerstoneDocsSite-" + Path.GetRandomFileName());
		Directory.CreateDirectory(root);
		try
		{
			File.WriteAllText(Path.Combine(root, "Readme.md"), "# Home\n\nSee [Other](Other.md#heading).");
			Directory.CreateDirectory(Path.Combine(root, "Nested"));
			File.WriteAllText(Path.Combine(root, "Other.md"), "# Other\n\nBack to [home](Readme.md).");
			File.WriteAllText(Path.Combine(root, "Nested", "Readme.md"), "# Nested\n\nSee [Deep](Deep.md).");
			File.WriteAllText(Path.Combine(root, "Nested", "Deep.md"), "# Deep\n\nBack to [nested](Readme.md).");

			var catalog = DocumentationCatalog.FromDirectory(root, "Readme.md");
			var output = Path.Combine(root, "out");
			DocumentationStaticSiteBuilder.Export(catalog, output);

			var theme = File.ReadAllText(Path.Combine(output, "theme.css"));
			IsTrue(theme.Contains("--Background00: #FFFFFF;"));
			IsTrue(File.Exists(Path.Combine(output, "site.css")));
			IsTrue(File.Exists(Path.Combine(output, "site.js")));
			IsTrue(File.Exists(Path.Combine(output, "fonts", "OpenSans-Regular.ttf")));
			IsTrue(File.Exists(Path.Combine(output, "fonts", "DejaVuSansMono.ttf")));
			var siteCss = File.ReadAllText(Path.Combine(output, "site.css"));
			IsTrue(siteCss.Contains("font-family: \"Open Sans\""));
			IsTrue(siteCss.Contains("url(\"fonts/OpenSans-Regular.ttf\")"));
			IsTrue(siteCss.Contains("font-family: \"DejaVu Sans Mono\""));
			IsTrue(siteCss.Contains("font-size: calc(var(--ControlFontSize) * 2.6);"));
			IsTrue(siteCss.Contains("gap: 10px;"));
			IsTrue(siteCss.Contains("content: \"• \";"));

			var index = File.ReadAllText(Path.Combine(output, "index.html"));
			IsTrue(index.Contains("<h1 id=\"home\">Home</h1>"));
			IsTrue(index.Contains("href=\"Other.html#heading\""));
			IsTrue(index.Contains("href=\"theme.css\""));
			IsTrue(index.Contains("id=\"theme-toggle\""));
			IsTrue(index.Contains("id=\"density\""));
			IsTrue(index.Contains("id=\"theme-color\""));
			IsTrue(index.Contains("data-theme-color=\"Blue\""));
			IsTrue(index.Contains("data-density=\"normal\""));
			IsTrue(File.ReadAllText(Path.Combine(output, "theme.css")).Contains(":root[data-density=\"large\"]"));
			IsTrue(index.Contains("src=\"site.js\""));

			var other = File.ReadAllText(Path.Combine(output, "Other.html"));
			IsTrue(other.Contains("href=\"index.html\""));
			IsTrue(other.Contains("<title>Other</title>"));
			IsTrue(other.Contains("class=\"breadcrumbs\""));
			IsTrue(other.Contains("aria-current=\"page\">Other</li>"));

			var nestedIndex = File.ReadAllText(Path.Combine(output, "Nested", "index.html"));
			IsTrue(nestedIndex.Contains("href=\"../index.html\">Documentation</a>"));
			IsTrue(nestedIndex.Contains("aria-current=\"page\">Nested</li>"));

			var deep = File.ReadAllText(Path.Combine(output, "Nested", "Deep.html"));
			IsTrue(deep.Contains("href=\"../index.html\">Documentation</a>"));
			IsTrue(deep.Contains("href=\"index.html\">Nested</a>"));
			IsTrue(deep.Contains("aria-current=\"page\">Deep</li>"));
		}
		finally
		{
			if (Directory.Exists(root))
			{
				Directory.Delete(root, true);
			}
		}
	}

	[TestMethod]
	public void ToHtmlRelativePathMapsReadmeToIndex()
	{
		AreEqual("index.html", DocumentationStaticSiteBuilder.ToHtmlRelativePath("Readme.md"));
		AreEqual("Agent/index.html", DocumentationStaticSiteBuilder.ToHtmlRelativePath("Agent/Readme.md"));
		AreEqual("Agent/Sync.html", DocumentationStaticSiteBuilder.ToHtmlRelativePath("Agent/Sync.md"));
	}

	#endregion
}
