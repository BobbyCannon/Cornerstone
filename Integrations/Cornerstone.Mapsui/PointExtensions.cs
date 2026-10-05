#region References

using Cornerstone.Presentation;
using Mapsui.Manipulations;

#endregion

namespace Cornerstone.Mapsui;

internal static class PointExtensions
{
	public static ScreenPosition ToScreenPosition(this Point point)
	{
		return new ScreenPosition(point.X, point.Y);
	}
}
