#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Rendering;

/// <summary>
/// Test class that verifies how clipping influences rendering in the compositor
/// </summary>
[TestClass]
public class CompositorInvalidationClippingTests : CompositorTestsBase
{
	#region Constants

	private const int TopLevelOverhead = 2; // TopLevel + TopLevelHost

	#endregion

	#region Methods

	[PresentationTestMethod]
	[DataRow(false, false, false, 1, 0)]
	[DataRow(true, false, false, 1, 0)]
	[DataRow(false, true, false, 1, 0)]
	[DataRow(false, false, true, 4 + TopLevelOverhead, 3 + TopLevelOverhead)]
	[DataRow(true, false, true, 4 + TopLevelOverhead, 3 + TopLevelOverhead)]
	[DataRow(false, true, true, 4 + TopLevelOverhead, 3 + TopLevelOverhead)]
	public void DoNotReRenderUnaffectedVisualTrees(bool clipToBounds, bool clipGeometry,
		bool canvasHasContent,
		int expectedVisitedVisualsCount, int expectedRenderedVisualsCount)
	{
		using (var s = new CompositorCanvas())
		{
			// #1 visual is top level
			// #2 is ContentPresenter
			// #3 visual is s.Canvas

			//# 4 visual is border1
			s.Canvas.Children.Add(new Border
			{
				[Canvas.LeftProperty] = 0, [Canvas.TopProperty] = 0,
				Width = 20, Height = 10,
				Background = Brushes.Red,
				ClipToBounds = clipToBounds,
				Clip = clipGeometry ? new RectangleGeometry(new Rect(new Size(20, 10))) : null
			});

			//# 5 visual is border2
			s.Canvas.Children.Add(new Border
			{
				[Canvas.LeftProperty] = 30, [Canvas.TopProperty] = 50,
				Width = 20, Height = 10,
				Background = Brushes.Red,
				ClipToBounds = clipToBounds,
				Clip = clipGeometry ? new RectangleGeometry(new Rect(new Size(20, 10))) : null
			});
			if (canvasHasContent)
			{
				s.Canvas.Background = Brushes.Green;
			}
			s.RunJobs();
			s.Events.Reset();
			if (CountVisuals(s.TopLevel) != 5)
			{
				CornerstoneTest.Fail("Layout part of the test is broken, expected 5 visuals in the tree");
			}

			//invalidate border1
			s.Canvas.Children[0].IsVisible = false;
			s.RunJobs();

			s.AssertRenderedVisuals(expectedVisitedVisualsCount, expectedRenderedVisualsCount);
		}
	}

	private int CountVisuals(Visual visual)
	{
		var count = 1; // Count the current visual
		foreach (var child in visual.VisualChildren)
		{
			count += CountVisuals(child);
		}
		return count;
	}

	#endregion
}