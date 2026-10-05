#region References

using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.VisualTree;

[TestClass]
public class VisualExtensionsGetVisualsAt
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldFindControl()
	{
		Border target;
		using var services = new CompositorTestServices(new Size(200, 200))
		{
			TopLevel =
			{
				Content = new StackPanel
				{
					Background = null,
					Children =
					{
						(target = new Border
						{
							Width = 100,
							Height = 200,
							Background = Brushes.Red
						}),
						new Border
						{
							Width = 100,
							Height = 200,
							Background = Brushes.Green
						}
					},
					Orientation = Orientation.Horizontal
				}
			}
		};

		services.RunJobs();
		var result = target.GetVisualsAt(new Point(50, 50));

		CornerstoneTest.Same(target, result.Single());
	}

	[PresentationTestMethod]
	public void ShouldFindControlWithGeometry()
	{
		Border target;
		using var services = new CompositorTestServices(new Size(200, 200))
		{
			TopLevel =
			{
				Content = new StackPanel
				{
					Background = null,
					Children =
					{
						(target = new Border
						{
							Width = 100,
							Height = 200,
							Background = Brushes.Red
						}),
						new Border
						{
							Width = 100,
							Height = 200,
							Background = Brushes.Green
						}
					},
					Orientation = Orientation.Horizontal
				}
			}
		};

		services.RunJobs();
		var geo = new RectangleGeometry(target.Bounds);
		var result = target.GetVisualsAt(geo);

		CornerstoneTest.AreEqual(new GeometryHitTestResult(target, IntersectionResult.FullyInside), result.Single());
	}

	[PresentationTestMethod]
	public void ShouldNotFindSiblingControl()
	{
		Border target;
		using var services = new CompositorTestServices(new Size(200, 200))
		{
			TopLevel =
			{
				Content = new StackPanel
				{
					Background = Brushes.White,
					Children =
					{
						(target = new Border
						{
							Width = 100,
							Height = 200,
							Background = Brushes.Red
						}),
						new Border
						{
							Width = 100,
							Height = 200,
							Background = Brushes.Green
						}
					},
					Orientation = Orientation.Horizontal
				}
			}
		};
		services.RunJobs();
		var result = target.GetVisualsAt(new Point(150, 50));

		CornerstoneTest.Empty(result);
	}

	#endregion
}