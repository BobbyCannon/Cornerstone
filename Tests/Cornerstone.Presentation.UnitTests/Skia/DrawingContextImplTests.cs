#region References

using Cornerstone.Presentation.Backends.Skia;
using Cornerstone.Presentation.Backends.Skia.Helpers;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
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

	private static DrawingContextImpl CreateTarget()
	{
		var canvas = new SKCanvas(new SKBitmap(100, 100));
		return (DrawingContextImpl) DrawingContextHelper.WrapSkiaCanvas(canvas, new Vector(96, 96));
	}

	#endregion
}