#region References

using Avalonia.Controls;
using Avalonia.Layout;
using Cornerstone.Avalonia.TreeDataGrid.Models;
using System;
using System.Linq.Expressions;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Columns;

/// <summary>
/// A column in an <see cref="ITreeDataGridSource" /> which displays its values as text.
/// </summary>
/// <typeparam name="TModel"> The model type. </typeparam>
/// <typeparam name="TValue"> The column data type. </typeparam>
public class TextColumn<TModel, TValue> : ColumnBase<TModel, TValue>, ITextSearchableColumn<TModel>
	where TModel : class
{
	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="TextColumn{TModel, TValue}" /> class.
	/// </summary>
	/// <param name="header"> The column header. </param>
	/// <param name="getter">
	/// An expression which given a row model, returns a cell value for the column.
	/// </param>
	/// <param name="width">
	/// The column width. If null defaults to <see cref="GridLength.Auto" />.
	/// </param>
	/// <param name="horizontalAlignment"> An optional horizontal alignment </param>
	/// <param name="options"> Additional column options. </param>
	public TextColumn(
		object header,
		Expression<Func<TModel, TValue>> getter,
		GridLength? width = null,
		HorizontalAlignment? horizontalAlignment = null,
		TextColumnOptions<TModel> options = null)
		: this(header, getter, null, width, horizontalAlignment, options ?? new())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TextColumn{TModel, TValue}" /> class.
	/// </summary>
	/// <param name="header"> The column header. </param>
	/// <param name="getter">
	/// An expression which given a row model, returns a cell value for the column.
	/// </param>
	/// <param name="setter">
	/// A method which given a row model and a cell value, writes the cell value to the
	/// row model.
	/// </param>
	/// <param name="width">
	/// The column width. If null defaults to <see cref="GridLength.Auto" />.
	/// </param>
	/// <param name="horizontalAlignment"> An optional horizontal alignment </param>
	/// <param name="options"> Additional column options. </param>
	public TextColumn(
		object header,
		Expression<Func<TModel, TValue>> getter,
		Action<TModel, TValue> setter,
		GridLength? width = null,
		HorizontalAlignment? horizontalAlignment = null,
		TextColumnOptions<TModel> options = null)
		: base(header, getter, setter, width, horizontalAlignment, options ?? new())
	{
	}

	#endregion

	#region Properties

	public new TextColumnOptions<TModel> Options => (TextColumnOptions<TModel>) base.Options;

	bool ITextSearchableColumn<TModel>.IsTextSearchEnabled => Options?.IsTextSearchEnabled ?? false;

	#endregion

	#region Methods

	public override ICell CreateCell(IRow<TModel> row)
	{
		var binding = CreateBindingExpression(row.Model);
		return new TextCell<TValue>(binding, binding, Binding.Write is null, Options);
	}

	string ITextSearchableColumn<TModel>.SelectValue(TModel model)
	{
		return ValueSelector(model)?.ToString();
	}

	#endregion
}