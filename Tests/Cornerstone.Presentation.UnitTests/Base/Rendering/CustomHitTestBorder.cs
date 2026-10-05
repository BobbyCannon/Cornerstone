#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Rendering;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Rendering;

internal class CustomHitTestBorder : Border, ICustomHitTest
{
	#region Methods

	public bool HitTest(Point point)
	{
		// Move hit testing window halfway to the left
		return new Rect(-Bounds.Width / 2, 0, Bounds.Width, Bounds.Height)
			.Contains(point);
	}

	public IntersectionResult HitTest(Geometry geometry)
	{
		return geometry.GetFillIntersectionResult(new RectangleGeometry(new Rect(-Bounds.Width / 2, 0, Bounds.Width, Bounds.Height))) ?? IntersectionResult.Empty;
	}

	#endregion
}