#region References

using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using Avalonia.Input;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Models;

/// <summary>
/// Holds information about an automatic row drag/drop operation carried out
/// by TreeDataGrid.AutoDragDropRows.
/// </summary>
public class DragInfo
{
	#region Constants

	/// <summary>
	/// Defines the data format in an IDataObject.
	/// </summary>
	public const string DataFormat = "TreeDataGridDragInfo";

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="DragInfo" /> class.
	/// </summary>
	/// <param name="source"> The source of the drag operation/ </param>
	/// <param name="indexes"> The indexes being dragged. </param>
	public DragInfo(ITreeDataGridSource source, IEnumerable<IndexPath> indexes)
	{
		Source = source;
		Indexes = indexes;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets the model indexes of the rows being dragged.
	/// </summary>
	public IEnumerable<IndexPath> Indexes { get; }

	/// <summary>
	/// Gets the <see cref="ITreeDataGridSource" /> that rows are being dragged from.
	/// </summary>
	public ITreeDataGridSource Source { get; }

	#endregion
}