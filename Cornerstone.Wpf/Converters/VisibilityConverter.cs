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
/// A converter for handling visibility of controls.
/// </summary>
public class VisibilityConverter :
	#if !NET48
	MarkupExtension,
	#endif
	IValueConverter
{
	#region Constructors

	/// <summary>
	/// Initialize the visibility converter.
	/// </summary>
	public VisibilityConverter()
	{
		Collapse = true;
		Inverted = false;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Collapse the control. If not set then the not visible controls will be Hidden instead.
	/// </summary>
	public bool Collapse { get; set; }

	/// <summary>
	/// True will invert the show / hide response.
	/// </summary>
	public bool Inverted { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		var hideMode = Collapse ? Visibility.Collapsed : Visibility.Hidden;
		var show = Inverted ? hideMode : Visibility.Visible;
		var hide = Inverted ? Visibility.Visible : hideMode;

		switch (value)
		{
			case bool b:
			{
				return b ? show : hide;
			}
			case string s:
			{
				var sBoolean = parameter != null
					? string.Equals(s, parameter.ToString())
					: !string.IsNullOrWhiteSpace(s);

				return sBoolean ? show : hide;
			}
			case Enum e:
			{
				var eParameter = int.TryParse(parameter?.ToString(), out var ep) ? ep : 0;
				return Equals(System.Convert.ToInt32(e), eParameter) ? show : hide;
			}
			case byte b:
			case short s:
			case ushort us:
			case int i:
			case uint ui:
			case long l:
			case ulong ul:
			case float f:
			case double d:
			case decimal dValue:
			{
				if ((parameter != null) && decimal.TryParse(parameter.ToString(), out var number))
				{
					var nValue = decimal.Parse(value.ToString() ?? "0");
					return nValue >= number ? show : hide;
				}

				return decimal.Parse(value.ToString() ?? "0") <= 0 ? hide : show;
			}
			case Type t:
			{
				return Equals(t.FullName, parameter) ? show : hide;
			}
			default:
			{
				return value != null ? show : hide;
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