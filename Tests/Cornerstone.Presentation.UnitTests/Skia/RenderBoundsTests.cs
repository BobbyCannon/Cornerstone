#region References

using Cornerstone.Presentation.Backends.Skia;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia;

[TestClass]
public class RenderBoundsTests
{
	#region Methods

	[PresentationTestMethod]
	[DataRow("M10 20 L 20 10 L 30 20", PenLineCap.Round, PenLineJoin.Miter, 2, 10,
		9, 8.585786819458008, 22.000001907348633, 12.414215087890625)]
	[DataRow("M10 10 L 20 10", PenLineCap.Round, PenLineJoin.Miter, 2, 10,
		9, 9, 12, 2)]
	[DataRow("M10 10 L 20 15 L 10 20", PenLineCap.Flat, PenLineJoin.Miter, 2, 20,
		9.552786827087402, 9.105572700500488, 12.683281898498535, 11.788853645324707)]
	[DataRow("M0,0 A128,128 0 0 0 128,0", PenLineCap.Flat, PenLineJoin.Bevel, 0, 0,
		0, 0, 128, 17.14875030517578)]
	public void RenderBoundsAreCorrectlyCalculated(string path, PenLineCap cap, PenLineJoin join, double thickness, double miterLimit, double x, double y, double width, double height)
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
					.With(renderInterface: new PlatformRenderInterface())))
		{
			var geo = PathGeometry.Parse(path);
			var pen = new Pen(Brushes.Black, thickness, null, cap, join, miterLimit);
			var bounds = geo.GetRenderBounds(pen);
			var tolerance = 0.001;

			CornerstoneTest.AreEqual(bounds.X, x, tolerance);
			CornerstoneTest.AreEqual(bounds.Y, y, tolerance);
			CornerstoneTest.AreEqual(bounds.Width, width, tolerance);
			CornerstoneTest.AreEqual(bounds.Height, height, tolerance);
		}
	}

	#endregion
}