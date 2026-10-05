#region References

using Cornerstone.Presentation;
using Cornerstone.Presentation.Input;

#endregion

namespace Cornerstone.Presentation.Controls.DockingManager;

internal static class VisualExtensions
{
	#region Methods

	public static Rect GetBoundsOf(this Visual self, Visual visual)
	{
		var topLeft = visual.TranslatePoint(new Point(0, 0), self)!.Value;
		var bottomRight = visual.TranslatePoint(new Point(visual.Bounds.Width, visual.Bounds.Height), self)!.Value;
		return new Rect(topLeft, bottomRight);
	}

	/// <summary>
	/// Pointer position in <paramref name="relativeTo" /> local coordinates via screen space
	/// so events originating in another top-level (torn-off tab) still map correctly.
	/// </summary>
	public static Point GetPointerPosition(this Visual relativeTo, PointerEventArgs e)
	{
		if (e?.Source is Visual source)
		{
			return relativeTo.PointToClient(source.PointToScreen(e.GetPosition(source)));
		}

		return e == null ? default : e.GetPosition(relativeTo);
	}

	public static PixelPoint GetPointerScreenPosition(PointerEventArgs e)
	{
		if (e?.Source is Visual source)
		{
			return source.PointToScreen(e.GetPosition(source));
		}

		return default;
	}

	#endregion
}