#region References

using Avalonia;
using Avalonia.Controls;

#endregion

namespace Cornerstone.Avalonia.Behaviors;

public static class ColumnDefinitionBehavior
{
	#region Fields

	private static readonly AttachedProperty<double?> LastMinWidthProperty =
		AvaloniaProperty.RegisterAttached<ColumnDefinition, double?>("LastMinWidth"
			, typeof(ColumnDefinitionBehavior));

	private static readonly AttachedProperty<GridLength?> LastWidthProperty =
		AvaloniaProperty.RegisterAttached<ColumnDefinition, GridLength?>("LastWidth"
			, typeof(ColumnDefinitionBehavior));

	private static readonly GridLength ZeroWidth = new(0, GridUnitType.Pixel);

	public static readonly AttachedProperty<bool> IsVisibleProperty =
		AvaloniaProperty.RegisterAttached<ColumnDefinition, bool>("IsVisible",
			typeof(ColumnDefinitionBehavior), true, coerce: IsVisibleChanged);

	#endregion

	#region Methods

	public static bool GetIsVisible(ColumnDefinition columnDefinition)
	{
		return columnDefinition.GetValue(IsVisibleProperty);
	}

	public static void SetIsVisible(ColumnDefinition columnDefinition, bool visibility)
	{
		columnDefinition.SetValue(IsVisibleProperty, visibility);
	}

	private static bool IsVisibleChanged(AvaloniaObject element, bool visibility)
	{
		if (visibility)
		{
			//var lastMinWidth = element.GetValue(LastMinWidthProperty);
			//if (lastMinWidth != null)
			//{
			//	element.SetValue(ColumnDefinition.MinWidthProperty, lastMinWidth);
			//}

			var lastWidth = element.GetValue(LastWidthProperty);
			if (lastWidth is not null)
			{
				element.SetValue(ColumnDefinition.WidthProperty, lastWidth);
			}
		}
		else
		{
			//var currentMinWidth = element.GetValue(ColumnDefinition.MinWidthProperty);
			//element.SetValue(LastMinWidthProperty, currentMinWidth);

			var currentWidth = element.GetValue(ColumnDefinition.WidthProperty);
			element.SetValue(LastWidthProperty, currentWidth);

			//element.SetValue(ColumnDefinition.MinWidthProperty, 0);
			element.SetValue(ColumnDefinition.WidthProperty, ZeroWidth);
		}

		return visibility;
	}

	#endregion
}