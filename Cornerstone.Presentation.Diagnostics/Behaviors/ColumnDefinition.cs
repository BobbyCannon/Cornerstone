#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Layout;

#endregion

namespace Cornerstone.Presentation.Diagnostics.Behaviors;

/// <summary>
/// See discussion discussions/6773
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
		IsVisibleProperty = PresentationProperty.RegisterAttached<Cornerstone.Presentation.Controls.Layout.ColumnDefinition, bool>("IsVisible"
			, typeof(ColumnDefinition)
			, true
			, coerce: (element, visibility) =>
			{
				var lastWidth = element.GetValue(LastWidthProperty);
				if (visibility && lastWidth is not null)
				{
					element.SetValue(Cornerstone.Presentation.Controls.Layout.ColumnDefinition.WidthProperty, lastWidth);
				}
				else if (!visibility)
				{
					element.SetValue(LastWidthProperty, element.GetValue(Cornerstone.Presentation.Controls.Layout.ColumnDefinition.WidthProperty));
					element.SetValue(Cornerstone.Presentation.Controls.Layout.ColumnDefinition.WidthProperty, ZeroWidth);
				}
				return visibility;
			}
		);
		LastWidthProperty = PresentationProperty.RegisterAttached<Cornerstone.Presentation.Controls.Layout.ColumnDefinition, GridLength?>("LastWidth"
			, typeof(ColumnDefinition));
	}

	#endregion

	#region Methods

	public static bool GetIsVisible(Cornerstone.Presentation.Controls.Layout.ColumnDefinition columnDefinition)
	{
		return columnDefinition.GetValue(IsVisibleProperty);
	}

	public static void SetIsVisible(Cornerstone.Presentation.Controls.Layout.ColumnDefinition columnDefinition, bool visibility)
	{
		columnDefinition.SetValue(IsVisibleProperty, visibility);
	}

	#endregion
}