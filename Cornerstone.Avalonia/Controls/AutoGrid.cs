#region References

using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Reactive;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Avalonia.Controls;

/// <summary>
/// Defines a flexible grid area that consists of columns and rows.
/// Depending on the orientation, either the rows or the columns are auto-generated,
/// and the children's position is set according to their index.
/// </summary>
/// <remarks>
/// Forked from: https://github.com/AvaloniaUI/AvaloniaAutoGrid
/// </remarks>
public class AutoGrid : Grid
{
	#region Fields

	public static readonly AvaloniaProperty ChildHorizontalAlignmentProperty;
	public static readonly AvaloniaProperty ChildMarginProperty;
	public static readonly AvaloniaProperty ChildVerticalAlignmentProperty;
	public static readonly AvaloniaProperty ColumnCountProperty;
	public static readonly AvaloniaProperty ColumnWidthProperty;
	public static readonly AvaloniaProperty IsAutoIndexingProperty;
	public static readonly AvaloniaProperty OrientationProperty;
	public static readonly AvaloniaProperty RowCountProperty;
	public static readonly AvaloniaProperty RowHeightProperty;

	#endregion

	#region Constructors

	static AutoGrid()
	{
		ChildHorizontalAlignmentProperty = AvaloniaProperty.Register<AutoGrid, HorizontalAlignment?>(nameof(ChildHorizontalAlignment));
		ChildMarginProperty = AvaloniaProperty.Register<AutoGrid, Thickness?>(nameof(ChildMargin));
		ChildVerticalAlignmentProperty = AvaloniaProperty.Register<AutoGrid, VerticalAlignment?>(nameof(ChildVerticalAlignment));
		ColumnCountProperty = AvaloniaProperty.RegisterAttached<Control, int>(nameof(ColumnCount), typeof(AutoGrid), 1);
		ColumnWidthProperty = AvaloniaProperty.RegisterAttached<Control, GridLength>(nameof(ColumnWidth), typeof(AutoGrid), GridLength.Auto);
		IsAutoIndexingProperty = AvaloniaProperty.Register<AutoGrid, bool>(nameof(IsAutoIndexing), true);
		OrientationProperty = AvaloniaProperty.Register<AutoGrid, Orientation>(nameof(Orientation));
		RowCountProperty = AvaloniaProperty.RegisterAttached<Control, int>(nameof(RowCount), typeof(AutoGrid), 1);
		RowHeightProperty = AvaloniaProperty.RegisterAttached<Control, GridLength>(nameof(RowHeight), typeof(AutoGrid), GridLength.Auto);

		AffectsMeasure<AutoGrid>(ChildHorizontalAlignmentProperty, ChildMarginProperty,
			ChildVerticalAlignmentProperty, ColumnCountProperty, ColumnWidthProperty, IsAutoIndexingProperty, OrientationProperty,
			RowHeightProperty);

		ChildHorizontalAlignmentProperty.Changed.Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(OnChildHorizontalAlignmentChanged));
		ChildMarginProperty.Changed.Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(OnChildMarginChanged));
		ChildVerticalAlignmentProperty.Changed.Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(OnChildVerticalAlignmentChanged));
		ColumnCountProperty.Changed.Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(ColumnCountChanged));
		RowCountProperty.Changed.Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(RowCountChanged));
		ColumnWidthProperty.Changed.Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(FixedColumnWidthChanged));
		RowHeightProperty.Changed.Subscribe(new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(FixedRowHeightChanged));
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets the child horizontal alignment.
	/// </summary>
	/// <value> The child horizontal alignment. </value>
	[Category("Layout")]
	[Description("Presets the horizontal alignment of all child controls")]
	public HorizontalAlignment? ChildHorizontalAlignment
	{
		get => (HorizontalAlignment?) GetValue(ChildHorizontalAlignmentProperty);
		set => SetValue(ChildHorizontalAlignmentProperty, value);
	}

	/// <summary>
	/// Gets or sets the child margin.
	/// </summary>
	/// <value> The child margin. </value>
	[Category("Layout")]
	[Description("Presets the margin of all child controls")]
	public Thickness? ChildMargin
	{
		get => (Thickness?) GetValue(ChildMarginProperty);
		set => SetValue(ChildMarginProperty, value);
	}

	/// <summary>
	/// Gets or sets the child vertical alignment.
	/// </summary>
	/// <value> The child vertical alignment. </value>
	[Category("Layout")]
	[Description("Presets the vertical alignment of all child controls")]
	public VerticalAlignment? ChildVerticalAlignment
	{
		get => (VerticalAlignment?) GetValue(ChildVerticalAlignmentProperty);
		set => SetValue(ChildVerticalAlignmentProperty, value);
	}

	/// <summary>
	/// Gets or sets the column count
	/// </summary>
	[Category("Layout")]
	[Description("Defines a set number of columns")]
	public int ColumnCount
	{
		get => (int) GetValue(ColumnCountProperty)!;
		set => SetValue(ColumnCountProperty, value);
	}

	/// <summary>
	/// Gets or sets the fixed column width
	/// </summary>
	[Category("Layout")]
	[Description("Presets the width of all columns set using the ColumnCount property")]

	public GridLength ColumnWidth
	{
		get => (GridLength) GetValue(ColumnWidthProperty)!;
		set => SetValue(ColumnWidthProperty, value);
	}

	/// <summary>
	/// Gets or sets a value indicating whether the children are automatically indexed.
	/// <remarks>
	/// The default is True.
	/// Note that if children are already indexed, setting this property to false will not remove their indices.
	/// </remarks>
	/// </summary>
	[Category("Layout")]
	[Description("Set to false to disable the auto layout functionality")]
	public bool IsAutoIndexing
	{
		get => (bool) GetValue(IsAutoIndexingProperty)!;
		set => SetValue(IsAutoIndexingProperty, value);
	}

	/// <summary>
	/// Gets or sets the orientation.
	/// <remarks> The default is Vertical. </remarks>
	/// </summary>
	/// <value> The orientation. </value>
	[Category("Layout")]
	[Description("Defines the directionality of the auto layout. Use vertical for a column first layout, horizontal for a row first layout.")]
	public Orientation Orientation
	{
		get => (Orientation) GetValue(OrientationProperty)!;
		set => SetValue(OrientationProperty, value);
	}

	/// <summary>
	/// Gets or sets the number of rows
	/// </summary>
	[Category("Layout")]
	[Description("Defines a set number of rows")]
	public int RowCount
	{
		get => (int) GetValue(RowCountProperty)!;
		set => SetValue(RowCountProperty, value);
	}

	/// <summary>
	/// Gets or sets the fixed row height
	/// </summary>
	[Category("Layout")]
	[Description("Presets the height of all rows set using the RowCount property")]
	public GridLength RowHeight
	{
		get => (GridLength) GetValue(RowHeightProperty)!;
		set => SetValue(RowHeightProperty, value);
	}

	#endregion

	#region Methods

	/// <summary>
	/// Handles the column count changed event
	/// </summary>
	public static void ColumnCountChanged(AvaloniaPropertyChangedEventArgs e)
	{
		if ((e.NewValue == null) || ((int) e.NewValue < 0))
		{
			return;
		}

		var grid = (AutoGrid) e.Sender;

		// look for an existing column definition for the height
		var width = grid.ColumnWidth;
		if (!grid.IsSet(ColumnWidthProperty) && (grid.ColumnDefinitions.Count > 0))
		{
			width = grid.ColumnDefinitions[0].Width;
		}

		// clear and rebuild
		grid.ColumnDefinitions.Clear();
		for (var i = 0; i < (int) e.NewValue; i++)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = width });
		}
	}

	/// <summary>
	/// Handle the fixed column width changed event
	/// </summary>
	public static void FixedColumnWidthChanged(AvaloniaPropertyChangedEventArgs e)
	{
		var grid = (AutoGrid) e.Sender;

		// add a default column if missing
		if (grid.ColumnDefinitions.Count == 0)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition());
		}

		if (e.NewValue == null)
		{
			return;
		}

		// set all existing columns to this width
		foreach (var t in grid.ColumnDefinitions)
		{
			t.Width = (GridLength) e.NewValue;
		}
	}

	/// <summary>
	/// Handle the fixed row height changed event
	/// </summary>
	public static void FixedRowHeightChanged(AvaloniaPropertyChangedEventArgs e)
	{
		var grid = (AutoGrid) e.Sender;

		// add a default row if missing
		if (grid.RowDefinitions.Count == 0)
		{
			grid.RowDefinitions.Add(new RowDefinition());
		}

		if (e.NewValue == null)
		{
			return;
		}

		// set all existing rows to this height
		foreach (var t in grid.RowDefinitions)
		{
			t.Height = (GridLength) e.NewValue;
		}
	}

	/// <summary>
	/// Handles the row count changed event
	/// </summary>
	public static void RowCountChanged(AvaloniaPropertyChangedEventArgs e)
	{
		if ((e.NewValue == null) || ((int) e.NewValue < 0))
		{
			return;
		}

		var grid = (AutoGrid) e.Sender;

		// look for an existing row to get the height
		var height = grid.RowHeight;
		if (!grid.IsSet(RowHeightProperty) && (grid.RowDefinitions.Count > 0))
		{
			height = grid.RowDefinitions[0].Height;
		}

		// clear and rebuild
		grid.RowDefinitions.Clear();
		for (var i = 0; i < (int) e.NewValue; i++)
		{
			grid.RowDefinitions.Add(new RowDefinition { Height = height });
		}
	}

	/// <summary>
	/// Measures the children of a <see cref="T:Grid" /> in anticipation of arranging them during the ArrangeOverride pass.
	/// </summary>
	/// <param name="constraint"> Indicates an upper limit size that should not be exceeded. </param>
	/// <returns>
	/// <see cref="Size" /> that represents the required size to arrange child content.
	/// </returns>
	protected override Size MeasureOverride(Size constraint)
	{
		PerformLayout();
		return base.MeasureOverride(constraint);
	}

	/// <summary>
	/// Apply child margins and layout effects such as alignment
	/// </summary>
	private void ApplyChildLayout(Control child)
	{
		if (ChildMargin != null)
		{
			child.SetDefaultIfNotSet(MarginProperty, ChildMargin.Value);
		}
		if (ChildHorizontalAlignment != null)
		{
			child.SetDefaultIfNotSet(HorizontalAlignmentProperty, ChildHorizontalAlignment.Value);
		}
		if (ChildVerticalAlignment != null)
		{
			child.SetDefaultIfNotSet(VerticalAlignmentProperty, ChildVerticalAlignment.Value);
		}
	}

	/// <summary>
	/// Called when [child horizontal alignment changed].
	/// </summary>
	private static void OnChildHorizontalAlignmentChanged(AvaloniaPropertyChangedEventArgs e)
	{
		var grid = (AutoGrid) e.Sender;
		foreach (var child in grid.Children)
		{
			child.SetValue(HorizontalAlignmentProperty, grid.ChildHorizontalAlignment ?? AvaloniaProperty.UnsetValue);
		}
	}

	/// <summary>
	/// Called when [child layout changed].
	/// </summary>
	private static void OnChildMarginChanged(AvaloniaPropertyChangedEventArgs e)
	{
		var grid = (AutoGrid) e.Sender;
		foreach (var child in grid.Children)
		{
			child.SetValue(MarginProperty, grid.ChildMargin ?? AvaloniaProperty.UnsetValue);
		}
	}

	/// <summary>
	/// Called when [child vertical alignment changed].
	/// </summary>
	private static void OnChildVerticalAlignmentChanged(AvaloniaPropertyChangedEventArgs e)
	{
		var grid = (AutoGrid) e.Sender;
		foreach (var child in grid.Children)
		{
			child.SetValue(VerticalAlignmentProperty, grid.ChildVerticalAlignment ?? AvaloniaProperty.UnsetValue);
		}
	}

	/// <summary>
	/// Perform the grid layout of row and column indexes
	/// </summary>
	private void PerformLayout()
	{
		var fillRowFirst = Orientation == Orientation.Horizontal;
		var rowCount = RowDefinitions.Count;
		var colCount = ColumnDefinitions.Count;

		if (rowCount == 0)
		{
			rowCount = RowCount;
		}
		if (colCount == 0)
		{
			colCount = ColumnCount;
		}

		var position = 0;
		var skip = new bool[rowCount, colCount];
		foreach (var child in Children.OfType<Control>())
		{
			var childIsCollapsed = !child.IsVisible;
			if (IsAutoIndexing && !childIsCollapsed)
			{
				if (fillRowFirst)
				{
					var row = (position / colCount).Clamp(rowCount - 1);
					var col = (position % colCount).Clamp(colCount - 1);
					if (skip[row, col])
					{
						position++;
						row = position / colCount;
						col = position % colCount;
					}

					SetRow(child, row);
					SetColumn(child, col);
					position += GetColumnSpan(child);

					var offset = GetRowSpan(child) - 1;
					while (offset > 0)
					{
						skip[row + offset--, col] = true;
					}
				}
				else
				{
					var row = (position % rowCount).Clamp(rowCount - 1);
					var col = (position / rowCount).Clamp(colCount - 1);
					if (skip[row, col])
					{
						position++;
						row = position % rowCount;
						col = position / rowCount;
					}

					SetRow(child, row);
					SetColumn(child, col);
					position += GetRowSpan(child);

					var offset = GetColumnSpan(child) - 1;
					while (offset > 0)
					{
						skip[row, col + offset--] = true;
					}
				}
			}

			ApplyChildLayout(child);
		}
	}

	#endregion
}