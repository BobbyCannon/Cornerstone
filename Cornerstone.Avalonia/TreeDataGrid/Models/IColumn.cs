#region References

using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Layout;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Models;

/// <summary>
/// Represents a column in an <see cref="ITreeDataGridSource" />.
/// </summary>
public interface IColumn : INotifyPropertyChanged
{
	#region Properties

	/// <summary>
	/// Gets the actual width of the column after measurement.
	/// </summary>
	/// <returns>
	/// The width of the column in pixels, or NaN if the column has not yet been laid out.
	/// </returns>
	double ActualWidth { get; }

	/// <summary>
	/// Gets a value indicating whether the user can resize the column.
	/// </summary>
	bool? CanUserResize { get; }

	/// <summary>
	/// Gets a value indicating whether the user can sort the column.
	/// </summary>
	bool? CanUserSort { get; }

	/// <summary>
	/// Gets the column header.
	/// </summary>
	object Header { get; }

	/// <summary>
	/// Gets the horizontal alignment for the column.
	/// </summary>
	HorizontalAlignment HorizontalAlignment { get; set; }

	/// <summary>
	/// Gets or sets the sort direction indicator that will be displayed on the column.
	/// </summary>
	/// <remarks>
	/// Note that changing this property does not change the sorting of the data, it is only
	/// used to display a sort direction indicator. To sort data according to a column use
	/// <see cref="ITreeDataGridSource.SortBy(IColumn, ListSortDirection)" />.
	/// </remarks>
	ListSortDirection? SortDirection { get; set; }

	/// <summary>
	/// Gets or sets a user-defined object attached to the column.
	/// </summary>
	object Tag { get; set; }

	/// <summary>
	/// Gets the width of the column.
	/// </summary>
	/// <remarks>
	/// To set the column width use <see cref="IColumns.SetColumnWidth(int, GridLength)" />.
	/// </remarks>
	GridLength Width { get; }

	#endregion
}