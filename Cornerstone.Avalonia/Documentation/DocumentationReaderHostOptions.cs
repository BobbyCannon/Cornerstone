#region References

using System;
using System.Reflection;

#endregion

namespace Cornerstone.Avalonia.Documentation;

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
	/// Optional catalog factory. Default: <see cref="DocumentationCatalog.FromDirectory" /> on <see cref="ContentRoot" />.
	/// </summary>
	public Func<DocumentationCatalog> BuildCatalog { get; set; }

	/// <summary>
	/// Markdown content root. Default: <see cref="AppContext.BaseDirectory" />.
	/// </summary>
	public string ContentRoot { get; set; }

	/// <summary>
	/// Entry document relative to the content root (default <c> Readme.md </c>).
	/// </summary>
	public string EntryRelativePath { get; set; } = "Readme.md";

	/// <summary>
	/// Optional resolver for a CLI <c> .md </c> argument to a catalog id.
	/// Return null to keep the default normalization / TryGet behavior.
	/// </summary>
	public Func<DocumentationCatalog, string, string> ResolveOpenDocumentId { get; set; }

	/// <summary>
	/// Main window title.
	/// </summary>
	public string WindowTitle { get; set; } = "Documentation";

	#endregion
}