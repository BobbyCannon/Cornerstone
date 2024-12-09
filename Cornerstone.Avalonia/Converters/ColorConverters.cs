#region References

using Avalonia.Data.Converters;
using Avalonia.Media;

#endregion

namespace Cornerstone.Avalonia.Converters;

public static class ColorConverters
{
	#region Fields

	public static readonly FuncValueConverter<Color, IBrush> ToBrush = new(x => new SolidColorBrush(x));

	#endregion
}