#region References

using System.IO;
using Cornerstone.Avalonia.Documentation;
using Cornerstone.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Controls;

[TestClass]
public class DocumentationReaderExportTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void GetExportFolderNameUsesCatalogName()
	{
		var catalog = new DocumentationCatalog([]) { Name = "Cornerstone.Documentation" };
		AreEqual("Cornerstone.Documentation", DocumentationReader.GetExportFolderName(catalog));
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
	public void ApplyOpenDocumentArgumentUsesResolver()
	{
		var catalog = new DocumentationCatalog(
		[
			new DocumentationDocument("Readme.md", "Readme.md", static () => "# Root"),
			new DocumentationDocument("EpicCoders/Foo.md", "EpicCoders/Foo.md", static () => "# Foo")
		], "Readme.md")
		{
			Name = "EpicCoders.Documentation"
		};

		var updated = DocumentationReaderHost.ApplyOpenDocumentArgument(
			catalog,
			["Foo.md"],
			new DocumentationReaderHostOptions
			{
				ResolveOpenDocumentId = (c, entry) =>
				{
					var normalized = DocumentationDocument.NormalizeId(entry);
					return c.TryGet("EpicCoders/" + normalized, out _)
						? "EpicCoders/" + normalized
						: normalized;
				}
			});

		Assert.AreEqual("EpicCoders/Foo.md", updated.Entry.Id, ignoreCase: true);
		AreEqual("EpicCoders.Documentation", updated.Name);
	}

	#endregion
}
