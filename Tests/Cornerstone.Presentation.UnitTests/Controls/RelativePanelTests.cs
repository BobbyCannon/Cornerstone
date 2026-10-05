#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Shapes;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class RelativePanelTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void AboveMeasuresCorrectly()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
		var rect1 = new Rectangle { Height = 20, Width = 20 };
		var rect2 = new Rectangle { Height = 20, Width = 20 };

		var target = new RelativePanel
		{
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
			Children =
			{
				rect1, rect2
			}
		};

		RelativePanel.SetAbove(rect2, rect1);
		target.Measure(new Size(400, 400));
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(20, 20), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 20, 20), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, -20, 20, 20), target.Children[1].Bounds);
	}

	[PresentationTestMethod]
	public void LaysOut1ChildBelowtheother()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
		var rect1 = new Rectangle { Height = 20, Width = 20 };
		var rect2 = new Rectangle { Height = 20, Width = 20 };

		var target = new RelativePanel
		{
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Left,
			Children =
			{
				rect1, rect2
			}
		};

		RelativePanel.SetAlignLeftWithPanel(rect1, true);
		RelativePanel.SetBelow(rect2, rect1);
		target.Measure(new Size(400, 400));
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(20, 40), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 20, 20), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 20, 20, 20), target.Children[1].Bounds);
	}

	[PresentationTestMethod]
	public void LaysOut1ChildNexttheother()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
		var rect1 = new Rectangle { Height = 20, Width = 20 };
		var rect2 = new Rectangle { Height = 20, Width = 20 };

		var target = new RelativePanel
		{
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Left,
			Children =
			{
				rect1, rect2
			}
		};

		RelativePanel.SetAlignLeftWithPanel(rect1, true);
		RelativePanel.SetRightOf(rect2, rect1);
		target.Measure(new Size(400, 400));
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(40, 20), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 20, 20), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(20, 0, 20, 20), target.Children[1].Bounds);
	}

	[PresentationTestMethod]
	public void LeftOfMeasuresCorrectly()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
		var rect1 = new Rectangle { Height = 20, Width = 20 };
		var rect2 = new Rectangle { Height = 20, Width = 20 };

		var target = new RelativePanel
		{
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
			Children =
			{
				rect1, rect2
			}
		};

		RelativePanel.SetLeftOf(rect2, rect1);
		target.Measure(new Size(400, 400));
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(20, 20), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 20, 20), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(-20, 0, 20, 20), target.Children[1].Bounds);
	}

	[PresentationTestMethod]
	public void RelativePanelCanCenter()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
		var rect1 = new Rectangle { Height = 20, Width = 20 };
		var rect2 = new Rectangle { Height = 20, Width = 20 };

		var target = new RelativePanel
		{
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
			Children =
			{
				rect1, rect2
			}
		};

		RelativePanel.SetAlignLeftWithPanel(rect1, true);
		RelativePanel.SetBelow(rect2, rect1);
		target.Measure(new Size(400, 400));
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(20, 40), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 20, 20), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 20, 20, 20), target.Children[1].Bounds);
	}

	[PresentationTestMethod]
	[DataRow(100, 100, 100, 100)]
	[DataRow(100, double.PositiveInfinity, 100, 40)]
	[DataRow(double.PositiveInfinity, 100, 20, 100)]
	[DataRow(double.PositiveInfinity, double.PositiveInfinity, 20, 40)]
	public void StretchedPanelMeasuresCorrectly(double availableWidth, double availableHeight, double desiredWidth, double desiredHeight)
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
		var rect1 = new Rectangle { Height = 20, Width = 20 };
		var rect2 = new Rectangle { Height = 20, Width = 20 };

		var target = new RelativePanel
		{
			VerticalAlignment = VerticalAlignment.Stretch,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Children =
			{
				rect1, rect2
			}
		};

		RelativePanel.SetBelow(rect2, rect1);
		target.Measure(new Size(availableWidth, availableHeight));
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(desiredWidth, target.DesiredSize.Width);
		CornerstoneTest.AreEqual(desiredHeight, target.DesiredSize.Height);
	}

	#endregion
}