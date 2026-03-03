namespace Cornerstone.Avalonia.TreeDataGrid.Models;

public interface ICellOptions
{
	#region Properties

	/// <summary>
	/// Gets the gesture(s) that will cause the cell to enter edit mode.
	/// </summary>
	BeginEditGestures BeginEditGestures { get; }

	#endregion
}