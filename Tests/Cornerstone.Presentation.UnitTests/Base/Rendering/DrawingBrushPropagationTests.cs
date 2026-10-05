#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Rendering;

[TestClass]
public class DrawingBrushPropagationTests : CompositorTestsBase
{
	#region Methods

	[PresentationTestMethod]
	public void MutatingGeometryInsideDrawingBrushInvalidatesConsumer()
	{
		using var services = new CompositorCanvas();

		var geometry = new RectangleGeometry(new Rect(0, 0, 20, 10));
		var brush = new DrawingBrush(new GeometryDrawing { Brush = Brushes.Red, Geometry = geometry });
		services.Canvas.Children.Add(CreateBorder(brush));
		services.RunJobs();
		services.Events.Rects.Clear();

		geometry.Rect = new Rect(0, 0, 30, 15);

		services.AssertRects(new Rect(30, 50, 20, 10));
	}

	[PresentationTestMethod]
	public void ReplacingDrawingInvalidatesConsumer()
	{
		using var services = new CompositorCanvas();

		var brush = new DrawingBrush(new GeometryDrawing
		{
			Brush = Brushes.Red,
			Geometry = new RectangleGeometry(new Rect(0, 0, 20, 10))
		});
		services.Canvas.Children.Add(CreateBorder(brush));
		services.RunJobs();
		services.Events.Rects.Clear();

		brush.Drawing = new GeometryDrawing
		{
			Brush = Brushes.Blue,
			Geometry = new RectangleGeometry(new Rect(0, 0, 20, 10))
		};

		services.AssertRects(new Rect(30, 50, 20, 10));
	}

	private static Border CreateBorder(DrawingBrush brush)
	{
		return new()
		{
			Background = brush,
			Width = 20,
			Height = 10,
			[Canvas.LeftProperty] = 30,
			[Canvas.TopProperty] = 50
		};
	}

	#endregion
}