#region References

using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Threading;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs;

/// <summary>
/// Ships Cornerstone.Documentation inside the Sample app via <see cref="DocumentationReader" />.
/// Omits agent-oriented and WIP trees (Agent, Todo).
/// </summary>
[SourceReflection]
public partial class TabDocumentation : CornerstoneUserControl
{
	#region Constants

	/// <summary>
	/// Manifest resource prefix for embedded Cornerstone.Documentation markdown
	/// (see EmbedDocumentationMarkdown target in Cornerstone.Sample.csproj).
	/// </summary>
	public const string DocumentationResourcePrefix = "Documents/Cornerstone/";

	public const string HeaderName = "Documentation";

	#endregion

	#region Constructors

	public TabDocumentation() : this(AppBootstrap.GetInstance<IRuntimeInformation>())
	{
	}

	[DependencyInjectionConstructor]
	public TabDocumentation(IRuntimeInformation runtimeInformation)
	{
		RuntimeInformation = runtimeInformation;
		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public IRuntimeInformation RuntimeInformation { get; }

	#endregion

	#region Methods

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);

		if (Reader.Catalog is null)
		{
			var catalog = TryBuildCatalog();
			if (catalog is null)
			{
				// Surface failure — previously a silent no-op left an empty reader.
				return;
			}

			Reader.Catalog = catalog;
		}

		// Always re-ensure entry is visible: first visit can race MarkdownView template;
		// later visits used to skip entirely when Catalog was already set.
		Reader.EnsureEntryDocumentLoaded();
		Dispatcher.UIThread.Post(
			() => Reader.EnsureEntryDocumentLoaded(),
			DispatcherPriority.Loaded);
	}

	/// <summary>
	/// Sample docs tree excludes Agent/ and Todo/
	/// (still present in the full Cornerstone.Documentation host).
	/// </summary>
	private static DocumentationCatalog ForSample(DocumentationCatalog catalog)
	{
		if (catalog is null || (catalog.Documents.Count == 0))
		{
			return catalog;
		}

		var docs = catalog.Documents.Where(d => !IsSampleExcluded(d.Id)).ToList();
		if (docs.Count == catalog.Documents.Count)
		{
			return catalog;
		}

		return new DocumentationCatalog(docs, catalog.Entry?.Id);
	}

	private static bool IsSampleExcluded(string documentId)
	{
		var id = DocumentationDocument.NormalizeId(documentId ?? string.Empty);
		return id.StartsWith("Agent/", StringComparison.OrdinalIgnoreCase)
			|| id.Equals("Agent", StringComparison.OrdinalIgnoreCase)
			|| id.StartsWith("Todo/", StringComparison.OrdinalIgnoreCase)
			|| id.Equals("Todo", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Prefer packaged markdown next to the app (Desktop Content copy).
	/// Fall back to embedded resources (required for Browser WASM / mobile packages).
	/// Last resort: monorepo source tree when running from a build output folder.
	/// </summary>
	private static DocumentationCatalog TryBuildCatalog()
	{
		var baseDir = AppContext.BaseDirectory;

		// Content Link: Documents/Cornerstone/** → treat that folder as the docs root (ids = Readme.md, Keystone.md, …)
		var packaged = Path.Combine(baseDir, "Documents", "Cornerstone");
		if (Directory.Exists(packaged) && File.Exists(Path.Combine(packaged, "Readme.md")))
		{
			return ForSample(DocumentationCatalog.FromDirectory(packaged, "Readme.md"));
		}

		// Also accept flat copy at output root (if packaging changes later)
		if (File.Exists(Path.Combine(baseDir, "Readme.md"))
			&& File.Exists(Path.Combine(baseDir, "Keystone.md")))
		{
			return ForSample(DocumentationCatalog.FromDirectory(baseDir, "Readme.md"));
		}

		// Embedded resources ship inside Cornerstone.Sample.dll — works on WASM/Android/iOS.
		var embedded = DocumentationCatalog.FromAssemblyResources(typeof(TabDocumentation).Assembly, DocumentationResourcePrefix, "Readme.md");
		if (embedded.Documents.Count > 0)
		{
			return ForSample(embedded);
		}

		// Dev: walk up from bin/… to Cornerstone.Documentation
		var dir = new DirectoryInfo(baseDir);
		while (dir is not null)
		{
			foreach (var candidate in new[]
					{
						Path.Combine(dir.FullName, "Cornerstone.Documentation"),
						Path.Combine(dir.FullName, "Cornerstone", "Cornerstone.Documentation")
					})
			{
				if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "Readme.md")))
				{
					return ForSample(DocumentationCatalog.FromDirectory(candidate, "Readme.md"));
				}
			}

			dir = dir.Parent;
		}

		return null;
	}

	#endregion
}