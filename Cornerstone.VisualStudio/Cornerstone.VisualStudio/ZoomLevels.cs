#region References

using System.Globalization;

#endregion

namespace Cornerstone.VisualStudio;

internal static class ZoomLevels
{
	#region Fields

	// Percentage-only. Fit All / Fit to Width were removed — they couple viewport
	// layout to host scaling and caused resize ↔ re-render feedback loops.
	public static readonly string[] Levels =
	[
		FmtZoomLevel(800), FmtZoomLevel(400), FmtZoomLevel(200), FmtZoomLevel(150), FmtZoomLevel(100),
		FmtZoomLevel(66.67), FmtZoomLevel(50), FmtZoomLevel(33.33), FmtZoomLevel(25), FmtZoomLevel(12.5)
	];

	#endregion

	#region Methods

	public static string FmtZoomLevel(double v)
	{
		return $"{v.ToString(CultureInfo.InvariantCulture)}%";
	}

	#endregion
}