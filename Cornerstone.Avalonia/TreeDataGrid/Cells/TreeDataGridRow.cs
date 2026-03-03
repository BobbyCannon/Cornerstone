#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Cornerstone.Avalonia.TreeDataGrid.Models;
using Cornerstone.Avalonia.TreeDataGrid.Selection;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Cells;

[PseudoClasses(":selected")]
public class TreeDataGridRow : TemplatedControl
{
	#region Constants

	private const double DragDistance = 3;

	#endregion

	#region Fields

	public static readonly DirectProperty<TreeDataGridRow, IColumns> ColumnsProperty;
	public static readonly DirectProperty<TreeDataGridRow, TreeDataGridElementFactory> ElementFactoryProperty;
	public static readonly DirectProperty<TreeDataGridRow, bool> IsSelectedProperty;
	public static readonly DirectProperty<TreeDataGridRow, IRows> RowsProperty;

	private IColumns _columns;
	private TreeDataGridElementFactory _elementFactory;
	private bool _isSelected;
	private Point _mouseDownPosition = s_InvalidPoint;
	private IRows _rows;
	private TreeDataGrid _treeDataGrid;
	private static readonly Point s_InvalidPoint;

	#endregion

	#region Constructors

	static TreeDataGridRow()
	{
		ColumnsProperty = AvaloniaProperty.RegisterDirect<TreeDataGridRow, IColumns>(nameof(Columns), o => o.Columns);
		ElementFactoryProperty = AvaloniaProperty.RegisterDirect<TreeDataGridRow, TreeDataGridElementFactory>(nameof(ElementFactory), o => o.ElementFactory, (o, v) => o.ElementFactory = v);
		IsSelectedProperty = AvaloniaProperty.RegisterDirect<TreeDataGridRow, bool>(nameof(IsSelected), o => o.IsSelected);
		RowsProperty = AvaloniaProperty.RegisterDirect<TreeDataGridRow, IRows>(nameof(Rows), o => o.Rows);
		s_InvalidPoint = new(double.NegativeInfinity, double.NegativeInfinity);
	}

	#endregion

	#region Properties

	public TreeDataGridCellsPresenter CellsPresenter { get; private set; }

	public IColumns Columns
	{
		get => _columns;
		private set => SetAndRaise(ColumnsProperty, ref _columns, value);
	}

	public TreeDataGridElementFactory ElementFactory
	{
		get => _elementFactory;
		set => SetAndRaise(ElementFactoryProperty, ref _elementFactory, value);
	}

	public bool IsSelected
	{
		get => _isSelected;
		private set => SetAndRaise(IsSelectedProperty, ref _isSelected, value);
	}

	public object Model => DataContext;
	public int RowIndex { get; private set; }

	public IRows Rows
	{
		get => _rows;
		private set => SetAndRaise(RowsProperty, ref _rows, value);
	}

	#endregion

	#region Methods

	public void Realize(
		TreeDataGridElementFactory elementFactory,
		ITreeDataGridSelectionInteraction selection,
		IColumns columns,
		IRows rows,
		int rowIndex)
	{
		ElementFactory = elementFactory;
		Columns = columns;
		Rows = rows;
		DataContext = rows?[rowIndex].Model;
		IsSelected = selection?.IsRowSelected(rowIndex) ?? false;
		RowIndex = rowIndex;
		UpdateSelection(selection);
		CellsPresenter?.Realize(rowIndex);
		_treeDataGrid?.RaiseRowPrepared(this, RowIndex);
	}

	public Control TryGetCell(int columnIndex)
	{
		return CellsPresenter?.TryGetElement(columnIndex);
	}

	public void Unrealize()
	{
		_treeDataGrid?.RaiseRowClearing(this, RowIndex);
		RowIndex = -1;
		DataContext = null;
		IsSelected = false;
		CellsPresenter?.Unrealize();
	}

	public void UnrealizeOnItemRemoved()
	{
		_treeDataGrid?.RaiseRowClearing(this, RowIndex);
		RowIndex = -1;
		DataContext = null;
		IsSelected = false;
		CellsPresenter?.UnrealizeOnRowRemoved();
	}

	public void UpdateIndex(int index)
	{
		if (RowIndex == -1)
		{
			throw new InvalidOperationException("Row is not realized.");
		}

		RowIndex = index;
		CellsPresenter?.UpdateRowIndex(index);
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		CellsPresenter = e.NameScope.Find<TreeDataGridCellsPresenter>("PART_CellsPresenter");

		if (RowIndex >= 0)
		{
			CellsPresenter?.Realize(RowIndex);
		}
	}

	protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
	{
		_treeDataGrid = this.FindLogicalAncestorOfType<TreeDataGrid>();
		base.OnAttachedToLogicalTree(e);
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);

		// The row may be realized before being parented. In this case raise the RowPrepared event here.
		if (_treeDataGrid is not null && (RowIndex >= 0))
		{
			_treeDataGrid.RaiseRowPrepared(this, RowIndex);
		}
	}

	protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
	{
		_treeDataGrid = null;
		base.OnDetachedFromLogicalTree(e);
	}

	protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
	{
		base.OnPointerCaptureLost(e);
		_mouseDownPosition = s_InvalidPoint;
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		base.OnPointerMoved(e);

		var currentPoint = e.GetCurrentPoint(this);
		var delta = currentPoint.Position - _mouseDownPosition;

		var pointerSupportsDrag = currentPoint.Pointer.Type switch
		{
			PointerType.Mouse => currentPoint.Properties.IsLeftButtonPressed,
			PointerType.Pen => currentPoint.Properties.IsRightButtonPressed,
			_ => false
		};

		if (!pointerSupportsDrag ||
			e.Handled ||
			((Math.Abs(delta.X) < DragDistance) && (Math.Abs(delta.Y) < DragDistance)) ||
			(_mouseDownPosition == s_InvalidPoint))
		{
			return;
		}

		_mouseDownPosition = s_InvalidPoint;

		var presenter = Parent as TreeDataGridRowsPresenter;
		var owner = presenter?.TemplatedParent as TreeDataGrid;
		owner?.RaiseRowDragStarted(e);
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		base.OnPointerPressed(e);
		_mouseDownPosition = !e.Handled ? e.GetPosition(this) : s_InvalidPoint;
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		base.OnPointerReleased(e);
		_mouseDownPosition = s_InvalidPoint;
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if (change.Property == IsSelectedProperty)
		{
			PseudoClasses.Set(":selected", IsSelected);
		}

		base.OnPropertyChanged(change);
	}

	internal void UpdateSelection(ITreeDataGridSelectionInteraction selection)
	{
		IsSelected = selection?.IsRowSelected(RowIndex) ?? false;
		CellsPresenter?.UpdateSelection(selection);
	}

	#endregion
}