#region References

using Avalonia;
using Avalonia.Layout;
using Cornerstone.Avalonia.Resources;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Avalonia.Controls;

public class PopupView : CornerstoneContentControl
{
	#region Fields

	public static readonly StyledProperty<HorizontalAlignment> PopupHorizontalAlignmentProperty;
	public static readonly StyledProperty<int> PopupMinWidthProperty;
	public static readonly StyledProperty<VerticalAlignment> PopupVerticalAlignmentProperty;
	public static readonly StyledProperty<bool> ShowBackgroundProperty;

	#endregion

	#region Constructors

	static PopupView()
	{
		PopupHorizontalAlignmentProperty = AvaloniaProperty.Register<PopupView, HorizontalAlignment>(nameof(PopupHorizontalAlignment));
		PopupMinWidthProperty = AvaloniaProperty.Register<PopupView, int>(nameof(PopupMinWidth), PopupViewModel.DefaultWidth);
		PopupVerticalAlignmentProperty = AvaloniaProperty.Register<PopupView, VerticalAlignment>(nameof(PopupVerticalAlignment));
		ShowBackgroundProperty = AvaloniaProperty.Register<PopupView, bool>(nameof(ShowBackground));
	}

	#endregion

	#region Properties

	public HorizontalAlignment PopupHorizontalAlignment
	{
		get => GetValue(PopupHorizontalAlignmentProperty);
		set => SetValue(PopupHorizontalAlignmentProperty, value);
	}

	public int PopupMinWidth
	{
		get => GetValue(PopupMinWidthProperty);
		set => SetValue(PopupMinWidthProperty, value);
	}

	public VerticalAlignment PopupVerticalAlignment
	{
		get => GetValue(PopupVerticalAlignmentProperty);
		set => SetValue(PopupVerticalAlignmentProperty, value);
	}

	public bool ShowBackground
	{
		get => GetValue(ShowBackgroundProperty);
		set => SetValue(ShowBackgroundProperty, value);
	}

	#endregion

	#region Methods

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if (change.Property == DataContextProperty)
		{
			if (DataContext is PopupViewModel popup)
			{
				popup.Check();
				BorderBrush = popup.IsDestructive
					? ResourceService.GetColorAsBrush("Red06")
					: ResourceService.GetColorAsBrush("Background06");
			}
		}

		base.OnPropertyChanged(change);
	}

	#endregion
}