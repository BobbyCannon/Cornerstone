#region References

using Avalonia.Data.Converters;
using Avalonia.Media;
using Cornerstone.Avalonia.Resources;

#endregion

namespace Cornerstone.Avalonia.Converters;

public static class ThemeConverters
{
	#region Fields

	public static readonly FuncValueConverter<ThemeColor, object, IBrush> GetColorBrushAtIndex;

	#endregion

	#region Constructors

	static ThemeConverters()
	{
		GetColorBrushAtIndex = new(Convert);
	}

	#endregion

	#region Methods

	public static IBrush Convert(ThemeColor value, object parameter)
	{
		var key = $"{value}{parameter}";
		var color = ResourceService.Get<Color>(key);
		return new SolidColorBrush(color);
	}

	#endregion
}