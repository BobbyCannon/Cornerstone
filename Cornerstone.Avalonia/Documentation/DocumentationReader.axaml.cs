#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Avalonia.Themes;
using Cornerstone.Avalonia.TreeDataGrid;
using Cornerstone.Avalonia.TreeDataGrid.Columns;
using Cornerstone.Avalonia.TreeDataGrid.Models;
using Cornerstone.Avalonia.TreeDataGrid.Selection;
using Cornerstone.Search;

#endregion

namespace Cornerstone.Avalonia.Documentation;

/// <summary>
/// Hosts a <see cref="MarkdownView" /> over a <see cref="DocumentationCatalog" />,
/// with optional left document tree, search, and reading chrome.
/// </summary>
public partial class DocumentationReader : UserControl
{
	#region Fields

	private readonly Stack<(string Id, string Fragment)> _backStack = new();
	private DocumentationCatalog _catalog;
	private ObservableCollection<DocumentTreeNode> _fullRoots = new();
	private string _pendingFragment;
	private HierarchicalTreeDataGridSource<DocumentTreeNode> _treeSource;

	#endregion

	#region Constructors

	public DocumentationReader()
	{
		InitializeComponent();
		InitializeTreeSource();

		// Mouse X1 (browser-style "back") — tunnel so child handlers (selection, etc.) do not swallow it.
		AddHandler(PointerPressedEvent, OnMouseBackPressed, RoutingStrategies.Tunnel);
		EnsureDefaultThemeColor();
		SyncThemeBoxes();
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
		get => DocumentsSplit?.IsPaneOpen ?? false;
		set
		{
			if (DocumentsSplit is not null)
			{
				DocumentsSplit.IsPaneOpen = value;
			}
		}
	}

	#endregion

	#region Methods

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

		if (MarkdownView is null || MarkdownView.Document is null)
		{
			// Template not ready — retry after layout.
			Dispatcher.UIThread.Post(() => EnsureEntryDocumentLoaded(), DispatcherPriority.Loaded);
			return false;
		}

		var hasContent = MarkdownView.Document.DocumentLength > 0;
		if (Current is not null && hasContent)
		{
			// Buffer already filled (tab reuse / prior NavigateTo) — force presenters to rebuild.
			// Home "fixes" empty UI only because it reloads; re-attach must refresh without reload.
			MarkdownView.RefreshPresentation();
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
					if (Current is null || (MarkdownView.Document.DocumentLength == 0))
					{
						NavigateTo(_catalog.Entry.Id, recordHistory: false);
					}
					else
					{
						MarkdownView.RefreshPresentation();
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
				MarkdownView.ScrollToHome();
			}

			MarkdownView.Document.Load(text);
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
				Dispatcher.UIThread.Post(() => MarkdownView.ScrollToHome(), DispatcherPriority.Loaded);
				Dispatcher.UIThread.Post(() => MarkdownView.ScrollToHome(), DispatcherPriority.Background);
			}

			return true;
		}
		catch (Exception ex)
		{
			SetStatus($"Failed to load: {ex.Message}");
			return false;
		}
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		MarkdownView.LinkClicked += MarkdownViewOnLinkClicked;
		RefreshChrome();

		// Tab switch: control may be reused with catalog/Current set but empty markdown presenters.
		EnsureEntryDocumentLoaded();
		Dispatcher.UIThread.Post(
			() =>
			{
				EnsureEntryDocumentLoaded();
				if (MarkdownView is not null && (MarkdownView.Document?.DocumentLength > 0))
				{
					MarkdownView.RefreshPresentation();
				}
			},
			DispatcherPriority.Loaded);
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		MarkdownView.LinkClicked -= MarkdownViewOnLinkClicked;
		base.OnDetachedFromVisualTree(e);
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
			if (EmptyFilterText is not null)
			{
				EmptyFilterText.IsVisible = false;
			}
		}
		else
		{
			roots = FilterTree(_fullRoots, filter);
			if (EmptyFilterText is not null)
			{
				EmptyFilterText.IsVisible = roots.Count == 0;
			}
			ExpandAll(roots);
		}

		if (_treeSource is not null)
		{
			_treeSource.Items = roots;
		}
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
		if (ColorBox?.SelectedItem is ThemeColor color)
		{
			var theme = Themes.Theme.GetCornerstoneTheme();
			if (theme is not null)
			{
				theme.ThemeColor = color;
			}
		}
	}

	private void DensityBoxOnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (DensityBox?.SelectedItem is ThemeDensity density)
		{
			CornerstoneTheme.SelectThemeDensity(density);
		}
	}

	private void DocumentsPaneButtonOnClick(object sender, RoutedEventArgs e)
	{
		if (DocumentsSplit is null)
		{
			return;
		}

		DocumentsSplit.IsPaneOpen = !DocumentsSplit.IsPaneOpen;
	}

	private static void EnsureDefaultThemeColor()
	{
		var theme = Themes.Theme.GetCornerstoneTheme();
		if (theme is null)
		{
			return;
		}

		if ((theme.ThemeColor == ThemeColor.None) || (theme.ThemeColor == ThemeColor.Current))
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

	private static string FormatBreadcrumb(string documentId)
	{
		if (string.IsNullOrEmpty(documentId))
		{
			return string.Empty;
		}

		var normalized = documentId.Replace('\\', '/').Trim('/');
		var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
		for (var i = 0; i < parts.Length; i++)
		{
			parts[i] = Path.GetFileNameWithoutExtension(parts[i]);
		}

		return string.Join("  ›  ", parts);
	}

	private static string FormatTitle(string documentId)
	{
		if (string.IsNullOrEmpty(documentId))
		{
			return "Documentation";
		}

		var name = Path.GetFileNameWithoutExtension(documentId.Replace('\\', '/'));
		return string.IsNullOrEmpty(name) ? documentId : name;
	}

	private void HomeButtonOnClick(object sender, RoutedEventArgs e)
	{
		GoHome();
	}

	private void InitializeTreeSource()
	{
		_fullRoots = new ObservableCollection<DocumentTreeNode>();
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

		DocTree.ItemsSource = _treeSource;
		if (_treeSource.RowSelection is not null)
		{
			_treeSource.RowSelection.SelectionChanged += TreeSelectionOnSelectionChanged;
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
				MarkdownView.ScrollToFragment(fragment);
				SetStatus(string.Empty);
				return;
			}

			NavigateTo(document.Id, fragment);
			return;
		}

		e.Handled = true;
		SetStatus("Link target not in catalog.");
	}

	private void OnMouseBackPressed(object sender, PointerPressedEventArgs e)
	{
		var update = e.GetCurrentPoint(this).Properties.PointerUpdateKind;
		if (update != PointerUpdateKind.XButton1Pressed)
		{
			return;
		}

		if (!CanGoBack)
		{
			return;
		}

		GoBack();
		e.Handled = true;
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
			ApplyFilter(SearchBox?.Text);
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

		ApplyFilter(SearchBox?.Text);
	}

	private void RefreshChrome()
	{
		if (BackButton is not null)
		{
			BackButton.IsEnabled = CanGoBack;
			BackButton.Opacity = CanGoBack ? 1.0 : 0.35;
		}

		var id = Current?.Id ?? string.Empty;
		if (TitleText is not null)
		{
			TitleText.Text = FormatTitle(id);
		}

		if (PathText is not null)
		{
			PathText.Text = FormatBreadcrumb(id);
			PathText.IsVisible = !string.IsNullOrEmpty(id);
		}

		if (ExportButton is not null)
		{
			var canExport = _catalog is not null && (_catalog.Documents.Count > 0);
			ExportButton.IsEnabled = canExport;
			ExportButton.Opacity = canExport ? 1.0 : 0.35;
		}

		SyncThemeBoxes();
	}

	private void SearchBoxOnTextChanged(object sender, TextChangedEventArgs e)
	{
		ApplyFilter(SearchBox?.Text);
	}

	private void SetStatus(string message)
	{
		var text = message ?? string.Empty;
		if (StatusText is not null)
		{
			StatusText.Text = text;
		}

		if (StatusHost is not null)
		{
			StatusHost.IsVisible = text.Length > 0;
		}
	}

	private void StatusCloseButtonOnClick(object sender, RoutedEventArgs e)
	{
		e.Handled = true;
		SetStatus(string.Empty);
	}

	private void SyncThemeBoxes()
	{
		var theme = Themes.Theme.GetCornerstoneTheme();
		var color = theme?.ThemeColor ?? ThemeColor.Blue;
		if ((color == ThemeColor.None) || (color == ThemeColor.Current))
		{
			color = ThemeColor.Blue;
		}

		if (ColorBox is not null && !Equals(ColorBox.SelectedItem, color))
		{
			ColorBox.SelectedItem = color;
		}

		var density = theme?.ThemeDensity ?? ThemeDensity.Normal;
		if (DensityBox is not null && !Equals(DensityBox.SelectedItem, density))
		{
			DensityBox.SelectedItem = density;
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
			if (string.IsNullOrWhiteSpace(SearchBox?.Text))
			{
				_treeSource.Items = _fullRoots;
			}
		}
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

		if (MarkdownView.ScrollToFragment(_pendingFragment))
		{
			_pendingFragment = null;
		}
	}

	#endregion
}