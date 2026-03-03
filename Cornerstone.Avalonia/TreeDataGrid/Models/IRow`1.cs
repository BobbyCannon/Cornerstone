namespace Cornerstone.Avalonia.TreeDataGrid.Models;

/// <summary>
/// Represents a row in an <see cref="ITreeDataGridSource" />.
/// </summary>
/// <typeparam name="TModel"> The model type. </typeparam>
public interface IRow<TModel> : IRow
{
	#region Properties

	/// <summary>
	/// Gets the row model.
	/// </summary>
	new TModel Model { get; }

	/// <summary>
	/// Gets the untyped row model.
	/// </summary>
	object IRow.Model => Model;

	#endregion

	#region Methods

	/// <summary>
	/// Updates the model index due to a change in the data source.
	/// </summary>
	/// <param name="delta"> The index delta. </param>
	void UpdateModelIndex(int delta);

	#endregion
}