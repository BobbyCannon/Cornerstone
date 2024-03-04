#region References

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
#if !NET48
using System.Windows.Markup;
#endif

#endregion

namespace Cornerstone.Wpf.Converters;

/// <summary>
/// A converter for inverting values.
/// </summary>
public class InvertValueConverter :
	#if !NET48
	MarkupExtension,
	#endif
	IValueConverter
{
	#region Constructors

	/// <summary>
	/// Initialize the visibility converter.
	/// </summary>
	public InvertValueConverter()
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		switch (value)
		{
			case bool b:
			{
				return !b;
			}
			default:
			{
				return value != null
					? null
					: targetType.CreateInstance();
			}
		}
	}

	/// <inheritdoc />
	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		var myValue = (Visibility) value;

		switch (targetType.Name)
		{
			case "Boolean":
				return myValue == Visibility.Visible;

			default:
				throw new NotSupportedException();
		}
	}

	/// <summary>
	/// When implemented in a derived class, returns an object that is provided as the value of the target property for this
	/// markup extension.
	/// </summary>
	/// <returns>
	/// The object value to set on the property where the extension is applied.
	/// </returns>
	/// <param name="serviceProvider"> A service provider helper that can provide services for the markup extension. </param>
	#if (NET48)
	public object ProvideValue(IServiceProvider serviceProvider)
		#else
	public override object ProvideValue(IServiceProvider serviceProvider)
		#endif
	{
		return this;
	}

	#endregion
}