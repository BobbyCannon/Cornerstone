#region References

using Cornerstone.Presentation.Backends.Skia;
using Cornerstone.Presentation.Backends.Skia.Helpers;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia;

[TestClass]
public class DrawingContextImplTests
{
	#region Methods

	[PresentationTestMethod]
	public void DrawLineWithZeroThicknessPenDoesNotThrow()
	{
		var target = CreateTarget();
		target.DrawLine(new Pen(Brushes.Black, 0), new Point(0, 0), new Point(10, 10));
	}

	[PresentationTestMethod]
	public void DrawRectangleWithZeroThicknessPenDoesNotThrow()
	{
		var target = CreateTarget();
		target.DrawRectangle(Brushes.Black, new Pen(Brushes.Black, 0), new RoundedRect(new Rect(0, 0, 100, 100), new CornerRadius(4)));
	}

	[PresentationTestMethod]
	[DataRow(1d)]
	[DataRow(2d)]
	public void RoundedBorderCornerStaysOpaqueInsideClip(double thickness)
	{
		using var bitmap = new SKBitmap(80, 80);
		using var canvas = new SKCanvas(bitmap);
		canvas.Clear(SKColors.Transparent);
		var target = (DrawingContextImpl) DrawingContextHelper.WrapSkiaCanvas(canvas, new Vector(96, 96));

		const double radius = 16;
		var outer = new RoundedRect(new Rect(0, 0, 80, 80), radius);
		target.PushClip(outer);
		target.DrawRectangle(null, new Pen(Brushes.White, thickness), outer.UniformBorderCenterline(thickness));
		target.PopClip();
		canvas.Flush();

		var edge = bitmap.GetPixel(40, 0).Alpha;
		var insideEdge = bitmap.GetPixel(40, (int) thickness).Alpha;
		var outside = bitmap.GetPixel(0, 0).Alpha;
		var corner = 0;
		var cornerX = 0;
		var cornerY = 0;
		// Stay on the bend. The straight edge enters this quadrant near x = 16.
		for (var y = 3; y <= 8; y++)
		{
			for (var x = 3; x <= 8; x++)
			{
				var alpha = bitmap.GetPixel(x, y).Alpha;
				if (alpha > corner)
				{
					corner = alpha;
					cornerX = x;
					cornerY = y;
				}
			}
		}

		CornerstoneTest.IsTrue(edge > 240, () => $"Straight edge alpha was {edge}.");
		CornerstoneTest.IsTrue(insideEdge < 48, () => $"Pixel inside the straight edge alpha was {insideEdge}.");
		CornerstoneTest.IsTrue(corner > 220, () => $"Corner alpha was {corner} at ({cornerX}, {cornerY}), edge was {edge}.");
		CornerstoneTest.IsTrue(outside < 32, () => $"Pixel outside the corner alpha was {outside}.");
	}

	private static DrawingContextImpl CreateTarget()
	{
		var canvas = new SKCanvas(new SKBitmap(100, 100));
		return (DrawingContextImpl) DrawingContextHelper.WrapSkiaCanvas(canvas, new Vector(96, 96));
	}

	#endregion
}