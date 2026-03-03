#region References

using System;
using Avalonia.Controls;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Models;

[Flags]
public enum BeginEditGestures
{
	/// <summary>
	/// A cell will only enter edit mode programmatically.
	/// </summary>
	None = 0x00,

	/// <summary>
	/// A cell will enter edit mode when the user presses F2.
	/// </summary>
	F2 = 0x01,

	/// <summary>
	/// A cell will enter edit mode when the user taps it.
	/// </summary>
	Tap = 0x02,

	/// <summary>
	/// A cell will enter edit mode when the user double-taps it.
	/// </summary>
	DoubleTap = 0x4,

	/// <summary>
	/// A cell will enter edit mode in conjunction with a gesture only when the cell or row
	/// is currently selected.
	/// </summary>
	WhenSelected = 0x1000,

	/// <summary>
	/// A cell will enter edit mode when the user presses F2 or double-taps it.
	/// </summary>
	Default = F2 | DoubleTap
}

/// <summary>
/// Holds less commonly-used options for an <see cref="IColumn{TModel}" />.
/// </summary>
/// <typeparam name="TModel"> The model type. </typeparam>
public class ColumnOptions<TModel>
{
	#region Constructors

	public ColumnOptions()
	{
		BeginEditGestures = BeginEditGestures.Default;
		MaxWidth = null;
		MinWidth = new(30, GridUnitType.Pixel);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets the gesture(s) that will cause a cell to enter edit mode.
	/// </summary>
	public BeginEditGestures BeginEditGestures { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the user can resize a column by dragging.
	/// </summary>
	/// <remarks>
	/// If null, the owner TreeDataGrid.CanUserResizeColumns property value will apply.
	/// </remarks>
	public bool? CanUserResizeColumn { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the user can sort a column by clicking.
	/// </summary>
	/// <remarks>
	/// If null, the owner TreeDataGrid.CanUserSortColumns property value will apply.
	/// </remarks>
	public bool? CanUserSortColumn { get; set; }

	/// <summary>
	/// Gets or sets a custom comparison for ascending ordered columns.
	/// </summary>
	public Comparison<TModel> CompareAscending { get; set; }

	/// <summary>
	/// Gets or sets a custom comparison for descending ordered columns.
	/// </summary>
	public Comparison<TModel> CompareDescending { get; set; }

	/// <summary>
	/// Gets or sets the maximum width for a column.
	/// </summary>
	public GridLength? MaxWidth { get; set; }

	/// <summary>
	/// Gets or sets the minimum width for a column.
	/// </summary>
	public GridLength MinWidth { get; set; }

	#endregion
}