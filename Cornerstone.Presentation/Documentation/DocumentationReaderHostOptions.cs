#region References

using System;
using System.Collections.Generic;
using System.Reflection;

#endregion

namespace Cornerstone.Presentation.Documentation;

/// <summary>
/// Configuration for <see cref="DocumentationReaderHost.Run" />.
/// </summary>
public sealed class DocumentationReaderHostOptions
{
	#region Properties

	/// <summary>
	/// Assembly passed to AppBootstrap.Initialize (typically the WinExe assembly).
	/// </summary>
	public Assembly ApplicationAssembly { get; set; }

	/// <summary>
	/// Bootstrap / <see cref="DocumentationCatalog.Name" /> application name.
	/// </summary>
	public string ApplicationName { get; set; }

	/// <summary>
	/// When true, a bare <c> --export </c> with no directory writes under <c> ./site/&lt;Catalog.Name&gt; </c>.
	/// </summary>
	public bool BareExportDefaultsToSiteFolder { get; set; }

	/// <summary>
	/// Optional catalog factory. Default: embedded resources, or <see cref="DocumentationCatalog.FromDirectory" />
	/// when <see cref="ContentRoot" /> is set.
	/// </summary>
	public Func<DocumentationCatalog> BuildCatalog { get; set; }

	/// <summary>
	/// Markdown content root. When set, the catalog is built from disk (tests).
	/// When unset, the host assembly's embedded <c>.md</c> resources are used.
	/// </summary>
	public string ContentRoot { get; set; }

	/// <summary>
	/// Entry document relative to the content root (default <c> Readme.md </c>).
	/// </summary>
	public string EntryRelativePath { get; set; } = "Readme.md";

	/// <summary>
	/// Optional export allowlist (catalog id prefixes). Empty means export the full packaged catalog.
	/// Unioned with <c>appsettings.json</c> and CLI <c>--export-include</c>.
	/// </summary>
	public IList<string> ExportIncludePaths { get; set; } = new List<string>();

	/// <summary>
	/// Prefix stripped from embedded resource LogicalNames (default empty: every <c>.md</c> in
	/// <see cref="ApplicationAssembly" />).
	/// </summary>
	public string ResourceNamePrefix { get; set; }

	/// <summary>
	/// Optional resolver for a CLI <c> .md </c> argument to a catalog id.
	/// Return null to keep the default normalization / TryGet behavior.
	/// </summary>
	public Func<DocumentationCatalog, string, string> ResolveOpenDocumentId { get; set; }

	/// <summary>
	/// Optional Cornerstone resource path for the main window icon (for example <c>/Assets/Cornerstone.ico</c>).
	/// Resolved against <see cref="ApplicationAssembly" /> as an <c>csres://</c> URI.
	/// </summary>
	public string WindowIcon { get; set; }

	/// <summary>
	/// Main window title.
	/// </summary>
	public string WindowTitle { get; set; } = "Documentation";

	#endregion
}