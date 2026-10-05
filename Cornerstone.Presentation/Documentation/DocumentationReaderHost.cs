#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Platforms;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Presentation.Documentation;

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

	/// <summary>
	/// User settings for the current desktop run. Null until <see cref="LoadSettings" />.
	/// </summary>
	public static DocumentationReaderSettings Settings { get; private set; }

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
			Name = catalogName,
			ExportIncludePaths = catalog.ExportIncludePaths
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
		else if (!string.IsNullOrWhiteSpace(options.ContentRoot))
		{
			catalog = DocumentationCatalog.FromDirectory(options.ContentRoot, options.EntryRelativePath ?? "Readme.md");
		}
		else
		{
			catalog = DocumentationCatalog.FromAssemblyResources(
				options.ApplicationAssembly,
				options.ResourceNamePrefix ?? string.Empty,
				options.EntryRelativePath ?? "Readme.md");
			if (catalog.Documents.Count == 0)
			{
				catalog = DocumentationCatalog.FromDirectory(
					AppContext.BaseDirectory,
					options.EntryRelativePath ?? "Readme.md");
			}
		}

		catalog.Name = AppBootstrap.RuntimeInformation.ApplicationName;
		options.ExportIncludePaths ??= new List<string>();
		catalog.ExportIncludePaths = new List<string>(
			DocumentationCatalog.NormalizePrefixes(options.ExportIncludePaths));
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
		options.ExportIncludePaths ??= new List<string>();
		UnionExportIncludePaths(options.ExportIncludePaths, TryLoadAppSettingsIncludePaths());
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
			options.ExportIncludePaths ??= new List<string>();
			UnionExportIncludePaths(options.ExportIncludePaths, TryLoadAppSettingsIncludePaths());
			UnionExportIncludePaths(options.ExportIncludePaths, DocumentationExportCommand.GetIncludePrefixes(args));
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

	/// <summary>
	/// Load ApplicationSettings.json from the application data folder. Safe to call once per process.
	/// </summary>
	[UnconditionalSuppressMessage("Aot", "IL3050", Justification = "Documentation reader settings JSON uses Serializer options at startup.")]
	[UnconditionalSuppressMessage("Trim", "IL2026", Justification = "Documentation reader settings JSON uses Serializer options at startup.")]
	public static void LoadSettings()
	{
		if (Settings is not null)
		{
			return;
		}

		Settings = new DocumentationReaderSettings(AppBootstrap.RuntimeInformation);
		Settings.LoadLifecycle();
	}

	/// <summary>
	/// Write ApplicationSettings.json when window placement or reading width changed.
	/// </summary>
	[UnconditionalSuppressMessage("Aot", "IL3050", Justification = "Documentation reader settings JSON uses Serializer options at shutdown.")]
	[UnconditionalSuppressMessage("Trim", "IL2026", Justification = "Documentation reader settings JSON uses Serializer options at shutdown.")]
	public static void SaveSettings()
	{
		Settings?.Save();
	}

	internal static void UnionExportIncludePaths(IList<string> target, IEnumerable<string> extras)
	{
		if (target is null || extras is null)
		{
			return;
		}

		foreach (var prefix in DocumentationCatalog.NormalizePrefixes(extras))
		{
			var exists = target.Any(existing =>
				DocumentationDocument.NormalizeId(existing).TrimEnd('/')
					.Equals(prefix, StringComparison.OrdinalIgnoreCase));
			if (!exists)
			{
				target.Add(prefix);
			}
		}
	}

	[UnconditionalSuppressMessage("Aot", "IL3050", Justification = "Deserializes DocumentationAppSettings from appsettings.json at documentation-reader startup.")]
	[UnconditionalSuppressMessage("Trim", "IL2026", Justification = "Deserializes DocumentationAppSettings from appsettings.json at documentation-reader startup.")]
	internal static IReadOnlyList<string> TryLoadAppSettingsIncludePaths()
	{
		foreach (var directory in EnumerateAppSettingsDirectories())
		{
			var path = Path.Combine(directory, "appsettings.json");
			if (!File.Exists(path))
			{
				continue;
			}

			try
			{
				var json = File.ReadAllText(path);
				var settings = JsonSerializer.Deserialize<DocumentationAppSettings>(json,
					new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
				var paths = settings?.Documentation?.ExportIncludePaths;
				if (paths is { Count: > 0 })
				{
					return paths;
				}
			}
			catch
			{
				return [];
			}
		}

		return [];
	}

	private static IEnumerable<string> EnumerateAppSettingsDirectories()
	{
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var directory in new[]
		{
			Path.GetDirectoryName(Environment.ProcessPath),
			AppContext.BaseDirectory,
			Environment.CurrentDirectory
		})
		{
			if (string.IsNullOrWhiteSpace(directory) || !seen.Add(Path.GetFullPath(directory)))
			{
				continue;
			}

			yield return directory;
		}
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