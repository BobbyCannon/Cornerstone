namespace Cornerstone.Avalonia.TreeDataGrid.Models;

/// <summary>
/// Represents a cell in an <see cref="ITreeDataGridSource" />.
/// </summary>
public interface ICell
{
	#region Properties

	/// <summary>
	/// Gets a value indicating whether the cell can enter edit mode.
	/// </summary>
	bool CanEdit { get; }

	/// <summary>
	/// Gets the gesture(s) that will cause the cell to enter edit mode.
	/// </summary>
	BeginEditGestures EditGestures { get; }

	/// <summary>
	/// Gets the value of the cell.
	/// </summary>
	object Value { get; }

	#endregion
}