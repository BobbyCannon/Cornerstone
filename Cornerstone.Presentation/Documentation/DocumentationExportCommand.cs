#region References

using System;
using System.Collections.Generic;
using System.IO;

#endregion

namespace Cornerstone.Presentation.Documentation;

/// <summary>
/// Parses <c> --export &lt;dir&gt; </c> and writes a catalog as a static HTML site
/// into a catalog-named subfolder of that directory (same layout as the reader toolbar).
/// </summary>
public static class DocumentationExportCommand
{
	#region Constants

	public const string ArgumentName = "--export";

	public const string IncludeArgumentName = "--export-include";

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

		var toExport = catalog.WithIncluded(catalog.ExportIncludePaths);
		if (toExport.Documents.Count == 0)
		{
			throw new InvalidOperationException("Nothing to export.");
		}

		var siteFolder = Path.Combine(Path.GetFullPath(parentDirectory), DocumentationReader.GetExportFolderName(catalog));
		Directory.CreateDirectory(siteFolder);
		DocumentationStaticSiteBuilder.Export(toExport, siteFolder);
		return siteFolder;
	}

	/// <summary>
	/// Values from repeatable <c> --export-include &lt;prefix&gt; </c> (order preserved, empties skipped).
	/// </summary>
	public static IReadOnlyList<string> GetIncludePrefixes(string[] args)
	{
		var prefixes = new List<string>();
		if (args is null || (args.Length == 0))
		{
			return prefixes;
		}

		for (var i = 0; i < args.Length; i++)
		{
			if (!string.Equals(args[i], IncludeArgumentName, StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(args[i], "-export-include", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if ((i + 1) >= args.Length)
			{
				continue;
			}

			var value = args[i + 1];
			if (string.IsNullOrWhiteSpace(value) || value.StartsWith('-'))
			{
				continue;
			}

			prefixes.Add(value.Trim());
			i++;
		}

		return prefixes;
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