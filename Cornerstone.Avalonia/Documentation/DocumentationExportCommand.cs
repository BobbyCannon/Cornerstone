#region References

using System;
using System.IO;

#endregion

namespace Cornerstone.Avalonia.Documentation;

/// <summary>
/// Parses <c> --export &lt;dir&gt; </c> and writes a catalog as a static HTML site
/// into a catalog-named subfolder of that directory (same layout as the reader toolbar).
/// </summary>
public static class DocumentationExportCommand
{
	#region Constants

	public const string ArgumentName = "--export";

	#endregion

	#region Methods

	/// <summary>
	/// Writes the catalog under <paramref name="parentDirectory" />/<see cref="DocumentationCatalog.Name" />.
	/// </summary>
	/// <returns> Absolute path of the site folder that was written. </returns>
	public static string ExportToParentDirectory(DocumentationCatalog catalog, string parentDirectory)
	{
		if (catalog is null)
		{
			throw new ArgumentNullException(nameof(catalog));
		}

		if (string.IsNullOrWhiteSpace(parentDirectory))
		{
			throw new ArgumentException("Export folder is required.", nameof(parentDirectory));
		}

		if (catalog.Documents.Count == 0)
		{
			throw new InvalidOperationException("Nothing to export.");
		}

		var siteFolder = Path.Combine(Path.GetFullPath(parentDirectory), DocumentationReader.GetExportFolderName(catalog));
		Directory.CreateDirectory(siteFolder);
		DocumentationStaticSiteBuilder.Export(catalog, siteFolder);
		return siteFolder;
	}

	/// <summary>
	/// True when args contain <c> --export </c> followed by a non-flag directory path.
	/// </summary>
	public static bool TryGetParentDirectory(string[] args, out string parentDirectory)
	{
		parentDirectory = null;
		if (args is null || (args.Length == 0))
		{
			return false;
		}

		for (var i = 0; i < args.Length; i++)
		{
			var arg = args[i];
			if (!string.Equals(arg, ArgumentName, StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(arg, "-export", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if ((i + 1) >= args.Length)
			{
				return false;
			}

			var value = args[i + 1];
			if (string.IsNullOrWhiteSpace(value) || value.StartsWith('-'))
			{
				return false;
			}

			parentDirectory = value.Trim();
			return true;
		}

		return false;
	}

	#endregion
}