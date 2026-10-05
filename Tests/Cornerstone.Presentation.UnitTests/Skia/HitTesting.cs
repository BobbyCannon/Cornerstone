#region References

using Cornerstone.Presentation.Backends.Skia;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Shapes;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Skia;

[TestClass]
public class HitTesting
{
	#region Methods

	[PresentationTestMethod]
	public void HitTestShouldRespectFill()
	{
		using (PresentationLocator.EnterScope())
		{
			SkiaPlatform.Initialize();

			using var services = new CompositorTestServices(new Size(100, 100),
				PresentationLocator.Current.GetRequiredService<IPlatformRenderInterface>())
			{
				TopLevel =
				{
					Content = new Ellipse
					{
						Width = 100,
						Height = 100,
						Fill = Brushes.Red,
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					}
				}
			};

			services.AssertHitTest(10, 10, null);
			services.AssertHitTest(50, 50, null, (Visual) services.TopLevel.Content);
		}
	}

	[PresentationTestMethod]
	public void HitTestShouldRespectStroke()
	{
		using (PresentationLocator.EnterScope())
		{
			SkiaPlatform.Initialize();

			using var services = new CompositorTestServices(new Size(100, 100),
				PresentationLocator.Current.GetRequiredService<IPlatformRenderInterface>())
			{
				TopLevel =
				{
					Content = new Ellipse
					{
						Width = 100,
						Height = 100,
						Stroke = Brushes.Red,
						StrokeThickness = 5,
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					}
				}
			};

			services.AssertHitTest(50, 50, null);
			services.AssertHitTest(1, 50, null, (Visual) services.TopLevel.Content);
		}
	}

	[PresentationTestMethod]
	public void Geometry_Hit_Test_Should_Detect_Stroke_Only_Shapes()
	{
		using (PresentationLocator.EnterScope())
		{
			SkiaPlatform.Initialize();

			Line line = null;

			using var services = new CompositorTestServices(new Size(100, 100),
				PresentationLocator.Current.GetRequiredService<IPlatformRenderInterface>())
			{
				TopLevel =
				{
					Content = line = new Line
					{
						StartPoint = new Point(0, 0),
						EndPoint = new Point(100, 0),
						Stroke = Brushes.Red,
						StrokeThickness = 5,
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					}
				}
			};

			services.AssertHitTest(new RectangleGeometry(new Rect(25, 25, 10, 10)), null);
			services.AssertHitTest(new RectangleGeometry(new Rect(45, 45, 10, 10)), null, new GeometryHitTestResult(line, IntersectionResult.Intersects));
		}
	}

	[PresentationTestMethod]
	public void Geometry_Hit_Test_Border_With_Background_And_Border()
	{
		using (PresentationLocator.EnterScope())
		{
			SkiaPlatform.Initialize();

			Border border = null;

			using var services = new CompositorTestServices(new Size(400, 400),
				PresentationLocator.Current.GetRequiredService<IPlatformRenderInterface>())
			{
				TopLevel =
				{
					Content = border = new Border
					{
						Width = 200,
						Height = 200,
						Background = Brushes.Red,
						BorderBrush = Brushes.Black,
						BorderThickness = new Thickness(10),
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					}
				}
			};

			services.AssertHitTest(new RectangleGeometry(new Rect(100, 100, 50, 50)), null,
				new GeometryHitTestResult(border, IntersectionResult.FullyContains));

			services.AssertHitTest(new RectangleGeometry(new Rect(195, 95, 10, 10)), null,
				new GeometryHitTestResult(border, IntersectionResult.Intersects));

			services.AssertHitTest(new RectangleGeometry(new Rect(310, 310, 10, 10)), null);
		}
	}

	[PresentationTestMethod]
	public void Geometry_Hit_Test_Border_Null_Background_With_BorderBrush()
	{
		using (PresentationLocator.EnterScope())
		{
			SkiaPlatform.Initialize();

			Border border = null;

			using var services = new CompositorTestServices(new Size(400, 400),
				PresentationLocator.Current.GetRequiredService<IPlatformRenderInterface>())
			{
				TopLevel =
				{
					Content = border = new Border
					{
						Width = 200,
						Height = 200,
						Background = null,
						BorderBrush = Brushes.Black,
						BorderThickness = new Thickness(10),
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					}
				}
			};

			services.AssertHitTest(new RectangleGeometry(new Rect(150, 150, 50, 50)), null);

			services.AssertHitTest(new RectangleGeometry(new Rect(190, 105, 10, 10)), null,
				new GeometryHitTestResult(border, IntersectionResult.Intersects));
		}
	}

	[PresentationTestMethod]
	public void Geometry_Hit_Test_Border_Null_Background_And_Null_BorderBrush_NoHit()
	{
		using (PresentationLocator.EnterScope())
		{
			SkiaPlatform.Initialize();

			using var services = new CompositorTestServices(new Size(400, 400),
				PresentationLocator.Current.GetRequiredService<IPlatformRenderInterface>())
			{
				TopLevel =
				{
					Content = new Border
					{
						Width = 200,
						Height = 200,
						Background = null,
						BorderBrush = null,
						BorderThickness = new Thickness(10),
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					}
				}
			};

			services.AssertHitTest(new RectangleGeometry(new Rect(100, 100, 50, 50)), null);
			services.AssertHitTest(new RectangleGeometry(new Rect(195, 100, 10, 10)), null);
		}
	}

	[PresentationTestMethod]
	public void Geometry_Hit_Test_Line_Intersects_RectangleGeometry()
	{
		using (PresentationLocator.EnterScope())
		{
			SkiaPlatform.Initialize();

			Line line = null;

			using var services = new CompositorTestServices(new Size(100, 100),
				PresentationLocator.Current.GetRequiredService<IPlatformRenderInterface>())
			{
				TopLevel =
				{
					Content = line = new Line
					{
						StartPoint = new Point(0, 0),
						EndPoint = new Point(100, 100),
						Stroke = Brushes.Red,
						StrokeThickness = 5,
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					}
				}
			};

			services.AssertHitTest(new RectangleGeometry(new Rect(0, 80, 10, 10)), null);

			services.AssertHitTest(new RectangleGeometry(new Rect(45, 45, 10, 10)), null,
				new GeometryHitTestResult(line, IntersectionResult.Intersects));
		}
	}

	#endregion
}