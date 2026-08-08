#region References

using System;
using System.Collections.Specialized;
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

			var row = Math.Clamp(Grid.GetRow(child), 0, rows - 1);
			var col = Math.Clamp(Grid.GetColumn(child), 0, cols - 1);

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

	protected override void ChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.OldItems != null)
		{
			foreach (Control item in e.OldItems)
			{
				item.PropertyChanged -= ChildOnPropertyChanged;
			}
		}

		if (e.NewItems != null)
		{
			foreach (Control item in e.NewItems)
			{
				item.PropertyChanged += ChildOnPropertyChanged;
			}
		}

		base.ChildrenChanged(sender, e);
		InvalidateMeasure();
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		var rows = Math.Max(1, RowCount);
		var cols = Math.Max(1, ColumnCount);

		// Measure children with a reasonable constraint per cell
		// Use availableSize if finite, otherwise give them a large but finite size
		var cellConstraint = new Size(
			double.IsFinite(availableSize.Width) ? availableSize.Width / cols : 1000,
			double.IsFinite(availableSize.Height) ? availableSize.Height / rows : 1000
		);

		foreach (var child in Children.OfType<Control>())
		{
			if (child.IsVisible)
			{
				child.Measure(cellConstraint);
			}
		}

		// Return desired size:
		// If parent gave us finite size → respect it (fill the space)
		// If infinite → return size based on largest child (or a default)
		if (double.IsFinite(availableSize.Width) && double.IsFinite(availableSize.Height))
		{
			return availableSize; // Fill the available space
		}

		// Fallback when availableSize is infinite (e.g. inside ScrollViewer or initial measure)
		double maxChildWidth = 0;
		double maxChildHeight = 0;

		foreach (var child in Children.OfType<Control>())
		{
			if (child.IsVisible)
			{
				maxChildWidth = Math.Max(maxChildWidth, child.DesiredSize.Width);
				maxChildHeight = Math.Max(maxChildHeight, child.DesiredSize.Height);
			}
		}

		return new Size(
			double.IsFinite(availableSize.Width) ? availableSize.Width : maxChildWidth * cols,
			double.IsFinite(availableSize.Height) ? availableSize.Height : maxChildHeight * rows
		);
	}

	private void ChildOnPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
	{
		if ((e.Property == Grid.RowProperty) || (e.Property == Grid.ColumnProperty))
		{
			InvalidateMeasure();
		}
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