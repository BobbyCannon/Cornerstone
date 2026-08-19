#region References

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Cornerstone.Avalonia.Resources;
using Cornerstone.Avalonia.Text;
using Cornerstone.Parsers.Markdown;

#endregion

namespace Cornerstone.Avalonia.Controls;

/// <summary>
/// Renders a GFM table as an Avalonia grid with per-cell inline projection (links, emphasis).
/// </summary>
public class MarkdownTablePresenter : TemplatedControl
{
	#region Fields

	private Border _border;
	private Grid _grid;
	private MarkdownBlockGroup _group;
	private readonly List<CellHost> _hosts;
	private MarkdownView _view;

	#endregion

	#region Constructors

	public MarkdownTablePresenter()
	{
		_hosts = new();
	}

	#endregion

	#region Methods

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		_border = e.NameScope.Find<Border>("PART_Border");
		_grid = e.NameScope.Find<Grid>("PART_Grid");
		AttachToView(this.FindAncestorOfType<MarkdownView>());
		Rebuild();
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);
		AttachToView(this.FindAncestorOfType<MarkdownView>());
		Rebuild();
	}

	protected override void OnDataContextChanged(EventArgs e)
	{
		if (_group is not null)
		{
			_group.ContentChanged -= GroupOnContentChanged;
		}

		base.OnDataContextChanged(e);
		_group = DataContext as MarkdownBlockGroup;
		if (_group is not null)
		{
			_group.ContentChanged += GroupOnContentChanged;
		}

		Rebuild();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (_group is not null)
		{
			_group.ContentChanged -= GroupOnContentChanged;
		}

		DetachFromView();
		ClearHosts();
		base.OnDetachedFromVisualTree(e);
	}

	private void AttachToView(MarkdownView view)
	{
		if (ReferenceEquals(_view, view))
		{
			return;
		}

		DetachFromView();
		_view = view;
		if (_view is not null)
		{
			_view.PropertyChanged += ViewOnPropertyChanged;
		}
	}

	private void ClearHosts()
	{
		foreach (var host in _hosts)
		{
			host.Dispose();
		}

		_hosts.Clear();
		_grid?.Children.Clear();
		_grid?.RowDefinitions.Clear();
		_grid?.ColumnDefinitions.Clear();
	}

	private CellHost CreateCell(
		string cellSource,
		bool headerRow,
		ColumnAlignment align,
		int row,
		int col,
		int colCount,
		int rowCount)
	{
		var border = new Border
		{
			BorderBrush = ResourceService.GetColorAsBrush("BorderBrush"),
			BorderThickness = new Thickness(
				col == 0 ? 0 : 1,
				row == 0 ? 0 : 1,
				0,
				0),
			Padding = new Thickness(8, 6),
			Background = headerRow
				? ResourceService.GetColorAsBrush("Background04")
				: Brushes.Transparent
		};

		var renderer = new TextRenderer
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			CanHorizontallyScroll = false
		};

		// Word wrap inside cells for long descriptions (Project also applies ShowCaret = false).
		var links = MarkdownInlineProjector.Project(cellSource, renderer, _view);
		if (renderer.ViewModel is not null)
		{
			renderer.ViewModel.WordWrap = true;
			if (headerRow)
			{
				renderer.FontWeight = FontWeight.Bold;
			}
		}

		// Horizontal alignment via renderer content
		renderer.HorizontalAlignment = align switch
		{
			ColumnAlignment.Center => HorizontalAlignment.Center,
			ColumnAlignment.Right => HorizontalAlignment.Right,
			_ => HorizontalAlignment.Left
		};

		border.Child = renderer;
		Grid.SetRow(border, row);
		Grid.SetColumn(border, col);

		var host = new CellHost(border, renderer, links, _view);
		host.Attach();
		return host;
	}

	private void DetachFromView()
	{
		if (_view is not null)
		{
			_view.PropertyChanged -= ViewOnPropertyChanged;
		}

		_view = null;
	}

	private void GroupOnContentChanged(object sender, EventArgs e)
	{
		Rebuild();
	}

	private void Rebuild()
	{
		if (_grid is null || _group is null || _view is null)
		{
			return;
		}

		if ((_group.Blocks.Count != 1)
			|| (_group.Blocks[0].Type != MarkdownTokenizer.TokenTypeTable))
		{
			ClearHosts();
			return;
		}

		var source = _view.SourceSnapshot;
		if (string.IsNullOrEmpty(source) && (_view.Document.DocumentLength == 0))
		{
			ClearHosts();
			return;
		}

		source ??= _view.Document.ToString();
		var block = _group.Blocks[0];
		if ((block.StartOffset < 0)
			|| (block.EndOffset > source.Length)
			|| (block.Length <= 0))
		{
			ClearHosts();
			return;
		}

		var tableSource = source.AsSpan(block.StartOffset, block.Length);
		var model = MarkdownTableModel.Parse(tableSource);
		if ((model.ColumnCount == 0) || (model.Rows.Count == 0))
		{
			ClearHosts();
			return;
		}

		ClearHosts();

		for (var c = 0; c < model.ColumnCount; c++)
		{
			_grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
		}

		for (var r = 0; r < model.Rows.Count; r++)
		{
			_grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
		}

		var isHeader = model.HasHeader;
		for (var r = 0; r < model.Rows.Count; r++)
		{
			var row = model.Rows[r];
			var headerRow = isHeader && (r == 0);
			for (var c = 0; c < model.ColumnCount; c++)
			{
				var cellSource = c < row.Cells.Count ? row.Cells[c].Source : string.Empty;
				var align = c < model.Alignments.Count ? model.Alignments[c] : ColumnAlignment.Left;
				var host = CreateCell(cellSource, headerRow, align, r, c, model.ColumnCount, model.Rows.Count);
				_hosts.Add(host);
				_grid.Children.Add(host.Root);
			}
		}
	}

	private void ViewOnPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
	{
		if ((e.Property == MarkdownView.WordWrapProperty)
			|| (e.Property == FontSizeProperty)
			|| (e.Property == FontFamilyProperty)
			|| (e.Property == ForegroundProperty))
		{
			Rebuild();
		}
	}

	#endregion

	#region Classes

	private sealed class CellHost
	{
		#region Constants

		private const double LinkDragThreshold = 4;

		#endregion

		#region Fields

		private readonly IReadOnlyList<MarkdownProjectedLink> _links;
		private MarkdownProjectedLink _pendingLink;
		private bool _pendingLinkArmed;
		private Point _pendingLinkOrigin;
		private readonly TextRenderer _renderer;
		private readonly MarkdownView _view;

		#endregion

		#region Constructors

		public CellHost(Border root, TextRenderer renderer, IReadOnlyList<MarkdownProjectedLink> links, MarkdownView view)
		{
			Root = root;
			_renderer = renderer;
			_links = links ?? Array.Empty<MarkdownProjectedLink>();
			_view = view;
		}

		#endregion

		#region Properties

		public Border Root { get; }

		#endregion

		#region Methods

		public void Attach()
		{
			if (_links.Count == 0)
			{
				return;
			}

			_renderer.PointerMoved += OnPointerMoved;
			_renderer.AddHandler(PointerPressedEvent, OnPointerPressed, handledEventsToo: true);
			_renderer.AddHandler(PointerReleasedEvent, OnPointerReleased, handledEventsToo: true);
		}

		public void Dispose()
		{
			_renderer.PointerMoved -= OnPointerMoved;
			_renderer.RemoveHandler(PointerPressedEvent, OnPointerPressed);
			_renderer.RemoveHandler(PointerReleasedEvent, OnPointerReleased);
			ClearPendingLink();
		}

		private void ClearPendingLink()
		{
			_pendingLinkArmed = false;
			_pendingLink = default;
		}

		private void OnPointerMoved(object sender, PointerEventArgs e)
		{
			var point = e.GetPosition(_renderer);
			if (TryGetLinkAtPoint(point, out _))
			{
				_renderer.Cursor = new Cursor(StandardCursorType.Hand);
			}
			else
			{
				_renderer.Cursor = Cursor.Default;
			}

			if (!_pendingLinkArmed)
			{
				return;
			}

			var dx = point.X - _pendingLinkOrigin.X;
			var dy = point.Y - _pendingLinkOrigin.Y;
			if (((dx * dx) + (dy * dy)) > (LinkDragThreshold * LinkDragThreshold))
			{
				ClearPendingLink();
			}
		}

		private void OnPointerPressed(object sender, PointerPressedEventArgs e)
		{
			ClearPendingLink();
			var point = e.GetCurrentPoint(_renderer);
			if (point.Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
			{
				return;
			}

			if (!TryGetLinkAtPoint(point.Position, out var link))
			{
				return;
			}

			_pendingLinkArmed = true;
			_pendingLink = link;
			_pendingLinkOrigin = point.Position;
			e.Handled = true;
		}

		private void OnPointerReleased(object sender, PointerReleasedEventArgs e)
		{
			if (!_pendingLinkArmed || _view is null)
			{
				ClearPendingLink();
				return;
			}

			var point = e.GetCurrentPoint(_renderer);
			if (point.Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonReleased)
			{
				ClearPendingLink();
				return;
			}

			var link = _pendingLink;
			ClearPendingLink();
			e.Handled = true;
			_view.RaiseLinkClicked(link.Href, link.Text);
		}

		private bool TryGetLinkAtPoint(Point point, out MarkdownProjectedLink link)
		{
			link = default;
			if (_renderer?.ViewModel is null || (_links.Count == 0))
			{
				return false;
			}

			// Use paint-matched TextLayout widths (proportional + bold), not monospace GetAdvance.
			if (!_renderer.TryGetDocumentOffsetAtPoint(point, out var offset))
			{
				return false;
			}

			foreach (var candidate in _links)
			{
				if (candidate.Contains(offset))
				{
					link = candidate;
					return true;
				}
			}

			return false;
		}

		#endregion
	}

	#endregion
}