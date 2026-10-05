#region References

using System;
using System.Globalization;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Media;

#endregion

namespace Cornerstone.Presentation.Diagnostics.Converters;

public class BrushSelectorConveter : PresentationObject, IValueConverter
{
	#region Fields

	public static readonly DirectProperty<BrushSelectorConveter, IBrush> BrushProperty =
		PresentationProperty.RegisterDirect<BrushSelectorConveter, IBrush>(nameof(Brush)
			, o => o.Brush
			, (o, v) => o.Brush = v);

	#endregion

	#region Properties

	public IBrush Brush { get; set; }

	#endregion

	#region Methods

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (ReferenceEquals(value, parameter))
		{
			return Brush;
		}
		if (value is ISolidColorBrush a
			&& parameter is ISolidColorBrush b
			&& (a.Color == b.Color)
			&& (a.Transform == b.Transform)
			&& (b.Opacity == a.Opacity)
			)
		{
			return Brush;
		}
		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return BindingOperations.DoNothing;
	}

	#endregion
}