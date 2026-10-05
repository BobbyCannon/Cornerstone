#region References

using System.Drawing;

#endregion

namespace Cornerstone.Presentation.Platforms.Windows;

internal static class Extensions
{
	#region Methods

	public static Color ToDrawingColor(this global::Cornerstone.Presentation.Media.Color color)
	{
		return Color.FromArgb(color.A, color.R, color.G, color.B);
	}

	#endregion
}