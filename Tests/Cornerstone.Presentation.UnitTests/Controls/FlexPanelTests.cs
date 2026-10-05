#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class FlexPanelTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void AppliesAbsoluteFlexBasisProperties()
	{
		var target = new FlexPanel
		{
			Width = 50,
			Children =
			{
				new Border
				{
					[Flex.BasisProperty] = new FlexBasis(20),
					Height = 15
				},
				new Border
				{
					[Flex.BasisProperty] = new FlexBasis(20),
					Height = 15
				}
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(50, 15), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 20, 15), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(20, 0, 20, 15), target.Children[1].Bounds);
	}

	[PresentationTestMethod]
	public void AppliesRelativeFlexBasisProperties()
	{
		var target = new FlexPanel
		{
			Width = 50,
			Children =
			{
				new Border
				{
					[Flex.BasisProperty] = new FlexBasis(50, FlexBasisKind.Relative),
					Height = 15
				},
				new Border
				{
					[Flex.BasisProperty] = new FlexBasis(50, FlexBasisKind.Relative),
					Height = 15
				}
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(50, 15), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 25, 15), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(25, 0, 25, 15), target.Children[1].Bounds);
	}

	[PresentationTestMethod]
	public void CanWrapItemsIntoNextColumn()
	{
		var target = new FlexPanel
		{
			Height = 60,
			Children =
			{
				new Border { Height = 50, Width = 100 },
				new Border { Height = 50, Width = 100 }
			},
			Wrap = FlexWrap.Wrap,
			Direction = FlexDirection.Column
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(200, 60), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 50), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(100, 0, 100, 50), target.Children[1].Bounds);
	}

	[PresentationTestMethod]
	public void CanWrapItemsIntoNextColumnInReverseWrap()
	{
		var target = new FlexPanel
		{
			Height = 60,
			Children =
			{
				new Border { Height = 50, Width = 100 },
				new Border { Height = 50, Width = 100 }
			},
			Wrap = FlexWrap.WrapReverse,
			Direction = FlexDirection.Column
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(200, 60), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(100, 0, 100, 50), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 50), target.Children[1].Bounds);
	}

	[PresentationTestMethod]
	public void CanWrapItemsIntoNextColumnWithSpacing()
	{
		var target = new FlexPanel
		{
			Height = 110,
			RowSpacing = 10,
			ColumnSpacing = 20,
			Children =
			{
				new Border { Width = 50, Height = 60 }, // line 0
				new Border { Width = 50, Height = 30 }, // line 0
				new Border { Width = 50, Height = 70 }, // line 1
				new Border { Width = 50, Height = 30 } // line 2
			},
			Wrap = FlexWrap.Wrap,
			Direction = FlexDirection.Column
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(190, 110), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 50, 60), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 70, 50, 30), target.Children[1].Bounds);
		CornerstoneTest.AreEqual(new Rect(70, 0, 50, 70), target.Children[2].Bounds);
		CornerstoneTest.AreEqual(new Rect(140, 0, 50, 30), target.Children[3].Bounds);
	}

	[PresentationTestMethod]
	public void CanWrapItemsIntoNextRow()
	{
		var target = new FlexPanel
		{
			Width = 100,
			Children =
			{
				new Border { Height = 50, Width = 100 },
				new Border { Height = 50, Width = 100 }
			},
			Wrap = FlexWrap.Wrap
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(100, 100), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 50), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 50, 100, 50), target.Children[1].Bounds);
	}

	[PresentationTestMethod]
	public void CanWrapItemsIntoNextRowInReverseWrap()
	{
		var target = new FlexPanel
		{
			Width = 100,
			Children =
			{
				new Border { Height = 50, Width = 100 },
				new Border { Height = 50, Width = 100 }
			},
			Wrap = FlexWrap.WrapReverse
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(100, 100), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 50, 100, 50), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 50), target.Children[1].Bounds);
	}

	[PresentationTestMethod]
	public void CanWrapItemsIntoNextRowWithSpacing()
	{
		var target = new FlexPanel
		{
			Width = 110,
			ColumnSpacing = 10,
			RowSpacing = 20,
			Children =
			{
				new Border { Height = 50, Width = 60 }, // line 0
				new Border { Height = 50, Width = 30 }, // line 0
				new Border { Height = 50, Width = 70 }, // line 1
				new Border { Height = 50, Width = 30 } // line 2
			},
			Wrap = FlexWrap.Wrap
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(110, 190), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 60, 50), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(70, 0, 30, 50), target.Children[1].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 70, 70, 50), target.Children[2].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 140, 30, 50), target.Children[3].Bounds);
	}

	[PresentationTestMethod]
	public void CanWrapItemsIntoNextRowWithSpacingAndInvisibleContent()
	{
		var target = new FlexPanel
		{
			ColumnSpacing = 10,
			Children =
			{
				new Border { Height = 50, Width = 60 }, // line 0
				new Border { Height = 50, Width = 30, IsVisible = false }, // line 0
				new Border { Height = 50, Width = 50 } // line 0
			},
			Wrap = FlexWrap.Wrap
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(120, 50), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 60, 50), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(70, 0, 50, 50), target.Children[2].Bounds);
	}

	[PresentationTestMethod]
	public void EmptyPanelDoesNotTakeUpAvailableSpace()
	{
		var target = new FlexPanel();
		var stack = new StackPanel { Children = { new Border { Width = 100, Height = 50 }, target } };

		var presenter = new ScrollContentPresenter { CanVerticallyScroll = true, Content = stack };

		presenter.UpdateChild();
		presenter.Measure(new Size(100, 100));

		CornerstoneTest.AreEqual(default, target.DesiredSize);
		CornerstoneTest.AreEqual(new Size(100, 50), stack.DesiredSize);
	}

	public static TestRows<FlexDirection, FlexAlignItems> GetAlignItemsValues()
	{
		var data = new TestRows<FlexDirection, FlexAlignItems>();
		foreach (var direction in Enum.GetValues<FlexDirection>())
		{
			foreach (var alignment in Enum.GetValues<FlexAlignItems>())
			{
				data.Add(direction, alignment);
			}
		}
		return data;
	}

	public static TestRows<FlexDirection, FlexJustifyContent> GetJustifyContentValues()
	{
		var data = new TestRows<FlexDirection, FlexJustifyContent>();
		foreach (var direction in Enum.GetValues<FlexDirection>())
		{
			foreach (var justify in Enum.GetValues<FlexJustifyContent>())
			{
				data.Add(direction, justify);
			}
		}
		return data;
	}

	[PresentationTestMethod]
	public void LaysItemsInASingleColumn()
	{
		var target = new FlexPanel
		{
			Direction = FlexDirection.Column,
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
	public void LaysItemsInASingleRow()
	{
		var target = new FlexPanel
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
	[TestData(nameof(GetAlignItemsValues))]
	public void LaysOutWithItemsAlignment(FlexDirection direction, FlexAlignItems itemsAlignment)
	{
		var target = new FlexPanel
		{
			Width = 200,
			Height = 200,
			Direction = direction,
			AlignItems = itemsAlignment,
			Children =
			{
				new Border { Height = 50, Width = 50 },
				new Border { Height = 50, Width = 50 }
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(200, 200), target.Bounds.Size);

		var rowBounds = target.Children[0].Bounds.Union(target.Children[1].Bounds);

		CornerstoneTest.AreEqual(direction switch
		{
			FlexDirection.Row => new(100, 50),
			FlexDirection.RowReverse => new(100, 50),
			FlexDirection.Column => new(50, 100),
			FlexDirection.ColumnReverse => new(50, 100),
			_ => throw new NotImplementedException()
		}, rowBounds.Size);

		CornerstoneTest.AreEqual((direction, itemsAlignment) switch
		{
			(FlexDirection.Row, FlexAlignItems.FlexStart) => new(0, 0),
			(FlexDirection.Column, FlexAlignItems.FlexStart) => new(0, 0),
			(FlexDirection.Row, FlexAlignItems.Center) => new(0, 75),
			(FlexDirection.Column, FlexAlignItems.Center) => new(75, 0),
			(FlexDirection.Row, FlexAlignItems.FlexEnd) => new(0, 150),
			(FlexDirection.Column, FlexAlignItems.FlexEnd) => new(150, 0),
			(FlexDirection.Row, FlexAlignItems.Stretch) => new(0, 75),
			(FlexDirection.Column, FlexAlignItems.Stretch) => new(75, 0),
			(FlexDirection.RowReverse, FlexAlignItems.FlexStart) => new(100, 0),
			(FlexDirection.ColumnReverse, FlexAlignItems.FlexStart) => new(0, 100),
			(FlexDirection.RowReverse, FlexAlignItems.Center) => new(100, 75),
			(FlexDirection.ColumnReverse, FlexAlignItems.Center) => new(75, 100),
			(FlexDirection.RowReverse, FlexAlignItems.FlexEnd) => new(100, 150),
			(FlexDirection.ColumnReverse, FlexAlignItems.FlexEnd) => new(150, 100),
			(FlexDirection.RowReverse, FlexAlignItems.Stretch) => new(100, 75),
			(FlexDirection.ColumnReverse, FlexAlignItems.Stretch) => new(75, 100),
			_ => throw new NotImplementedException()
		}, rowBounds.Position);
	}

	[PresentationTestMethod]
	[TestData(nameof(GetJustifyContentValues))]
	public void LaysOutWithJustifyContent(FlexDirection direction, FlexJustifyContent justify)
	{
		var target = new FlexPanel
		{
			Width = 200,
			Height = 200,
			Direction = direction,
			JustifyContent = justify,
			AlignItems = FlexAlignItems.FlexStart,
			Children =
			{
				new Border { Height = 50, Width = 50 },
				new Border { Height = 50, Width = 50 }
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(200, 200), target.Bounds.Size);

		var rowBounds = target.Children[0].Bounds.Union(target.Children[1].Bounds);

		CornerstoneTest.AreEqual((direction, justify) switch
		{
			(FlexDirection.Row, FlexJustifyContent.FlexStart) => new(0, 0),
			(FlexDirection.Column, FlexJustifyContent.FlexStart) => new(0, 0),
			(FlexDirection.Row, FlexJustifyContent.Center) => new(50, 0),
			(FlexDirection.Column, FlexJustifyContent.Center) => new(0, 50),
			(FlexDirection.Row, FlexJustifyContent.FlexEnd) => new(100, 0),
			(FlexDirection.Column, FlexJustifyContent.FlexEnd) => new(0, 100),
			(FlexDirection.Row, FlexJustifyContent.SpaceAround) => new(25, 0),
			(FlexDirection.Column, FlexJustifyContent.SpaceAround) => new(0, 25),
			(FlexDirection.Row, FlexJustifyContent.SpaceBetween) => new(0, 0),
			(FlexDirection.Column, FlexJustifyContent.SpaceBetween) => new(0, 0),
			(FlexDirection.Row, FlexJustifyContent.SpaceEvenly) => new(33, 0),
			(FlexDirection.Column, FlexJustifyContent.SpaceEvenly) => new(0, 33),
			(FlexDirection.RowReverse, FlexJustifyContent.FlexStart) => new(100, 0),
			(FlexDirection.ColumnReverse, FlexJustifyContent.FlexStart) => new(0, 100),
			(FlexDirection.RowReverse, FlexJustifyContent.Center) => new(50, 0),
			(FlexDirection.ColumnReverse, FlexJustifyContent.Center) => new(0, 50),
			(FlexDirection.RowReverse, FlexJustifyContent.FlexEnd) => new(0, 0),
			(FlexDirection.ColumnReverse, FlexJustifyContent.FlexEnd) => new(0, 0),
			(FlexDirection.RowReverse, FlexJustifyContent.SpaceAround) => new(25, 0),
			(FlexDirection.ColumnReverse, FlexJustifyContent.SpaceAround) => new(0, 25),
			(FlexDirection.RowReverse, FlexJustifyContent.SpaceBetween) => new(0, 0),
			(FlexDirection.ColumnReverse, FlexJustifyContent.SpaceBetween) => new(0, 0),
			(FlexDirection.RowReverse, FlexJustifyContent.SpaceEvenly) => new(33, 0),
			(FlexDirection.ColumnReverse, FlexJustifyContent.SpaceEvenly) => new(0, 33),
			_ => throw new NotImplementedException()
		}, rowBounds.Position);
	}

	#endregion
}