#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using Avalonia.Controls;
using Avalonia.Layout;
using Cornerstone.Avalonia.TreeDataGrid.Experimental.Data;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Models;

/// <summary>
/// A column in an <see cref="HierarchicalTreeDataGridSource{TModel}" /> whose cells show an
/// expander to reveal nested data.
/// </summary>
/// <typeparam name="TModel"> The model type. </typeparam>
public class HierarchicalExpanderColumn<TModel> : Notifiable,
	IColumn<TModel>,
	IExpanderColumn<TModel>,
	IUpdateColumnLayout
	where TModel : class
{
	#region Fields

	private double _actualWidth;
	private readonly Func<TModel, IEnumerable<TModel>> _childSelector;
	private readonly TypedBinding<TModel, bool> _hasChildrenSelector;
	private readonly TypedBinding<TModel, bool> _isExpandedBinding;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="HierarchicalExpanderColumn{TModel}" /> class.
	/// </summary>
	/// <param name="inner"> The inner column which defines how the column will be displayed. </param>
	/// <param name="childSelector"> The model children selector. </param>
	/// <param name="hasChildrenSelector">
	/// A selector which is used to determine whether the model has children without invoking
	/// <paramref name="childSelector" />. This is only needed if the initialization of a node's
	/// children is an expensive operation; where that is not true, pass null.
	/// </param>
	/// <param name="isExpandedSelector">
	/// Selects a read/write boolean property which stores the expanded state for the row.
	/// </param>
	public HierarchicalExpanderColumn(
		IColumn<TModel> inner,
		Func<TModel, IEnumerable<TModel>> childSelector,
		Expression<Func<TModel, bool>> hasChildrenSelector = null,
		Expression<Func<TModel, bool>> isExpandedSelector = null)
	{
		_actualWidth = double.NaN;
		Inner = inner;
		Inner.PropertyChanged += OnInnerPropertyChanged;
		_childSelector = childSelector;
		_hasChildrenSelector = hasChildrenSelector is not null ? TypedBinding<TModel>.OneWay(hasChildrenSelector) : null;
		_isExpandedBinding = isExpandedSelector is not null ? TypedBinding<TModel>.TwoWay(isExpandedSelector) : null;
		_actualWidth = inner.ActualWidth;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets the actual width of the column after measurement.
	/// </summary>
	public double ActualWidth
	{
		get => _actualWidth;
		private set => SetProperty(ref _actualWidth, value);
	}

	public bool? CanUserResize => Inner.CanUserResize;

	public bool? CanUserSort => Inner.CanUserSort;

	public object Header => Inner.Header;

	public HorizontalAlignment HorizontalAlignment
	{
		get => Inner.HorizontalAlignment;
		set => Inner.HorizontalAlignment = value;
	}

	public IColumn<TModel> Inner { get; }

	public ListSortDirection? SortDirection
	{
		get => Inner.SortDirection;
		set => Inner.SortDirection = value;
	}

	/// <summary>
	/// Gets or sets a user-defined object attached to the column.
	/// </summary>
	public object Tag
	{
		get => Inner.Tag;
		set => Inner.Tag = value;
	}

	public GridLength Width => Inner.Width;
	double IUpdateColumnLayout.MaxActualWidth => ((IUpdateColumnLayout) Inner).MaxActualWidth;
	double IUpdateColumnLayout.MinActualWidth => ((IUpdateColumnLayout) Inner).MinActualWidth;
	bool IUpdateColumnLayout.StarWidthWasConstrained => ((IUpdateColumnLayout) Inner).StarWidthWasConstrained;

	#endregion

	#region Methods

	public ICell CreateCell(IRow<TModel> row)
	{
		if (row is HierarchicalRow<TModel> r)
		{
			var showExpander = new ShowExpanderObservable<TModel>(_childSelector, _hasChildrenSelector, r.Model);
			var isExpanded = _isExpandedBinding?.Instance(r.Model);
			return new ExpanderCell<TModel>(Inner.CreateCell(r), r, showExpander, isExpanded);
		}

		throw new NotSupportedException();
	}

	public IEnumerable<TModel> GetChildModels(TModel model)
	{
		return _childSelector(model);
	}

	public Comparison<TModel> GetComparison(ListSortDirection direction)
	{
		return Inner.GetComparison(direction);
	}

	public bool HasChildren(TModel model)
	{
		return _hasChildrenSelector?.Read!(model) ?? _childSelector(model)?.Any() ?? false;
	}

	void IUpdateColumnLayout.CalculateStarWidth(double availableWidth, double totalStars)
	{
		((IUpdateColumnLayout) Inner).CalculateStarWidth(availableWidth, totalStars);
		ActualWidth = Inner.ActualWidth;
	}

	double IUpdateColumnLayout.CellMeasured(double width, int rowIndex)
	{
		return ((IUpdateColumnLayout) Inner).CellMeasured(width, rowIndex);
	}

	bool IUpdateColumnLayout.CommitActualWidth()
	{
		var result = ((IUpdateColumnLayout) Inner).CommitActualWidth();
		ActualWidth = Inner.ActualWidth;
		return result;
	}

	private void OnInnerPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName
			is nameof(CanUserResize)
			or nameof(CanUserSort)
			or nameof(Header)
			or nameof(SortDirection)
			or nameof(Width))
		{
			OnPropertyChanged(e.PropertyName);
		}
	}

	void IExpanderColumn<TModel>.SetModelIsExpanded(IExpanderRow<TModel> row)
	{
		_isExpandedBinding?.Write!.Invoke(row.Model, row.IsExpanded);
	}

	void IUpdateColumnLayout.SetWidth(GridLength width)
	{
		SetWidth(width);
	}

	private void SetWidth(GridLength width)
	{
		((IUpdateColumnLayout) Inner).SetWidth(width);

		if (width.IsAbsolute)
		{
			ActualWidth = width.Value;
		}
	}

	#endregion
}