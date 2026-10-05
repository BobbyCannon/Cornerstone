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
public class WrapPanelTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void AppliesItemWidthAndItemHeightProperties()
	{
		var target = new WrapPanel
		{
			Orientation = Orientation.Horizontal,
			Width = 50,
			ItemWidth = 20,
			ItemHeight = 15,
			Children =
			{
				new Border(),
				new Border()
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(50, 15), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 20, 15), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(20, 0, 20, 15), target.Children[1].Bounds);
	}

	[PresentationTestMethod]
	[DataRow(true, 0)]
	[DataRow(false, 60)]
	public void ArrangeRespectsLayoutRoundingAtWrapBoundary(bool useLayoutRounding, double expectedSecondChildY)
	{
		var target = new WrapPanel
		{
			UseLayoutRounding = useLayoutRounding,
			ItemSpacing = 10,
			LineSpacing = 10,
			Children =
			{
				new Border { Width = 45, Height = 50, UseLayoutRounding = false },
				new Border
				{
					Width = 45 + (LayoutHelper.LayoutEpsilon / 2),
					Height = 50,
					UseLayoutRounding = false
				}
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(0, 0, 100, 110));

		CornerstoneTest.AreEqual(expectedSecondChildY, target.Children[1].Bounds.Y);
	}

	public static TestRows<Orientation, WrapPanelItemsAlignment> GetItemsAlignmentValues()
	{
		var data = new TestRows<Orientation, WrapPanelItemsAlignment>();
		foreach (var orientation in Enum.GetValues<Orientation>())
		{
			foreach (var alignment in Enum.GetValues<WrapPanelItemsAlignment>())
			{
				data.Add(orientation, alignment);
			}
		}
		return data;
	}

	[PresentationTestMethod]
	public void ItemHeightTriggerInvalidateMeasure()
	{
		var target = new WrapPanel();

		target.Measure(new Size(10, 10));

		CornerstoneTest.IsTrue(target.IsMeasureValid);

		target.ItemHeight = 1;

		CornerstoneTest.IsFalse(target.IsMeasureValid);
	}

	[PresentationTestMethod]
	public void ItemWidthTriggerInvalidateMeasure()
	{
		var target = new WrapPanel();

		target.Measure(new Size(10, 10));

		CornerstoneTest.IsTrue(target.IsMeasureValid);

		target.ItemWidth = 1;

		CornerstoneTest.IsFalse(target.IsMeasureValid);
	}

	[PresentationTestMethod]
	[DataRow(Orientation.Horizontal)]
	[DataRow(Orientation.Vertical)]
	public void JustifyPositionsUnequalItems(Orientation orientation)
	{
		var target = new WrapPanel
		{
			Width = 100,
			Height = 100,
			Orientation = orientation,
			ItemsAlignment = WrapPanelItemsAlignment.Justify,
			UseLayoutRounding = false
		};

		if (orientation is Orientation.Horizontal)
		{
			target.Children.Add(new Border { Width = 40, Height = 50 });
			target.Children.Add(new Border { Width = 20, Height = 50 });
		}
		else
		{
			target.Children.Add(new Border { Width = 50, Height = 40 });
			target.Children.Add(new Border { Width = 50, Height = 20 });
		}

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		var firstBounds = target.Children[0].Bounds;
		var secondBounds = target.Children[1].Bounds;
		if (orientation is Orientation.Vertical)
		{
			firstBounds = new Rect(firstBounds.Y, firstBounds.X, firstBounds.Height, firstBounds.Width);
			secondBounds = new Rect(secondBounds.Y, secondBounds.X, secondBounds.Height, secondBounds.Width);
		}

		CornerstoneTest.AreEqual(new Rect(0, 0, 40, 50), firstBounds);
		CornerstoneTest.AreEqual(new Rect(80, 0, 20, 50), secondBounds);
		CornerstoneTest.AreEqual(40, secondBounds.X - firstBounds.Right);
	}

	[PresentationTestMethod]
	public void LaysOutHorizontallyOnASingleLine()
	{
		var target = new WrapPanel
		{
			Width = 200,
			Children =
			{
				new Border { Height = 50, Width = 100 },
				new Border { Height = 50, Width = 100 }
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(200, 50), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 50), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(100, 0, 100, 50), target.Children[1].Bounds);
	}

	[PresentationTestMethod]
	public void LaysOutHorizontallyOnSeparateLines()
	{
		var target = new WrapPanel
		{
			Width = 100,
			Children =
			{
				new Border { Height = 50, Width = 100 },
				new Border { Height = 50, Width = 100 }
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(100, 100), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 50), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 50, 100, 50), target.Children[1].Bounds);
	}

	[PresentationTestMethod]
	public void LaysOutHorizontallyOnSeparateLinesWithSpacing()
	{
		var target = new WrapPanel
		{
			Width = 100,
			ItemSpacing = 10,
			LineSpacing = 20,
			Children =
			{
				new Border { Height = 50, Width = 60 }, // line 0
				new Border { Height = 50, Width = 30 }, // line 0
				new Border { Height = 50, Width = 70 }, // line 1
				new Border { Height = 50, Width = 30 } // line 2
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(100, 190), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 60, 50), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(70, 0, 30, 50), target.Children[1].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 70, 70, 50), target.Children[2].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 140, 30, 50), target.Children[3].Bounds);
	}

	[PresentationTestMethod]
	public void LaysOutHorizontallyOnSeparateLinesWithSpacingInvisible()
	{
		var target = new WrapPanel
		{
			ItemSpacing = 10,
			Children =
			{
				new Border { Height = 50, Width = 60 }, // line 0
				new Border { Height = 50, Width = 30, IsVisible = false }, // line 0
				new Border { Height = 50, Width = 50 } // line 0
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(120, 50), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 60, 50), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(70, 0, 50, 50), target.Children[2].Bounds);
	}

	[PresentationTestMethod]
	public void LaysOutHorizontallyOnSeparateLinesWithSpacingVertical()
	{
		var target = new WrapPanel
		{
			Height = 100,
			Orientation = Orientation.Vertical,
			ItemSpacing = 10,
			LineSpacing = 20,
			Children =
			{
				new Border { Width = 50, Height = 60 }, // line 0
				new Border { Width = 50, Height = 30 }, // line 0
				new Border { Width = 50, Height = 70 }, // line 1
				new Border { Width = 50, Height = 30 } // line 2
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(190, 100), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 50, 60), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 70, 50, 30), target.Children[1].Bounds);
		CornerstoneTest.AreEqual(new Rect(70, 0, 50, 70), target.Children[2].Bounds);
		CornerstoneTest.AreEqual(new Rect(140, 0, 50, 30), target.Children[3].Bounds);
	}

	[PresentationTestMethod]
	public void LaysOutVerticallyChildrenOnASingleLine()
	{
		var target = new WrapPanel
		{
			Orientation = Orientation.Vertical,
			Height = 120,
			Children =
			{
				new Border { Height = 50, Width = 100 },
				new Border { Height = 50, Width = 100 }
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(100, 120), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 50), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 50, 100, 50), target.Children[1].Bounds);
	}

	[PresentationTestMethod]
	public void LaysOutVerticallyOnSeparateLines()
	{
		var target = new WrapPanel
		{
			Orientation = Orientation.Vertical,
			Height = 60,
			Children =
			{
				new Border { Height = 50, Width = 100 },
				new Border { Height = 50, Width = 100 }
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(200, 60), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 50), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(100, 0, 100, 50), target.Children[1].Bounds);
	}

	[PresentationTestMethod]
	[TestData(nameof(GetItemsAlignmentValues))]
	public void LaysOutWithItemsAlignment(Orientation orientation, WrapPanelItemsAlignment itemsAlignment)
	{
		var lineHeight = 50d;
		var target = new WrapPanel
		{
			Width = 200,
			Height = 200,
			Orientation = orientation,
			ItemsAlignment = itemsAlignment,
			UseLayoutRounding = false
		};

		if (orientation is Orientation.Horizontal)
		{
			target.ItemHeight = lineHeight;
			target.Children.Add(new Border { MinWidth = 50 });
			target.Children.Add(new Border { MinWidth = 100 });
			target.Children.Add(new Border { MinWidth = 150 });
		}
		else
		{
			target.ItemWidth = lineHeight;
			target.Children.Add(new Border { MinHeight = 50 });
			target.Children.Add(new Border { MinHeight = 100 });
			target.Children.Add(new Border { MinHeight = 150 });
		}

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(200, 200), target.Bounds.Size);

		var row1Bounds = target.Children[0].Bounds.Union(target.Children[1].Bounds);
		var row2Bounds = target.Children[2].Bounds;

		row1Bounds = new Rect(
			Math.Round(row1Bounds.X),
			Math.Round(row1Bounds.Y),
			Math.Round(row1Bounds.Width),
			Math.Round(row1Bounds.Height));

		if (orientation is Orientation.Vertical)
		{
			row1Bounds = new Rect(row1Bounds.Y, row1Bounds.X, row1Bounds.Height, row1Bounds.Width);
			row2Bounds = new Rect(row2Bounds.Y, row2Bounds.X, row2Bounds.Height, row2Bounds.Width);
		}

		CornerstoneTest.AreEqual(itemsAlignment switch
		{
			WrapPanelItemsAlignment.Stretch or WrapPanelItemsAlignment.StretchAll or WrapPanelItemsAlignment.Justify => new(0, 0, 200, lineHeight),
			WrapPanelItemsAlignment.Center => new(25, 0, 150, lineHeight),
			WrapPanelItemsAlignment.End => new(50, 0, 150, lineHeight),
			_ => new(0, 0, 150, lineHeight)
		}, row1Bounds);

		CornerstoneTest.AreEqual(itemsAlignment switch
		{
			WrapPanelItemsAlignment.StretchAll => new(0, lineHeight, 200, lineHeight),
			WrapPanelItemsAlignment.Center => new(25, lineHeight, 150, lineHeight),
			WrapPanelItemsAlignment.End => new(50, lineHeight, 150, lineHeight),
			_ => new(0, 50, 150, 50)
		}, row2Bounds);
	}

	[PresentationTestMethod]
	[DataRow(true, 50)]
	[DataRow(false, 110)]
	public void MeasureRespectsLayoutRoundingAtWrapBoundary(bool useLayoutRounding, double expectedHeight)
	{
		var target = new WrapPanel
		{
			UseLayoutRounding = useLayoutRounding,
			ItemSpacing = 10,
			LineSpacing = 10,
			Children =
			{
				new Border { Width = 45, Height = 50 },
				new Border { Width = 45, Height = 50 }
			}
		};

		target.Measure(new Size(100 - (LayoutHelper.LayoutEpsilon / 2), double.PositiveInfinity));

		CornerstoneTest.AreEqual(expectedHeight, target.DesiredSize.Height);
	}

	[PresentationTestMethod]
	public void StretchLastLineUsesUniformItemWidth()
	{
		var target = new WrapPanel
		{
			Width = 220,
			ItemWidth = 100,
			ItemSpacing = 10,
			ItemsAlignment = WrapPanelItemsAlignment.Stretch,
			UseLayoutRounding = false,
			Children =
			{
				new Border { Height = 40 },
				new Border { Height = 40 },
				new Border { Height = 40 }
			}
		};

		target.Measure(new Size(220, double.PositiveInfinity));
		target.Arrange(new Rect(0, 0, 220, target.DesiredSize.Height));

		CornerstoneTest.AreEqual(new Rect(0, 0, 105, 40), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(115, 0, 105, 40), target.Children[1].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 40, 105, 40), target.Children[2].Bounds);
	}

	[PresentationTestMethod]
	public void StretchHandlesAZeroWidthLine()
	{
		var target = new WrapPanel
		{
			Width = 100,
			ItemSpacing = 1,
			ItemsAlignment = WrapPanelItemsAlignment.Stretch,
			Children =
			{
				new Border { Width = 0, Height = 50 },
				new Border { Width = 100, Height = 50 }
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Rect(0, 0, 0, 50), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 50, 100, 50), target.Children[1].Bounds);
	}

	[PresentationTestMethod]
	public void ZeroSizeVisibleChild()
	{
		var target = new WrapPanel
		{
			Orientation = Orientation.Horizontal,
			Width = 50,
			ItemSpacing = 10,
			LineSpacing = 10,
			Children =
			{
				new Border(), // line 0
				new Border // line 1
				{
					Width = 50,
					Height = 50
				}
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(50, 60), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 0, 0), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 10, 50, 50), target.Children[1].Bounds);
	}

	#endregion
}