#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Rendering;

[TestClass]
public class DrawingImagePropagationTests : CompositorTestsBase
{
	#region Methods

	[PresentationTestMethod]
	public void MutatingGeometryInsideDrawingImageInvalidatesConsumer()
	{
		using var services = new CompositorCanvas();

		var geometry = new RectangleGeometry(new Rect(0, 0, 20, 10));
		var image = new DrawingImage(new GeometryDrawing { Brush = Brushes.Red, Geometry = geometry });
		services.Canvas.Children.Add(CreateImage(image));
		services.RunJobs();
		services.Events.Rects.Clear();

		geometry.Rect = new Rect(0, 0, 30, 15);

		services.AssertRects(
			new Rect(30, 50, 20, 10),
			new Rect(30, 50, 30, 15));
	}

	private static Image CreateImage(DrawingImage image)
	{
		return new()
		{
			Source = image,
			Width = 20,
			Height = 10,
			[Canvas.LeftProperty] = 30,
			[Canvas.TopProperty] = 50
		};
	}

	#endregion
}