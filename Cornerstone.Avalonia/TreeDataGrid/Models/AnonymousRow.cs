#region References

using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid.Models;

/// <summary>
/// A row that can be reused for situations where creating a separate object for each row is
/// not necessary.
/// </summary>
/// <typeparam name="TModel"> The model type. </typeparam>
/// <remarks>
/// In a flat grid where rows cannot be resized, it is not necessary to persist any information
/// about rows; the same row object can be updated and reused when a new row is requested.
/// </remarks>
internal class AnonymousRow<TModel> : IRow<TModel>, IModelIndexableRow
{
	#region Properties

	public object Header => ModelIndex;

	public GridLength Height
	{
		get => GridLength.Auto;
		set { }
	}

	[field: AllowNull]
	public TModel Model { get; private set; }

	public int ModelIndex { get; private set; }

	public IndexPath ModelIndexPath => ModelIndex;

	#endregion

	#region Methods

	public AnonymousRow<TModel> Update(int modelIndex, TModel model)
	{
		ModelIndex = modelIndex;
		Model = model;
		return this;
	}

	public void UpdateModelIndex(int delta)
	{
		throw new NotSupportedException();
	}

	#endregion
}