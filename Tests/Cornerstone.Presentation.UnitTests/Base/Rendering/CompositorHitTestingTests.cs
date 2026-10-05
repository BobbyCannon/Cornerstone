#region References

using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Shapes;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Rendering.Composition;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Rendering;

[TestClass]
public class CompositorHitTestingTests : CompositorTestsBase
{
	#region Methods

	[PresentationTestMethod]
	public void HitTestFilterShouldFilterOutChildren()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			Border child, parent;
			s.TopLevel.Content = parent = new Border
			{
				Width = 100,
				Height = 100,
				Background = Brushes.Red,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Child = child = new Border
				{
					Background = Brushes.Red,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch
				}
			};

			s.AssertHitTest(new Point(100, 100), null, child, parent);
			s.AssertHitTest(new Point(100, 100), v => v != parent);
		}
	}

	[PresentationTestMethod]
	public void HitTestFirstGeometryShouldSkipElementChildCompositionVisual()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			var target = new Border
			{
				Width = 200,
				Height = 200,
				Background = Brushes.Red
			};

			s.TopLevel.Content = target;
			s.RunJobs();

			var childVisual = s.Compositor.CreateSolidColorVisual();
			childVisual.Size = new Vector(200, 200);
			childVisual.Color = Colors.Blue;
			ElementComposition.SetElementChildVisual(target, childVisual);

			s.AssertHitTestFirst(new RectangleGeometry(new Rect(100, 100, 10, 10)), null, target);
		}
	}

	[PresentationTestMethod]
	public void HitTestFirstShouldSkipElementChildCompositionVisual()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			var target = new Border
			{
				Width = 200,
				Height = 200,
				Background = Brushes.Red
			};

			s.TopLevel.Content = target;
			s.RunJobs();

			var childVisual = s.Compositor.CreateSolidColorVisual();
			childVisual.Size = new Vector(200, 200);
			childVisual.Color = Colors.Blue;
			ElementComposition.SetElementChildVisual(target, childVisual);

			s.AssertHitTestFirst(new Point(100, 100), null, target);
		}
	}

	[PresentationTestMethod]
	public void HitTestGeometryFilterShouldFilterOutChildren()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			Border child, parent;
			s.TopLevel.Content = parent = new Border
			{
				Width = 100,
				Height = 100,
				Background = Brushes.Red,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Child = child = new Border
				{
					Background = Brushes.Red,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch
				}
			};

			s.AssertHitTest(new RectangleGeometry(new Rect(100, 100, 10, 10)), null, new GeometryHitTestResult(child, IntersectionResult.FullyContains),
				new GeometryHitTestResult(parent, IntersectionResult.FullyContains));
			s.AssertHitTest(new RectangleGeometry(new Rect(100, 100, 10, 10)), v => v != parent);
		}
	}

	[PresentationTestMethod]
	public void HitTestGeometryFindControlTranslatedOutsideParentBounds()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			Border target;
			var container = new Panel
			{
				Width = 200,
				Height = 200,
				Background = Brushes.Red,
				ClipToBounds = false,
				Children =
				{
					new Border
					{
						Width = 100,
						Height = 100,
						ZIndex = 1,
						Background = Brushes.Red,
						HorizontalAlignment = HorizontalAlignment.Left,
						VerticalAlignment = VerticalAlignment.Top,
						Child = target = new Border
						{
							Width = 50,
							Height = 50,
							Background = Brushes.Red,
							HorizontalAlignment = HorizontalAlignment.Left,
							VerticalAlignment = VerticalAlignment.Top,
							RenderTransform = new TranslateTransform(110, 110)
						}
					}
				}
			};
			s.TopLevel.Content = container;

			s.AssertHitTest(new RectangleGeometry(new Rect(120, 120, 50, 50)), null, new GeometryHitTestResult(target, IntersectionResult.Intersects),
				new GeometryHitTestResult(container, IntersectionResult.FullyContains));
		}
	}

	[PresentationTestMethod]
	public void HitTestGeometryShouldAccommodateICustomHitTest()
	{
		using (var s = new CompositorTestServices(new Size(300, 200)))
		{
			Border border = new CustomHitTestBorder
			{
				ClipToBounds = false,
				Width = 100,
				Height = 100,
				Background = Brushes.Red,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};

			s.TopLevel.Content = border;

			s.AssertHitTest(new RectangleGeometry(new Rect(75, 100, 10, 10)), null, new GeometryHitTestResult(border, IntersectionResult.FullyContains));
			s.AssertHitTest(new RectangleGeometry(new Rect(125, 45, 10, 10)), null, new GeometryHitTestResult(border, IntersectionResult.Intersects));
			s.AssertHitTest(new RectangleGeometry(new Rect(175, 100, 10, 10)), null);
		}
	}

	[PresentationTestMethod]
	public void HitTestGeometryShouldFilterResults()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			Border parent, child;
			s.TopLevel.Content = parent = new Border
			{
				Width = 100,
				Height = 100,
				Background = Brushes.Red,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Child = child = new Border
				{
					Background = Brushes.Blue,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch
				}
			};
			s.RunJobs();

			var geometry = new RectangleGeometry { Rect = new Rect(50, 50, 100, 100) };

			s.AssertHitTestFirst(geometry, null, child);
			s.AssertHitTestFirst(geometry, v => v != parent, null);
			s.AssertHitTestFirst(geometry, v => v != child, parent);
		}
	}

	[PresentationTestMethod]
	public void HitTestGeometryShouldFindControlWithManySiblings()
	{
		using (var s = new CompositorTestServices(new Size(1000, 200)))
		{
			Border target = null!;
			var canvas = new Canvas { Width = 1000, Height = 200 };

			for (var i = 0; i < 70; i++)
			{
				var child = new Border { Width = 8, Height = 8, Background = Brushes.Red, Name = $"{i}" };
				Canvas.SetLeft(child, i * 12);
				canvas.Children.Add(child);

				if (i == 0)
				{
					target = child;
				}
			}

			s.TopLevel.Content = canvas;
			s.AssertHitTestFirst(new RectangleGeometry(new Rect(4, 4, 2, 2)), null, target);
		}
	}

	[PresentationTestMethod]
	public void HitTestGeometryShouldFindControlWithinGeometryBounds()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			var border = new Border
			{
				Width = 100,
				Height = 100,
				Background = Brushes.Red,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};

			s.TopLevel.Content = border;
			s.RunJobs();

			var geometry = new RectangleGeometry { Rect = new Rect(80, 80, 40, 40) };
			s.AssertHitTestFirst(geometry, null, border);
		}
	}

	[PresentationTestMethod]
	public void HitTestGeometryShouldFindMultipleControlsWithinBounds()
	{
		using (var s = new CompositorTestServices(new Size(300, 200)))
		{
			var container = new Panel
			{
				Width = 300,
				Height = 200,
				Children =
				{
					new Border
					{
						Name = "border1",
						Width = 50,
						Height = 50,
						Background = Brushes.Red,
						HorizontalAlignment = HorizontalAlignment.Left,
						VerticalAlignment = VerticalAlignment.Top
					},
					new Border
					{
						Name = "border2",
						Width = 50,
						Height = 50,
						Background = Brushes.Blue,
						HorizontalAlignment = HorizontalAlignment.Left,
						VerticalAlignment = VerticalAlignment.Center
					}
				}
			};
			s.TopLevel.Content = container;
			s.RunJobs();

			var geometry = new RectangleGeometry { Rect = new Rect(0, 0, 100, 200) };
			s.AssertHitTestFirst(geometry, null, container.Children[1]);
		}
	}

	[PresentationTestMethod]
	public void HitTestGeometryShouldFindTopControlWithZIndex()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			var container = new Panel
			{
				Width = 200,
				Height = 200,
				Children =
				{
					new Border
					{
						Width = 100,
						Height = 100,
						Background = Brushes.Red,
						ZIndex = 1,
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					},
					new Border
					{
						Width = 100,
						Height = 100,
						Background = Brushes.Blue,
						ZIndex = 2,
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center,
						Margin = new Thickness(10)
					}
				}
			};
			s.TopLevel.Content = container;
			s.RunJobs();

			var geometry = new RectangleGeometry { Rect = new Rect(50, 50, 100, 100) };
			s.AssertHitTestFirst(geometry, null, container.Children[1]);
		}
	}

	[PresentationTestMethod]
	public void HitTestGeometryShouldNotFindControlOutsideGeometryBounds()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			var border = new Border
			{
				Width = 100,
				Height = 100,
				Background = Brushes.Red,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};

			s.TopLevel.Content = border;
			s.RunJobs();

			var geometry = new RectangleGeometry { Rect = new Rect(10, 10, 30, 30) };
			s.AssertHitTestFirst(geometry, null, null);
		}
	}

	[PresentationTestMethod]
	public void HitTestGeometryShouldNotFindControlOutsideParentBoundsWhenClipped()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			Border target;
			var container = new Panel
			{
				Width = 100,
				Height = 200,
				Background = Brushes.Red,
				Children =
				{
					new Panel
					{
						Width = 100,
						Height = 100,
						Background = Brushes.Red,
						Margin = new Thickness(0, 100, 0, 0),
						ClipToBounds = true,
						Children =
						{
							(target = new Border
							{
								Width = 100,
								Height = 100,
								Background = Brushes.Red,
								Margin = new Thickness(0, -100, 0, 0)
							})
						}
					}
				}
			};
			s.TopLevel.Content = container;

			s.AssertHitTest(new RectangleGeometry(new Rect(50, 50, 50, 50)), null, new GeometryHitTestResult(container, IntersectionResult.FullyContains));
		}
	}

	[PresentationTestMethod]
	public void HitTestGeometryShouldNotFindControlOutsideScrollViewport()
	{
		using (var s = new CompositorTestServices(new Size(100, 200)))
		{
			Border target;
			Border item1;
			Border item2;
			ScrollContentPresenter scroll;
			var container = new Panel
			{
				Width = 100,
				Height = 200,
				Background = Brushes.Red,
				Children =
				{
					(target = new Border
					{
						Name = "b1",
						Width = 100,
						Height = 100,
						Background = Brushes.Red
					}),
					new Border
					{
						Name = "b2",
						Width = 100,
						Height = 100,
						Background = Brushes.Red,
						Margin = new Thickness(0, 100, 0, 0),
						Child = scroll = new ScrollContentPresenter
						{
							CanHorizontallyScroll = true,
							CanVerticallyScroll = true,
							Content = new StackPanel
							{
								Children =
								{
									(item1 = new Border
									{
										Name = "b3",
										Width = 100,
										Height = 100,
										Background = Brushes.Red
									}),
									(item2 = new Border
									{
										Name = "b4",
										Width = 100,
										Height = 100,
										Background = Brushes.Red
									})
								}
							}
						}
					}
				}
			};
			s.TopLevel.Content = container;

			scroll.UpdateChild();

			s.AssertHitTestFirst(new RectangleGeometry(new Rect(50, 150, 50, 50)), null, item1);

			s.AssertHitTestFirst(new RectangleGeometry(new Rect(50, 50, 50, 50)), null, target);

			scroll.Offset = new Vector(0, 100);

			s.AssertHitTestFirst(new RectangleGeometry(new Rect(50, 150, 50, 50)), null, item2);

			s.AssertHitTestFirst(new RectangleGeometry(new Rect(50, 50, 50, 50)), null, target);
		}
	}

	[PresentationTestMethod]
	public void HitTestGeometryShouldNotFindPathWhenOutsideFill()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			var path = new Path
			{
				Width = 200,
				Height = 200,
				Fill = Brushes.Red,
				Data = new RectangleGeometry(new Rect(50, 50, 100, 100))
			};
			s.TopLevel.Content = path;

			s.AssertHitTest(new RectangleGeometry(new Rect(95, 95, 50, 50)), null, new GeometryHitTestResult(path, IntersectionResult.FullyContains));
			s.AssertHitTest(new RectangleGeometry(new Rect(0, 0, 10, 10)), null);
		}
	}

	[PresentationTestMethod]
	public void HitTestGeometryShouldNotHitControlsNextPixel()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			Border targetRectangle;

			var stackPanel = new StackPanel
			{
				Orientation = Orientation.Vertical,
				HorizontalAlignment = HorizontalAlignment.Left,
				Children =
				{
					new Border { Width = 10, Height = 10, Background = Brushes.Red },
					{ targetRectangle = new Border { Width = 10, Height = 10, Background = Brushes.Green, Name = "Target" } }
				}
			};

			s.TopLevel.Content = stackPanel;

			s.AssertHitTest(new RectangleGeometry(new Rect(5, 10, 10, 10)), null, new GeometryHitTestResult(targetRectangle, IntersectionResult.Intersects));
		}
	}

	[PresentationTestMethod]
	public void HitTestGeometryShouldRespectControlVisibility()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			var border = new Border
			{
				Width = 100,
				Height = 100,
				Background = Brushes.Red,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Child = new Border
				{
					Background = Brushes.Blue,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch,
					IsVisible = false
				}
			};
			s.TopLevel.Content = border;

			s.RunJobs();

			var geometry = new RectangleGeometry { Rect = new Rect(50, 50, 100, 100) };
			s.AssertHitTestFirst(geometry, null, border);
		}
	}

	[PresentationTestMethod]
	public void HitTestGeometryShouldRespectGeometryClip()
	{
		using (var s = new CompositorTestServices(new Size(400, 400)))
		{
			Canvas canvas;
			var border = new Border
			{
				Background = Brushes.Red,
				Clip = StreamGeometry.Parse("M100,0 L0,100 100,100"),
				Width = 200,
				Height = 200,
				Child = canvas = new Canvas
				{
					Background = Brushes.Yellow,
					Margin = new Thickness(10)
				}
			};
			s.TopLevel.Content = border;

			s.RunJobs();
			CornerstoneTest.AreEqual(new Rect(100, 100, 200, 200), border.Bounds);

			s.AssertHitTest(new RectangleGeometry(new Rect(195, 195, 10, 10)), null, new GeometryHitTestResult(canvas, IntersectionResult.FullyContains),
				new GeometryHitTestResult(border, IntersectionResult.FullyContains));

			s.AssertHitTest(new RectangleGeometry(new Rect(110, 110, 10, 10)), null);
		}
	}

	[PresentationTestMethod]
	[DataRow(50, 50, 100, 100, 50, 50, IntersectionResult.FullyInside)]
	[DataRow(80, 80, 100, 100, 50, 50, IntersectionResult.Intersects)]
	[DataRow(95, 95, 40, 40, 80, 80, IntersectionResult.FullyContains)]
	[DataRow(0, 0, 10, 10, 50, 50, IntersectionResult.Empty)]
	public void HitTestGeometryShouldReturnCorrectIntersectionResult(
		double geomX, double geomY, double geomWidth, double geomHeight,
		double elemWidth, double elemHeight, IntersectionResult expectedResult)
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			var border = new Border
			{
				Width = elemWidth,
				Height = elemHeight,
				Background = Brushes.Red,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};

			s.TopLevel.Content = border;
			s.RunJobs();

			var geometry = new RectangleGeometry(new Rect(geomX, geomY, geomWidth, geomHeight));

			if (expectedResult == IntersectionResult.Empty)
			{
				// Geometry doesn't intersect element
				s.AssertHitTest(geometry, null);
			}
			else
			{
				// Geometry intersects element
				s.AssertHitTest(geometry, null,
					new GeometryHitTestResult(border, expectedResult));
			}
		}
	}

	[PresentationTestMethod]
	public void HitTestGeometryShouldReturnTopControlsFirst()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			var container = new Panel
			{
				Width = 200,
				Height = 200,
				Children =
				{
					new Border
					{
						Width = 100,
						Height = 100,
						Background = Brushes.Red,
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					},
					new Border
					{
						Width = 50,
						Height = 50,
						Background = Brushes.Blue,
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					}
				}
			};
			s.TopLevel.Content = container;

			s.AssertHitTest(new RectangleGeometry(new Rect(100, 100, 50, 50)), null,
				new GeometryHitTestResult(container.Children[1], IntersectionResult.Intersects),
				new GeometryHitTestResult(container.Children[0], IntersectionResult.FullyContains));
		}
	}

	[PresentationTestMethod]
	public void HitTestGeometryShouldReturnTopControlsFirstWithManyOverlappingSiblings()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			Border top = null!;
			var canvas = new Canvas { Width = 200, Height = 200 };

			for (var i = 0; i < 70; i++)
			{
				var child = new Border { Width = 100, Height = 100, Background = Brushes.Red };
				Canvas.SetLeft(child, 50);
				Canvas.SetTop(child, 50);
				canvas.Children.Add(child);

				if (i == 69)
				{
					top = child;
				}
			}

			s.TopLevel.Content = canvas;
			s.AssertHitTestFirst(new RectangleGeometry(new Rect(100, 100, 10, 10)), null, top);
			s.AssertHitTest(new RectangleGeometry(new Rect(100, 100, 10, 10)), null, canvas.Children.Cast<Visual>().Reverse().Select(x => new GeometryHitTestResult(x, IntersectionResult.FullyContains)).ToArray());
		}
	}

	[PresentationTestMethod]
	public void HitTestGeometryShouldReturnTopControlsFirstWithZIndex()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			var container = new Panel
			{
				Width = 200,
				Height = 200,
				Children =
				{
					new Border
					{
						Width = 100,
						Height = 100,
						ZIndex = 1,
						Background = Brushes.Red,
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					},
					new Border
					{
						Width = 50,
						Height = 50,
						Background = Brushes.Red,
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					},
					new Border
					{
						Width = 75,
						Height = 75,
						ZIndex = 2,
						Background = Brushes.Red,
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					}
				}
			};
			s.TopLevel.Content = container;

			s.AssertHitTest(new RectangleGeometry(new Rect(100, 100, 50, 50)), null, new GeometryHitTestResult(container.Children[2], IntersectionResult.Intersects),
				new GeometryHitTestResult(container.Children[0], IntersectionResult.FullyContains),
				new GeometryHitTestResult(container.Children[1], IntersectionResult.Intersects));
		}
	}

	[PresentationTestMethod]
	public void HitTestGeometryShouldUpdateManySiblingIndexWhenChildIsAddedAndRemoved()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			Border top = null!;
			var canvas = new Canvas { Width = 200, Height = 200 };

			for (var i = 0; i < 70; i++)
			{
				var child = new Border { Width = 100, Height = 100, Background = Brushes.Red };
				Canvas.SetLeft(child, 50);
				Canvas.SetTop(child, 50);
				canvas.Children.Add(child);

				if (i == 69)
				{
					top = child;
				}
			}

			s.TopLevel.Content = canvas;
			s.AssertHitTestFirst(new RectangleGeometry(new Rect(100, 100, 10, 10)), null, top);

			var added = new Border { Width = 100, Height = 100, Background = Brushes.Blue };
			Canvas.SetLeft(added, 50);
			Canvas.SetTop(added, 50);
			canvas.Children.Add(added);

			s.AssertHitTestFirst(new RectangleGeometry(new Rect(100, 100, 10, 10)), null, added);

			canvas.Children.Remove(added);
			s.AssertHitTestFirst(new RectangleGeometry(new Rect(100, 100, 10, 10)), null, top);
		}
	}

	[PresentationTestMethod]
	public void HitTestGeometryShouldUpdateManySiblingIndexWhenChildMoves()
	{
		using (var s = new CompositorTestServices(new Size(1000, 200)))
		{
			Border moving = null!;
			var canvas = new Canvas { Width = 1000, Height = 200 };

			for (var i = 0; i < 70; i++)
			{
				var child = new Border { Width = 8, Height = 8, Background = Brushes.Red };
				Canvas.SetLeft(child, i * 12);
				canvas.Children.Add(child);

				if (i == 69)
				{
					moving = child;
				}
			}

			s.TopLevel.Content = canvas;
			s.AssertHitTestFirst(new RectangleGeometry(new Rect((69 * 12) + 4, 4, 2, 2)), null, moving);

			Canvas.SetLeft(moving, 10);
			Canvas.SetTop(moving, 100);
			s.AssertHitTestFirst(new RectangleGeometry(new Rect(14, 104, 2, 2)), null, moving);
		}
	}

	[PresentationTestMethod]
	public void HitTestShouldAccommodateICustomHitTest()
	{
		using (var s = new CompositorTestServices(new Size(300, 200)))
		{
			Border border = new CustomHitTestBorder
			{
				ClipToBounds = false,
				Width = 100,
				Height = 100,
				Background = Brushes.Red,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};

			s.TopLevel.Content = border;

			s.AssertHitTest(75, 100, null, border);
			s.AssertHitTest(125, 100, null, border);
			s.AssertHitTest(175, 100, null);
		}
	}

	[PresentationTestMethod]
	public void HitTestShouldFindControlTranslatedOutsideParentBounds()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			Border target;
			var container = new Panel
			{
				Width = 200,
				Height = 200,
				Background = Brushes.Red,
				ClipToBounds = false,
				Children =
				{
					new Border
					{
						Width = 100,
						Height = 100,
						ZIndex = 1,
						Background = Brushes.Red,
						HorizontalAlignment = HorizontalAlignment.Left,
						VerticalAlignment = VerticalAlignment.Top,
						Child = target = new Border
						{
							Width = 50,
							Height = 50,
							Background = Brushes.Red,
							HorizontalAlignment = HorizontalAlignment.Left,
							VerticalAlignment = VerticalAlignment.Top,
							RenderTransform = new TranslateTransform(110, 110)
						}
					}
				}
			};
			s.TopLevel.Content = container;

			s.AssertHitTest(new Point(120, 120), null, target, container);
		}
	}

	[PresentationTestMethod]
	public void HitTestShouldFindControlWithManySiblings()
	{
		using (var s = new CompositorTestServices(new Size(1000, 200)))
		{
			Border target = null!;
			var canvas = new Canvas { Width = 1000, Height = 200 };

			for (var i = 0; i < 70; i++)
			{
				var child = new Border { Width = 8, Height = 8, Background = Brushes.Red };
				Canvas.SetLeft(child, i * 12);
				canvas.Children.Add(child);

				if (i == 0)
				{
					target = child;
				}
			}

			s.TopLevel.Content = canvas;
			s.AssertHitTestFirst(new Point(4, 4), null, target);
		}
	}

	[PresentationTestMethod]
	public void HitTestShouldFindControlsAtGeometry()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			var border = new Border
			{
				Width = 100,
				Height = 100,
				Background = Brushes.Red,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};

			s.TopLevel.Content = border;

			s.AssertHitTest(new RectangleGeometry(new Rect(100, 100, 50, 50)), null, new GeometryHitTestResult(border, IntersectionResult.FullyContains));
		}
	}

	[PresentationTestMethod]
	public void HitTestShouldFindControlsAtPoint()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			var border = new Border
			{
				Width = 100,
				Height = 100,
				Background = Brushes.Red,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};

			s.TopLevel.Content = border;

			s.AssertHitTest(new Point(100, 100), null, border);
		}
	}

	[PresentationTestMethod]
	[DataRow(false, false)]
	[DataRow(true, false)]
	[DataRow(false, true)]
	[DataRow(true, true)]
	public void HitTestShouldFindZeroOpacityControlsAtGeometry(bool parent, bool child)
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			Border visible, border;
			s.TopLevel.Content = border = new Border
			{
				Width = 100,
				Height = 100,
				Background = Brushes.Red,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Opacity = parent ? 0 : 1,
				Child = visible = new Border
				{
					Opacity = child ? 0 : 1,
					Background = Brushes.Red,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch
				}
			};

			s.AssertHitTest(new RectangleGeometry(new Rect(100, 100, 50, 50)), null,
				new GeometryHitTestResult(visible, IntersectionResult.FullyContains),
				new GeometryHitTestResult(border, IntersectionResult.FullyContains));
		}
	}

	[PresentationTestMethod]
	[DataRow(false, false)]
	[DataRow(true, false)]
	[DataRow(false, true)]
	[DataRow(true, true)]
	public void HitTestShouldFindZeroOpacityControlsAtPoint(bool parent, bool child)
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			Border visible, border;
			s.TopLevel.Content = border = new Border
			{
				Width = 100,
				Height = 100,
				Background = Brushes.Red,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Opacity = parent ? 0 : 1,
				Child = visible = new Border
				{
					Opacity = child ? 0 : 1,
					Background = Brushes.Red,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch
				}
			};

			s.AssertHitTest(new Point(100, 100), null, visible, border);
		}
	}

	[PresentationTestMethod]
	public void HitTestShouldNotFindControlOutsideGeometry()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			var border = new Border
			{
				Width = 100,
				Height = 100,
				Background = Brushes.Red,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			s.TopLevel.Content = border;

			s.AssertHitTest(new RectangleGeometry(new Rect(10, 10, 10, 10)), null);
		}
	}

	[PresentationTestMethod]
	public void HitTestShouldNotFindControlOutsideParentBoundsWhenClipped()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			Border target;
			var container = new Panel
			{
				Width = 100,
				Height = 200,
				Background = Brushes.Red,
				Children =
				{
					new Panel
					{
						Width = 100,
						Height = 100,
						Background = Brushes.Red,
						Margin = new Thickness(0, 100, 0, 0),
						ClipToBounds = true,
						Children =
						{
							(target = new Border
							{
								Width = 100,
								Height = 100,
								Background = Brushes.Red,
								Margin = new Thickness(0, -100, 0, 0)
							})
						}
					}
				}
			};
			s.TopLevel.Content = container;

			s.AssertHitTest(new Point(50, 50), null, container);
		}
	}

	[PresentationTestMethod]
	public void HitTestShouldNotFindControlOutsidePoint()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			var border = new Border
			{
				Width = 100,
				Height = 100,
				Background = Brushes.Red,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			s.TopLevel.Content = border;

			s.AssertHitTest(new Point(10, 10), null);
		}
	}

	[PresentationTestMethod]
	public void HitTestShouldNotFindControlOutsideScrollViewport()
	{
		using (var s = new CompositorTestServices(new Size(100, 200)))
		{
			Border target;
			Border item1;
			Border item2;
			ScrollContentPresenter scroll;
			var container = new Panel
			{
				Width = 100,
				Height = 200,
				Background = Brushes.Red,
				Children =
				{
					(target = new Border
					{
						Name = "b1",
						Width = 100,
						Height = 100,
						Background = Brushes.Red
					}),
					new Border
					{
						Name = "b2",
						Width = 100,
						Height = 100,
						Background = Brushes.Red,
						Margin = new Thickness(0, 100, 0, 0),
						Child = scroll = new ScrollContentPresenter
						{
							CanHorizontallyScroll = true,
							CanVerticallyScroll = true,
							Content = new StackPanel
							{
								Children =
								{
									(item1 = new Border
									{
										Name = "b3",
										Width = 100,
										Height = 100,
										Background = Brushes.Red
									}),
									(item2 = new Border
									{
										Name = "b4",
										Width = 100,
										Height = 100,
										Background = Brushes.Red
									})
								}
							}
						}
					}
				}
			};
			s.TopLevel.Content = container;

			scroll.UpdateChild();

			s.AssertHitTestFirst(new Point(50, 150), null, item1);

			s.AssertHitTestFirst(new Point(50, 50), null, target);

			scroll.Offset = new Vector(0, 100);

			s.AssertHitTestFirst(new Point(50, 150), null, item2);

			s.AssertHitTestFirst(new Point(50, 50), null, target);
		}
	}

	[PresentationTestMethod]
	public void HitTestShouldNotFindEmptyControlsAtGeometry()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			var border = new Border
			{
				Width = 100,
				Height = 100,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};

			s.TopLevel.Content = border;

			s.AssertHitTest(new RectangleGeometry(new Rect(100, 100, 50, 50)), null);
		}
	}

	[PresentationTestMethod]
	public void HitTestShouldNotFindEmptyControlsAtPoint()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			var border = new Border
			{
				Width = 100,
				Height = 100,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};

			s.TopLevel.Content = border;

			s.AssertHitTest(new Point(100, 100), null);
		}
	}

	[PresentationTestMethod]
	public void HitTestShouldNotFindInvisibleControlsAtGeometry()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			Border visible, border;
			s.TopLevel.Content = border = new Border
			{
				Width = 100,
				Height = 100,
				Background = Brushes.Red,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				IsVisible = false,
				Child = visible = new Border
				{
					Background = Brushes.Red,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch
				}
			};

			s.AssertHitTest(new RectangleGeometry(new Rect(100, 100, 50, 50)), null);
		}
	}

	[PresentationTestMethod]
	public void HitTestShouldNotFindInvisibleControlsAtPoint()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			Border visible, border;
			s.TopLevel.Content = border = new Border
			{
				Width = 100,
				Height = 100,
				Background = Brushes.Red,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				IsVisible = false,
				Child = visible = new Border
				{
					Background = Brushes.Red,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					VerticalAlignment = VerticalAlignment.Stretch
				}
			};

			s.AssertHitTest(new Point(100, 100), null);
		}
	}

	[PresentationTestMethod]
	public void HitTestShouldNotFindPathWhenOutsideFill()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			var path = new Path
			{
				Width = 200,
				Height = 200,
				Fill = Brushes.Red,
				Data = StreamGeometry.Parse("M100,0 L0,100 100,100")
			};
			s.TopLevel.Content = path;

			s.AssertHitTest(new Point(100, 100), null, path);
			s.AssertHitTest(new Point(10, 10), null);
		}
	}

	[PresentationTestMethod]
	public void HitTestShouldNotHitControlsNextPixel()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			Border targetRectangle;

			var stackPanel = new StackPanel
			{
				Orientation = Orientation.Vertical,
				HorizontalAlignment = HorizontalAlignment.Left,
				Children =
				{
					new Border { Width = 10, Height = 10, Background = Brushes.Red },
					{ targetRectangle = new Border { Width = 10, Height = 10, Background = Brushes.Green } }
				}
			};

			s.TopLevel.Content = stackPanel;

			s.AssertHitTest(new Point(5, 10), null, targetRectangle);
		}
	}

	[PresentationTestMethod]
	public void HitTestShouldRespectGeometryClip()
	{
		using (var s = new CompositorTestServices(new Size(400, 400)))
		{
			Canvas canvas;
			var border = new Border
			{
				Background = Brushes.Red,
				Clip = StreamGeometry.Parse("M100,0 L0,100 100,100"),
				Width = 200,
				Height = 200,
				Child = canvas = new Canvas
				{
					Background = Brushes.Yellow,
					Margin = new Thickness(10)
				}
			};
			s.TopLevel.Content = border;

			s.RunJobs();
			CornerstoneTest.AreEqual(new Rect(100, 100, 200, 200), border.Bounds);

			s.AssertHitTest(new Point(200, 200), null, canvas, border);

			s.AssertHitTest(new Point(110, 110), null);
		}
	}

	[PresentationTestMethod]
	public void HitTestShouldReturnTopControlsFirst()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			var container = new Panel
			{
				Width = 200,
				Height = 200,
				Children =
				{
					new Border
					{
						Width = 100,
						Height = 100,
						Background = Brushes.Red,
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					},
					new Border
					{
						Width = 50,
						Height = 50,
						Background = Brushes.Blue,
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					}
				}
			};
			s.TopLevel.Content = container;

			s.AssertHitTest(new Point(100, 100), null, container.Children[1], container.Children[0]);
		}
	}

	[PresentationTestMethod]
	public void HitTestShouldReturnTopControlsFirstWithManyOverlappingSiblings()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			Border top = null!;
			var canvas = new Canvas { Width = 200, Height = 200 };

			for (var i = 0; i < 70; i++)
			{
				var child = new Border { Width = 100, Height = 100, Background = Brushes.Red };
				Canvas.SetLeft(child, 50);
				Canvas.SetTop(child, 50);
				canvas.Children.Add(child);

				if (i == 69)
				{
					top = child;
				}
			}

			s.TopLevel.Content = canvas;
			s.AssertHitTestFirst(new Point(100, 100), null, top);
			s.AssertHitTest(new Point(100, 100), null, canvas.Children.Cast<Visual>().Reverse().ToArray());
		}
	}

	[PresentationTestMethod]
	public void HitTestShouldReturnTopControlsFirstWithZIndex()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			var container = new Panel
			{
				Width = 200,
				Height = 200,
				Children =
				{
					new Border
					{
						Width = 100,
						Height = 100,
						ZIndex = 1,
						Background = Brushes.Red,
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					},
					new Border
					{
						Width = 50,
						Height = 50,
						Background = Brushes.Red,
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					},
					new Border
					{
						Width = 75,
						Height = 75,
						ZIndex = 2,
						Background = Brushes.Red,
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center
					}
				}
			};
			s.TopLevel.Content = container;

			s.AssertHitTest(new Point(100, 100), null, container.Children[2], container.Children[0], container.Children[1]);
		}
	}

	[PresentationTestMethod]
	public void HitTestShouldUpdateManySiblingIndexWhenChildIsAddedAndRemoved()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			Border top = null!;
			var canvas = new Canvas { Width = 200, Height = 200 };

			for (var i = 0; i < 70; i++)
			{
				var child = new Border { Width = 100, Height = 100, Background = Brushes.Red };
				Canvas.SetLeft(child, 50);
				Canvas.SetTop(child, 50);
				canvas.Children.Add(child);

				if (i == 69)
				{
					top = child;
				}
			}

			s.TopLevel.Content = canvas;
			s.AssertHitTestFirst(new Point(100, 100), null, top);

			var added = new Border { Width = 100, Height = 100, Background = Brushes.Blue };
			Canvas.SetLeft(added, 50);
			Canvas.SetTop(added, 50);
			canvas.Children.Add(added);

			s.AssertHitTestFirst(new Point(100, 100), null, added);

			canvas.Children.Remove(added);
			s.AssertHitTestFirst(new Point(100, 100), null, top);
		}
	}

	[PresentationTestMethod]
	public void HitTestShouldUpdateManySiblingIndexWhenChildMoves()
	{
		using (var s = new CompositorTestServices(new Size(1000, 200)))
		{
			Border moving = null!;
			var canvas = new Canvas { Width = 1000, Height = 200 };

			for (var i = 0; i < 70; i++)
			{
				var child = new Border { Width = 8, Height = 8, Background = Brushes.Red };
				Canvas.SetLeft(child, i * 12);
				canvas.Children.Add(child);

				if (i == 69)
				{
					moving = child;
				}
			}

			s.TopLevel.Content = canvas;
			s.AssertHitTestFirst(new Point((69 * 12) + 4, 4), null, moving);

			Canvas.SetLeft(moving, 10);
			Canvas.SetTop(moving, 100);
			s.AssertHitTestFirst(new Point(14, 104), null, moving);
		}
	}

	[PresentationTestMethod]
	public void RemovingICustomHitTestChildShouldRestoreSubTreeBoundsOptimization()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			var custom = new CustomHitTestBorder
			{
				Width = 100,
				Height = 100,
				Background = Brushes.Red
			};
			var container = new Panel
			{
				Width = 200,
				Height = 200,
				Children = { custom }
			};

			s.TopLevel.Content = container;
			s.RunJobs();

			var containerVisual = ElementComposition.GetElementVisual(container);
			CornerstoneTest.IsNotNull(containerVisual);

			// While the subtree contains an ICustomHitTest visual, the bounds-based optimization must be disabled.
			CornerstoneTest.IsTrue(containerVisual!.DisableSubTreeBoundsHitTestOptimization);

			container.Children.Remove(custom);
			s.RunJobs();

			// Once the custom hit test visual leaves the subtree, the accounting must return to zero and re-enable
			// the optimization. A sign bug when attaching to a new parent leaves the count stuck at a non-zero value.
			CornerstoneTest.IsFalse(containerVisual.DisableSubTreeBoundsHitTestOptimization);
		}
	}

	[PresentationTestMethod]
	public void HitTest_Geometry_Should_Handle_Line_Target()
	{
		using (var s = new CompositorTestServices(new Size(200, 200)))
		{
			var line = new Line
			{
				Stroke = Brushes.Red,
				StrokeThickness = 4,
				StartPoint = new Point(5, 5),
				EndPoint = new Point(190, 5)
			};

			s.TopLevel.Content = line;

			s.AssertHitTest(new RectangleGeometry(new Rect(0, 0, 50, 50)), null,
				new GeometryHitTestResult(line, IntersectionResult.Intersects));
		}
	}

	#endregion
}