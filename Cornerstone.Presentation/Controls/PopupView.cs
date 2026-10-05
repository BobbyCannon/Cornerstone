#region References

using System.ComponentModel;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Theming;

#endregion

namespace Cornerstone.Presentation.Controls;

public class PopupView : ContentControl
{
	#region Fields

	private PopupViewModel _popup;

	public static readonly StyledProperty<HorizontalAlignment> PopupHorizontalAlignmentProperty;
	public static readonly StyledProperty<VerticalAlignment> PopupVerticalAlignmentProperty;
	public static readonly StyledProperty<bool> ShowBackgroundProperty;

	#endregion

	#region Constructors

	static PopupView()
	{
		PopupHorizontalAlignmentProperty = PresentationProperty.Register<PopupView, HorizontalAlignment>(nameof(PopupHorizontalAlignment));
		PopupVerticalAlignmentProperty = PresentationProperty.Register<PopupView, VerticalAlignment>(nameof(PopupVerticalAlignment));
		ShowBackgroundProperty = PresentationProperty.Register<PopupView, bool>(nameof(ShowBackground));
	}

	#endregion

	#region Properties

	public HorizontalAlignment PopupHorizontalAlignment
	{
		get => GetValue(PopupHorizontalAlignmentProperty);
		set => SetValue(PopupHorizontalAlignmentProperty, value);
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

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		if (change.Property == DataContextProperty)
		{
			if (_popup != null)
			{
				_popup.PropertyChanged -= OnPopupPropertyChanged;
			}

			_popup = DataContext as PopupViewModel;
			if (_popup != null)
			{
				_popup.PropertyChanged += OnPopupPropertyChanged;
			}

			ApplyDestructiveBorder();
		}

		base.OnPropertyChanged(change);
	}

	private void ApplyDestructiveBorder()
	{
		if (_popup == null)
		{
			return;
		}

		var key = _popup.IsDestructive ? "Red06" : "Background06";
		BorderBrush = ThemeBrushes.Get(this, key);
	}

	private void OnPopupPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(PopupViewModel.IsDestructive))
		{
			ApplyDestructiveBorder();
		}
	}

	#endregion
}