#region References

using System;
using System.Linq;
using Cornerstone.Presentation.Documentation;
using Cornerstone.Sample.Tabs.Documentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation.Controls;

[TestClass]
public class DocumentationCatalogTests
{
	#region Methods

	[TestMethod]
	public void CreateBreadcrumbSegmentsSplitsDocumentId()
	{
		var segments = DocumentationReader.CreateBreadcrumbSegments("Agent/Sync.md");
		Assert.AreEqual(2, segments.Count);
		Assert.AreEqual("Agent", segments[0].Title);
		Assert.AreEqual("Agent", segments[0].Key);
		Assert.IsFalse(segments[0].IsCurrent);
		Assert.AreEqual("Sync", segments[1].Title);
		Assert.AreEqual("Agent/Sync.md", segments[1].Key);
		Assert.IsTrue(segments[1].IsCurrent);
	}

	[TestMethod]
	public void CreateBreadcrumbSegmentsKeepsDotsInFolderNames()
	{
		var segments = DocumentationReader.CreateBreadcrumbSegments(
			"Cornerstone/Cornerstone.Documentation/AppBootstrap.md");
		Assert.AreEqual(3, segments.Count);
		Assert.AreEqual("Cornerstone", segments[0].Title);
		Assert.AreEqual("Cornerstone.Documentation", segments[1].Title);
		Assert.AreEqual("Cornerstone/Cornerstone.Documentation", segments[1].Key);
		Assert.AreEqual("AppBootstrap", segments[2].Title);
		Assert.AreEqual("Cornerstone/Cornerstone.Documentation/AppBootstrap.md", segments[2].Key);
		Assert.IsTrue(segments[2].IsCurrent);
	}

	[TestMethod]
	public void FromAssemblyResourcesMapsSampleMarkdownPaths()
	{
		var catalog = DocumentationCatalog.FromAssemblyResources(
			typeof(TabDocumentation).Assembly,
			TabDocumentation.DocumentationResourcePrefix,
			"Readme.md");

		Assert.IsTrue(catalog.Documents.Count > 0, "Expected embedded documentation resources in Cornerstone.Sample.");
		Assert.IsNotNull(catalog.Entry);
		Assert.AreEqual("Readme.md", catalog.Entry.Id, true);
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
		Assert.AreEqual("Keystone.md", document.Id, true);
		Assert.IsNull(fragment);

		Assert.IsTrue(catalog.TryResolve("Readme.md", "Keystone.md#structure", out document, out fragment));
		Assert.AreEqual("Keystone.md", document.Id, true);
		Assert.AreEqual("structure", fragment);
	}

	[TestMethod]
	public void FromResourceReadsManifestText()
	{
		var assembly = typeof(TabDocumentation).Assembly;
		const string resourceName = "Documents/Cornerstone/Readme.md";
		var document = DocumentationDocument.FromResource("Readme.md", "Readme.md", assembly, resourceName);
		var text = document.ReadAllText();
		Assert.IsFalse(string.IsNullOrWhiteSpace(text));
		Assert.IsTrue(text.Contains('#') || (text.Length > 20));
	}

	[TestMethod]
	public void MatchesPrefixDoesNotTreatPartialFileNameAsFolder()
	{
		Assert.IsTrue(DocumentationCatalog.MatchesPrefix("Agent/Sync.md", "Agent"));
		Assert.IsFalse(DocumentationCatalog.MatchesPrefix("Agenda.md", "Agent"));
		Assert.IsTrue(DocumentationCatalog.MatchesPrefix("Controls", "Controls"));
		Assert.IsTrue(DocumentationCatalog.MatchesPrefix("Controls/Foo.md", "Controls/"));
	}

	[TestMethod]
	public void MatchesPrefixSupportsWildcards()
	{
		Assert.IsTrue(DocumentationCatalog.MatchesPrefix("Guides/Readme.md", "Guides/*"));
		Assert.IsTrue(DocumentationCatalog.MatchesPrefix("Guides/Sample/Foo.md", "Guides/*"));
		Assert.IsFalse(DocumentationCatalog.MatchesPrefix("Readme.md", "Guides/*"));
		Assert.IsTrue(DocumentationCatalog.MatchesPrefix("Lifecycle.md", "*.md"));
		Assert.IsFalse(DocumentationCatalog.MatchesPrefix("Agent/Sync.md", "*.md"));
		Assert.IsTrue(DocumentationCatalog.MatchesPrefix("Cornerstone/Cornerstone.Documentation/Readme.md",
			"Cornerstone/Cornerstone.Documentation/*.md"));
		Assert.IsFalse(DocumentationCatalog.MatchesPrefix("Cornerstone/Cornerstone.Documentation/Controls/Foo.md",
			"Cornerstone/Cornerstone.Documentation/*.md"));
		Assert.IsTrue(DocumentationCatalog.MatchesPrefix("Cornerstone/Cornerstone.Documentation/Controls/Foo.md",
			"Cornerstone/Cornerstone.Documentation/Controls"));
		Assert.IsTrue(DocumentationCatalog.MatchesPrefix("a/b/c.md", "**/c.md"));
		Assert.IsFalse(DocumentationCatalog.MatchesPrefix("Agenda.md", "Agen?.md"));
		Assert.IsTrue(DocumentationCatalog.MatchesPrefix("Agent.md", "Agen?.md"));
	}

	[TestMethod]
	public void SampleAssemblyEmbedsDocumentationWithSlashLogicalNames()
	{
		var names = typeof(TabDocumentation).Assembly.GetManifestResourceNames()
			.Where(n => n.StartsWith("Documents/Cornerstone/", StringComparison.OrdinalIgnoreCase)
				&& n.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
			.ToArray();

		Assert.IsTrue(names.Length > 0, "No Documents/Cornerstone/*.md embedded resources found.");
		Assert.IsTrue(names.Any(n => n.Equals("Documents/Cornerstone/Readme.md", StringComparison.OrdinalIgnoreCase)));
		Assert.IsTrue(names.Any(n => n.Equals("Documents/Cornerstone/Keystone.md", StringComparison.OrdinalIgnoreCase)),
			"Expected LogicalName with '/' separators (Documents/Cornerstone/...), not dotted folder segments.");

		// Confirm intentional exclusions stay out of the Sample assembly.
		Assert.IsFalse(names.Any(n => n.Contains("/Agent/", StringComparison.OrdinalIgnoreCase)
			|| n.EndsWith("/Agent", StringComparison.OrdinalIgnoreCase)));
		Assert.IsFalse(names.Any(n => n.Contains("/Todo/", StringComparison.OrdinalIgnoreCase)
			|| n.EndsWith("/Todo", StringComparison.OrdinalIgnoreCase)));
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
		Assert.AreEqual("Agent/Sync.md", document.Id, true);
		Assert.IsNull(fragment);

		Assert.IsTrue(catalog.TryResolve("Agent/Sync.md", "../Keystone.md#what-it-is", out document, out fragment));
		Assert.AreEqual("Keystone.md", document.Id, true);
		Assert.AreEqual("what-it-is", fragment);
	}

	[TestMethod]
	public void TryResolvePrefixPrefersReadmeThenFirstChild()
	{
		var catalog = new DocumentationCatalog(
		[
			new DocumentationDocument("Readme.md", "Readme.md", static () => "# Root"),
			new DocumentationDocument("Agent/Readme.md", "Agent/Readme.md", static () => "# Agent"),
			new DocumentationDocument("Agent/Sync.md", "Agent/Sync.md", static () => "# Sync")
		], "Readme.md");

		Assert.IsTrue(catalog.TryResolvePrefix("Agent", out var document));
		Assert.AreEqual("Agent/Readme.md", document.Id, true);
		Assert.IsTrue(catalog.TryResolvePrefix("Agent/Sync.md", out document));
		Assert.AreEqual("Agent/Sync.md", document.Id, true);
	}

	[TestMethod]
	public void WithIncludedEmptyReturnsSameCatalog()
	{
		var catalog = new DocumentationCatalog(
		[
			new DocumentationDocument("Readme.md", "Readme.md", static () => "# Root"),
			new DocumentationDocument("Agent/Sync.md", "Agent/Sync.md", static () => "# Sync")
		], "Readme.md");

		Assert.AreSame(catalog, catalog.WithIncluded(null));
		Assert.AreSame(catalog, catalog.WithIncluded([]));
		Assert.AreSame(catalog, catalog.WithIncluded(["  "]));
	}

	[TestMethod]
	public void WithIncludedFallsBackWhenEntryOmitted()
	{
		var catalog = new DocumentationCatalog(
		[
			new DocumentationDocument("Readme.md", "Readme.md", static () => "# Root"),
			new DocumentationDocument("Controls/Readme.md", "Controls/Readme.md", static () => "# Controls")
		], "Readme.md");

		var included = catalog.WithIncluded(["Controls"]);
		Assert.AreEqual(1, included.Documents.Count);
		Assert.AreEqual("Controls/Readme.md", included.Entry.Id, true);
	}

	[TestMethod]
	public void WithIncludedKeepsPrefixMatchesNotAgenda()
	{
		var catalog = new DocumentationCatalog(
		[
			new DocumentationDocument("Readme.md", "Readme.md", static () => "# Root"),
			new DocumentationDocument("Controls/Foo.md", "Controls/Foo.md", static () => "# Foo"),
			new DocumentationDocument("Agent/Sync.md", "Agent/Sync.md", static () => "# Sync"),
			new DocumentationDocument("Agenda.md", "Agenda.md", static () => "# Agenda")
		], "Readme.md");

		var included = catalog.WithIncluded(["Controls", "Readme.md"]);
		Assert.AreEqual(2, included.Documents.Count);
		Assert.IsTrue(included.TryGet("Controls/Foo.md", out _));
		Assert.IsTrue(included.TryGet("Readme.md", out _));
		Assert.IsFalse(included.TryGet("Agent/Sync.md", out _));
		Assert.IsFalse(included.TryGet("Agenda.md", out _));
		Assert.AreEqual("Readme.md", included.Entry.Id, true);
	}

	#endregion
}