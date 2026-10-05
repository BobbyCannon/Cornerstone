#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Layout;

[TestClass]
public class LayoutableTestsLayoutRounding : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ArrangeAdjustsBoundsUpwardsWithMargin()
	{
		var target = new TestLayoutable(new Size(101, 101), 1);
		var root = CreateRoot(1.5, target);

		root.LayoutManager.ExecuteInitialLayoutPass();

		// - 1 pixel margin is rounded up to 1.3333
		// - Size of 101 gets rounded up to 101.3333
		AssertEqual(new Point(1.3333333333333333, 1.3333333333333333), target.Bounds.Position);
		AssertEqual(new Size(101.33333333333333, 101.33333333333333), target.Bounds.Size);
	}

	[PresentationTestMethod]
	[DataRow(16, 6, 5.333333333333333)]
	[DataRow(18, 10, 4)]
	public void ArrangesCenterAlignmentCorrectlyWithFractionalScaling(
		double containerWidth,
		double childWidth,
		double expectedX)
	{
		Border target;
		var root = new TestRoot
		{
			LayoutScaling = 1.5,
			UseLayoutRounding = true,
			Child = new Decorator
			{
				Width = containerWidth,
				Height = 100,
				Child = target = new Border
				{
					Width = childWidth,
					HorizontalAlignment = HorizontalAlignment.Center
				}
			}
		};

		root.Measure(new Size(100, 100));
		root.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Rect(expectedX, 0, childWidth, 100), target.Bounds);
	}

	[PresentationTestMethod]
	[DataRow(100, 100)]
	[DataRow(101, 101.33333333333333)]
	[DataRow(103, 103.33333333333333)]
	public void MeasureAdjustsDesiredSizeUpwardsWhenConstraintAllows(double desiredSize, double expectedSize)
	{
		var target = new TestLayoutable(new Size(desiredSize, desiredSize));
		var root = CreateRoot(1.5, target);

		root.LayoutManager.ExecuteInitialLayoutPass();

		CornerstoneTest.AreEqual(new Size(expectedSize, expectedSize), target.DesiredSize);
	}

	[PresentationTestMethod]
	public void MeasureAdjustsDesiredSizeUpwardsWhenMarginPresent()
	{
		var target = new TestLayoutable(new Size(101, 101), 1);
		var root = CreateRoot(1.5, target);

		root.LayoutManager.ExecuteInitialLayoutPass();

		// - 1 pixel margin is rounded up to 1.3333; for both sides it is 2.6666
		// - Size of 101 gets rounded up to 101.3333
		// - Final size = 101.3333 + 2.6666 = 104
		AssertEqual(new Size(104, 104), target.DesiredSize);
	}

	[PresentationTestMethod]
	public void MeasureConstrainsAdjustedDesiredSizeToConstraint()
	{
		var target = new TestLayoutable(new Size(101, 101));
		var root = CreateRoot(1.5, target, new Size(101, 101));

		root.LayoutManager.ExecuteInitialLayoutPass();

		// Desired width/height with layout rounding is 101.3333 but constraint is 101,101 so
		// layout rounding should be ignored.
		CornerstoneTest.AreEqual(new Size(101, 101), target.DesiredSize);
	}

	private static void AssertEqual(Point expected, Point actual)
	{
		if (!expected.NearlyEquals(actual))
		{
			throw EqualException.ForMismatchedValues(expected.ToString(), actual.ToString());
		}
	}

	private static void AssertEqual(Size expected, Size actual)
	{
		if (!expected.NearlyEquals(actual))
		{
			throw EqualException.ForMismatchedValues(expected.ToString(), actual.ToString());
		}
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

	#region Classes

	private class TestLayoutable : Control
	{
		#region Fields

		private readonly Size _desiredSize;

		#endregion

		#region Constructors

		public TestLayoutable(Size desiredSize, double margin = 0)
		{
			_desiredSize = desiredSize;
			Margin = new Thickness(margin);
		}

		#endregion

		#region Methods

		protected override Size MeasureOverride(Size availableSize)
		{
			return _desiredSize;
		}

		#endregion
	}

	#endregion
}