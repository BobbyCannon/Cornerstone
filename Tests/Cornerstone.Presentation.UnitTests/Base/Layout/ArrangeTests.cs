#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Layout;

[TestClass]
public class ArrangeTests
{
	#region Methods

	[PresentationTestMethod]
	public void ArrangeOverrideReceivesAvailableSizeMinusMarginWhenStretched()
	{
		var target = new TestControl
		{
			MeasureResult = new Size(100, 100),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Margin = new Thickness(8)
		};

		target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		target.Arrange(new Rect(0, 0, 200, 200));

		CornerstoneTest.AreEqual(new Size(184, 184), target.ArrangeFinalSize);
	}

	[PresentationTestMethod]
	public void ArrangeOverrideReceivesDesiredSizeWhenCentered()
	{
		var target = new TestControl
		{
			MeasureResult = new Size(100, 100),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(8)
		};

		target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		target.Arrange(new Rect(0, 0, 200, 200));

		CornerstoneTest.AreEqual(new Size(100, 100), target.ArrangeFinalSize);
	}

	[PresentationTestMethod]
	public void ArrangeOverrideReceivesRequestedSizeWhenArrangedToDesiredSize()
	{
		var target = new TestControl
		{
			MeasureResult = new Size(100, 100),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(8)
		};

		target.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(100, 100), target.ArrangeFinalSize);
	}

	[PresentationTestMethod]
	public void ArrangeWithIsMeasureValidFalseCallsMeasure()
	{
		var target = new TestControl();

		CornerstoneTest.IsFalse(target.IsMeasureValid);
		target.Arrange(new Rect(0, 0, 120, 120));
		CornerstoneTest.IsTrue(target.IsMeasureValid);
		CornerstoneTest.AreEqual(new Size(120, 120), target.MeasureConstraint);
	}

	[PresentationTestMethod]
	public void ArrangeWithIsMeasureValidFalseCallsMeasureWithPreviousSizeIfAvailable()
	{
		var target = new TestControl();

		CornerstoneTest.IsFalse(target.IsMeasureValid);
		target.Arrange(new Rect(0, 0, 120, 120));
		target.InvalidateMeasure();
		target.Arrange(new Rect(0, 0, 100, 100));
		CornerstoneTest.IsTrue(target.IsMeasureValid);
		CornerstoneTest.AreEqual(new Size(120, 120), target.MeasureConstraint);
	}

	[PresentationTestMethod]
	public void BoundsShouldNotIncludeMargin()
	{
		var target = new Decorator
		{
			Width = 100,
			Height = 100,
			Margin = new Thickness(5)
		};

		CornerstoneTest.IsFalse(target.IsMeasureValid);
		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));
		CornerstoneTest.AreEqual(new Rect(5, 5, 100, 100), target.Bounds);
	}

	[PresentationTestMethod]
	public void MarginShouldBeSubtractedFromArrangeFinalSize()
	{
		var target = new TestControl
		{
			Width = 100,
			Height = 100,
			Margin = new Thickness(8)
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(100, 100), target.ArrangeFinalSize);
	}

	#endregion

	#region Classes

	private class TestControl : Decorator
	{
		#region Properties

		public Size ArrangeFinalSize { get; private set; }
		public Size MeasureConstraint { get; private set; }

		public Size MeasureResult { get; set; }

		#endregion

		#region Methods

		protected override Size ArrangeOverride(Size finalSize)
		{
			ArrangeFinalSize = finalSize;
			return base.ArrangeOverride(finalSize);
		}

		protected override Size MeasureOverride(Size constraint)
		{
			MeasureConstraint = constraint;
			return MeasureResult;
		}

		#endregion
	}

	#endregion
}