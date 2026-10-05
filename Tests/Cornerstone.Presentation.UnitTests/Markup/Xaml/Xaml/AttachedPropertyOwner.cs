#nullable enable

#region References

using Cornerstone.Presentation.Controls;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

public class AttachedPropertyOwner
{
	#region Fields

	public static readonly AttachedProperty<double> DoubleProperty =
		PresentationProperty.RegisterAttached<AttachedPropertyOwner, Control, double>("Double");

	#endregion

	#region Methods

	public static double GetDouble(Control control)
	{
		return control.GetValue(DoubleProperty);
	}

	public static void SetDouble(Control control, double value)
	{
		control.SetValue(DoubleProperty, value);
	}

	#endregion
}