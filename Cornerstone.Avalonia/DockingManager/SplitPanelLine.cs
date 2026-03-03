#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;

#endregion

namespace Cornerstone.Avalonia.DockingManager;

public class SplitPanelLine : Line
{
	#region Fields

	private readonly IObservable<object> _defaultColor;
	private readonly IObservable<object> _hoverColor;

	#endregion

	#region Constructors

	public SplitPanelLine()
	{
		_defaultColor = Application.Current.GetResourceObservable("Background02",
			x => new SolidColorBrush(x is Color c ? c : Colors.DarkGray)
		);

		_hoverColor = Application.Current.GetResourceObservable("ThemeColor06",
			x => new SolidColorBrush(x is Color c ? c : Colors.Gray)
		);

		StrokeThickness = 4;
		Bind(StrokeProperty, _defaultColor);
		Opacity = 0.2;
	}

	#endregion

	#region Properties

	protected override Type StyleKeyOverride => typeof(SplitPanelLine);

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void OnPointerEntered(PointerEventArgs e)
	{
		Bind(StrokeProperty, _hoverColor);
		Opacity = 0.75;
		base.OnPointerEntered(e);
	}

	/// <inheritdoc />
	protected override void OnPointerExited(PointerEventArgs e)
	{
		Bind(StrokeProperty, _defaultColor);
		Opacity = 0.2;
		base.OnPointerExited(e);
	}

	#endregion
}