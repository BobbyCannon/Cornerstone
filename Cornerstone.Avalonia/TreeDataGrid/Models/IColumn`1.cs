#region References

using System;
using System.ComponentModel;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Models;

/// <summary>
/// Represents a column in an <see cref="ITreeDataGridSource" /> which selects cell values from
/// a model.
/// </summary>
/// <typeparam name="TModel"> The model type. </typeparam>
public interface IColumn<TModel> : IColumn
{
	#region Methods

	/// <summary>
	/// Creates a cell for this column on the specified row.
	/// </summary>
	/// <param name="row"> The row. </param>
	/// <returns> The cell. </returns>
	ICell CreateCell(IRow<TModel> row);

	/// <summary>
	/// Gets a comparison function for the column.
	/// </summary>
	/// <param name="direction"> The sort direction. </param>
	/// <returns>
	/// The comparison function or null if sorting cannot be performed on the column.
	/// </returns>
	Comparison<TModel> GetComparison(ListSortDirection direction);

	#endregion
}