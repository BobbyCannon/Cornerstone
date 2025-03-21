#region References

using System.Drawing;

#endregion

namespace Cornerstone.Avalonia.Platforms.Windows;

internal static class Extensions
{
	#region Methods

	public static Color ToDrawingColor(this global::Avalonia.Media.Color color)
	{
		return Color.FromArgb(color.A, color.R, color.G, color.B);
	}

	#endregion
}