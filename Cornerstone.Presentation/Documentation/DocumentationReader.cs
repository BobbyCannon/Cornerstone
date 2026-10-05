#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Documents;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Metadata;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Navigation;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Resources;
using Cornerstone.Presentation.Controls.StyleClasses;
using Cornerstone.Presentation.Controls.Text;
using Cornerstone.Presentation.Controls.TreeDataGrid;
using Cornerstone.Presentation.Controls.TreeDataGrid.Columns;
using Cornerstone.Presentation.Controls.TreeDataGrid.Models;
using Cornerstone.Presentation.Controls.TreeDataGrid.Selection;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform.Storage;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Theme.Theming;
using Cornerstone.Search;
using Dispatcher = Cornerstone.Presentation.Threading.Dispatcher;

#endregion

namespace Cornerstone.Presentation.Documentation;

/// <summary>
/// Hosts a <see cref="MarkdownView" /> over a <see cref="DocumentationCatalog" />,
/// with optional left document tree, search, and reading chrome.
/// </summary>
[TemplatePart(PartBackButton, typeof(Button))]
[TemplatePart(PartColorBox, typeof(ComboBox))]
[TemplatePart(PartDensityBox, typeof(ComboBox))]
[TemplatePart(PartDocTree, typeof(TreeDataGrid))]
[TemplatePart(PartDocumentTrail, typeof(BreadcrumbTrail))]
[TemplatePart(PartDocumentsPaneButton, typeof(Button))]
[TemplatePart(PartDocumentsSplit, typeof(SplitView))]
[TemplatePart(PartEmptyFilterText, typeof(TextBlock))]
[TemplatePart(PartExportButton, typeof(Button))]
[TemplatePart(PartHomeButton, typeof(Button))]
[TemplatePart(PartMarkdownView, typeof(MarkdownView))]
[TemplatePart(PartSearchBox, typeof(TextBox))]
[TemplatePart(PartStatusCloseButton, typeof(Button))]
[TemplatePart(PartStatusHost, typeof(Border))]
[TemplatePart(PartStatusText, typeof(TextBlock))]
[TemplatePart(PartThemeButton, typeof(Button))]
[TemplatePart(PartThemeIcon, typeof(PathIcon))]
[TemplatePart(PartWidthButton, typeof(ToggleButton))]
[PseudoClasses(":fullwidth")]
public class DocumentationReader : TemplatedControl
{
	#region Constants

	public const string PartBackButton = "PART_BackButton";
	public const string PartColorBox = "PART_ColorBox";
	public const string PartDensityBox = "PART_DensityBox";
	public const string PartDocTree = "PART_DocTree";
	public const string PartDocumentTrail = "PART_DocumentTrail";
	public const string PartDocumentsPaneButton = "PART_DocumentsPaneButton";
	public const string PartDocumentsSplit = "PART_DocumentsSplit";
	public const string PartEmptyFilterText = "PART_EmptyFilterText";
	public const string PartExportButton = "PART_ExportButton";
	public const string PartHomeButton = "PART_HomeButton";
	public const string PartMarkdownView = "PART_MarkdownView";
	public const string PartSearchBox = "PART_SearchBox";
	public const string PartStatusCloseButton = "PART_StatusCloseButton";
	public const string PartStatusHost = "PART_StatusHost";
	public const string PartStatusText = "PART_StatusText";
	public const string PartThemeButton = "PART_ThemeButton";
	public const string PartThemeIcon = "PART_ThemeIcon";
	public const string PartWidthButton = "PART_WidthButton";

	/// <summary>
	/// Reading column cap. <see cref="IsFullWidth" /> removes it.
	/// </summary>
	public const double ReadingColumnMaxWidth = 920;

	#endregion

	#region Fields

	public static readonly StyledProperty<bool> IsFullWidthProperty;

	private Button _backButton;

	private readonly Stack<(string Id, string Fragment)> _backStack = new();

	private TopLevel _backTopLevel;
	private DocumentationCatalog _catalog;
	private ComboBox _colorBox;
	private ComboBox _densityBox;
	private TreeDataGrid _docTree;
	private BreadcrumbTrail _documentTrail;
	private Button _documentsPaneButton;
	private SplitView _documentsSplit;
	private TextBlock _emptyFilterText;
	private Button _exportButton;
	private ObservableCollection<DocumentTreeNode> _fullRoots = new();
	private Button _homeButton;
	private MarkdownView _markdownView;
	private string _pendingFragment;
	private TextBox _searchBox;
	private Button _statusCloseButton;
	private Border _statusHost;
	private TextBlock _statusText;
	private Button _themeButton;
	private PathIcon _themeIcon;
	private HierarchicalTreeDataGridSource<DocumentTreeNode> _treeSource;
	private ToggleButton _widthButton;

	#endregion

	#region Constructors

	public DocumentationReader()
	{
		EnsureDefaultThemeColor();
		SyncThemeBoxes();
		SyncFullWidth();
	}

	static DocumentationReader()
	{
		IsFullWidthProperty = PresentationProperty.Register<DocumentationReader, bool>(nameof(IsFullWidth), true);
	}

	#endregion

	#region Properties

	public bool CanGoBack => _backStack.Count > 0;

	public DocumentationCatalog Catalog
	{
		get => _catalog;
		set
		{
			_catalog = value;
			_backStack.Clear();
			RebuildDocumentTree();
			RefreshChrome();

			// May run before the visual tree / MarkdownView template is ready — EnsureEntryDocumentLoaded
			// is also invoked on attach and retries until content is actually shown.
			EnsureEntryDocumentLoaded();
		}
	}

	public DocumentationDocument Current { get; private set; }

	public bool IsDocumentPaneOpen
	{
		get => _documentsSplit?.IsPaneOpen ?? false;
		set
		{
			if (_documentsSplit is not null)
			{
				_documentsSplit.IsPaneOpen = value;
			}
		}
	}

	/// <summary>
	/// When set, the article column fills the reader. Otherwise, it stays within <see cref="ReadingColumnMaxWidth" />.
	/// </summary>
	public bool IsFullWidth
	{
		get => GetValue(IsFullWidthProperty);
		set => SetValue(IsFullWidthProperty, value);
	}

	#endregion

	#region Methods

	public static IReadOnlyList<BreadcrumbSegment> CreateBreadcrumbSegments(string documentId)
	{
		var segments = new List<BreadcrumbSegment>();
		if (string.IsNullOrEmpty(documentId))
		{
			return segments;
		}

		var normalized = documentId.Replace('\\', '/').Trim('/');
		var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
		var key = string.Empty;
		for (var i = 0; i < parts.Length; i++)
		{
			if (key.Length > 0)
			{
				key += "/";
			}

			key += parts[i];
			var isCurrent = i == (parts.Length - 1);

			// Folders may contain dots (Cornerstone.Documentation). Only the document file drops its extension.
			var title = isCurrent ? Path.GetFileNameWithoutExtension(parts[i]) : parts[i];
			segments.Add(new BreadcrumbSegment(title, key, isCurrent, i > 0));
		}

		return segments;
	}

	/// <summary>
	/// Opens the catalog entry document if nothing is loaded yet (or the markdown surface is empty).
	/// Safe to call repeatedly; used when the tab/visual tree becomes ready after Catalog is set.
	/// </summary>
	public bool EnsureEntryDocumentLoaded()
	{
		if (_catalog?.Entry is null)
		{
			if (_catalog is not null && (_catalog.Documents.Count == 0))
			{
				SetStatus("Documentation catalog is empty.");
			}
			else if (_catalog is not null)
			{
				SetStatus("No entry document (Readme.md) in catalog.");
			}

			return false;
		}

		if (_markdownView is null || _markdownView.Document is null)
		{
			// Template not ready — retry after layout.
			Dispatcher.UIThread.Post(() => EnsureEntryDocumentLoaded(), DispatcherPriority.Loaded);
			return false;
		}

		var hasContent = _markdownView.Document.DocumentLength > 0;
		if (Current is not null && hasContent)
		{
			// Buffer already filled (tab reuse / prior NavigateTo) — force presenters to rebuild.
			// Home "fixes" empty UI only because it reloads; re-attach must refresh without reload.
			_markdownView.RefreshPresentation();
			RefreshChrome();
			return true;
		}

		// Prefer last Current if still in catalog; otherwise entry (Readme).
		var targetId = Current is not null && _catalog.TryGet(Current.Id, out _)
			? Current.Id
			: _catalog.Entry.Id;

		var ok = NavigateTo(targetId, recordHistory: false);
		if (!ok)
		{
			// One more attempt after the control is fully loaded.
			Dispatcher.UIThread.Post(
				() =>
				{
					if (Current is null || (_markdownView.Document.DocumentLength == 0))
					{
						NavigateTo(_catalog.Entry.Id, recordHistory: false);
					}
					else
					{
						_markdownView.RefreshPresentation();
					}
				},
				DispatcherPriority.Loaded);
		}

		return ok;
	}

	/// <summary>
	/// Writes the current catalog as a static HTML site into a catalog-named
	/// subfolder of the chosen directory.
	/// </summary>
	public async Task ExportStaticSiteAsync()
	{
		if (_catalog is null || (_catalog.Documents.Count == 0))
		{
			SetStatus("Nothing to export.");
			return;
		}

		var topLevel = TopLevel.GetTopLevel(this);
		if (topLevel?.StorageProvider is null)
		{
			SetStatus("Cannot open a folder picker here.");
			return;
		}

		var selected = await topLevel.StorageProvider.OpenFolderPickerAsync(
			new FolderPickerOpenOptions
			{
				Title = "Export documentation site",
				AllowMultiple = false
			});
		if (selected.Count == 0)
		{
			return;
		}

		var folder = selected[0].Path.LocalPath;
		if (string.IsNullOrWhiteSpace(folder))
		{
			SetStatus("Export folder was empty.");
			return;
		}

		try
		{
			var siteFolder = DocumentationExportCommand.ExportToParentDirectory(_catalog, folder);
			SetStatus("Exported site to " + siteFolder);
			OpenExternalUrl(siteFolder);
		}
		catch (Exception ex)
		{
			SetStatus("Export failed: " + ex.Message);
		}
	}

	public void GoBack()
	{
		if (_backStack.Count == 0)
		{
			return;
		}

		var (id, fragment) = _backStack.Pop();
		RefreshChrome();
		NavigateTo(id, fragment, false);
	}

	public void GoHome()
	{
		if (_catalog?.Entry is null)
		{
			return;
		}

		NavigateTo(_catalog.Entry.Id);
	}

	public bool NavigateTo(string documentId, string fragment = null, bool recordHistory = true)
	{
		if (_catalog is null || !_catalog.TryGet(documentId, out var document))
		{
			SetStatus("Document not in catalog.");
			return false;
		}

		if (recordHistory && Current is not null && !string.Equals(Current.Id, document.Id, StringComparison.OrdinalIgnoreCase))
		{
			_backStack.Push((Current.Id, null));
		}

		try
		{
			var text = document.ReadAllText();
			Current = document;
			_pendingFragment = fragment;

			// Queue scroll intent before Load so the first markdown paint does not AutoScroll-to-end
			// and so a missing ScrollViewer (pre-template) still applies home after ApplyTemplate.
			if (string.IsNullOrEmpty(fragment))
			{
				_markdownView.ScrollToHome();
			}

			_markdownView.Document.Load(text);
			RefreshChrome();
			SyncTreeSelection(document.Id);
			SetStatus(string.Empty);

			if (!string.IsNullOrEmpty(fragment))
			{
				// Header link: scroll after layout has presenters for the new document.
				Dispatcher.UIThread.Post(TryScrollToPendingFragment, DispatcherPriority.Loaded);
				Dispatcher.UIThread.Post(TryScrollToPendingFragment, DispatcherPriority.Background);
			}
			else
			{
				// Reinforce top after layout / throttle (ScrollToHome is pending-aware).
				Dispatcher.UIThread.Post(() => _markdownView.ScrollToHome(), DispatcherPriority.Loaded);
				Dispatcher.UIThread.Post(() => _markdownView.ScrollToHome(), DispatcherPriority.Background);
			}

			return true;
		}
		catch (Exception ex)
		{
			SetStatus($"Failed to load: {ex.Message}");
			return false;
		}
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		DetachTemplate();
		_documentsPaneButton = e.NameScope.Find<Button>(PartDocumentsPaneButton);
		_backButton = e.NameScope.Find<Button>(PartBackButton);
		_homeButton = e.NameScope.Find<Button>(PartHomeButton);
		_documentTrail = e.NameScope.Find<BreadcrumbTrail>(PartDocumentTrail);
		_colorBox = e.NameScope.Find<ComboBox>(PartColorBox);
		_densityBox = e.NameScope.Find<ComboBox>(PartDensityBox);
		_themeButton = e.NameScope.Find<Button>(PartThemeButton);
		_themeIcon = e.NameScope.Find<PathIcon>(PartThemeIcon);
		_widthButton = e.NameScope.Find<ToggleButton>(PartWidthButton);
		if (_widthButton is not null)
		{
			_widthButton.IsCheckedChanged += WidthButtonOnIsCheckedChanged;
		}

		_exportButton = e.NameScope.Find<Button>(PartExportButton);
		_statusHost = e.NameScope.Find<Border>(PartStatusHost);
		_statusText = e.NameScope.Find<TextBlock>(PartStatusText);
		_statusCloseButton = e.NameScope.Find<Button>(PartStatusCloseButton);
		_documentsSplit = e.NameScope.Find<SplitView>(PartDocumentsSplit);
		_searchBox = e.NameScope.Find<TextBox>(PartSearchBox);
		_emptyFilterText = e.NameScope.Find<TextBlock>(PartEmptyFilterText);
		_docTree = e.NameScope.Find<TreeDataGrid>(PartDocTree);
		_markdownView = e.NameScope.Find<MarkdownView>(PartMarkdownView);

		if (_documentsPaneButton is not null)
		{
			_documentsPaneButton.Click += DocumentsPaneButtonOnClick;
		}

		if (_backButton is not null)
		{
			_backButton.Click += BackButtonOnClick;
		}

		if (_homeButton is not null)
		{
			_homeButton.Click += HomeButtonOnClick;
		}

		if (_documentTrail is not null)
		{
			_documentTrail.CrumbClicked += DocumentTrailOnCrumbClicked;
		}

		if (_colorBox is not null)
		{
			_colorBox.SelectionChanged += ColorBoxOnSelectionChanged;
		}

		if (_densityBox is not null)
		{
			_densityBox.SelectionChanged += DensityBoxOnSelectionChanged;
		}

		if (_themeButton is not null)
		{
			_themeButton.Click += ThemeButtonOnClick;
		}

		if (_exportButton is not null)
		{
			_exportButton.Click += ExportButtonOnClick;
		}

		if (_statusCloseButton is not null)
		{
			_statusCloseButton.Click += StatusCloseButtonOnClick;
		}

		if (_searchBox is not null)
		{
			_searchBox.TextChanged += SearchBoxOnTextChanged;
		}

		if (_treeSource is null)
		{
			InitializeTreeSource();
		}
		else if (_docTree is not null)
		{
			_docTree.ItemsSource = _treeSource;
		}

		AttachMarkdown();
		SyncThemeBoxes();
		SyncFullWidth();
		RefreshChrome();
		EnsureEntryDocumentLoaded();
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		SystemBack.Subscribe(this, ref _backTopLevel, TopLevelOnBackRequested);
		AttachMarkdown();
		SyncThemeIcon();
		RefreshChrome();

		// Tab switch: control may be reused with catalog/Current set but empty markdown presenters.
		EnsureEntryDocumentLoaded();
		Dispatcher.UIThread.Post(
			() =>
			{
				EnsureEntryDocumentLoaded();
				if (_markdownView is not null && (_markdownView.Document?.DocumentLength > 0))
				{
					_markdownView.RefreshPresentation();
				}
			},
			DispatcherPriority.Loaded);
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		SystemBack.Unsubscribe(ref _backTopLevel, TopLevelOnBackRequested);
		if (_markdownView is not null)
		{
			_markdownView.LinkClicked -= MarkdownViewOnLinkClicked;
		}

		base.OnDetachedFromVisualTree(e);
	}

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == ThemeVariant.ActualThemeVariantProperty)
		{
			SyncThemeIcon();
		}
		else if (change.Property == IsFullWidthProperty)
		{
			SyncFullWidth();
		}
	}

	/// <summary>
	/// Folder name under the picked directory; prefers <see cref="DocumentationCatalog.Name" />.
	/// </summary>
	internal static string GetExportFolderName(DocumentationCatalog catalog)
	{
		var name = catalog?.Name?.Trim();
		if (string.IsNullOrEmpty(name))
		{
			name = "Documentation";
		}

		foreach (var c in Path.GetInvalidFileNameChars())
		{
			name = name.Replace(c, '_');
		}

		return name;
	}

	private static void AddDocumentPath(ObservableCollection<DocumentTreeNode> roots, string documentId)
	{
		var parts = documentId.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0)
		{
			return;
		}

		var current = roots;
		for (var i = 0; i < parts.Length; i++)
		{
			var part = parts[i];
			var isLeaf = i == (parts.Length - 1);
			var existing = current.FirstOrDefault(n =>
				string.Equals(n.Name, isLeaf ? Path.GetFileNameWithoutExtension(part) : part, StringComparison.OrdinalIgnoreCase)
				|| (isLeaf && string.Equals(n.DocumentId, documentId, StringComparison.OrdinalIgnoreCase)));

			if (isLeaf)
			{
				var display = Path.GetFileNameWithoutExtension(part);
				if (existing is null)
				{
					current.Add(new DocumentTreeNode(display, documentId));
				}
				return;
			}

			if (existing is null)
			{
				existing = new DocumentTreeNode(part);
				current.Add(existing);
			}

			current = existing.Children;
		}
	}

	private void ApplyFilter(string filter)
	{
		filter = filter?.Trim() ?? string.Empty;
		ObservableCollection<DocumentTreeNode> roots;

		if (filter.Length == 0)
		{
			roots = _fullRoots;
			if (_emptyFilterText is not null)
			{
				_emptyFilterText.IsVisible = false;
			}
		}
		else
		{
			roots = FilterTree(_fullRoots, filter);
			if (_emptyFilterText is not null)
			{
				_emptyFilterText.IsVisible = roots.Count == 0;
			}
			ExpandAll(roots);
		}

		if (_treeSource is not null)
		{
			_treeSource.Items = roots;
		}
	}

	private void AttachMarkdown()
	{
		if (_markdownView is null)
		{
			return;
		}

		_markdownView.LinkClicked -= MarkdownViewOnLinkClicked;
		_markdownView.LinkClicked += MarkdownViewOnLinkClicked;
	}

	private void BackButtonOnClick(object sender, RoutedEventArgs e)
	{
		GoBack();
	}

	private static IEnumerable<DocumentTreeNode> CloneBranch(DocumentTreeNode node)
	{
		foreach (var child in node.Children)
		{
			if (child.IsFolder)
			{
				var folder = new DocumentTreeNode(child.Name) { IsExpanded = true };
				foreach (var c in CloneBranch(child))
				{
					folder.Children.Add(c);
				}
				if (folder.Children.Count > 0)
				{
					yield return folder;
				}
			}
			else
			{
				yield return new DocumentTreeNode(child.Name, child.DocumentId);
			}
		}
	}

	private void ColorBoxOnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_colorBox?.SelectedItem is ThemeColor color)
		{
			var theme = ApplicationTheme.GetCurrent();
			if (theme is not null)
			{
				theme.ThemeColor = color;
			}
		}
	}

	private void DensityBoxOnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_densityBox?.SelectedItem is ThemeDensity density)
		{
			var theme = ApplicationTheme.GetCurrent();
			if (theme is not null)
			{
				theme.ThemeDensity = density;
			}
		}
	}

	private void DetachMarkdown()
	{
		if (_markdownView is null)
		{
			return;
		}

		_markdownView.LinkClicked -= MarkdownViewOnLinkClicked;
		_markdownView = null;
	}

	private void DetachTemplate()
	{
		if (_documentsPaneButton is not null)
		{
			_documentsPaneButton.Click -= DocumentsPaneButtonOnClick;
			_documentsPaneButton = null;
		}

		if (_backButton is not null)
		{
			_backButton.Click -= BackButtonOnClick;
			_backButton = null;
		}

		if (_homeButton is not null)
		{
			_homeButton.Click -= HomeButtonOnClick;
			_homeButton = null;
		}

		if (_documentTrail is not null)
		{
			_documentTrail.CrumbClicked -= DocumentTrailOnCrumbClicked;
			_documentTrail = null;
		}

		if (_colorBox is not null)
		{
			_colorBox.SelectionChanged -= ColorBoxOnSelectionChanged;
			_colorBox = null;
		}

		if (_densityBox is not null)
		{
			_densityBox.SelectionChanged -= DensityBoxOnSelectionChanged;
			_densityBox = null;
		}

		if (_themeButton is not null)
		{
			_themeButton.Click -= ThemeButtonOnClick;
			_themeButton = null;
		}

		if (_widthButton is not null)
		{
			_widthButton.IsCheckedChanged -= WidthButtonOnIsCheckedChanged;
			_widthButton = null;
		}

		if (_exportButton is not null)
		{
			_exportButton.Click -= ExportButtonOnClick;
			_exportButton = null;
		}

		if (_statusCloseButton is not null)
		{
			_statusCloseButton.Click -= StatusCloseButtonOnClick;
			_statusCloseButton = null;
		}

		if (_searchBox is not null)
		{
			_searchBox.TextChanged -= SearchBoxOnTextChanged;
			_searchBox = null;
		}

		DetachMarkdown();
		_themeIcon = null;
		_statusHost = null;
		_statusText = null;
		_documentsSplit = null;
		_emptyFilterText = null;
		_docTree = null;
	}

	private void DocumentTrailOnCrumbClicked(object sender, BreadcrumbClickedEventArgs e)
	{
		if (e.Segment is null || string.IsNullOrEmpty(e.Segment.Key) || e.Segment.IsCurrent)
		{
			return;
		}

		if (_catalog is not null && _catalog.TryResolvePrefix(e.Segment.Key, out var document))
		{
			NavigateTo(document.Id);
		}
	}

	private void DocumentsPaneButtonOnClick(object sender, RoutedEventArgs e)
	{
		if (_documentsSplit is null)
		{
			return;
		}

		_documentsSplit.IsPaneOpen = !_documentsSplit.IsPaneOpen;
	}

	private static void EnsureDefaultThemeColor()
	{
		var theme = ApplicationTheme.GetCurrent();
		if (theme is null)
		{
			return;
		}

		if ((theme.ThemeColor == ThemeColor.None)
			|| (theme.ThemeColor == ThemeColor.Current))
		{
			theme.ThemeColor = ThemeColor.Blue;
		}
	}

	private static void ExpandAll(IEnumerable<DocumentTreeNode> nodes)
	{
		foreach (var node in nodes)
		{
			if (node.IsFolder)
			{
				node.IsExpanded = true;
				ExpandAll(node.Children);
			}
		}
	}

	private static bool ExpandPathToDocument(IEnumerable<DocumentTreeNode> nodes, string documentId)
	{
		foreach (var node in nodes)
		{
			if (!node.IsFolder && string.Equals(node.DocumentId, documentId, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			if (node.IsFolder && ExpandPathToDocument(node.Children, documentId))
			{
				node.IsExpanded = true;
				return true;
			}
		}

		return false;
	}

	private async void ExportButtonOnClick(object sender, RoutedEventArgs e)
	{
		e.Handled = true;
		await ExportStaticSiteAsync();
	}

	private static ObservableCollection<DocumentTreeNode> FilterTree(IEnumerable<DocumentTreeNode> nodes, string filter)
	{
		var result = new ObservableCollection<DocumentTreeNode>();
		foreach (var node in nodes)
		{
			if (node.IsFolder)
			{
				var filteredChildren = FilterTree(node.Children, filter);
				var nameMatch = TokenTextFilter.Matches(filter, node.Name);
				if ((filteredChildren.Count > 0) || nameMatch)
				{
					var copy = new DocumentTreeNode(node.Name) { IsExpanded = true };
					foreach (var child in filteredChildren)
					{
						copy.Children.Add(child);
					}

					// If name matches folder but no children matched, still include matching descendant docs from original
					if ((filteredChildren.Count == 0) && nameMatch)
					{
						foreach (var child in CloneBranch(node))
						{
							copy.Children.Add(child);
						}
					}
					result.Add(copy);
				}
			}
			else
			{
				var id = node.DocumentId ?? string.Empty;
				if (TokenTextFilter.Matches(filter, node.Name, id))
				{
					result.Add(new DocumentTreeNode(node.Name, node.DocumentId));
				}
			}
		}

		return result;
	}

	private void HomeButtonOnClick(object sender, RoutedEventArgs e)
	{
		GoHome();
	}

	private void InitializeTreeSource()
	{
		// Catalog can be assigned before the template exists. RebuildDocumentTree already
		// filled _fullRoots; replacing it here left the filter pane empty.
		_fullRoots ??= new ObservableCollection<DocumentTreeNode>();
		_treeSource = new HierarchicalTreeDataGridSource<DocumentTreeNode>(_fullRoots)
		{
			Columns =
			{
				new HierarchicalExpanderColumn<DocumentTreeNode>(
					new TextColumn<DocumentTreeNode, string>(
						"Name",
						x => x.Name,
						new GridLength(1, GridUnitType.Star)),
					x => x.Children,
					x => x.Children.Count > 0,
					x => x.IsExpanded)
			}
		};

		if (_docTree is not null)
		{
			_docTree.ItemsSource = _treeSource;
		}

		if (_treeSource.RowSelection is not null)
		{
			_treeSource.RowSelection.SelectionChanged += TreeSelectionOnSelectionChanged;
		}

		if (_catalog is not null && (_fullRoots.Count == 0))
		{
			RebuildDocumentTree();
		}
		else
		{
			ApplyFilter(_searchBox?.Text);
		}
	}

	private void MarkdownViewOnLinkClicked(object sender, MarkdownLinkClickedEventArgs e)
	{
		if (e.Handled || Current is null || _catalog is null)
		{
			return;
		}

		var href = e.Href?.Trim() ?? string.Empty;
		if (href.Length == 0)
		{
			return;
		}

		if (href.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
			|| href.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			e.Handled = true;
			OpenExternalUrl(href);
			return;
		}

		if (_catalog.TryResolve(Current.Id, href, out var document, out var fragment))
		{
			e.Handled = true;
			if (string.Equals(document.Id, Current.Id, StringComparison.OrdinalIgnoreCase)
				&& !string.IsNullOrEmpty(fragment))
			{
				_markdownView.ScrollToFragment(fragment);
				SetStatus(string.Empty);
				return;
			}

			NavigateTo(document.Id, fragment);
			return;
		}

		e.Handled = true;
		SetStatus("Link target not in catalog.");
	}

	private static void OpenExternalUrl(string url)
	{
		try
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				Process.Start("xdg-open", url);
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				Process.Start("open", url);
			}
		}
		catch
		{
			// Ignore launch failures.
		}
	}

	private void RebuildDocumentTree()
	{
		_fullRoots.Clear();
		if (_catalog is null)
		{
			ApplyFilter(_searchBox?.Text);
			return;
		}

		foreach (var doc in _catalog.Documents.OrderBy(d => d.Id, StringComparer.OrdinalIgnoreCase))
		{
			AddDocumentPath(_fullRoots, doc.Id);
		}

		// Expand top-level folders for orientation
		foreach (var root in _fullRoots)
		{
			root.IsExpanded = true;
		}

		ApplyFilter(_searchBox?.Text);
	}

	private void RefreshChrome()
	{
		if (_backButton is not null)
		{
			_backButton.IsEnabled = CanGoBack;
			_backButton.Opacity = CanGoBack ? 1.0 : 0.35;
		}

		var id = Current?.Id ?? string.Empty;
		_documentTrail?.SetItems(CreateBreadcrumbSegments(id));

		if (_exportButton is not null)
		{
			var canExport = _catalog is not null && (_catalog.Documents.Count > 0);
			_exportButton.IsEnabled = canExport;
			_exportButton.Opacity = canExport ? 1.0 : 0.35;
		}

		SyncThemeBoxes();
	}

	private void SearchBoxOnTextChanged(object sender, TextChangedEventArgs e)
	{
		ApplyFilter(_searchBox?.Text);
	}

	private void SetStatus(string message)
	{
		var text = message ?? string.Empty;
		if (_statusText is not null)
		{
			_statusText.Text = text;
		}

		if (_statusHost is not null)
		{
			_statusHost.IsVisible = text.Length > 0;
		}
	}

	private void StatusCloseButtonOnClick(object sender, RoutedEventArgs e)
	{
		e.Handled = true;
		SetStatus(string.Empty);
	}

	private void SyncFullWidth()
	{
		PseudoClasses.Set(":fullwidth", IsFullWidth);

		if (_widthButton is null)
		{
			return;
		}

		var isFull = IsFullWidth;
		if (_widthButton.IsChecked != isFull)
		{
			_widthButton.IsChecked = isFull;
		}

		ToolTip.SetTip(_widthButton, isFull ? "Reading width" : "Full width");
	}

	private void SyncThemeBoxes()
	{
		var theme = ApplicationTheme.GetCurrent();
		var color = theme?.ThemeColor ?? ThemeColor.Blue;
		if ((color == ThemeColor.None) || (color == ThemeColor.Current))
		{
			color = ThemeColor.Blue;
		}

		if (_colorBox is not null && !Equals(_colorBox.SelectedItem, color))
		{
			_colorBox.SelectedItem = color;
		}

		var density = theme?.ThemeDensity ?? ThemeDensity.Normal;
		if (_densityBox is not null && !Equals(_densityBox.SelectedItem, density))
		{
			_densityBox.SelectedItem = density;
		}

		SyncThemeIcon();
	}

	private void SyncThemeIcon()
	{
		if (_themeIcon is null)
		{
			return;
		}

		var variant = ActualThemeVariant;
		var key = variant == ThemeVariant.Dark
			? "Icons.Sun"
			: variant == ThemeVariant.Light
				? "Icons.Moon"
				: "Icons.Moon.Sun";

		// TryGetResource only sees this control. Icons live on the application styles.
		object found = null;
		if (!this.TryFindResource(key, variant, out found))
		{
			found = Application.Current?.FindResource(key);
		}

		if (found is Geometry geometry)
		{
			_themeIcon.Data = geometry;
		}
	}

	private void SyncTreeSelection(string documentId)
	{
		// Selection sync in hierarchical grids is index-based; for v1 we expand and rely on user click.
		// Expand path to current document in full tree.
		ExpandPathToDocument(_fullRoots, documentId);
		if (_treeSource is not null)
		{
			// Refresh expander state when showing unfiltered tree
			if (string.IsNullOrWhiteSpace(_searchBox?.Text))
			{
				_treeSource.Items = _fullRoots;
			}
		}
	}

	private void ThemeButtonOnClick(object sender, RoutedEventArgs e)
	{
		e.Handled = true;
		var application = Application.Current;
		if (application is null)
		{
			return;
		}

		application.RequestedThemeVariant = application.RequestedThemeVariant == ThemeVariant.Dark
			? ThemeVariant.Light
			: ThemeVariant.Dark;
		SyncThemeIcon();
	}

	private void TopLevelOnBackRequested(object sender, RoutedEventArgs e)
	{
		if (!SystemBack.TryClaim(this, e, CanGoBack))
		{
			return;
		}

		GoBack();
		e.Handled = true;
	}

	private void TreeSelectionOnSelectionChanged(object sender, TreeSelectionModelSelectionChangedEventArgs<DocumentTreeNode> e)
	{
		if (_treeSource?.RowSelection is null)
		{
			return;
		}

		var selected = _treeSource.RowSelection.SelectedItem;
		if (selected is null || selected.IsFolder || string.IsNullOrEmpty(selected.DocumentId))
		{
			return;
		}

		if (Current is not null && string.Equals(Current.Id, selected.DocumentId, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		NavigateTo(selected.DocumentId);
	}

	private void TryScrollToPendingFragment()
	{
		if (string.IsNullOrEmpty(_pendingFragment))
		{
			return;
		}

		if (_markdownView.ScrollToFragment(_pendingFragment))
		{
			_pendingFragment = null;
		}
	}

	private void WidthButtonOnIsCheckedChanged(object sender, RoutedEventArgs e)
	{
		var isFull = _widthButton?.IsChecked == true;
		if (IsFullWidth != isFull)
		{
			IsFullWidth = isFull;
		}
	}

	#endregion
}