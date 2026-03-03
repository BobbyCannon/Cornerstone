#region References

using System.Drawing;

#endregion

namespace Cornerstone.Avalonia.Extensions;

public static class ColorExtensions
{
	#region Methods

	public static string ToHtmlString(this Color color)
	{
		return color.A < 255
			? $"rgba({color.R}, {color.G}, {color.B}, {color.A / 255.0:0.###})"
			: $"#{color.R:X2}{color.G:X2}{color.B:X2}";
	}

	#endregion
}