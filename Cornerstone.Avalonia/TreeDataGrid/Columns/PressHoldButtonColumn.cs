#region References

using System;
using System.ComponentModel;
using System.Linq.Expressions;
using Avalonia.Controls;
using Cornerstone.Avalonia.TreeDataGrid.Models;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Columns;

/// <summary>
/// A column in an <see cref="ITreeDataGridSource" /> which displays a checkbox.
/// </summary>
/// <typeparam name="TModel"> The model type. </typeparam>
/// <typeparam name="TValue"> The value type </typeparam>
public class PressHoldButtonColumn<TModel, TValue> : ColumnBase<TModel>
	where TModel : class
{
	#region Fields

	private readonly Action<TModel> _commandAction;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="PressHoldButtonColumn{TModel,TValue}" /> class.
	/// </summary>
	/// <param name="header"> The column header. </param>
	/// <param name="getter">
	/// An expression which given a row model, returns a cell value for the column.
	/// </param>
	/// <param name="commandAction">
	/// A method which given a row model and a cell value, writes the cell value to the
	/// row model. If not supplied then the column will be read-only.
	/// </param>
	/// <param name="width">
	/// The column width. If null defaults to <see cref="GridLength.Auto" />.
	/// </param>
	/// <param name="options"> Additional column options. </param>
	public PressHoldButtonColumn(
		object header,
		Expression<Func<TModel, TValue>> getter,
		Action<TModel> commandAction = null,
		GridLength? width = null,
		PressHoldButtonColumnOptions<TModel> options = null
	) : base(header, width, options ?? new PressHoldButtonColumnOptions<TModel>())
	{
		_commandAction = commandAction;
		ValueSelector = getter.Compile();
	}

	#endregion

	#region Properties

	public new PressHoldButtonColumnOptions<TModel> Options => (PressHoldButtonColumnOptions<TModel>) base.Options;

	public Func<TModel, TValue> ValueSelector { get; }

	#endregion

	#region Methods

	public override ICell CreateCell(IRow<TModel> row)
	{
		return new PressHoldButtonCell(
			new RelayCommand(x => _commandAction?.Invoke((TModel) x)),
			row.Model,
			ValueSelector.Invoke(row.Model),
			Options.HoldDuration,
			Options.Height
		);
	}

	public override Comparison<TModel> GetComparison(ListSortDirection direction)
	{
		return (x, y) => 0;
	}

	#endregion
}