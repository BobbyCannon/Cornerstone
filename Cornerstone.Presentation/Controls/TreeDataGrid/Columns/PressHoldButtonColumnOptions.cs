#region References

using System;
using Cornerstone.Presentation.Controls.TreeDataGrid.Models;

#endregion

namespace Cornerstone.Presentation.Controls.TreeDataGrid.Columns;

/// <summary>
/// Holds less commonly-used options for a <see cref="PressHoldButtonColumn{TModel,TValue}" />.
/// </summary>
/// <typeparam name="TModel"> The model type. </typeparam>
public class PressHoldButtonColumnOptions<TModel> : ColumnOptions<TModel>
{
	#region Constructors

	public PressHoldButtonColumnOptions()
	{
		HoldDuration = TimeSpan.FromSeconds(1);
	}

	#endregion

	#region Properties

	public double? Height { get; set; }

	public TimeSpan HoldDuration { get; set; }

	public double? Width { get; set; }

	#endregion
}