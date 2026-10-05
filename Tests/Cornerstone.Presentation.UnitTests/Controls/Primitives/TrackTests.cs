#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Primitives;

[TestClass]
public class TrackTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void MeasureShouldReturnThumbDesiredHeightInHorizontalOrientation()
	{
		var thumb = new Thumb
		{
			Height = 12
		};

		var target = new Track
		{
			Thumb = thumb,
			Orientation = Orientation.Horizontal
		};

		target.Measure(new Size(100, 100));

		CornerstoneTest.AreEqual(new Size(0, 12), target.DesiredSize);
	}

	[PresentationTestMethod]
	public void MeasureShouldReturnThumbDesiredWidthInVerticalOrientation()
	{
		var thumb = new Thumb
		{
			Width = 12
		};

		var target = new Track
		{
			Thumb = thumb,
			Orientation = Orientation.Vertical
		};

		target.Measure(new Size(100, 100));

		CornerstoneTest.AreEqual(new Size(12, 0), target.DesiredSize);
	}

	[PresentationTestMethod]
	public void ShouldArrangeThumbInHorizontalOrientation()
	{
		var thumb = new Thumb
		{
			Height = 12
		};

		var target = new Track
		{
			Thumb = thumb,
			Orientation = Orientation.Horizontal,
			Minimum = 100,
			Maximum = 200,
			Height = 12,
			Value = 150,
			ViewportSize = 50
		};

		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Rect(33, 0, 34, 12), thumb.Bounds);
	}

	[PresentationTestMethod]
	public void ShouldArrangeThumbInVerticalOrientation()
	{
		var thumb = new Thumb
		{
			Width = 12
		};

		var target = new Track
		{
			Thumb = thumb,
			Orientation = Orientation.Vertical,
			Minimum = 100,
			Maximum = 200,
			Value = 150,
			ViewportSize = 50,
			Width = 12
		};

		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Rect(0, 33, 12, 34), thumb.Bounds);
	}

	[PresentationTestMethod]
	public void ShouldNotPassInvalidArrangeRect()
	{
		var thumb = new Thumb { Width = 100.873106060606 };
		var increaseButton = new Button { Width = 10 };
		var decreaseButton = new Button { Width = 10 };

		var target = new Track
		{
			Height = 12,
			Thumb = thumb,
			IncreaseButton = increaseButton,
			DecreaseButton = decreaseButton,
			Orientation = Orientation.Horizontal,
			Minimum = 0,
			Maximum = 287,
			Value = 287,
			ViewportSize = 241
		};

		target.Measure(Size.Infinity);

		// #1297 was occuring here.
		target.Arrange(new Rect(0, 0, 221, 12));
	}

	[PresentationTestMethod]
	public void ThumbShouldBeLogicalChild()
	{
		var thumb = new Thumb
		{
			Height = 12
		};

		var target = new Track
		{
			Height = 12,
			Thumb = thumb,
			Orientation = Orientation.Horizontal,
			Minimum = 100,
			Maximum = 100
		};

		CornerstoneTest.Same(thumb.Parent, target);
		CornerstoneTest.AreEqual(new[] { thumb }, ((ILogical) target).LogicalChildren);
	}

	[PresentationTestMethod]
	public void ThumbShouldHaveZeroWidthWhenMinimumEqualsMaximum()
	{
		var thumb = new Thumb
		{
			Height = 12
		};

		var target = new Track
		{
			Height = 12,
			Thumb = thumb,
			Orientation = Orientation.Horizontal,
			Minimum = 100,
			Maximum = 100
		};

		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Rect(0, 0, 0, 12), thumb.Bounds);
	}

	#endregion
}