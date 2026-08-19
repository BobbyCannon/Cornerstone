#region References

using System.Collections.Generic;
using Avalonia.Controls;
using Cornerstone.Avalonia.TreeDataGrid.Columns;
using Cornerstone.Avalonia.TreeDataGrid.Models;

#endregion

namespace Cornerstone.Avalonia.TreeDataGrid;

/// <summary>
/// Sample rows and sources for TreeDataGrid.axaml Design.PreviewWith only.
/// </summary>
public static class TreeDataGridDesignData
{
	#region Constructors

	static TreeDataGridDesignData()
	{
		var parent = new PreviewRow
		{
			Name = "Parent",
			IsActive = true,
			IsExpanded = true,
			Children =
			{
				new PreviewRow { Name = "Child A", IsActive = true },
				new PreviewRow { Name = "Child B", IsActive = false }
			}
		};

		var hierarchical = new HierarchicalTreeDataGridSource<PreviewRow>([parent])
		{
			Columns =
			{
				new HierarchicalExpanderColumn<PreviewRow>(
					new TextColumn<PreviewRow, string>("Name", x => x.Name, new GridLength(1, GridUnitType.Star)),
					x => x.Children,
					x => x.Children.Count > 0,
					x => x.IsExpanded),
				new CheckBoxColumn<PreviewRow>("Active", x => x.IsActive, (row, value) => row.IsActive = value)
			}
		};
		hierarchical.RowSelection.SelectedIndex = new IndexPath(0);
		HierarchicalSource = hierarchical;

		var flat = new FlatTreeDataGridSource<PreviewRow>([parent, .. parent.Children])
		{
			Columns =
			{
				new TextColumn<PreviewRow, string>("Name", x => x.Name, new GridLength(1, GridUnitType.Star)),
				new CheckBoxColumn<PreviewRow>("Active", x => x.IsActive, (row, value) => row.IsActive = value)
			}
		};
		flat.RowSelection.SelectedIndex = new IndexPath(1);
		FlatSource = flat;
	}

	#endregion

	#region Properties

	public static ITreeDataGridSource FlatSource { get; }

	public static ITreeDataGridSource HierarchicalSource { get; }

	#endregion

	#region Classes

	public class PreviewRow
	{
		#region Properties

		public IList<PreviewRow> Children { get; } = [];

		public bool IsActive { get; set; }

		public bool IsExpanded { get; set; }

		public string Name { get; set; } = string.Empty;

		#endregion
	}

	#endregion
}
