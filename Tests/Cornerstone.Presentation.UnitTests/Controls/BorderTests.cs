#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class BorderTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ChildShouldArrangeWithZeroHeightWidthIfPaddingGreaterThanChildSize()
	{
		Border content;

		var target = new Border
		{
			Padding = new Thickness(6),
			MaxHeight = 12,
			MaxWidth = 12,
			Child = content = new Border
			{
				Height = 0,
				Width = 0
			}
		};

		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Rect(6, 6, 0, 0), content.Bounds);
	}

	[PresentationTestMethod]
	public void MeasureShouldReturnBorderThicknessPlusPaddingWhenNoChildPresent()
	{
		var target = new Border
		{
			Padding = new Thickness(6),
			BorderThickness = new Thickness(4)
		};

		target.Measure(new Size(100, 100));

		CornerstoneTest.AreEqual(new Size(20, 20), target.DesiredSize);
	}

	[PresentationTestMethod]
	public void ShouldRejectNaNOrInfiniteThicknesses()
	{
		var target = new Border();

		SetValues(target, Layoutable.MarginProperty);
		SetValues(target, Decorator.PaddingProperty);
		SetValues(target, Border.BorderThicknessProperty);

		static void SetValues(Border target, PresentationProperty<Thickness> property)
		{
			Assert.Throws<ArgumentException>(() => target.SetValue(property, new Thickness(0, 0, 0, double.NaN)));
			Assert.Throws<ArgumentException>(() => target.SetValue(property, new Thickness(0, 0, 0, double.PositiveInfinity)));
			Assert.Throws<ArgumentException>(() => target.SetValue(property, new Thickness(0, 0, 0, double.NegativeInfinity)));
		}
	}

	#endregion

	#region Classes

	[TestClass]
	public class UseLayoutRounding : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void MeasureArrangesChildToRoundedBorderThickness()
		{
			Canvas child;
			var target = new Border
			{
				BorderThickness = new Thickness(1),
				Width = 82,
				Height = 82,
				Child = child = new Canvas()
			};

			var root = CreateRoot(1.5, target);

			root.LayoutManager.ExecuteInitialLayoutPass();

			// - 1 pixel border thickness is rounded up to 1.3333; for both sides it is 2.6666
			// - Size of 82 needs no rounding
			// - Minus border thickness, space for child is 82 - 2.6666 = 79.3333
			CornerstoneTest.AreEqual(1.3333, child.Bounds.Left, 3);
			CornerstoneTest.AreEqual(1.3333, child.Bounds.Top, 3);
			CornerstoneTest.AreEqual(79.3333, child.Bounds.Width, 3);
			CornerstoneTest.AreEqual(79.3333, child.Bounds.Height, 3);
		}

		[PresentationTestMethod]
		public void MeasureArrangesChildWithRoundedMargin()
		{
			Border child;
			var target = new Border
			{
				Width = 220,
				Height = 220,
				Child = child = new Border
				{
					Margin = new Thickness(0, 25, 25, 25)
				}
			};

			var root = CreateRoot(1.5, target);

			root.LayoutManager.ExecuteInitialLayoutPass();

			// - 25 margin gets rounded up to 25.3333
			// - Size of 220 needs no rounding
			CornerstoneTest.AreEqual(0, child.Bounds.Left, 3);
			CornerstoneTest.AreEqual(25.3333, child.Bounds.Top, 3);
			CornerstoneTest.AreEqual(194.6666, child.Bounds.Width, 3);
			CornerstoneTest.AreEqual(169.3333, child.Bounds.Height, 3);
		}

		[PresentationTestMethod]
		public void MeasureRoundsBorderThickness()
		{
			var target = new Border
			{
				BorderThickness = new Thickness(1),
				Child = new Canvas
				{
					Width = 101,
					Height = 101
				}
			};

			var root = CreateRoot(1.5, target);

			root.LayoutManager.ExecuteInitialLayoutPass();

			// - 1 pixel border thickness is rounded up to 1.3333; for both sides it is 2.6666
			// - Size of 101 gets rounded up to 101.3333
			// - Desired size = 101.3333 + 2.6666 = 104
			CornerstoneTest.AreEqual(new Size(104, 104), target.DesiredSize);
		}

		[PresentationTestMethod]
		public void MeasureRoundsPadding()
		{
			var target = new Border
			{
				Padding = new Thickness(1),
				Child = new Canvas
				{
					Width = 101,
					Height = 101
				}
			};

			var root = CreateRoot(1.5, target);

			root.LayoutManager.ExecuteInitialLayoutPass();

			// - 1 pixel padding is rounded up to 1.3333; for both sides it is 2.6666
			// - Size of 101 gets rounded up to 101.3333
			// - Desired size = 101.3333 + 2.6666 = 104
			CornerstoneTest.AreEqual(new Size(104, 104), target.DesiredSize);
		}

		private static TestRoot CreateRoot(
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