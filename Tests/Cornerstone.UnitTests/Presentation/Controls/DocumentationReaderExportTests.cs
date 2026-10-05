#region References

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cornerstone.Presentation.Documentation;
using Cornerstone.Runtime;
using Cornerstone.Sample.Tabs.Documentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation.Controls;

[TestClass]
public class DocumentationReaderExportTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ApplyOpenDocumentArgumentUsesResolver()
	{
		var catalog = new DocumentationCatalog(
		[
			new DocumentationDocument("Readme.md", "Readme.md", static () => "# Root"),
			new DocumentationDocument("Guides/Foo.md", "Guides/Foo.md", static () => "# Foo")
		], "Readme.md")
		{
			Name = "Guides.Documentation"
		};

		var updated = DocumentationReaderHost.ApplyOpenDocumentArgument(
			catalog,
			["Foo.md"],
			new DocumentationReaderHostOptions
			{
				ResolveOpenDocumentId = (c, entry) =>
				{
					var normalized = DocumentationDocument.NormalizeId(entry);
					return c.TryGet("Guides/" + normalized, out _)
						? "Guides/" + normalized
						: normalized;
				}
			});

		Assert.AreEqual("Guides/Foo.md", updated.Entry.Id, true);
		AreEqual("Guides.Documentation", updated.Name);
	}

	[TestMethod]
	public void BuildCatalogUsesEmbeddedResourcesWhenContentRootUnset()
	{
		AppBootstrap.EnsureInitialized("Unit.Documentation", typeof(DocumentationReaderExportTests).Assembly);
		AppBootstrap.RuntimeInformation.SetPlatformOverride(
			nameof(IRuntimeInformation.ApplicationName),
			"Unit.Documentation");

		var catalog = DocumentationReaderHost.BuildCatalog(new DocumentationReaderHostOptions
		{
			ApplicationName = "Unit.Documentation",
			ApplicationAssembly = typeof(TabDocumentation).Assembly,
			ResourceNamePrefix = TabDocumentation.DocumentationResourcePrefix
		});

		IsTrue(catalog.Documents.Count > 0);
		IsTrue(catalog.TryGet("Readme.md", out _));
		IsFalse(catalog.TryGet("Agent/Sync.md", out _));
	}

	[TestMethod]
	public void ExportToParentDirectoryAppliesIncludeList()
	{
		var root = Path.Combine(Path.GetTempPath(), "CornerstoneDocsExportInclude-" + Path.GetRandomFileName());
		Directory.CreateDirectory(root);
		try
		{
			File.WriteAllText(Path.Combine(root, "Readme.md"), "# Home\n");
			Directory.CreateDirectory(Path.Combine(root, "Agent"));
			File.WriteAllText(Path.Combine(root, "Agent", "Sync.md"), "# Sync\n");
			Directory.CreateDirectory(Path.Combine(root, "Controls"));
			File.WriteAllText(Path.Combine(root, "Controls", "Foo.md"), "# Foo\n");

			var catalog = DocumentationCatalog.FromDirectory(root, "Readme.md");
			catalog.Name = "Include.Docs";
			catalog.ExportIncludePaths = ["Readme.md", "Controls"];

			var parent = Path.Combine(root, "parent");
			var site = DocumentationExportCommand.ExportToParentDirectory(catalog, parent);
			IsTrue(File.Exists(Path.Combine(site, "index.html")));
			IsTrue(File.Exists(Path.Combine(site, "Controls", "Foo.html")));
			IsFalse(File.Exists(Path.Combine(site, "Agent", "Sync.html")));
			Assert.IsTrue(catalog.TryGet("Agent/Sync.md", out _), "Runtime catalog must stay full.");
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
	public void ExportToParentDirectoryWritesUnderCatalogName()
	{
		var root = Path.Combine(Path.GetTempPath(), "CornerstoneDocsExport-" + Path.GetRandomFileName());
		Directory.CreateDirectory(root);
		try
		{
			File.WriteAllText(Path.Combine(root, "Readme.md"), "# Home\n");
			var catalog = DocumentationCatalog.FromDirectory(root, "Readme.md");
			catalog.Name = "Cornerstone.Documentation";

			var parent = Path.Combine(root, "parent");
			Directory.CreateDirectory(parent);
			var site = DocumentationExportCommand.ExportToParentDirectory(catalog, parent);

			AreEqual(Path.Combine(parent, "Cornerstone.Documentation"), site);
			IsTrue(File.Exists(Path.Combine(site, "index.html")));
			IsTrue(File.Exists(Path.Combine(site, "theme.css")));
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
	public void GetExportFolderNameFallsBackWhenNameMissing()
	{
		AreEqual("Documentation", DocumentationReader.GetExportFolderName(new DocumentationCatalog([])));
		AreEqual("Documentation", DocumentationReader.GetExportFolderName(new DocumentationCatalog([]) { Name = "  " }));
		AreEqual("Documentation", DocumentationReader.GetExportFolderName(null));
	}

	[TestMethod]
	public void GetExportFolderNameSanitizesInvalidFileNameCharacters()
	{
		var catalog = new DocumentationCatalog([]) { Name = "My:Catalog*Name?" };
		var folder = DocumentationReader.GetExportFolderName(catalog);
		IsFalse(folder.Contains(':'));
		IsFalse(folder.Contains('*'));
		IsFalse(folder.Contains('?'));
		AreEqual("My_Catalog_Name_", folder);
	}

	[TestMethod]
	public void GetExportFolderNameUsesCatalogName()
	{
		var catalog = new DocumentationCatalog([]) { Name = "Cornerstone.Documentation" };
		AreEqual("Cornerstone.Documentation", DocumentationReader.GetExportFolderName(catalog));
	}

	[TestMethod]
	public void GetIncludePrefixesParsesRepeatableArguments()
	{
		var prefixes = DocumentationExportCommand.GetIncludePrefixes(
			["--export", @"C:\Out", "--export-include", "Controls", "-export-include", "Readme.md"]);
		AreEqual(2, prefixes.Count);
		AreEqual("Controls", prefixes[0]);
		AreEqual("Readme.md", prefixes[1]);
	}

	[TestMethod]
	public void TryExportUnionsCliIncludePaths()
	{
		var root = Path.Combine(Path.GetTempPath(), "CornerstoneDocsHostInclude-" + Path.GetRandomFileName());
		Directory.CreateDirectory(root);
		try
		{
			File.WriteAllText(Path.Combine(root, "Readme.md"), "# Home\n");
			Directory.CreateDirectory(Path.Combine(root, "Agent"));
			File.WriteAllText(Path.Combine(root, "Agent", "Sync.md"), "# Sync\n");

			var parent = Path.Combine(root, "parent");
			Directory.CreateDirectory(parent);

			var options = new DocumentationReaderHostOptions
			{
				ApplicationName = "Unit.Documentation",
				ApplicationAssembly = typeof(DocumentationReaderExportTests).Assembly,
				ContentRoot = root,
				ExportIncludePaths = ["Readme.md"]
			};

			AppBootstrap.EnsureInitialized("Unit.Documentation", typeof(DocumentationReaderExportTests).Assembly);
			AppBootstrap.RuntimeInformation.SetPlatformOverride(
				nameof(IRuntimeInformation.ApplicationName),
				"Unit.Documentation");

			IsTrue(DocumentationReaderHost.TryExport(["--export", parent, "--export-include", "Readme.md"], options, out var exitCode));
			AreEqual(0, exitCode);
			var site = Path.Combine(parent, "Unit.Documentation");
			IsTrue(File.Exists(Path.Combine(site, "index.html")));
			IsFalse(Directory.Exists(Path.Combine(site, "Agent")));
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
	public void TryExportWritesUnderApplicationNameWhenBootstrapped()
	{
		var root = Path.Combine(Path.GetTempPath(), "CornerstoneDocsHost-" + Path.GetRandomFileName());
		Directory.CreateDirectory(root);
		try
		{
			File.WriteAllText(Path.Combine(root, "Readme.md"), "# Home\n");
			var parent = Path.Combine(root, "parent");
			Directory.CreateDirectory(parent);

			var options = new DocumentationReaderHostOptions
			{
				ApplicationName = "Unit.Documentation",
				ApplicationAssembly = typeof(DocumentationReaderExportTests).Assembly,
				ContentRoot = root
			};

			// Host.TryExport requires AppBootstrap; unit tests already initialize it.
			AppBootstrap.EnsureInitialized("Unit.Documentation", typeof(DocumentationReaderExportTests).Assembly);
			AppBootstrap.RuntimeInformation.SetPlatformOverride(
				nameof(IRuntimeInformation.ApplicationName),
				"Unit.Documentation");

			IsTrue(DocumentationReaderHost.TryExport(["--export", parent], options, out var exitCode));
			AreEqual(0, exitCode);
			var site = Path.Combine(parent, "Unit.Documentation");
			IsTrue(Directory.Exists(site));
			IsTrue(File.Exists(Path.Combine(site, "index.html")));
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
	public void TryGetParentDirectoryParsesExportArgument()
	{
		IsTrue(DocumentationExportCommand.TryGetParentDirectory(["--export", @"C:\Out"], out var dir));
		AreEqual(@"C:\Out", dir);
		IsTrue(DocumentationExportCommand.TryGetParentDirectory(["-export", @"D:\Sites"], out dir));
		AreEqual(@"D:\Sites", dir);
		IsFalse(DocumentationExportCommand.TryGetParentDirectory(["--export"], out _));
		IsFalse(DocumentationExportCommand.TryGetParentDirectory(["Readme.md"], out _));
	}

	[TestMethod]
	public void UnionExportIncludePathsIsCaseInsensitive()
	{
		var target = new List<string> { "Controls" };
		DocumentationReaderHost.UnionExportIncludePaths(target, ["controls", "Agent"]);
		AreEqual(2, target.Count);
		IsTrue(target.Any(p => p.Equals("Agent", StringComparison.OrdinalIgnoreCase)));
	}

	#endregion
}