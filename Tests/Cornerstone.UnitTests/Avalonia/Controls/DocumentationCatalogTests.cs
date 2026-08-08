#region References

using System.Linq;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Sample.Tabs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Controls;

[TestClass]
public class DocumentationCatalogTests
{
	#region Methods

	[TestMethod]
	public void FromAssemblyResourcesMapsSampleMarkdownPaths()
	{
		var catalog = DocumentationCatalog.FromAssemblyResources(
			typeof(TabDocumentation).Assembly,
			TabDocumentation.DocumentationResourcePrefix,
			"Readme.md");

		Assert.IsTrue(catalog.Documents.Count > 0, "Expected embedded documentation resources in Cornerstone.Sample.");
		Assert.IsNotNull(catalog.Entry);
		Assert.AreEqual("Readme.md", catalog.Entry.Id, ignoreCase: true);
		Assert.IsTrue(catalog.TryGet("Keystone.md", out var keystone));
		Assert.IsFalse(string.IsNullOrWhiteSpace(keystone.ReadAllText()));

		// Sample intentionally omits agent-oriented / WIP trees (see Cornerstone.Sample.csproj).
		Assert.IsFalse(catalog.TryGet("Agent/Sync.md", out _),
			"Agent/ docs should not be embedded in Cornerstone.Sample.");
		Assert.IsFalse(catalog.TryGet("Todo/Sync.md", out _),
			"Todo/ docs should not be embedded in Cornerstone.Sample.");
	}

	[TestMethod]
	public void FromAssemblyResourcesResolvesRelativeLinks()
	{
		var catalog = DocumentationCatalog.FromAssemblyResources(
			typeof(TabDocumentation).Assembly,
			TabDocumentation.DocumentationResourcePrefix,
			"Readme.md");

		Assert.IsTrue(catalog.TryResolve("Readme.md", "Keystone.md", out var document, out var fragment));
		Assert.AreEqual("Keystone.md", document.Id, ignoreCase: true);
		Assert.IsNull(fragment);

		Assert.IsTrue(catalog.TryResolve("Readme.md", "Keystone.md#structure", out document, out fragment));
		Assert.AreEqual("Keystone.md", document.Id, ignoreCase: true);
		Assert.AreEqual("structure", fragment);
	}

	[TestMethod]
	public void TryResolveHandlesNestedPathsAndParentSegments()
	{
		// Nested folders are not part of the Sample embed set; exercise path resolution in isolation.
		var catalog = new DocumentationCatalog(
		[
			new DocumentationDocument("Readme.md", "Readme.md", static () => "# Root"),
			new DocumentationDocument("Keystone.md", "Keystone.md", static () => "# Keystone"),
			new DocumentationDocument("Agent/Sync.md", "Agent/Sync.md", static () => "# Sync")
		], "Readme.md");

		Assert.IsTrue(catalog.TryResolve("Readme.md", "Agent/Sync.md", out var document, out var fragment));
		Assert.AreEqual("Agent/Sync.md", document.Id, ignoreCase: true);
		Assert.IsNull(fragment);

		Assert.IsTrue(catalog.TryResolve("Agent/Sync.md", "../Keystone.md#what-it-is", out document, out fragment));
		Assert.AreEqual("Keystone.md", document.Id, ignoreCase: true);
		Assert.AreEqual("what-it-is", fragment);
	}

	[TestMethod]
	public void SampleAssemblyEmbedsDocumentationWithSlashLogicalNames()
	{
		var names = typeof(TabDocumentation).Assembly.GetManifestResourceNames()
			.Where(n => n.StartsWith("Documents/Cornerstone/", System.StringComparison.OrdinalIgnoreCase)
				&& n.EndsWith(".md", System.StringComparison.OrdinalIgnoreCase))
			.ToArray();

		Assert.IsTrue(names.Length > 0, "No Documents/Cornerstone/*.md embedded resources found.");
		Assert.IsTrue(names.Any(n => n.Equals("Documents/Cornerstone/Readme.md", System.StringComparison.OrdinalIgnoreCase)));
		Assert.IsTrue(names.Any(n => n.Equals("Documents/Cornerstone/Keystone.md", System.StringComparison.OrdinalIgnoreCase)),
			"Expected LogicalName with '/' separators (Documents/Cornerstone/...), not dotted folder segments.");

		// Confirm intentional exclusions stay out of the Sample assembly.
		Assert.IsFalse(names.Any(n => n.Contains("/Agent/", System.StringComparison.OrdinalIgnoreCase)
			|| n.EndsWith("/Agent", System.StringComparison.OrdinalIgnoreCase)));
		Assert.IsFalse(names.Any(n => n.Contains("/Todo/", System.StringComparison.OrdinalIgnoreCase)
			|| n.EndsWith("/Todo", System.StringComparison.OrdinalIgnoreCase)));
	}

	[TestMethod]
	public void FromResourceReadsManifestText()
	{
		var assembly = typeof(TabDocumentation).Assembly;
		const string resourceName = "Documents/Cornerstone/Readme.md";
		var document = DocumentationDocument.FromResource("Readme.md", "Readme.md", assembly, resourceName);
		var text = document.ReadAllText();
		Assert.IsFalse(string.IsNullOrWhiteSpace(text));
		Assert.IsTrue(text.Contains('#') || text.Length > 20);
	}

	#endregion
}
