#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Avalonia.Controls;

[DoNotNotify]
public class ToggleableLayoutGrid : Grid
{
	#region Fields

	public static readonly StyledProperty<bool> UseHorizontalProperty = AvaloniaProperty.Register<ToggleableLayoutGrid, bool>(nameof(UseHorizontal));

	public static readonly StyledProperty<bool> UseLayoutGridProperty = AvaloniaProperty.Register<ToggleableLayoutGrid, bool>(nameof(UseLayoutGrid), true);

	#endregion

	#region Constructors

	static ToggleableLayoutGrid()
	{
		UseLayoutGridProperty.Changed.AddClassHandler<ToggleableLayoutGrid>((o, _) => o.RefreshLayout());
		UseHorizontalProperty.Changed.AddClassHandler<ToggleableLayoutGrid>((o, _) => o.RefreshLayout());
	}

	#endregion

	#region Properties

	public bool UseHorizontal
	{
		get => GetValue(UseHorizontalProperty);
		set => SetValue(UseHorizontalProperty, value);
	}

	public bool UseLayoutGrid
	{
		get => GetValue(UseLayoutGridProperty);
		set => SetValue(UseLayoutGridProperty, value);
	}

	protected override Type StyleKeyOverride => typeof(Grid);

	#endregion

	#region Methods

	public override void ApplyTemplate()
	{
		base.ApplyTemplate();
		RefreshLayout();
	}

	private void RefreshLayout()
	{
		var rowSpan = RowDefinitions.Count;
		var colSpan = ColumnDefinitions.Count;

		if (UseHorizontal)
		{
			for (var i = 0; i < Children.Count; i++)
			{
				var child = Children[i];
				child.SetValue(RowProperty, 0);
				child.SetValue(RowSpanProperty, rowSpan);
				child.SetValue(ColumnProperty, i);
				child.SetValue(ColumnSpanProperty, (i == 0) && !UseLayoutGrid ? colSpan : 1);
				child.IsVisible = (i == 0) || UseLayoutGrid;
			}
		}
		else
		{
			for (var i = 0; i < Children.Count; i++)
			{
				var child = Children[i];
				child.SetValue(RowProperty, i);
				child.SetValue(RowSpanProperty, (i == 0) && !UseLayoutGrid ? rowSpan : 1);
				child.SetValue(ColumnProperty, 0);
				child.SetValue(ColumnSpanProperty, colSpan);
				child.IsVisible = (i == 0) || UseLayoutGrid;
			}
		}

		InvalidateArrange();
	}

	#endregion
}