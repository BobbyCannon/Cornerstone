#region References

using Avalonia.Controls;

#endregion

namespace Avalonia.Diagnostics.Behaviors;

/// <summary>
/// See discussion https://github.com/AvaloniaUI/Avalonia/discussions/6773
/// </summary>
internal static class ColumnDefinition
{
	#region Fields

	public static readonly AttachedProperty<bool> IsVisibleProperty;
	private static readonly AttachedProperty<GridLength?> LastWidthProperty;
	private static readonly GridLength ZeroWidth;

	#endregion

	#region Constructors

	static ColumnDefinition()
	{
		ZeroWidth = new(0, GridUnitType.Pixel);
		IsVisibleProperty = AvaloniaProperty.RegisterAttached<Avalonia.Controls.ColumnDefinition, bool>("IsVisible"
			, typeof(ColumnDefinition)
			, true
			, coerce: (element, visibility) =>
			{
				var lastWidth = element.GetValue(LastWidthProperty);
				if (visibility && lastWidth is not null)
				{
					element.SetValue(Avalonia.Controls.ColumnDefinition.WidthProperty, lastWidth);
				}
				else if (!visibility)
				{
					element.SetValue(LastWidthProperty, element.GetValue(Avalonia.Controls.ColumnDefinition.WidthProperty));
					element.SetValue(Avalonia.Controls.ColumnDefinition.WidthProperty, ZeroWidth);
				}
				return visibility;
			}
		);
		LastWidthProperty = AvaloniaProperty.RegisterAttached<Avalonia.Controls.ColumnDefinition, GridLength?>("LastWidth"
			, typeof(ColumnDefinition));
	}

	#endregion

	#region Methods

	public static bool GetIsVisible(Avalonia.Controls.ColumnDefinition columnDefinition)
	{
		return columnDefinition.GetValue(IsVisibleProperty);
	}

	public static void SetIsVisible(Avalonia.Controls.ColumnDefinition columnDefinition, bool visibility)
	{
		columnDefinition.SetValue(IsVisibleProperty, visibility);
	}

	#endregion
}