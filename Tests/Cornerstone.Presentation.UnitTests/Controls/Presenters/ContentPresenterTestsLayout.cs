#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Presenters;

[TestClass]
public class ContentPresenterTestsLayout : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ChildArrangeWithZeroHeightWhenPaddingHeightGreaterThanChildHeight()
	{
		Border content;
		var target = new ContentPresenter
		{
			Padding = new Thickness(32),
			MaxHeight = 32,
			MaxWidth = 32,
			HorizontalContentAlignment = HorizontalAlignment.Center,
			VerticalContentAlignment = VerticalAlignment.Center,
			Content = content = new Border
			{
				Height = 0,
				Width = 0
			}
		};

		target.UpdateChild();

		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Rect(32, 32, 0, 0), content.Bounds);
	}

	[PresentationTestMethod]
	[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 10, 10, 80, 80)]
	[DataRow(HorizontalAlignment.Left, VerticalAlignment.Stretch, 10, 10, 16, 80)]
	[DataRow(HorizontalAlignment.Right, VerticalAlignment.Stretch, 74, 10, 16, 80)]
	[DataRow(HorizontalAlignment.Center, VerticalAlignment.Stretch, 42, 10, 16, 80)]
	[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Top, 10, 10, 80, 16)]
	[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, 10, 74, 80, 16)]
	[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Center, 10, 42, 80, 16)]
	public void ContentAlignmentAndPaddingAreAppliedToChildBounds(
		HorizontalAlignment h,
		VerticalAlignment v,
		double expectedX,
		double expectedY,
		double expectedWidth,
		double expectedHeight)
	{
		Border content;
		var target = new ContentPresenter
		{
			HorizontalContentAlignment = h,
			VerticalContentAlignment = v,
			Padding = new Thickness(10),
			Content = content = new Border
			{
				MinWidth = 16,
				MinHeight = 16
			}
		};

		target.UpdateChild();
		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Rect(expectedX, expectedY, expectedWidth, expectedHeight), content.Bounds);
	}

	[PresentationTestMethod]
	[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 0, 0, 100, 100)]
	[DataRow(HorizontalAlignment.Left, VerticalAlignment.Stretch, 0, 0, 16, 100)]
	[DataRow(HorizontalAlignment.Right, VerticalAlignment.Stretch, 84, 0, 16, 100)]
	[DataRow(HorizontalAlignment.Center, VerticalAlignment.Stretch, 42, 0, 16, 100)]
	[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Top, 0, 0, 100, 16)]
	[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, 0, 84, 100, 16)]
	[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Center, 0, 42, 100, 16)]
	public void ContentAlignmentIsAppliedToChildBounds(
		HorizontalAlignment h,
		VerticalAlignment v,
		double expectedX,
		double expectedY,
		double expectedWidth,
		double expectedHeight)
	{
		Border content;
		var target = new ContentPresenter
		{
			HorizontalContentAlignment = h,
			VerticalContentAlignment = v,
			Content = content = new Border
			{
				MinWidth = 16,
				MinHeight = 16
			}
		};

		target.UpdateChild();
		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Rect(expectedX, expectedY, expectedWidth, expectedHeight), content.Bounds);
	}

	[PresentationTestMethod]
	public void ContentCanBeBottomAligned()
	{
		Border content;
		var target = new ContentPresenter
		{
			Content = content = new Border
			{
				MinWidth = 16,
				MinHeight = 16,
				VerticalAlignment = VerticalAlignment.Bottom
			}
		};

		target.UpdateChild();
		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Rect(0, 84, 100, 16), content.Bounds);
	}

	[PresentationTestMethod]
	public void ContentCanBeRightAligned()
	{
		Border content;
		var target = new ContentPresenter
		{
			Content = content = new Border
			{
				MinWidth = 16,
				MinHeight = 16,
				HorizontalAlignment = HorizontalAlignment.Right
			}
		};

		target.UpdateChild();
		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Rect(84, 0, 16, 100), content.Bounds);
	}

	[PresentationTestMethod]
	public void ContentCanBeStretched()
	{
		Border content;
		var target = new ContentPresenter
		{
			Content = content = new Border
			{
				MinWidth = 16,
				MinHeight = 16
			}
		};

		target.UpdateChild();
		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), content.Bounds);
	}

	[PresentationTestMethod]
	public void ContentCanBeTopLeftAligned()
	{
		Border content;
		var target = new ContentPresenter
		{
			Content = content = new Border
			{
				MinWidth = 16,
				MinHeight = 16,
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Top
			}
		};

		target.UpdateChild();
		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Rect(84, 0, 16, 16), content.Bounds);
	}

	[PresentationTestMethod]
	public void ContentCanBeTopRightAligned()
	{
		Border content;
		var target = new ContentPresenter
		{
			Content = content = new Border
			{
				MinWidth = 16,
				MinHeight = 16,
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Top
			}
		};

		target.UpdateChild();
		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Rect(84, 0, 16, 16), content.Bounds);
	}

	[PresentationTestMethod]
	public void ShouldCorrectlyAlignChildWithFixedSize()
	{
		Border content;
		var target = new ContentPresenter
		{
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			VerticalContentAlignment = VerticalAlignment.Stretch,
			Content = content = new Border
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Bottom,
				Width = 16,
				Height = 16
			}
		};

		target.UpdateChild();
		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		// Check correct result for Issue #1447.
		CornerstoneTest.AreEqual(new Rect(0, 84, 16, 16), content.Bounds);
	}

	#endregion

	#region Classes

	[TestClass]
	public class UseLayoutRounding : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void MeasureRoundsBorderThickness()
		{
			var target = new ContentPresenter
			{
				BorderThickness = new Thickness(1),
				Content = new Canvas
				{
					Width = 101,
					Height = 101
				}
			};

			var root = CreatedRoot(1.5, target);

			root.LayoutManager.ExecuteInitialLayoutPass();

			// - 1 pixel border thickness is rounded up to 1.3333; for both sides it is 2.6666
			// - Size of 101 gets rounded up to 101.3333
			// - Desired size = 101.3333 + 2.6666 = 104
			CornerstoneTest.AreEqual(new Size(104, 104), target.DesiredSize);
		}

		[PresentationTestMethod]
		public void MeasureRoundsPadding()
		{
			var target = new ContentPresenter
			{
				Padding = new Thickness(1),
				Content = new Canvas
				{
					Width = 101,
					Height = 101
				}
			};

			var root = CreatedRoot(1.5, target);

			root.LayoutManager.ExecuteInitialLayoutPass();

			// - 1 pixel padding is rounded up to 1.3333; for both sides it is 2.6666
			// - Size of 101 gets rounded up to 101.3333
			// - Desired size = 101.3333 + 2.6666 = 104
			CornerstoneTest.AreEqual(new Size(104, 104), target.DesiredSize);
		}

		private static TestRoot CreatedRoot(
			double scaling,
			Control child,
			Size? constraint = null)
		{
			return new TestRoot
			{
				LayoutScaling = scaling,
				UseLayoutRounding = true,
				Child = child,
				ClientSize = constraint ?? new Size(1000, 1000)
			};
		}

		#endregion
	}

	#endregion
}