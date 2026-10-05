namespace Cornerstone.Presentation.Data.Converters;

/// <summary>
/// Binding converters for colors and solid brushes.
/// </summary>
public static class ColorConverters
{
	#region Fields

	public static readonly IValueConverter WithOpacity;

	#endregion

	#region Constructors

	static ColorConverters()
	{
		WithOpacity = new ColorOpacityConverter();
	}

	#endregion
}
