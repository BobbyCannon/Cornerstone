#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Cornerstone.Avalonia.TreeDataGrid.Cells;
using Cornerstone.Avalonia.TreeDataGrid.Models;
using Cornerstone.Avalonia.TreeDataGrid.Selection;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid;

public partial class TreeDataGrid : TemplatedControl
{
	#region Constants

	private const double AutoScrollMargin = 60;
	private const int AutoScrollSpeed = 50;

	#endregion

	#region Fields

	public static readonly DirectProperty<TreeDataGrid, IColumns> ColumnsProperty;
	public static readonly DataFormat<TreeDataGridDragInfo> DragDropDataFormat;
	public static readonly DirectProperty<TreeDataGrid, TreeDataGridElementFactory> ElementFactoryProperty;
	public static readonly DirectProperty<TreeDataGrid, ITreeDataGridSource> ItemsSourceProperty;
	public static readonly RoutedEvent<TreeDataGridRowDragEventArgs> RowDragOverEvent;
	public static readonly RoutedEvent<TreeDataGridRowDragStartedEventArgs> RowDragStartedEvent;
	public static readonly RoutedEvent<TreeDataGridRowDragEventArgs> RowDropEvent;
	public static readonly DirectProperty<TreeDataGrid, IRows> RowsProperty;
	public static readonly DirectProperty<TreeDataGrid, ScrollViewer> ScrollProperty;
	public static readonly StyledProperty<bool> ShowColumnHeadersProperty;

	private bool _autoScrollDirection;
	private DispatcherTimer _autoScrollTimer;
	private TreeDataGridCellEventArgs _cellArgs;
	private IColumns _columns;
	private Canvas _dragAdorner;
	private TreeDataGridElementFactory _elementFactory;
	private IScrollable _headerScroll;
	private bool _hideDragAdorner;
	private ITreeDataGridSource _itemsSource;
	private TreeDataGridRowEventArgs _rowArgs;
	private IRows _rows;
	private ScrollViewer _scrollViewer;
	private ITreeDataGridSelectionInteraction _selection;
	private Control _userSortColumn;
	private ListSortDirection _userSortDirection;

	#endregion

	#region Constructors

	public TreeDataGrid()
	{
		AddHandler(Button.ClickEvent, OnClick);
		AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
		AddHandler(DoubleTappedEvent, OnDoubleTappedEvent, RoutingStrategies.Tunnel);
	}

	static TreeDataGrid()
	{
		ColumnsProperty = AvaloniaProperty.RegisterDirect<TreeDataGrid, IColumns>(nameof(Columns), o => o.Columns);
		ElementFactoryProperty = AvaloniaProperty.RegisterDirect<TreeDataGrid, TreeDataGridElementFactory>(nameof(ElementFactory), o => o.ElementFactory, (o, v) => o.ElementFactory = v);
		ItemsSourceProperty = AvaloniaProperty.RegisterDirect<TreeDataGrid, ITreeDataGridSource>(nameof(ItemsSource), o => o.ItemsSource, (o, v) => o.ItemsSource = v);
		RowDragOverEvent = RoutedEvent.Register<TreeDataGrid, TreeDataGridRowDragEventArgs>(nameof(RowDragOver), RoutingStrategies.Bubble);
		RowDragStartedEvent = RoutedEvent.Register<TreeDataGrid, TreeDataGridRowDragStartedEventArgs>(nameof(RowDragStarted), RoutingStrategies.Bubble);
		RowDropEvent = RoutedEvent.Register<TreeDataGrid, TreeDataGridRowDragEventArgs>(nameof(RowDrop), RoutingStrategies.Bubble);
		RowsProperty = AvaloniaProperty.RegisterDirect<TreeDataGrid, IRows>(nameof(Rows), o => o.Rows, (o, v) => o.Rows = v);
		ScrollProperty = AvaloniaProperty.RegisterDirect<TreeDataGrid, ScrollViewer>(nameof(ScrollViewer), o => o.ScrollViewer);
		ShowColumnHeadersProperty = AvaloniaProperty.Register<TreeDataGrid, bool>(nameof(ShowColumnHeaders), true);

		DragDrop.DragOverEvent.AddClassHandler<TreeDataGrid>((x, e) => x.OnDragOver(e));
		DragDrop.DragLeaveEvent.AddClassHandler<TreeDataGrid>((x, e) => x.OnDragLeave(e));
		DragDrop.DropEvent.AddClassHandler<TreeDataGrid>((x, e) => x.OnDrop(e));
		DragDropDataFormat = DataFormat.CreateInProcessFormat<TreeDataGridDragInfo>("cornerstone-treedatagrid");
	}

	#endregion

	#region Properties

	[StyledProperty]
	public partial bool AutoDragDropRows { get; set; }

	[StyledProperty(DefaultValue = true)]
	public partial bool CanUserResizeColumns { get; set; }

	[StyledProperty(DefaultValue = true)]
	public partial bool CanUserSortColumns { get; set; }

	public TreeDataGridColumnHeadersPresenter ColumnHeadersPresenter { get; private set; }

	public IColumns Columns
	{
		get => _columns;
		private set => SetAndRaise(ColumnsProperty, ref _columns, value);
	}

	public ITreeDataGridCellSelectionModel ColumnSelection => ItemsSource?.Selection as ITreeDataGridCellSelectionModel;

	public TreeDataGridElementFactory ElementFactory
	{
		get => _elementFactory ??= CreateDefaultElementFactory();
		set
		{
			_ = value ?? throw new ArgumentNullException(nameof(value));
			SetAndRaise(ElementFactoryProperty, ref _elementFactory!, value);
		}
	}

	public ITreeDataGridSource ItemsSource
	{
		get => _itemsSource;
		set
		{
			if (_itemsSource == value)
			{
				return;
			}

			if (_itemsSource != null)
			{
				_itemsSource.PropertyChanged -= OnItemsSourcePropertyChanged;
				_itemsSource.Sorted -= OnItemsSourceSorted;
			}

			var oldSource = _itemsSource;
			_itemsSource = value;

			Columns = _itemsSource?.Columns;
			Rows = _itemsSource?.Rows;
			SelectionInteraction = _itemsSource?.Selection as ITreeDataGridSelectionInteraction;

			if (_itemsSource != null)
			{
				_itemsSource.PropertyChanged += OnItemsSourcePropertyChanged;
				_itemsSource.Sorted += OnItemsSourceSorted;
			}

			RaisePropertyChanged(ItemsSourceProperty, oldSource, _itemsSource);
		}
	}

	[StyledProperty(DefaultValue = 28)]
	public partial int MinRowHeight { get; set; }

	/// <summary>
	/// When true (default), every row is laid out at MinRowHeight so scroll extent is exact.
	/// When false, rows size to content (MinHeight only); scroll uses height estimates and can thrash.
	/// </summary>
	[StyledProperty(DefaultValue = true)]
	public partial bool UseFixedRowHeight { get; set; }

	public IRows Rows
	{
		get => _rows;
		private set => SetAndRaise(RowsProperty, ref _rows, value);
	}

	public ITreeDataGridRowSelectionModel RowSelection => ItemsSource?.Selection as ITreeDataGridRowSelectionModel;

	public TreeDataGridRowsPresenter RowsPresenter { get; private set; }

	public ScrollViewer ScrollViewer
	{
		get => _scrollViewer;
		private set => SetAndRaise(ScrollProperty, ref _scrollViewer, value);
	}

	public bool ShowColumnHeaders
	{
		get => GetValue(ShowColumnHeadersProperty);
		set => SetValue(ShowColumnHeadersProperty, value);
	}

	internal ITreeDataGridSelectionInteraction SelectionInteraction
	{
		get => _selection;
		set
		{
			if (_selection != value)
			{
				if (_selection != null)
				{
					_selection.SelectionChanged -= OnSelectionInteractionChanged;
				}
				_selection = value;
				if (_selection != null)
				{
					_selection.SelectionChanged += OnSelectionInteractionChanged;
				}
			}
		}
	}

	#endregion

	#region Methods

	public bool QueryCancelSelection()
	{
		if (SelectionChanging is null)
		{
			return false;
		}
		var e = new CancelEventArgs();
		SelectionChanging(this, e);
		return e.Cancel;
	}

	public void ScrollIntoView(object item)
	{
		var source = ItemsSource;
		var itemsList = (IList) source.Items;
		var index = itemsList.IndexOf(item);
		if (index >= 0)
		{
			RowsPresenter.BringIntoView(index);
		}
	}

	public Control TryGetCell(int columnIndex, int rowIndex)
	{
		if (TryGetRow(rowIndex) is { } row
			&& row.TryGetCell(columnIndex) is { } cell)
		{
			return cell;
		}

		return null;
	}

	public bool TryGetCell(Control element, [NotNullWhen(true)] out TreeDataGridCell result)
	{
		if (element.FindAncestorOfType<TreeDataGridCell>(true)
			is { ColumnIndex: >= 0, RowIndex: >= 0 } cell)
		{
			result = cell;
			return true;
		}

		result = null;
		return false;
	}

	public TreeDataGridRow TryGetRow(int rowIndex)
	{
		return RowsPresenter?.TryGetElement(rowIndex) as TreeDataGridRow;
	}

	public bool TryGetRow(Control element, [NotNullWhen(true)] out TreeDataGridRow result)
	{
		if (element is TreeDataGridRow { RowIndex: >= 0 } row)
		{
			result = row;
			return true;
		}

		do
		{
			result = element?.FindAncestorOfType<TreeDataGridRow>();
			if (result?.RowIndex >= 0)
			{
				break;
			}
			element = result;
		} while (result is not null);

		return result is not null;
	}

	public bool TryGetRowModel<TModel>(Control element, [NotNullWhen(true)] out TModel result)
		where TModel : notnull
	{
		if (ItemsSource is not null
			&& TryGetRow(element, out var row)
			&& (row.RowIndex < ItemsSource.Rows.Count)
			&& ItemsSource.Rows[row.RowIndex] is IRow<TModel> rowWithModel)
		{
			result = rowWithModel.Model;
			return true;
		}

		result = default;
		return false;
	}

	protected virtual TreeDataGridElementFactory CreateDefaultElementFactory()
	{
		return new();
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		if (ScrollViewer is { } s && _headerScroll is ScrollViewer h)
		{
			s.ScrollChanged -= OnScrollChanged;
			h.ScrollChanged -= OnHeaderScrollChanged;
		}

		base.OnApplyTemplate(e);
		ColumnHeadersPresenter = e.NameScope.Find<TreeDataGridColumnHeadersPresenter>("PART_ColumnHeadersPresenter");
		RowsPresenter = e.NameScope.Find<TreeDataGridRowsPresenter>("PART_RowsPresenter");
		ScrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
		_headerScroll = e.NameScope.Find<ScrollViewer>("PART_HeaderScrollViewer");

		if (ScrollViewer is { } s1
			&& _headerScroll is ScrollViewer h1)
		{
			s1.ScrollChanged += OnScrollChanged;
			h1.ScrollChanged += OnHeaderScrollChanged;
		}
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnDetachedFromVisualTree(e);
		StopDrag();
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		_selection?.OnKeyDown(this, e);
	}

	protected override void OnKeyUp(KeyEventArgs e)
	{
		base.OnKeyUp(e);
		_selection?.OnKeyUp(this, e);
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		base.OnPointerMoved(e);
		_selection?.OnPointerMoved(this, e);
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		base.OnPointerPressed(e);
		_selection?.OnPointerPressed(this, e);
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		base.OnPointerReleased(e);
		_selection?.OnPointerReleased(this, e);
	}

	protected void OnPreviewKeyDown(object o, KeyEventArgs e)
	{
		_selection?.OnPreviewKeyDown(this, e);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property == AutoDragDropRowsProperty)
		{
			DragDrop.SetAllowDrop(this, change.GetNewValue<bool>());
		}
		else if (change.Property == MinRowHeightProperty)
		{
			ApplyRowHeightToRealizedRows();
		}
		else if (change.Property == UseFixedRowHeightProperty)
		{
			ApplyRowHeightToRealizedRows();
		}
	}

	private void ApplyRowHeightToRealizedRows()
	{
		if (RowsPresenter is null)
		{
			return;
		}

		var minHeight = (double) MinRowHeight;
		var fixedHeight = UseFixedRowHeight;
		foreach (var element in RowsPresenter.GetRealizedElements())
		{
			if (element is not TreeDataGridRow row)
			{
				continue;
			}

			ApplyRowHeight(row, minHeight, fixedHeight);
		}

		RowsPresenter.InvalidateMeasure();
	}

	/// <summary>
	/// Apply host row-height policy to a realized row (fixed slot vs content-sized).
	/// </summary>
	internal static void ApplyRowHeight(TreeDataGridRow row, double minHeight, bool useFixedHeight)
	{
		if (row is null)
		{
			return;
		}

		row.MinHeight = minHeight;
		if (useFixedHeight)
		{
			row.Height = minHeight;
			row.MaxHeight = minHeight;
		}
		else
		{
			// Clear forced slot so content can measure naturally (non-uniform rows).
			row.ClearValue(HeightProperty);
			row.ClearValue(MaxHeightProperty);
		}
	}

	protected override void OnTextInput(TextInputEventArgs e)
	{
		base.OnTextInput(e);

		if (e.Text is { Length: > 0 } && char.IsControl(e.Text[0]))
		{
			return;
		}

		_selection?.OnTextInput(this, e);
	}

	/// <summary>
	/// Avalonia 12 drag start – accepts PointerEventArgs (the correct type)
	/// </summary>
	internal void OnDragStarted(PointerPressedEventArgs trigger)
	{
		if (_itemsSource is null || RowSelection is null)
		{
			return;
		}

		var allowedEffects = AutoDragDropRows //&& !_itemsSource.IsSorted
			? DragDropEffects.Move
			: DragDropEffects.None;

		var route = BuildEventRoute(RowDragStartedEvent);
		if (route.HasHandlers)
		{
			var e = new TreeDataGridRowDragStartedEventArgs(RowSelection.SelectedItems!)
			{
				AllowedEffects = allowedEffects
			};
			RaiseEvent(e);
			allowedEffects = e.AllowedEffects;
		}

		if (allowedEffects == DragDropEffects.None)
		{
			return;
		}

		var data = new DataTransfer();
		var dragInfo = new TreeDataGridDragInfo(this, _itemsSource, RowSelection.SelectedIndexes.ToList());
		data.Add(DataTransferItem.Create(DragDropDataFormat, dragInfo));

		_ = DragDrop.DoDragDropAsync(trigger, data, allowedEffects);
	}

	internal void RaiseCellClearing(TreeDataGridCell cell, int columnIndex, int rowIndex)
	{
		if (CellClearing is not null)
		{
			_cellArgs ??= new TreeDataGridCellEventArgs();
			_cellArgs.Update(cell, columnIndex, rowIndex);
			CellClearing(this, _cellArgs);
			_cellArgs.Update(null, -1, -1);
		}
	}

	internal void RaiseCellPrepared(TreeDataGridCell cell, int columnIndex, int rowIndex)
	{
		if (CellPrepared is not null)
		{
			_cellArgs ??= new TreeDataGridCellEventArgs();
			_cellArgs.Update(cell, columnIndex, rowIndex);
			CellPrepared(this, _cellArgs);
			_cellArgs.Update(null, -1, -1);
		}
	}

	internal void RaiseCellValueChanged(TreeDataGridCell cell, int columnIndex, int rowIndex)
	{
		if (CellValueChanged is not null)
		{
			_cellArgs ??= new TreeDataGridCellEventArgs();
			_cellArgs.Update(cell, columnIndex, rowIndex);
			CellValueChanged(this, _cellArgs);
			_cellArgs.Update(null, -1, -1);
		}
	}

	internal void RaiseRowClearing(TreeDataGridRow row, int rowIndex)
	{
		if (RowClearing is not null)
		{
			_rowArgs ??= new TreeDataGridRowEventArgs();
			_rowArgs.Update(row, rowIndex);
			RowClearing(this, _rowArgs);
			_rowArgs.Update(null, -1);
		}
	}

	internal void RaiseRowPrepared(TreeDataGridRow row, int rowIndex)
	{
		if (RowPrepared is not null)
		{
			_rowArgs ??= new TreeDataGridRowEventArgs();
			_rowArgs.Update(row, rowIndex);
			RowPrepared(this, _rowArgs);
			_rowArgs.Update(null, -1);
		}
	}

	private void AutoScroll(bool direction)
	{
		if (_autoScrollTimer is null)
		{
			_autoScrollTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(AutoScrollSpeed)
			};
			_autoScrollTimer.Tick += OnAutoScrollTick;
		}

		_autoScrollDirection = direction;

		if (!_autoScrollTimer.IsEnabled)
		{
			OnAutoScrollTick(null, EventArgs.Empty);
		}

		_autoScrollTimer.Start();
	}

	[MemberNotNullWhen(true, nameof(_itemsSource))]
	private bool CalculateAutoDragDrop(
		TreeDataGridRow targetRow,
		DragEventArgs e,
		[NotNullWhen(true)] out TreeDataGridDragInfo data,
		out TreeDataGridRowDropPosition position)
	{
		data = null;
		position = TreeDataGridRowDropPosition.None;

		if (!AutoDragDropRows
			|| _itemsSource is null
			|| _itemsSource.IsSorted
			|| targetRow is null)
		{
			return false;
		}

		try
		{
			var info = e.DataTransfer.TryGetValue(DragDropDataFormat);
			if (info is null || (info.Source != _itemsSource))
			{
				return false;
			}

			position = GetDropPosition(_itemsSource, e, targetRow);

			var targetIndex = _itemsSource.Rows.RowIndexToModelIndex(targetRow.RowIndex);

			// We can't drop rows into themselves or their descendants.
			foreach (var sourceIndex in info.Indexes)
			{
				if (sourceIndex.IsAncestorOf(targetIndex)
					|| ((sourceIndex == targetIndex)
						&& (position == TreeDataGridRowDropPosition.Inside)))
				{
					position = TreeDataGridRowDropPosition.None;
					return false;
				}
			}

			data = info;
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static TreeDataGridRowDropPosition GetDropPosition(
		ITreeDataGridSource source,
		DragEventArgs e,
		TreeDataGridRow row)
	{
		var rowY = e.GetPosition(row).Y / row.Bounds.Height;

		if (source.IsHierarchical)
		{
			if (rowY < 0.33)
			{
				return TreeDataGridRowDropPosition.Before;
			}
			if (rowY > 0.66)
			{
				return TreeDataGridRowDropPosition.After;
			}
			return TreeDataGridRowDropPosition.Inside;
		}

		return rowY < 0.5
			? TreeDataGridRowDropPosition.Before
			: TreeDataGridRowDropPosition.After;
	}

	private Canvas GetOrCreateDragAdorner()
	{
		_hideDragAdorner = false;

		if (_dragAdorner is not null)
		{
			return _dragAdorner;
		}

		var adornerLayer = AdornerLayer.GetAdornerLayer(this);

		if (adornerLayer is null)
		{
			return null;
		}

		_dragAdorner ??= new Canvas
		{
			Children =
			{
				new Rectangle
				{
					Stroke = TextElement.GetForeground(this),
					StrokeThickness = 2
				}
			},
			IsHitTestVisible = false
		};

		adornerLayer.Children.Add(_dragAdorner);
		AdornerLayer.SetAdornedElement(_dragAdorner, this);
		return _dragAdorner;
	}

	private void HideDragAdorner()
	{
		_hideDragAdorner = true;

		DispatcherTimer.RunOnce(() =>
		{
			if (_hideDragAdorner && _dragAdorner?.Parent is AdornerLayer layer)
			{
				layer.Children.Remove(_dragAdorner);
				_dragAdorner = null;
			}
		}, TimeSpan.FromMilliseconds(50));
	}

	private void OnAutoScrollTick(object sender, EventArgs e)
	{
		if (ScrollViewer is { } scroll)
		{
			if (!_autoScrollDirection)
			{
				scroll.LineUp();
			}
			else
			{
				scroll.LineDown();
			}
		}
	}

	private void OnClick(object sender, RoutedEventArgs e)
	{
		if (_itemsSource is not null
			&& e.Source is TreeDataGridColumnHeader columnHeader
			&& (columnHeader.ColumnIndex >= 0)
			&& (columnHeader.ColumnIndex < _itemsSource.Columns.Count)
			&& CanUserSortColumns)
		{
			if (_userSortColumn != columnHeader)
			{
				_userSortColumn = columnHeader;
				_userSortDirection = ListSortDirection.Ascending;
			}
			else
			{
				_userSortDirection = _userSortDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
			}

			var column = _itemsSource.Columns[columnHeader.ColumnIndex];
			_itemsSource.SortBy(column, _userSortDirection);
		}
	}

	private void OnDoubleTappedEvent(object sender, TappedEventArgs e)
	{
	}

	private void OnDragLeave(RoutedEventArgs e)
	{
		StopDrag();
	}

	private void OnDragOver(DragEventArgs e)
	{
		// Attempt to find a row. Do not return early if none is found.
		// This allows handling drops on empty areas or headers.
		TryGetRow(e.Source as Control, out var row);

		// If we have a row, try to calculate the drop position.
		// This will return false if row is null or AutoDragDropRows is disabled.
		CalculateAutoDragDrop(row, e, out _, out var position);

		// If auto drag/drop logic fails (e.g., no row found), we still want to raise the event
		// so external handlers can process the drag over.
		var route = BuildEventRoute(RowDragOverEvent);

		if (route.HasHandlers)
		{
			var ev = new TreeDataGridRowDragEventArgs(RowDragOverEvent, row, e) { Position = position };
			RaiseEvent(ev);
		}

		// Only show adorner if we have a valid row and position.
		if (row != null)
		{
			ShowDragAdorner(row, position);
		}

		if (ScrollViewer is { } scroller)
		{
			var rowsPosition = e.GetPosition(scroller);

			if (rowsPosition.Y < AutoScrollMargin)
			{
				AutoScroll(false);
			}
			else if (rowsPosition.Y > (Bounds.Height - AutoScrollMargin))
			{
				AutoScroll(true);
			}
			else
			{
				_autoScrollTimer?.Stop();
			}
		}
	}

	private void OnDrop(DragEventArgs e)
	{
		StopDrag();

		TryGetRow(e.Source as Control, out var row);

		var autoDrop = CalculateAutoDragDrop(row, e, out var data, out var position);
		var route = BuildEventRoute(RowDropEvent);

		if (route.HasHandlers)
		{
			var ev = new TreeDataGridRowDragEventArgs(RowDropEvent, row, e) { Position = position };
			RaiseEvent(ev);

			if (ev.Handled || (e.DragEffects != DragDropEffects.Move))
			{
				return;
			}

			position = ev.Position;
		}

		if (autoDrop &&
			_itemsSource is not null &&
			row is not null &&
			(position != TreeDataGridRowDropPosition.None))
		{
			var targetIndex = _itemsSource.Rows.RowIndexToModelIndex(row.RowIndex);
			_itemsSource.DragDropRows(_itemsSource, data!.Indexes, targetIndex, position, e.DragEffects);
		}
	}

	private void OnHeaderScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		if (ScrollViewer is not null && _headerScroll is not null && !DoubleExtensions.IsZero(e.OffsetDelta.X))
		{
			ScrollViewer.Offset = ScrollViewer.Offset.WithX(_headerScroll.Offset.X);
		}
	}

	private void OnItemsSourcePropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(ITreeDataGridSource.Selection))
		{
			SelectionInteraction = ItemsSource?.Selection as ITreeDataGridSelectionInteraction;
			RowsPresenter?.UpdateSelection(SelectionInteraction);
		}
	}

	private void OnItemsSourceSorted()
	{
		RowsPresenter?.RecycleAllElements();
		RowsPresenter?.InvalidateMeasure();
	}

	private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		if (ScrollViewer is not null && _headerScroll is not null && !DoubleExtensions.IsZero(e.OffsetDelta.X))
		{
			_headerScroll.Offset = _headerScroll.Offset.WithX(ScrollViewer.Offset.X);
		}
	}

	private void OnSelectionInteractionChanged(object sender, EventArgs e)
	{
		RowsPresenter?.UpdateSelection(SelectionInteraction);
	}

	private void ShowDragAdorner(TreeDataGridRow row, TreeDataGridRowDropPosition position)
	{
		if ((position == TreeDataGridRowDropPosition.None) ||
			row.TransformToVisual(this) is not { } transform)
		{
			HideDragAdorner();
			return;
		}

		var adorner = GetOrCreateDragAdorner();
		if (adorner is null)
		{
			return;
		}

		var rectangle = (Rectangle) adorner.Children[0];
		var rowBounds = new Rect(row.Bounds.Size).TransformToAABB(transform);

		Canvas.SetLeft(rectangle, rowBounds.Left);
		rectangle.Width = rowBounds.Width;

		switch (position)
		{
			case TreeDataGridRowDropPosition.Before:
				Canvas.SetTop(rectangle, rowBounds.Top);
				rectangle.Height = 0;
				break;
			case TreeDataGridRowDropPosition.After:
				Canvas.SetTop(rectangle, rowBounds.Bottom);
				rectangle.Height = 0;
				break;
			case TreeDataGridRowDropPosition.Inside:
				Canvas.SetTop(rectangle, rowBounds.Top);
				rectangle.Height = rowBounds.Height;
				break;
		}
	}

	private void StopDrag()
	{
		HideDragAdorner();
		_autoScrollTimer?.Stop();
	}

	#endregion

	#region Events

	public event EventHandler<TreeDataGridCellEventArgs> CellClearing;
	public event EventHandler<TreeDataGridCellEventArgs> CellPrepared;
	public event EventHandler<TreeDataGridCellEventArgs> CellValueChanged;
	public event EventHandler<TreeDataGridRowEventArgs> RowClearing;

	public event EventHandler<TreeDataGridRowDragEventArgs> RowDragOver
	{
		add => AddHandler(RowDragOverEvent, value!);
		remove => RemoveHandler(RowDragOverEvent, value!);
	}

	public event EventHandler<TreeDataGridRowDragStartedEventArgs> RowDragStarted
	{
		add => AddHandler(RowDragStartedEvent, value!);
		remove => RemoveHandler(RowDragStartedEvent, value!);
	}

	public event EventHandler<TreeDataGridRowDragEventArgs> RowDrop
	{
		add => AddHandler(RowDropEvent, value!);
		remove => RemoveHandler(RowDropEvent, value!);
	}

	public event EventHandler<TreeDataGridRowEventArgs> RowPrepared;

	public event CancelEventHandler SelectionChanging;

	#endregion

	#region Records

	public sealed record TreeDataGridDragInfo(TreeDataGrid TreeDataGrid, ITreeDataGridSource Source, List<IndexPath> Indexes);

	#endregion
}