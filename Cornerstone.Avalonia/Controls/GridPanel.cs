#region References

using System;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Reactive;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Avalonia.Controls;

[SourceReflection]
public partial class GridPanel : Panel
{
	#region Constructors

	static GridPanel()
	{
		AffectsMeasure<GridPanel>(ColumnCountProperty, RowCountProperty);
		ColumnCountProperty.Changed.Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(OnCountChanged));
		RowCountProperty.Changed.Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(OnCountChanged));
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets the column count (minimum 1)
	/// </summary>
	[Category("Layout")]
	[Description("Defines a set number of columns")]
	[AttachedProperty]
	public partial int ColumnCount { get; set; }

	/// <summary>
	/// Gets or sets the row count (minimum 1)
	/// </summary>
	[Category("Layout")]
	[Description("Defines a set number of rows")]
	[AttachedProperty]
	public partial int RowCount { get; set; }

	#endregion

	#region Methods

	protected override Size ArrangeOverride(Size finalSize)
	{
		var rows = Math.Max(1, RowCount);
		var cols = Math.Max(1, ColumnCount);
		var cellWidth = finalSize.Width / cols;
		var cellHeight = finalSize.Height / rows;

		foreach (var child in Children.OfType<Control>())
		{
			if (!child.IsVisible)
			{
				continue;
			}

			// Respect explicit Grid.Row / Grid.Column if set and valid
			var row = Grid.GetRow(child);
			var col = Grid.GetColumn(child);

			// Safe clamp without modifying the property
			row = Math.Clamp(row, 0, rows - 1);
			col = Math.Clamp(col, 0, cols - 1);

			var rect = new Rect(
				col * cellWidth,
				row * cellHeight,
				cellWidth * Grid.GetColumnSpan(child),
				cellHeight * Grid.GetRowSpan(child)
			);

			child.Arrange(rect);
		}

		return finalSize;
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		var rows = Math.Max(1, RowCount);
		var cols = Math.Max(1, ColumnCount);

		// Measure all children with infinite size first (or constraint if you want tight measurement)
		var childConstraint = new Size(availableSize.Width / cols, availableSize.Height / rows);
		foreach (var child in Children.OfType<Control>())
		{
			child.Measure(childConstraint); // or Size.Infinity if you prefer
		}

		// For Star-like behavior: return the size needed for the grid
		var cellWidth = availableSize.Width / cols;
		var cellHeight = availableSize.Height / rows;

		return new Size(cellWidth * cols, cellHeight * rows);
	}

	private static void OnCountChanged(AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Sender is GridPanel grid)
		{
			grid.InvalidateMeasure();
		}
	}

	#endregion
}