#region References

using System;
using System.IO;
using System.Linq;
using Avalonia;
using Cornerstone.Avalonia.Platforms;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Avalonia.Documentation;

/// <summary>
/// Shared WinExe entry for documentation readers: bootstrap, optional <c> --export </c>, then desktop UI.
/// </summary>
public static class DocumentationReaderHost
{
	#region Properties

	/// <summary>
	/// Options for the current <see cref="Run{TApp}" /> call (read by <see cref="DocumentationReaderApplication" />).
	/// </summary>
	public static DocumentationReaderHostOptions CurrentOptions { get; private set; }

	#endregion

	#region Methods

	/// <summary>
	/// Applies an optional <c> .md </c> CLI argument as the catalog entry document.
	/// </summary>
	public static DocumentationCatalog ApplyOpenDocumentArgument(DocumentationCatalog catalog, string[] args, DocumentationReaderHostOptions options)
	{
		if (catalog is null || args is null || (args.Length == 0))
		{
			return catalog;
		}

		var entry = args.FirstOrDefault(a => a.EndsWith(".md", StringComparison.OrdinalIgnoreCase));
		if (string.IsNullOrEmpty(entry))
		{
			return catalog;
		}

		var catalogName = catalog.Name;
		var normalized = options?.ResolveOpenDocumentId?.Invoke(catalog, entry);
		if (string.IsNullOrEmpty(normalized))
		{
			normalized = DocumentationDocument.NormalizeId(entry);
		}

		if (!catalog.TryGet(normalized, out _))
		{
			return catalog;
		}

		return new DocumentationCatalog(catalog.Documents, normalized)
		{
			Name = catalogName
		};
	}

	/// <summary>
	/// Builds a catalog with <see cref="DocumentationCatalog.Name" /> set from <see cref="IRuntimeInformation.ApplicationName" />.
	/// </summary>
	public static DocumentationCatalog BuildCatalog(DocumentationReaderHostOptions options)
	{
		if (options is null)
		{
			throw new ArgumentNullException(nameof(options));
		}

		DocumentationCatalog catalog;
		if (options.BuildCatalog is not null)
		{
			catalog = options.BuildCatalog() ?? new DocumentationCatalog([]);
		}
		else
		{
			var root = string.IsNullOrWhiteSpace(options.ContentRoot)
				? AppContext.BaseDirectory
				: options.ContentRoot;
			catalog = DocumentationCatalog.FromDirectory(root, options.EntryRelativePath ?? "Readme.md");
		}

		catalog.Name = AppBootstrap.RuntimeInformation.ApplicationName;
		return catalog;
	}

	/// <summary>
	/// Initializes bootstrap, handles --export when present, otherwise starts the supplied
	/// <paramref name="appBuilder" /> with classic desktop lifetime (caller adds UsePlatformDetect).
	/// </summary>
	public static int Run(string[] args, DocumentationReaderHostOptions options, AppBuilder appBuilder)
	{
		if (options is null)
		{
			throw new ArgumentNullException(nameof(options));
		}

		if (string.IsNullOrWhiteSpace(options.ApplicationName))
		{
			throw new ArgumentException("ApplicationName is required.", nameof(options));
		}

		if (options.ApplicationAssembly is null)
		{
			throw new ArgumentException("ApplicationAssembly is required.", nameof(options));
		}

		if (appBuilder is null)
		{
			throw new ArgumentNullException(nameof(appBuilder));
		}

		CurrentOptions = options;
		AppBootstrap.Initialize(options.ApplicationName, options.ApplicationAssembly, args);

		if (TryExport(args, options, out var exitCode))
		{
			return exitCode;
		}

		appBuilder
			.UseCornerstone(args)
			.StartWithClassicDesktopLifetime(args);

		return 0;
	}

	/// <summary>
	/// True when args request export. Writes under <c> parent/Catalog.Name </c>.
	/// </summary>
	public static bool TryExport(string[] args, DocumentationReaderHostOptions options, out int exitCode)
	{
		exitCode = 0;
		if (args is null || (args.Length == 0) || options is null)
		{
			return false;
		}

		if (DocumentationExportCommand.TryGetParentDirectory(args, out var parentDirectory))
		{
			// --export <dir>
		}
		else if (options.BareExportDefaultsToSiteFolder && HasBareExportFlag(args))
		{
			parentDirectory = Path.Combine(Environment.CurrentDirectory, "site");
		}
		else
		{
			return false;
		}

		try
		{
			var catalog = BuildCatalog(options);
			var siteFolder = DocumentationExportCommand.ExportToParentDirectory(catalog, parentDirectory);
			Console.Out.WriteLine("Exported site to " + siteFolder);
			exitCode = 0;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine("Export failed: " + ex.Message);
			exitCode = 1;
		}

		return true;
	}

	private static bool HasBareExportFlag(string[] args)
	{
		for (var i = 0; i < args.Length; i++)
		{
			if (!string.Equals(args[i], DocumentationExportCommand.ArgumentName, StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(args[i], "-export", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var hasValue = ((i + 1) < args.Length)
				&& !string.IsNullOrWhiteSpace(args[i + 1])
				&& !args[i + 1].StartsWith('-');
			return !hasValue;
		}

		return false;
	}

	#endregion
}