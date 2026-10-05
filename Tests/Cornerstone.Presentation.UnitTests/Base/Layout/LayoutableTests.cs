#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Layout;

[TestClass]
public class LayoutableTests
{
	#region Methods

	[PresentationTestMethod]
	public void AttachingControlToTreeInvalidatesParentMeasure()
	{
		var target = new StubLayoutManager();
		var control = new Decorator();
		var root = new LayoutTestRoot
		{
			Child = control,
			LayoutManager = target
		};

		root.Measure(Size.Infinity);
		root.Arrange(new Rect(root.DesiredSize));
		CornerstoneTest.IsTrue(control.IsMeasureValid);

		root.Child = null;
		root.Measure(Size.Infinity);
		root.Arrange(new Rect(root.DesiredSize));

		CornerstoneTest.IsFalse(control.IsMeasureValid);
		CornerstoneTest.IsTrue(root.IsMeasureValid);

		target.Calls.Clear();

		root.Child = control;

		CornerstoneTest.IsFalse(root.IsMeasureValid);
		CornerstoneTest.IsFalse(control.IsMeasureValid);
		target.Calls.VerifyCalled("InvalidateMeasure", 1);
	}

	[PresentationTestMethod]
	public void ConstraintAndNegativeMargin()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var textBlock = new TextBlock
		{
			Margin = new Thickness(-10),
			Text = "Lorem ipsum dolor sit amet"
		};

		var border = new Border
		{
			MaxWidth = 100,
			Child = textBlock
		};

		border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		border.Arrange(new Rect(default, border.DesiredSize));

		CornerstoneTest.Multiple(() =>
		{
			CornerstoneTest.AreEqual(new Size(100, 0), border.DesiredSize);
			CornerstoneTest.AreEqual(new Rect(0, 0, 100, 0), border.Bounds);
			CornerstoneTest.AreEqual(new Size(100, 0), textBlock.DesiredSize);
			CornerstoneTest.AreEqual(new Rect(-10, -10, 120, 20), textBlock.Bounds);
		});
	}

	[PresentationTestMethod]
	[DataRow(HorizontalAlignment.Stretch, 100)]
	[DataRow(HorizontalAlignment.Left, 10)]
	[DataRow(HorizontalAlignment.Center, 10)]
	[DataRow(HorizontalAlignment.Right, 10)]
	public void HorizontalAlignmentIsAppliedToArrangeOverrideSize(
		HorizontalAlignment h,
		double expectedWidth)
	{
		var target = new TestLayoutable
		{
			HorizontalAlignment = h
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Size(expectedWidth, 100), target.ArrangeSize);
	}

	[PresentationTestMethod]
	public void LayoutManagerLayoutUpdatedIsSubscribedWhenAttachedToTree()
	{
		Border border1;
		var layoutManager = new StubLayoutManager();

		var root = new TestRoot
		{
			Child = border1 = new Border(),
			LayoutManager = layoutManager
		};

		var border2 = new Border();
		border2.LayoutUpdated += (s, e) => { };

		layoutManager.Calls.Clear();
		border1.Child = border2;

		layoutManager.Calls.VerifyCalled("add_LayoutUpdated", 1);
	}

	[PresentationTestMethod]
	public void LayoutManagerLayoutUpdatedIsUnsubscribedWhenDetachedFromTree()
	{
		Border border1;
		var layoutManager = new StubLayoutManager();

		var root = new TestRoot
		{
			Child = border1 = new Border(),
			LayoutManager = layoutManager
		};

		var border2 = new Border();
		border2.LayoutUpdated += (s, e) => { };
		border1.Child = border2;

		layoutManager.Calls.Clear();
		border1.Child = null;

		layoutManager.Calls.VerifyCalled("remove_LayoutUpdated", 1);
	}

	[PresentationTestMethod]
	public void LayoutManagerLayoutUpdatedShouldNotBeSubscribedTwiceInAttachedToVisualTree()
	{
		Border border1;
		var layoutManager = new StubLayoutManager();

		_ = new TestRoot
		{
			Child = border1 = new Border(),
			LayoutManager = layoutManager
		};

		var border2 = new Border();
		border2.AttachedToVisualTree += (_, _) => border2.LayoutUpdated += (_, _) => { };

		layoutManager.Calls.Clear();
		border1.Child = border2;

		layoutManager.Calls.VerifyCalled("add_LayoutUpdated", 1);
	}

	[PresentationTestMethod]
	public void LayoutUpdatedIsCalledAtEndOfLayoutPass()
	{
		Border border1;
		Border border2;
		var root = new TestRoot
		{
			Child = border1 = new Border
			{
				Child = border2 = new Border()
			}
		};
		var raised = 0;

		void ValidateBounds(object sender, EventArgs e)
		{
			CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), border1.Bounds);
			CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), border2.Bounds);
			++raised;
		}

		root.LayoutUpdated += ValidateBounds;
		border1.LayoutUpdated += ValidateBounds;
		border2.LayoutUpdated += ValidateBounds;

		root.Measure(new Size(100, 100));
		root.Arrange(new Rect(0, 0, 100, 100));

		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(3, raised);
		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), border1.Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), border2.Bounds);
	}

	[PresentationTestMethod]
	public void LayoutUpdatedSubscribesToLayoutManager()
	{
		Border target;
		var layoutManager = new StubLayoutManager();

		var root = new TestRoot
		{
			Child = new Border
			{
				Child = target = new Border()
			},
			LayoutManager = layoutManager
		};

		void Handler(object sender, EventArgs e)
		{
		}

		layoutManager.Calls.Clear();
		target.LayoutUpdated += Handler;

		layoutManager.Calls.VerifyCalled("add_LayoutUpdated", 1);

		layoutManager.Calls.Clear();
		target.LayoutUpdated -= Handler;

		layoutManager.Calls.VerifyCalled("remove_LayoutUpdated", 1);
	}

	[PresentationTestMethod]
	public void MakingControlInvisibleShouldInvalidateParentMeasure()
	{
		Border child;
		var target = new StackPanel
		{
			Children =
			{
				(child = new Border
				{
					Width = 100
				})
			}
		};

		target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.IsTrue(target.IsMeasureValid);
		CornerstoneTest.IsTrue(target.IsArrangeValid);
		CornerstoneTest.IsTrue(child.IsMeasureValid);
		CornerstoneTest.IsTrue(child.IsArrangeValid);

		child.IsVisible = false;

		CornerstoneTest.IsFalse(target.IsMeasureValid);
		CornerstoneTest.IsFalse(target.IsArrangeValid);
		CornerstoneTest.IsTrue(child.IsMeasureValid);
		CornerstoneTest.IsTrue(child.IsArrangeValid);
	}

	[PresentationTestMethod]
	public void MakingControlVisibleShouldInvalidateOwnAndParentMeasure()
	{
		Border child;
		var target = new StackPanel
		{
			Children =
			{
				(child = new Border
				{
					Width = 100,
					IsVisible = false
				})
			}
		};

		target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.IsTrue(target.IsMeasureValid);
		CornerstoneTest.IsTrue(target.IsArrangeValid);
		CornerstoneTest.IsTrue(child.IsMeasureValid);
		CornerstoneTest.IsFalse(child.IsArrangeValid);

		child.IsVisible = true;

		CornerstoneTest.IsFalse(target.IsMeasureValid);
		CornerstoneTest.IsFalse(target.IsArrangeValid);
		CornerstoneTest.IsFalse(child.IsMeasureValid);
		CornerstoneTest.IsFalse(child.IsArrangeValid);
	}

	[PresentationTestMethod]
	[DataRow(0, 0, 0, 0, 100, 100)]
	[DataRow(10, 0, 0, 0, 90, 100)]
	[DataRow(10, 0, 5, 0, 85, 100)]
	[DataRow(0, 10, 0, 0, 100, 90)]
	[DataRow(0, 10, 0, 5, 100, 85)]
	[DataRow(4, 4, 6, 7, 90, 89)]
	public void MarginIsAppliedToArrangeOverrideSize(
		double l,
		double t,
		double r,
		double b,
		double expectedWidth,
		double expectedHeight)
	{
		var target = new TestLayoutable
		{
			Margin = new Thickness(l, t, r, b)
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Size(expectedWidth, expectedHeight), target.ArrangeSize);
	}

	[PresentationTestMethod]
	[DataRow(0, 0, 0, 0, 100, 100)]
	[DataRow(10, 0, 0, 0, 90, 100)]
	[DataRow(10, 0, 5, 0, 85, 100)]
	[DataRow(0, 10, 0, 0, 100, 90)]
	[DataRow(0, 10, 0, 5, 100, 85)]
	[DataRow(4, 4, 6, 7, 90, 89)]
	public void MarginIsAppliedToMeasureOverrideSize(
		double l,
		double t,
		double r,
		double b,
		double expectedWidth,
		double expectedHeight)
	{
		var target = new TestLayoutable
		{
			Margin = new Thickness(l, t, r, b)
		};

		target.Measure(new Size(100, 100));

		CornerstoneTest.AreEqual(new Size(expectedWidth, expectedHeight), target.MeasureSize);
	}

	[PresentationTestMethod]
	public void MeasuringInvisibleControlShouldNotInvalidateParentMeasure()
	{
		Border child;
		var target = new StackPanel
		{
			Children =
			{
				(child = new Border
				{
					Width = 100
				})
			}
		};

		target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.IsTrue(target.IsMeasureValid);
		CornerstoneTest.IsTrue(target.IsArrangeValid);
		CornerstoneTest.AreEqual(new Size(100, 0), child.DesiredSize);

		child.IsVisible = false;
		CornerstoneTest.AreEqual(default, child.DesiredSize);

		target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		target.Arrange(new Rect(target.DesiredSize));
		child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

		CornerstoneTest.IsTrue(target.IsMeasureValid);
		CornerstoneTest.IsTrue(target.IsArrangeValid);
		CornerstoneTest.AreEqual(default, child.DesiredSize);
	}

	[PresentationTestMethod]
	public void OnlyCallsLayoutManagerInvalidateArrangeOnce()
	{
		var target = new StubLayoutManager();
		var control = new Decorator();
		var root = new LayoutTestRoot
		{
			Child = control,
			LayoutManager = target
		};

		root.Measure(Size.Infinity);
		root.Arrange(new Rect(root.DesiredSize));
		target.Calls.Clear();

		control.InvalidateArrange();
		control.InvalidateArrange();

		target.Calls.VerifyCalled("InvalidateArrange", 1);
	}

	[PresentationTestMethod]
	public void OnlyCallsLayoutManagerInvalidateMeasureOnce()
	{
		var target = new StubLayoutManager();
		var control = new Decorator();
		var root = new LayoutTestRoot
		{
			Child = control,
			LayoutManager = target
		};

		root.Measure(Size.Infinity);
		root.Arrange(new Rect(root.DesiredSize));
		target.Calls.Clear();

		control.InvalidateMeasure();
		control.InvalidateMeasure();

		target.Calls.VerifyCalled("InvalidateMeasure", 1);
	}

	[PresentationTestMethod]
	public void SizePropertiesRejectInvalidValues()
	{
		var target = new Layoutable();

		CornerstoneTest.Multiple(() =>
		{
			SetShouldThrow([Layoutable.WidthProperty, Layoutable.HeightProperty], double.PositiveInfinity);
			SetShouldThrow([Layoutable.WidthProperty, Layoutable.HeightProperty], -10);

			SetShouldThrow([Layoutable.MinWidthProperty, Layoutable.MinHeightProperty], double.PositiveInfinity);
			SetShouldThrow([Layoutable.MinWidthProperty, Layoutable.MinHeightProperty], -10);

			SetShouldThrow([Layoutable.MaxWidthProperty, Layoutable.MaxHeightProperty], -10);

			void SetShouldThrow(IEnumerable<StyledProperty<double>> properies, double value)
			{
				foreach (var prop in properies)
				{
					Assert.Throws<ArgumentException>(() => target.SetValue(prop, value));
				}
			}
		});
	}

	[PresentationTestMethod]
	[DataRow(VerticalAlignment.Stretch, 100)]
	[DataRow(VerticalAlignment.Top, 10)]
	[DataRow(VerticalAlignment.Center, 10)]
	[DataRow(VerticalAlignment.Bottom, 10)]
	public void VerticalAlignmentIsAppliedToArrangeOverrideSize(
		VerticalAlignment v,
		double expectedHeight)
	{
		var target = new TestLayoutable
		{
			VerticalAlignment = v
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Size(100, expectedHeight), target.ArrangeSize);
	}

	#endregion

	#region Classes

	private class TestLayoutable : Layoutable
	{
		#region Properties

		public Size ArrangeSize { get; private set; }
		public Size MeasureResult { get; } = new(10, 10);
		public Size MeasureSize { get; private set; }

		#endregion

		#region Methods

		protected override Size ArrangeOverride(Size finalSize)
		{
			ArrangeSize = finalSize;
			return base.ArrangeOverride(finalSize);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			MeasureSize = availableSize;
			return MeasureResult;
		}

		#endregion
	}

	#endregion
}