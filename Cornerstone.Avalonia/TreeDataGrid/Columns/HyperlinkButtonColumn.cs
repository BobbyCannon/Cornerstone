#region References

using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Windows.Input;
using Avalonia.Controls;
using Cornerstone.Avalonia.TreeDataGrid.Models;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Columns;

/// <summary>
/// A column in an <see cref="ITreeDataGridSource" /> which displays a hyperlink.
/// </summary>
/// <typeparam name="TModel"> The model type. </typeparam>
/// <typeparam name="TValue"> The value type </typeparam>
public class HyperlinkButtonColumn<TModel, TValue> : ColumnBase<TModel>
	where TModel : class
{
	#region Fields

	private readonly ICommand _commandAction;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="HyperlinkButtonColumn{TModel,TValue}" /> class.
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
	public HyperlinkButtonColumn(
		object header,
		Expression<Func<TModel, TValue>> getter,
		ICommand commandAction = null,
		GridLength? width = null,
		HyperlinkButtonColumnOptions<TModel> options = null
	) : base(header, width, options ?? new HyperlinkButtonColumnOptions<TModel>())
	{
		_commandAction = commandAction;
		ValueSelector = getter.Compile();
	}

	#endregion

	#region Properties

	public new HyperlinkButtonColumnOptions<TModel> Options => (HyperlinkButtonColumnOptions<TModel>) base.Options;

	public Func<TModel, TValue> ValueSelector { get; }

	#endregion

	#region Methods

	public override ICell CreateCell(IRow<TModel> row)
	{
		return new HyperlinkButtonCell(_commandAction, row.Model, ValueSelector.Invoke(row.Model));
	}

	public override Comparison<TModel> GetComparison(ListSortDirection direction)
	{
		return (x, y) => 0;
	}

	#endregion
}