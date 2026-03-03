#region References

using Avalonia;
using Avalonia.Controls;

#endregion

namespace Cornerstone.Avalonia.Controls;

public static class ControlHelper
{
	#region Fields

	public static readonly AttachedProperty<string> DisplayNameProperty;
	public static readonly AttachedProperty<double> DisplayNameWidthProperty;
	public static readonly AttachedProperty<object> InnerRightContentProperty;

	#endregion

	#region Constructors

	static ControlHelper()
	{
		DisplayNameProperty = AvaloniaProperty.RegisterAttached<Control, string>("DisplayName", typeof(ControlHelper));
		DisplayNameWidthProperty = AvaloniaProperty.RegisterAttached<Control, double>("DisplayNameWidth", typeof(ControlHelper));
		InnerRightContentProperty = AvaloniaProperty.RegisterAttached<Control, object>("InnerRightContent", typeof(ControlHelper));
	}

	#endregion

	#region Methods

	public static string GetDisplayName(Control control)
	{
		return control.GetValue(DisplayNameProperty);
	}

	public static double GetDisplayNameWidth(Control control)
	{
		return control.GetValue(DisplayNameWidthProperty);
	}

	public static object GetInnerRightContent(Control control)
	{
		return control.GetValue(InnerRightContentProperty);
	}

	public static void SetDisplayName(Control control, string value)
	{
		control.SetValue(DisplayNameProperty, value);
	}

	public static void SetDisplayNameWidth(Control control, double value)
	{
		control.SetValue(DisplayNameWidthProperty, value);
	}

	public static void SetInnerRightContent(Control control, object value)
	{
		control.SetValue(InnerRightContentProperty, value);
	}

	#endregion
}