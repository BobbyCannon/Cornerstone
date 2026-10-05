#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class DockPanelTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ChangingChildDockInvalidatesMeasure()
	{
		Border child;
		var target = new DockPanel
		{
			Children =
			{
				(child = new Border
				{
					[DockPanel.DockProperty] = Dock.Left
				})
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));
		CornerstoneTest.IsTrue(target.IsMeasureValid);

		DockPanel.SetDock(child, Dock.Right);

		CornerstoneTest.IsFalse(target.IsMeasureValid);
	}

	[PresentationTestMethod]
	public void DockPanelWithoutChild()
	{
		var target = new DockPanel
		{
			Width = 10,
			Height = 10
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Rect(0, 0, 10, 10), target.Bounds);
	}

	[PresentationTestMethod]
	public void ShouldDockControlsHorizontalFirst()
	{
		var target = new DockPanel
		{
			Children =
			{
				new Border { Width = 500, Height = 50, [DockPanel.DockProperty] = Dock.Top },
				new Border { Width = 500, Height = 50, [DockPanel.DockProperty] = Dock.Bottom },
				new Border { Width = 50, Height = 400, [DockPanel.DockProperty] = Dock.Left },
				new Border { Width = 50, Height = 400, [DockPanel.DockProperty] = Dock.Right },
				new Border()
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Rect(0, 0, 500, 500), target.Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 0, 500, 50), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 450, 500, 50), target.Children[1].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 50, 50, 400), target.Children[2].Bounds);
		CornerstoneTest.AreEqual(new Rect(450, 50, 50, 400), target.Children[3].Bounds);
		CornerstoneTest.AreEqual(new Rect(50, 50, 400, 400), target.Children[4].Bounds);
	}

	[PresentationTestMethod]
	public void ShouldDockControlsVerticalFirst()
	{
		var target = new DockPanel
		{
			Children =
			{
				new Border { Width = 50, Height = 400, [DockPanel.DockProperty] = Dock.Left },
				new Border { Width = 50, Height = 400, [DockPanel.DockProperty] = Dock.Right },
				new Border { Width = 500, Height = 50, [DockPanel.DockProperty] = Dock.Top },
				new Border { Width = 500, Height = 50, [DockPanel.DockProperty] = Dock.Bottom },
				new Border()
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Rect(0, 0, 600, 400), target.Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 0, 50, 400), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(550, 0, 50, 400), target.Children[1].Bounds);
		CornerstoneTest.AreEqual(new Rect(50, 0, 500, 50), target.Children[2].Bounds);
		CornerstoneTest.AreEqual(new Rect(50, 350, 500, 50), target.Children[3].Bounds);
		CornerstoneTest.AreEqual(new Rect(50, 50, 500, 300), target.Children[4].Bounds);
	}

	[PresentationTestMethod]
	public void ShouldDockControlsWithSpacing()
	{
		var target = new DockPanel
		{
			HorizontalSpacing = 10,
			VerticalSpacing = 10,
			Children =
			{
				new Border { Width = 500, Height = 50, [DockPanel.DockProperty] = Dock.Top },
				new Border { Width = 500, Height = 50, [DockPanel.DockProperty] = Dock.Bottom },
				new Border { Width = 50, Height = 400, [DockPanel.DockProperty] = Dock.Left },
				new Border { Width = 50, Height = 400, [DockPanel.DockProperty] = Dock.Right },
				new Border()
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Rect(0, 0, 500, 520), target.Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 0, 500, 50), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 470, 500, 50), target.Children[1].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 60, 50, 400), target.Children[2].Bounds);
		CornerstoneTest.AreEqual(new Rect(450, 60, 50, 400), target.Children[3].Bounds);
		CornerstoneTest.AreEqual(new Rect(60, 60, 380, 400), target.Children[4].Bounds);
	}

	#endregion
}