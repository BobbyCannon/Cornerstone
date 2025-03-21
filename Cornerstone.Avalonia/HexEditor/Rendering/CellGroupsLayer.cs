#region References

using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media;
using Cornerstone.Avalonia.HexEditor.Document;

#endregion

namespace Cornerstone.Avalonia.HexEditor.Rendering;

/// <summary>
/// Provides a render layer for a hex view that visually separates groups of cells.
/// </summary>
public class CellGroupsLayer : Layer
{
	#region Fields

	/// <summary>
	/// Defines the <see cref="Backgrounds" /> property.
	/// </summary>
	public static readonly DirectProperty<CellGroupsLayer, ObservableCollection<IBrush>> BackgroundsProperty =
		AvaloniaProperty.RegisterDirect<CellGroupsLayer, ObservableCollection<IBrush>>(
			nameof(Backgrounds),
			x => x.Backgrounds
		);

	/// <summary>
	/// Defines the <see cref="Border" /> property.
	/// </summary>
	public static readonly StyledProperty<IPen> BorderProperty =
		AvaloniaProperty.Register<CellGroupsLayer, IPen>(
			nameof(Border));

	/// <summary>
	/// Defines the <see cref="BytesPerGroupProperty" /> property.
	/// </summary>
	public static readonly StyledProperty<int> BytesPerGroupProperty =
		AvaloniaProperty.Register<CellGroupsLayer, int>(nameof(BytesPerGroup), 8);

	#endregion

	#region Constructors

	static CellGroupsLayer()
	{
		AffectsRender<CellGroupsLayer>(
			BytesPerGroupProperty,
			BorderProperty,
			BackgroundsProperty
		);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets a collection of background brushes that each vertical cell group is rendered with.
	/// </summary>
	public ObservableCollection<IBrush> Backgrounds { get; } = new();

	/// <summary>
	/// Gets or sets the pen used for rendering the separation lines between each group.
	/// </summary>
	public IPen Border
	{
		get => GetValue(BorderProperty);
		set => SetValue(BorderProperty, value);
	}

	/// <summary>
	/// Gets or sets a value indicating the number of cells each group consists of.
	/// </summary>
	public int BytesPerGroup
	{
		get => GetValue(BytesPerGroupProperty);
		set => SetValue(BytesPerGroupProperty, value);
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void Render(DrawingContext context)
	{
		base.Render(context);

		if (HexView is null || Border is null || (HexView.VisualLines.Count == 0))
		{
			return;
		}

		foreach (var c in HexView.Columns)
		{
			if (c is not CellBasedColumn { IsVisible: true } column)
			{
				continue;
			}

			DivideColumn(context, column);
		}
	}

	private void DivideColumn(DrawingContext context, CellBasedColumn column)
	{
		var groupIndex = 0;

		var left = column.Bounds.Left;

		var line = HexView!.VisualLines[0];
		for (uint offset = 0; offset < HexView.ActualBytesPerLine; offset += (uint) BytesPerGroup, groupIndex++)
		{
			var right1 = new BitLocation((line.Range.Start.ByteIndex + (uint) BytesPerGroup + offset) - 1, 0);
			var right2 = new BitLocation(line.Range.Start.ByteIndex + (uint) BytesPerGroup + offset, 7);
			var rightCell1 = column.GetCellBounds(line, right1);
			var rightCell2 = column.GetCellBounds(line, right2);

			var right = Math.Min(column.Bounds.Right, 0.5 * (rightCell1.Right + rightCell2.Left));

			if (Backgrounds.Count > 0)
			{
				var background = Backgrounds[groupIndex % Backgrounds.Count];
				if (background is not null)
				{
					context.FillRectangle(background, new Rect(left, 0, right - left, column.Bounds.Height));
				}
			}

			if (groupIndex > 0)
			{
				context.DrawLine(
					Border!,
					new Point(left, 0),
					new Point(left, HexView.Bounds.Height)
				);
			}

			left = right;
		}
	}

	#endregion
}