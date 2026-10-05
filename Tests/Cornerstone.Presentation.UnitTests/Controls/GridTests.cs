#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class GridTests : ScopedTestBase
{
	#region Fields

	private readonly ITestLog output;

	#endregion

	#region Constructors

	public GridTests()
	{
		output = new NullTestLog();
	}

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void AddingChildShouldInvalidateGridAndBeOperational()
	{
		var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("*,Auto") };

		grid.Children.Add(new Decorator { [Grid.ColumnProperty] = 0 });

		var size = new Size(100, 100);
		grid.Measure(size);
		grid.Arrange(new Rect(size));

		CornerstoneTest.IsTrue(grid.IsMeasureValid);
		CornerstoneTest.IsTrue(grid.IsArrangeValid);

		CornerstoneTest.AreEqual(100, grid.Children[0].Bounds.Width);

		grid.Children.Add(new Decorator { Width = 10, Height = 10, [Grid.ColumnProperty] = 1 });

		CornerstoneTest.IsFalse(grid.IsMeasureValid);
		CornerstoneTest.IsFalse(grid.IsArrangeValid);

		grid.Measure(size);
		grid.Arrange(new Rect(size));

		CornerstoneTest.IsTrue(grid.IsMeasureValid);
		CornerstoneTest.IsTrue(grid.IsArrangeValid);

		CornerstoneTest.AreEqual(90, grid.Children[0].Bounds.Width);
		CornerstoneTest.AreEqual(10, grid.Children[1].Bounds.Width);
	}

	[PresentationTestMethod]
	public void AddingColumnShouldInvalidateGrid()
	{
		var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("1*,1*") };

		ChangePropertyAndVerifyMeasureRequested(grid, () => { grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(5))); });
	}

	[PresentationTestMethod]
	public void AddingRowShouldInvalidateGrid()
	{
		var grid = new Grid { RowDefinitions = RowDefinitions.Parse("1*,1*") };

		ChangePropertyAndVerifyMeasureRequested(grid, () => { grid.RowDefinitions.Add(new RowDefinition(new GridLength(5))); });
	}

	[PresentationTestMethod]
	public void CalculatesColspanCorrectly()
	{
		var target = new Grid
		{
			ColumnDefinitions = new ColumnDefinitions
			{
				new ColumnDefinition(GridLength.Auto),
				new ColumnDefinition(new GridLength(4, GridUnitType.Pixel)),
				new ColumnDefinition(GridLength.Auto)
			},
			RowDefinitions = new RowDefinitions
			{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
			},
			Children =
			{
				new Border
				{
					Width = 100,
					Height = 25,
					[Grid.ColumnSpanProperty] = 3
				},
				new Border
				{
					Width = 150,
					Height = 25,
					[Grid.RowProperty] = 1
				},
				new Border
				{
					Width = 50,
					Height = 25,
					[Grid.RowProperty] = 1,
					[Grid.ColumnProperty] = 2
				}
			}
		};

		target.Measure(Size.Infinity);

		// Issue #25 only appears after a second measure
		target.InvalidateMeasure();
		target.Measure(Size.Infinity);

		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(204, 50), target.Bounds.Size);
		CornerstoneTest.AreEqual(150d, target.ColumnDefinitions[0].ActualWidth);
		CornerstoneTest.AreEqual(4d, target.ColumnDefinitions[1].ActualWidth);
		CornerstoneTest.AreEqual(50d, target.ColumnDefinitions[2].ActualWidth);
		CornerstoneTest.AreEqual(new Rect(52, 0, 100, 25), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 25, 150, 25), target.Children[1].Bounds);
		CornerstoneTest.AreEqual(new Rect(154, 25, 50, 25), target.Children[2].Bounds);
	}

	[PresentationTestMethod]
	public void ChangingChildColumnInvalidatesMeasure()
	{
		Border child;
		var target = new Grid
		{
			ColumnDefinitions = new ColumnDefinitions("*,*"),
			Children =
			{
				(child = new Border
				{
					[Grid.ColumnProperty] = 0
				})
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));
		CornerstoneTest.IsTrue(target.IsMeasureValid);

		Grid.SetColumn(child, 1);

		CornerstoneTest.IsFalse(target.IsMeasureValid);
	}

	[PresentationTestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void ChangingColumnMaxWidthShouldInvalidateGrid(bool setUsingPresentationProperty)
	{
		var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("1*,1*") };

		ChangePropertyAndVerifyMeasureRequested(grid, () =>
		{
			if (setUsingPresentationProperty)
			{
				grid.ColumnDefinitions[0][ColumnDefinition.MaxWidthProperty] = 5;
			}
			else
			{
				grid.ColumnDefinitions[0].MaxWidth = 5;
			}
		});
	}

	[PresentationTestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void ChangingColumnMinWidthShouldInvalidateGrid(bool setUsingPresentationProperty)
	{
		var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("1*,1*") };

		ChangePropertyAndVerifyMeasureRequested(grid, () =>
		{
			if (setUsingPresentationProperty)
			{
				grid.ColumnDefinitions[0][ColumnDefinition.MinWidthProperty] = 5;
			}
			else
			{
				grid.ColumnDefinitions[0].MinWidth = 5;
			}
		});
	}

	[PresentationTestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void ChangingColumnWidthShouldInvalidateGrid(bool setUsingPresentationProperty)
	{
		var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("1*,1*") };

		ChangePropertyAndVerifyMeasureRequested(grid, () =>
		{
			if (setUsingPresentationProperty)
			{
				grid.ColumnDefinitions[0][ColumnDefinition.WidthProperty] = new GridLength(5);
			}
			else
			{
				grid.ColumnDefinitions[0].Width = new GridLength(5);
			}
		});
	}

	[PresentationTestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void ChangingRowHeightShouldInvalidateGrid(bool setUsingPresentationProperty)
	{
		var grid = new Grid { RowDefinitions = RowDefinitions.Parse("1*,1*") };

		ChangePropertyAndVerifyMeasureRequested(grid, () =>
		{
			if (setUsingPresentationProperty)
			{
				grid.RowDefinitions[0][RowDefinition.HeightProperty] = new GridLength(5);
			}
			else
			{
				grid.RowDefinitions[0].Height = new GridLength(5);
			}
		});
	}

	[PresentationTestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void ChangingRowMaxHeightShouldInvalidateGrid(bool setUsingPresentationProperty)
	{
		var grid = new Grid { RowDefinitions = RowDefinitions.Parse("1*,1*") };

		ChangePropertyAndVerifyMeasureRequested(grid, () =>
		{
			if (setUsingPresentationProperty)
			{
				grid.RowDefinitions[0][RowDefinition.MaxHeightProperty] = 5;
			}
			else
			{
				grid.RowDefinitions[0].MaxHeight = 5;
			}
		});
	}

	[PresentationTestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void ChangingRowMinHeightShouldInvalidateGrid(bool setUsingPresentationProperty)
	{
		var grid = new Grid { RowDefinitions = RowDefinitions.Parse("1*,1*") };

		ChangePropertyAndVerifyMeasureRequested(grid, () =>
		{
			if (setUsingPresentationProperty)
			{
				grid.RowDefinitions[0][RowDefinition.MinHeightProperty] = 5;
			}
			else
			{
				grid.RowDefinitions[0].MinHeight = 5;
			}
		});
	}

	[PresentationTestMethod]
	public void CollectionChangesAreTracked()
	{
		var grid = CreateGrid(
			("A", new GridLength(20)),
			("A", new GridLength(30)),
			("A", new GridLength(40)),
			(null, new GridLength()));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(40, cd.ActualWidth));

		grid.ColumnDefinitions.RemoveAt(2);

		// NOTE: THIS IS BROKEN IN WPF
		// grid.Measure(new Size(200, 200));
		// grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		// PrintColumnDefinitions(grid);
		// CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(30, cd.ActualWidth));

		grid.ColumnDefinitions.Insert(1, new ColumnDefinition { Width = new GridLength(30), SharedSizeGroup = "A" });

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(30, cd.ActualWidth));

		grid.ColumnDefinitions[1] = new ColumnDefinition { Width = new GridLength(10), SharedSizeGroup = "A" };

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(30, cd.ActualWidth));

		grid.ColumnDefinitions[1] = new ColumnDefinition { Width = new GridLength(50), SharedSizeGroup = "A" };

		// NOTE: THIS IS BROKEN IN WPF
		// grid.Measure(new Size(200, 200));
		// grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		// PrintColumnDefinitions(grid);
		// CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void ColumnDefinitionsCollectionIsReadOnly()
	{
		var grid = CreateGrid(
			("A", new GridLength(50)),
			("A", new GridLength(50)),
			("A", new GridLength(50)),
			("A", new GridLength(50)));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(50, cd.ActualWidth));

		grid.ColumnDefinitions[0] = new ColumnDefinition { Width = new GridLength(25), SharedSizeGroup = "A" };
		grid.ColumnDefinitions[1] = new ColumnDefinition { Width = new GridLength(75), SharedSizeGroup = "B" };
		grid.ColumnDefinitions[2] = new ColumnDefinition { Width = new GridLength(75), SharedSizeGroup = "B" };
		grid.ColumnDefinitions[3] = new ColumnDefinition { Width = new GridLength(25), SharedSizeGroup = "A" };

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(25, cd.ActualWidth));
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => CornerstoneTest.AreEqual(75, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void ColumnDefinitionsCollectionResetSharedSizeGroup()
	{
		var grid = CreateGrid(
			("A", new GridLength(25)),
			("B", new GridLength(75)),
			("B", new GridLength(75)),
			("A", new GridLength(25)));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(25, cd.ActualWidth));
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => CornerstoneTest.AreEqual(75, cd.ActualWidth));

		grid.ColumnDefinitions[0].SharedSizeGroup = null;
		grid.ColumnDefinitions[0].Width = new GridLength(50);
		grid.ColumnDefinitions[1].SharedSizeGroup = null;
		grid.ColumnDefinitions[1].Width = new GridLength(50);
		grid.ColumnDefinitions[2].SharedSizeGroup = null;
		grid.ColumnDefinitions[2].Width = new GridLength(50);
		grid.ColumnDefinitions[3].SharedSizeGroup = null;
		grid.ColumnDefinitions[3].Width = new GridLength(50);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == null), cd => CornerstoneTest.AreEqual(50, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void CorrectGridBoundsWhenChildControlHasDesiredSizeLargerThanAvailableSpace()
	{
		// Issue #2746
		var grid = new Grid
		{
			RowDefinitions = RowDefinitions.Parse("Auto"),
			Children =
			{
				new TestControl
				{
					MeasureSize = new Size(150, 150)
				}
			}
		};

		var parent = new Decorator { Child = grid };

		parent.Measure(new Size(100, 100));
		parent.Arrange(new Rect(grid.DesiredSize));

		CornerstoneTest.AreEqual(new Size(100, 100), grid.Bounds.Size);
	}

	[PresentationTestMethod]
	public void GridControlsWithSpacingWithSpan()
	{
		var target = new Grid
		{
			ColumnSpacing = 20,
			RowDefinitions = RowDefinitions.Parse("Auto"),
			ColumnDefinitions = ColumnDefinitions.Parse("20,20"),
			Children =
			{
				new Border
				{
					Height = 100,
					[Grid.ColumnSpanProperty] = 2
				}
			}
		};
		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Rect(0, 0, 60, 100), target.Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 0, 60, 100), target.Children[0].Bounds);
	}

	[PresentationTestMethod]
	public void GridControlsWithSpacingWithSpanAndSharedSize()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		var grid1 = new Grid
		{
			[Grid.RowProperty] = 0,
			RowDefinitions = RowDefinitions.Parse("Auto,*,Auto,Auto"),
			ColumnDefinitions =
			[
				new ColumnDefinition(GridLength.Auto),
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Auto),
				new ColumnDefinition(GridLength.Auto)
				{
					SharedSizeGroup = "C3"
				}
			],
			RowSpacing = 10,
			ColumnSpacing = 10,
			Children =
			{
				new ScrollViewer
				{
					[Grid.RowProperty] = 0,
					[Grid.ColumnProperty] = 0,
					[Grid.RowSpanProperty] = 3,
					[Grid.ColumnSpanProperty] = 3,
					Content = new TextBlock
					{
						FontSize = 10,
						Text = @"0: 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890
1: 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890
2: 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890
3: 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890
4: 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890
5: 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890
6: 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890
7: 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890
8: 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890
9: 1234567890 1234567890 1234567890 1234567890 1234567890 1234567890"
					}
				},
				new Button
				{
					[Grid.RowProperty] = 3,
					[Grid.ColumnProperty] = 0,
					Width = 100,
					Height = 40
				},
				new Button
				{
					[Grid.RowProperty] = 3,
					[Grid.ColumnProperty] = 2,
					Width = 100,
					Height = 40
				},
				new Button
				{
					[Grid.RowProperty] = 0,
					[Grid.ColumnProperty] = 3,
					Width = 100,
					Height = 40
				},
				new Button
				{
					[Grid.RowProperty] = 2,
					[Grid.ColumnProperty] = 3,
					Width = 100,
					Height = 40
				}
			}
		};

		var grid2 = new Grid
		{
			[Grid.RowProperty] = 1,
			ColumnDefinitions =
			[
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Auto)
				{
					SharedSizeGroup = "C3"
				}
			],
			Children =
			{
				new TextBlock
				{
					[Grid.ColumnProperty] = 1,
					Height = 20,
					Width = 100,
					Text = "1234567890"
				}
			}
		};

		var root = new Grid
		{
			[Grid.IsSharedSizeScopeProperty] = true,
			RowDefinitions = RowDefinitions.Parse("*,Auto"),
			RowSpacing = 10,
			Margin = new Thickness(10)
		};
		root.Children.Add(grid1);
		root.Children.Add(grid2);
		root.Measure(new Size(550, 240));
		root.Arrange(new Rect(new Point(), new Point(550, 240)));

		CornerstoneTest.AreEqual(new Rect(0, 0, 420, 140), grid1.Children[0].Bounds);
		CornerstoneTest.AreEqual(grid1.Children[4].Bounds.Left, grid2.Children[0].Bounds.Left);
		CornerstoneTest.AreEqual(grid1.Children[4].Bounds.Width, grid2.Children[0].Bounds.Width);
	}

	[PresentationTestMethod]
	public void GridGridLengthSameSizeAuto()
	{
		var grid = CreateGrid(
			(null, new GridLength(0, GridUnitType.Auto)),
			(null, new GridLength(0, GridUnitType.Auto)),
			(null, new GridLength(0, GridUnitType.Auto)),
			(null, new GridLength(0, GridUnitType.Auto)));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, false);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == null), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void GridGridLengthSameSizePixel0()
	{
		var grid = CreateGrid(
			(null, new GridLength()),
			(null, new GridLength()),
			(null, new GridLength()),
			(null, new GridLength()));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, false);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == null), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void GridGridLengthSameSizePixel50()
	{
		var grid = CreateGrid(
			(null, new GridLength(50)),
			(null, new GridLength(50)),
			(null, new GridLength(50)),
			(null, new GridLength(50)));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, false);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == null), cd => CornerstoneTest.AreEqual(50, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void GridGridLengthSameSizeStar()
	{
		var grid = CreateGrid(
			(null, new GridLength(1, GridUnitType.Star)),
			(null, new GridLength(1, GridUnitType.Star)),
			(null, new GridLength(1, GridUnitType.Star)),
			(null, new GridLength(1, GridUnitType.Star)));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, false);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == null), cd => CornerstoneTest.AreEqual(50, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void GridWithColumnSpacingAndColumnDefinitionsUnset()
	{
		var target = new Grid
		{
			Height = 300,
			Width = 100,
			ColumnSpacing = 10,
			RowDefinitions = RowDefinitions.Parse("Auto,*"), //Set RowDefinitions to avoid 
			Children =
			{
				new Border
				{
					[Grid.RowProperty] = 0,
					Height = 80,
					Margin = new Thickness(10)
				},
				new Border
				{
					[Grid.RowProperty] = 1,
					Margin = new Thickness(20)
				}
			}
		};
		target.Measure(new Size(100, 300));
		target.Arrange(new Rect(target.DesiredSize));
		CornerstoneTest.AreEqual(new Rect(10, 10, 80, 80), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(20, 120, 60, 160), target.Children[1].Bounds);
	}

	[PresentationTestMethod]
	public void LayoutEmptyColumnRowLayoutLikeANormalPanel()
	{
		// Arrange & Action
		var grid = GridMock.New(arrange: new Size(600, 200));

		// Assert
		GridAssert.ChildrenWidth(grid, 600);
		GridAssert.ChildrenHeight(grid, 200);
	}

	[PresentationTestMethod]
	public void LayoutMixPixelStarRowColumnBoundsCorrect()
	{
		// Arrange & Action
		var rowGrid = GridMock.New(new RowDefinitions("1*,2*,150"), 600);
		var columnGrid = GridMock.New(new ColumnDefinitions("1*,2*,150"), 600);

		// Assert
		GridAssert.ChildrenHeight(rowGrid, 150, 300, 150);
		GridAssert.ChildrenWidth(columnGrid, 150, 300, 150);
	}

	[PresentationTestMethod]
	public void LayoutPixelRowColumnBoundsCorrect()
	{
		// Arrange & Action
		var rowGrid = GridMock.New(new RowDefinitions("100,200,300"));
		var columnGrid = GridMock.New(new ColumnDefinitions("50,100,150"));

		// Assert
		GridAssert.ChildrenHeight(rowGrid, 100, 200, 300);
		GridAssert.ChildrenWidth(columnGrid, 50, 100, 150);
	}

	[PresentationTestMethod]
	public void LayoutStarRowColumnBoundsCorrect()
	{
		// Arrange & Action
		var rowGrid = GridMock.New(new RowDefinitions("1*,2*,3*"), 600);
		var columnGrid = GridMock.New(new ColumnDefinitions("*,*,2*"), 600);

		// Assert
		GridAssert.ChildrenHeight(rowGrid, 100, 200, 300);
		GridAssert.ChildrenWidth(columnGrid, 150, 150, 300);
	}

	[PresentationTestMethod]
	public void LayoutStarRowColumnWithMaxLengthBoundsCorrect()
	{
		// Arrange & Action
		var rowGrid = GridMock.New(new RowDefinitions
		{
			new RowDefinition(1, GridUnitType.Star) { MaxHeight = 200 },
			new RowDefinition(1, GridUnitType.Star),
			new RowDefinition(1, GridUnitType.Star)
		}, 800);
		var columnGrid = GridMock.New(new ColumnDefinitions
		{
			new ColumnDefinition(1, GridUnitType.Star) { MaxWidth = 200 },
			new ColumnDefinition(1, GridUnitType.Star),
			new ColumnDefinition(1, GridUnitType.Star)
		}, 800);

		// Assert
		GridAssert.ChildrenHeight(rowGrid, 200, 300, 300);
		GridAssert.ChildrenWidth(columnGrid, 200, 300, 300);
	}

	[PresentationTestMethod]
	public void LayoutStarRowColumnWithMinLengthBoundsCorrect()
	{
		// Arrange & Action
		var rowGrid = GridMock.New(new RowDefinitions
		{
			new RowDefinition(1, GridUnitType.Star) { MinHeight = 200 },
			new RowDefinition(1, GridUnitType.Star),
			new RowDefinition(1, GridUnitType.Star)
		}, 300);
		var columnGrid = GridMock.New(new ColumnDefinitions
		{
			new ColumnDefinition(1, GridUnitType.Star) { MinWidth = 200 },
			new ColumnDefinition(1, GridUnitType.Star),
			new ColumnDefinition(1, GridUnitType.Star)
		}, 300);

		// Assert
		GridAssert.ChildrenHeight(rowGrid, 200, 50, 50);
		GridAssert.ChildrenWidth(columnGrid, 200, 50, 50);
	}

	[PresentationTestMethod]
	public void MovingDefinitionBetweenGridsMovesItsSharedSizeRegistration()
	{
		var shared = new ColumnDefinition { Width = GridLength.Auto, SharedSizeGroup = "A" };
		var source = new Grid();
		source.ColumnDefinitions.Add(shared);
		source.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		source.Children.Add(new Border { Width = 50, Height = 10 });

		var sourcePartner = new Grid();
		sourcePartner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, SharedSizeGroup = "A" });
		sourcePartner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		var target = new Grid();
		target.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		var targetPartner = new Grid();
		targetPartner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, SharedSizeGroup = "A" });
		targetPartner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		targetPartner.Children.Add(new Border { Width = 20, Height = 10 });

		var sourceScope = new StackPanel
		{
			[Grid.IsSharedSizeScopeProperty] = true,
			Children = { source, sourcePartner }
		};
		var targetScope = new StackPanel
		{
			[Grid.IsSharedSizeScopeProperty] = true,
			Children = { target, targetPartner }
		};
		var root = new TestRoot(new StackPanel { Children = { sourceScope, targetScope } });

		root.ExecuteInitialLayoutPass();
		root.LayoutManager.ExecuteLayoutPass();
		CornerstoneTest.AreEqual(50, sourcePartner.ColumnDefinitions[0].ActualWidth);
		CornerstoneTest.AreEqual(20, targetPartner.ColumnDefinitions[0].ActualWidth);

		source.ColumnDefinitions.Remove(shared);
		target.ColumnDefinitions.Insert(0, shared);
		root.LayoutManager.ExecuteLayoutPass();
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.Same(target, shared.Parent);
		CornerstoneTest.AreEqual(0, sourcePartner.ColumnDefinitions[0].ActualWidth);
		CornerstoneTest.AreEqual(20, target.ColumnDefinitions[0].ActualWidth);
	}

	[PresentationTestMethod]
	public void ReassigningTheSameDefinitionCollectionIsInert()
	{
		var grid = new Grid();
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, SharedSizeGroup = "A" });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		grid.Children.Add(new Border { Width = 50, Height = 10 });

		var other = new Grid();
		other.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, SharedSizeGroup = "A" });
		other.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		var scope = new StackPanel
		{
			[Grid.IsSharedSizeScopeProperty] = true,
			Children = { grid, other }
		};
		var root = new TestRoot(scope);

		root.ExecuteInitialLayoutPass();
		root.LayoutManager.ExecuteLayoutPass();
		CornerstoneTest.AreEqual(50, other.ColumnDefinitions[0].ActualWidth);

		var definitions = grid.ColumnDefinitions;
		grid.ColumnDefinitions = definitions;
		root.LayoutManager.ExecuteLayoutPass();
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.Same(definitions, grid.ColumnDefinitions);
		CornerstoneTest.AreEqual(50, other.ColumnDefinitions[0].ActualWidth);
	}

	[PresentationTestMethod]
	public void RemovingChildShouldInvalidateGridAndBeOperational()
	{
		var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("*,Auto") };

		grid.Children.Add(new Decorator { [Grid.ColumnProperty] = 0 });
		grid.Children.Add(new Decorator { Width = 10, Height = 10, [Grid.ColumnProperty] = 1 });

		var size = new Size(100, 100);
		grid.Measure(size);
		grid.Arrange(new Rect(size));

		CornerstoneTest.IsTrue(grid.IsMeasureValid);
		CornerstoneTest.IsTrue(grid.IsArrangeValid);

		CornerstoneTest.AreEqual(90, grid.Children[0].Bounds.Width);
		CornerstoneTest.AreEqual(10, grid.Children[1].Bounds.Width);

		grid.Children.RemoveAt(1);

		CornerstoneTest.IsFalse(grid.IsMeasureValid);
		CornerstoneTest.IsFalse(grid.IsArrangeValid);

		grid.Measure(size);
		grid.Arrange(new Rect(size));

		CornerstoneTest.IsTrue(grid.IsMeasureValid);
		CornerstoneTest.IsTrue(grid.IsArrangeValid);

		CornerstoneTest.AreEqual(100, grid.Children[0].Bounds.Width);
	}

	[PresentationTestMethod]
	public void RemovingColumnShouldInvalidateGrid()
	{
		var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("1*,1*") };

		ChangePropertyAndVerifyMeasureRequested(grid, () => { grid.ColumnDefinitions.RemoveAt(0); });
	}

	[PresentationTestMethod]
	public void RemovingDefinitionDetachesItFromTheGrid()
	{
		var shared = new ColumnDefinition { Width = GridLength.Auto, SharedSizeGroup = "A" };
		var grid = new Grid();
		grid.ColumnDefinitions.Add(shared);
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		grid.Children.Add(new Border { Width = 50, Height = 10 });

		var other = new Grid();
		other.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, SharedSizeGroup = "A" });
		other.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		var scope = new StackPanel
		{
			[Grid.IsSharedSizeScopeProperty] = true,
			Children = { grid, other }
		};
		var root = new TestRoot(scope);

		root.ExecuteInitialLayoutPass();
		root.LayoutManager.ExecuteLayoutPass();
		CornerstoneTest.AreEqual(50, other.ColumnDefinitions[0].ActualWidth);

		grid.ColumnDefinitions.Remove(shared);
		root.LayoutManager.ExecuteLayoutPass();
		root.LayoutManager.ExecuteLayoutPass();
		CornerstoneTest.AreEqual(0, other.ColumnDefinitions[0].ActualWidth);
		CornerstoneTest.IsNull(shared.Parent);

		// A definition that has left the grid must no longer see the grid's scope.
		shared.SharedSizeGroup = null;
		shared.SharedSizeGroup = "A";
		root.LayoutManager.ExecuteLayoutPass();
		root.LayoutManager.ExecuteLayoutPass();
		CornerstoneTest.AreEqual(0, other.ColumnDefinitions[0].ActualWidth);
	}

	[PresentationTestMethod]
	public void RemovingRowShouldInvalidateGrid()
	{
		var grid = new Grid { RowDefinitions = RowDefinitions.Parse("1*,1*") };

		ChangePropertyAndVerifyMeasureRequested(grid, () => { grid.RowDefinitions.RemoveAt(0); });
	}

	[PresentationTestMethod]
	public void ReplacingColumnsShouldInvalidateGrid()
	{
		var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("1*,1*") };

		ChangePropertyAndVerifyMeasureRequested(grid, () => { grid.ColumnDefinitions = ColumnDefinitions.Parse("2*,1*"); });
	}

	[PresentationTestMethod]
	public void ReplacingDefinitionCollectionReleasesItsSharedSizeGroup()
	{
		// The outgoing definitions are no longer reachable from the grid, so nothing resets their
		// measured minimum. Left registered, they keep the group pinned at whatever size they
		// last contributed.
		var grid = new Grid();
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, SharedSizeGroup = "A" });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		grid.Children.Add(new Border { Width = 50, Height = 10 });

		var other = new Grid();
		other.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, SharedSizeGroup = "A" });
		other.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		var scope = new StackPanel
		{
			[Grid.IsSharedSizeScopeProperty] = true,
			Children = { grid, other }
		};
		var root = new TestRoot(scope);

		root.ExecuteInitialLayoutPass();

		// Shared groups validate after layout and apply any resulting invalidation on the next pass.
		root.LayoutManager.ExecuteLayoutPass();
		CornerstoneTest.AreEqual(50, other.ColumnDefinitions[0].ActualWidth);

		grid.ColumnDefinitions = new ColumnDefinitions
		{
			new ColumnDefinition { Width = GridLength.Auto },
			new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
		};
		root.LayoutManager.ExecuteLayoutPass();
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(0, other.ColumnDefinitions[0].ActualWidth);
	}

	[PresentationTestMethod]
	public void ReplacingRowsShouldInvalidateGrid()
	{
		var grid = new Grid { RowDefinitions = RowDefinitions.Parse("1*,1*") };

		ChangePropertyAndVerifyMeasureRequested(grid, () => { grid.RowDefinitions = RowDefinitions.Parse("2*,1*"); });
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizeAuto()
	{
		var grid = CreateGrid(
			("A", new GridLength(0, GridUnitType.Auto)),
			("A", new GridLength(0, GridUnitType.Auto)),
			("A", new GridLength(0, GridUnitType.Auto)),
			("A", new GridLength(0, GridUnitType.Auto)));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizeAutoFirstAndLastColumn0()
	{
		var grid = CreateGrid(
			(null, new GridLength()),
			("A", new GridLength(0, GridUnitType.Auto)),
			("A", new GridLength(0, GridUnitType.Auto)),
			("A", new GridLength(0, GridUnitType.Auto)),
			("A", new GridLength(0, GridUnitType.Auto)),
			(null, new GridLength()));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizeAutoFirstAndLastColumn0TwoGroups()
	{
		var grid = CreateGrid(
			(null, new GridLength()),
			("A", new GridLength(0, GridUnitType.Auto)),
			("B", new GridLength(0, GridUnitType.Auto)),
			("B", new GridLength(0, GridUnitType.Auto)),
			("A", new GridLength(0, GridUnitType.Auto)),
			(null, new GridLength()));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizeAutoFirstColumn0()
	{
		var grid = CreateGrid(
			(null, new GridLength()),
			("A", new GridLength(0, GridUnitType.Auto)),
			("A", new GridLength(0, GridUnitType.Auto)),
			("A", new GridLength(0, GridUnitType.Auto)),
			("A", new GridLength(0, GridUnitType.Auto)));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizeAutoFirstColumn0TwoGroups()
	{
		var grid = CreateGrid(
			(null, new GridLength()),
			("A", new GridLength(0, GridUnitType.Auto)),
			("B", new GridLength(0, GridUnitType.Auto)),
			("B", new GridLength(0, GridUnitType.Auto)),
			("A", new GridLength(0, GridUnitType.Auto)));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizeAutoLastColumn0()
	{
		var grid = CreateGrid(
			("A", new GridLength(0, GridUnitType.Auto)),
			("A", new GridLength(0, GridUnitType.Auto)),
			("A", new GridLength(0, GridUnitType.Auto)),
			("A", new GridLength(0, GridUnitType.Auto)),
			(null, new GridLength()));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizeAutoLastColumn0TwoGroups()
	{
		var grid = CreateGrid(
			("A", new GridLength(0, GridUnitType.Auto)),
			("B", new GridLength(0, GridUnitType.Auto)),
			("B", new GridLength(0, GridUnitType.Auto)),
			("A", new GridLength(0, GridUnitType.Auto)),
			(null, new GridLength()));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizeAutoTwoGroups()
	{
		var grid = CreateGrid(
			("A", new GridLength(0, GridUnitType.Auto)),
			("B", new GridLength(0, GridUnitType.Auto)),
			("B", new GridLength(0, GridUnitType.Auto)),
			("A", new GridLength(0, GridUnitType.Auto)));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizePixel0()
	{
		var grid = CreateGrid(
			("A", new GridLength()),
			("A", new GridLength()),
			("A", new GridLength()),
			("A", new GridLength()));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizePixel0FirstAndLastColumn0()
	{
		var grid = CreateGrid(
			(null, new GridLength()),
			("A", new GridLength()),
			("A", new GridLength()),
			("A", new GridLength()),
			("A", new GridLength()),
			(null, new GridLength()));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizePixel0FirstAndLastColumn0TwoGroups()
	{
		var grid = CreateGrid(
			(null, new GridLength()),
			("A", new GridLength()),
			("B", new GridLength()),
			("B", new GridLength()),
			("A", new GridLength()),
			(null, new GridLength()));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizePixel0FirstColumn0()
	{
		var grid = CreateGrid(
			(null, new GridLength()),
			("A", new GridLength()),
			("A", new GridLength()),
			("A", new GridLength()),
			("A", new GridLength()));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizePixel0FirstColumn0TwoGroups()
	{
		var grid = CreateGrid(
			(null, new GridLength()),
			("A", new GridLength()),
			("B", new GridLength()),
			("B", new GridLength()),
			("A", new GridLength()));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizePixel0LastColumn0()
	{
		var grid = CreateGrid(
			("A", new GridLength()),
			("A", new GridLength()),
			("A", new GridLength()),
			("A", new GridLength()),
			(null, new GridLength()));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizePixel0LastColumn0TwoGroups()
	{
		var grid = CreateGrid(
			("A", new GridLength()),
			("B", new GridLength()),
			("B", new GridLength()),
			("A", new GridLength()),
			(null, new GridLength()));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizePixel0TwoGroups()
	{
		var grid = CreateGrid(
			("A", new GridLength()),
			("B", new GridLength()),
			("B", new GridLength()),
			("A", new GridLength()));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizePixel50()
	{
		var grid = CreateGrid(
			("A", new GridLength(50)),
			("A", new GridLength(50)),
			("A", new GridLength(50)),
			("A", new GridLength(50)));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(50, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizePixel50FirstAndLastColumn0()
	{
		var grid = CreateGrid(
			(null, new GridLength()),
			("A", new GridLength(50)),
			("A", new GridLength(50)),
			("A", new GridLength(50)),
			("A", new GridLength(50)),
			(null, new GridLength()));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(50, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizePixel50FirstAndLastColumn0TwoGroups()
	{
		var grid = CreateGrid(
			(null, new GridLength()),
			("A", new GridLength(25)),
			("B", new GridLength(75)),
			("B", new GridLength(75)),
			("A", new GridLength(25)),
			(null, new GridLength()));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(25, cd.ActualWidth));
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => CornerstoneTest.AreEqual(75, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizePixel50FirstColumn0()
	{
		var grid = CreateGrid(
			(null, new GridLength()),
			("A", new GridLength(50)),
			("A", new GridLength(50)),
			("A", new GridLength(50)),
			("A", new GridLength(50)));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(50, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizePixel50FirstColumn0TwoGroups()
	{
		var grid = CreateGrid(
			(null, new GridLength()),
			("A", new GridLength(25)),
			("B", new GridLength(75)),
			("B", new GridLength(75)),
			("A", new GridLength(25)));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(25, cd.ActualWidth));
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => CornerstoneTest.AreEqual(75, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizePixel50LastColumn0()
	{
		var grid = CreateGrid(
			("A", new GridLength(50)),
			("A", new GridLength(50)),
			("A", new GridLength(50)),
			("A", new GridLength(50)),
			(null, new GridLength()));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(50, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizePixel50LastColumn0TwoGroups()
	{
		var grid = CreateGrid(
			("A", new GridLength(25)),
			("B", new GridLength(75)),
			("B", new GridLength(75)),
			("A", new GridLength(25)),
			(null, new GridLength()));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(25, cd.ActualWidth));
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => CornerstoneTest.AreEqual(75, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizePixel50TwoGroups()
	{
		var grid = CreateGrid(
			("A", new GridLength(25)),
			("B", new GridLength(75)),
			("B", new GridLength(75)),
			("A", new GridLength(25)));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(25, cd.ActualWidth));
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => CornerstoneTest.AreEqual(75, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizeStar()
	{
		var grid = CreateGrid(
			("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			("A", new GridLength(1, GridUnitType.Star))); // Star sizing is treated as Auto, 1 is ignored

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizeStarFirstAndLastColumn0()
	{
		var grid = CreateGrid(
			(null, new GridLength()),
			("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			(null, new GridLength()));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizeStarFirstAndLastColumn0TwoGroups()
	{
		var grid = CreateGrid(
			(null, new GridLength()),
			("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			("B", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			("B", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			(null, new GridLength()));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizeStarFirstColumn0()
	{
		var grid = CreateGrid(
			(null, new GridLength()),
			("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			("A", new GridLength(1, GridUnitType.Star))); // Star sizing is treated as Auto, 1 is ignored

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizeStarFirstColumn0TwoGroups()
	{
		var grid = CreateGrid(
			(null, new GridLength()),
			("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			("B", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			("B", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			("A", new GridLength(1, GridUnitType.Star))); // Star sizing is treated as Auto, 1 is ignored

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizeStarLastColumn0()
	{
		var grid = CreateGrid(
			("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			(null, new GridLength()));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizeStarLastColumn0TwoGroups()
	{
		var grid = CreateGrid(
			("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			("B", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			("B", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			(null, new GridLength()));

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGridGridLengthSameSizeStarTwoGroups()
	{
		var grid = CreateGrid(
			("A", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			("B", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			("B", new GridLength(1, GridUnitType.Star)), // Star sizing is treated as Auto, 1 is ignored
			("A", new GridLength(1, GridUnitType.Star))); // Star sizing is treated as Auto, 1 is ignored

		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(200, 200));
		grid.Arrange(new Rect(new Point(), new Point(200, 200)));
		PrintColumnDefinitions(grid);
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "B"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGroupIsRegisteredForDefinitionsAssignedAsACollection()
	{
		// Definitions supplied through the ColumnDefinitions setter - an object initializer, a
		// shared resource, or ColumnDefinitions="Auto,*" - are already in the collection when the
		// grid claims it, so they never pass through the collection-changed handler that joins
		// them to the parent tree.
		var grids = new[]
		{
			new Grid
			{
				ColumnDefinitions = new ColumnDefinitions
				{
					new ColumnDefinition { Width = GridLength.Auto, SharedSizeGroup = "A" },
					new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
				}
			},
			new Grid
			{
				ColumnDefinitions = new ColumnDefinitions
				{
					new ColumnDefinition { Width = GridLength.Auto, SharedSizeGroup = "A" },
					new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
				}
			}
		};
		grids[0].Children.Add(new Border { Width = 50, Height = 10 });

		var scope = new StackPanel
		{
			[Grid.IsSharedSizeScopeProperty] = true,
			Children = { grids[0], grids[1] }
		};
		var root = new TestRoot(scope);

		root.ExecuteInitialLayoutPass();

		// Shared groups validate after layout and apply any resulting invalidation on the next pass.
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(50, grids[0].ColumnDefinitions[0].ActualWidth);
		CornerstoneTest.AreEqual(50, grids[1].ColumnDefinitions[0].ActualWidth);
	}

	[PresentationTestMethod]
	public void SharedSizeGroupIsRegisteredForRowDefinitionsAssignedAsACollection()
	{
		var grids = new[]
		{
			new Grid
			{
				RowDefinitions = new RowDefinitions
				{
					new RowDefinition { Height = GridLength.Auto, SharedSizeGroup = "A" },
					new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
				}
			},
			new Grid
			{
				RowDefinitions = new RowDefinitions
				{
					new RowDefinition { Height = GridLength.Auto, SharedSizeGroup = "A" },
					new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
				}
			}
		};
		grids[0].Children.Add(new Border { Width = 10, Height = 50 });

		var scope = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			[Grid.IsSharedSizeScopeProperty] = true,
			Children = { grids[0], grids[1] }
		};
		var root = new TestRoot(scope);

		root.ExecuteInitialLayoutPass();
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(50, grids[0].RowDefinitions[0].ActualHeight);
		CornerstoneTest.AreEqual(50, grids[1].RowDefinitions[0].ActualHeight);
	}

	[PresentationTestMethod]
	public void SharedSizeGroupShrinksWhenContentIsHidden()
	{
		var grids = new[]
		{
			CreateGrid(("A", GridLength.Auto)),
			CreateGrid(("A", GridLength.Auto))
		};
		var content = new Border
		{
			Width = 50,
			IsVisible = false
		};
		grids[1].Children.Add(content);

		var scope = new StackPanel
		{
			[Grid.IsSharedSizeScopeProperty] = true,
			Children =
			{
				grids[0],
				grids[1]
			}
		};
		var root = new TestRoot(scope);

		void ExecuteSharedSizeLayoutPass()
		{
			// Shared groups validate after layout and apply any resulting invalidation on the next pass.
			root.LayoutManager.ExecuteLayoutPass();
			root.LayoutManager.ExecuteLayoutPass();
		}

		root.ExecuteInitialLayoutPass();
		CornerstoneTest.All(grids, grid => CornerstoneTest.AreEqual(0, grid.ColumnDefinitions[0].ActualWidth));

		content.IsVisible = true;
		ExecuteSharedSizeLayoutPass();
		CornerstoneTest.All(grids, grid => CornerstoneTest.AreEqual(50, grid.ColumnDefinitions[0].ActualWidth));

		content.IsVisible = false;
		ExecuteSharedSizeLayoutPass();
		CornerstoneTest.All(grids, grid => CornerstoneTest.AreEqual(0, grid.ColumnDefinitions[0].ActualWidth));
	}

	[PresentationTestMethod]
	public void SharedSizeGroupShrinksWhenParticipantUsesCyclicMeasurePath()
	{
		// A grid mixing an auto-column/star-row cell with a star-column/auto-row cell cannot
		// resolve stars in either direction up front, so Grid measures it through its cyclic
		// dependency path. That path saves definition min sizes before the repeated measure and
		// restores them afterwards. Saving the effective min size folds the group minimum into
		// the definition's own contribution, which then reclassifies it as a long pole - and
		// long poles are deliberately never remeasured, so the group stays pinned open.
		var cyclicGrid = CreateGrid(("A", GridLength.Auto), (null, new GridLength(1, GridUnitType.Star)));
		cyclicGrid.Height = 100; // star rows collapse to auto under an infinite constraint.
		cyclicGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		cyclicGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

		var autoColumnStarRowChild = new Border { Width = 10, Height = 10 };
		Grid.SetColumn(autoColumnStarRowChild, 0);
		Grid.SetRow(autoColumnStarRowChild, 1);

		var starColumnAutoRowChild = new Border { Width = 10, Height = 10 };
		Grid.SetColumn(starColumnAutoRowChild, 1);
		Grid.SetRow(starColumnAutoRowChild, 0);

		cyclicGrid.Children.Add(autoColumnStarRowChild);
		cyclicGrid.Children.Add(starColumnAutoRowChild);

		var plainGrid = CreateGrid(("A", GridLength.Auto), (null, new GridLength(1, GridUnitType.Star)));
		var wideContent = new Border { Width = 50, Height = 10 };
		plainGrid.Children.Add(wideContent);

		var scope = new StackPanel
		{
			[Grid.IsSharedSizeScopeProperty] = true,
			Children = { cyclicGrid, plainGrid }
		};
		var root = new TestRoot(scope);

		void ExecuteSharedSizeLayoutPass()
		{
			// Shared groups validate after layout and apply any resulting invalidation on the next pass.
			root.LayoutManager.ExecuteLayoutPass();
			root.LayoutManager.ExecuteLayoutPass();
		}

		root.ExecuteInitialLayoutPass();
		ExecuteSharedSizeLayoutPass();
		CornerstoneTest.AreEqual(50, cyclicGrid.ColumnDefinitions[0].ActualWidth);
		CornerstoneTest.AreEqual(50, plainGrid.ColumnDefinitions[0].ActualWidth);

		wideContent.IsVisible = false;
		ExecuteSharedSizeLayoutPass();
		CornerstoneTest.AreEqual(10, cyclicGrid.ColumnDefinitions[0].ActualWidth);
		CornerstoneTest.AreEqual(10, plainGrid.ColumnDefinitions[0].ActualWidth);
	}

	[PresentationTestMethod]
	public void ShouldGridControlsWithSpacing()
	{
		var target = new Grid
		{
			RowSpacing = 10,
			ColumnSpacing = 10,
			RowDefinitions = RowDefinitions.Parse("100,100"),
			ColumnDefinitions = ColumnDefinitions.Parse("100,100"),
			Children =
			{
				new Border(),
				new Border { [Grid.ColumnProperty] = 1 },
				new Border { [Grid.RowProperty] = 1 },
				new Border { [Grid.RowProperty] = 1, [Grid.ColumnProperty] = 1 }
			}
		};
		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Rect(0, 0, 210, 210), target.Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(110, 0, 100, 100), target.Children[1].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 110, 100, 100), target.Children[2].Bounds);
		CornerstoneTest.AreEqual(new Rect(110, 110, 100, 100), target.Children[3].Bounds);
	}

	[PresentationTestMethod]
	public void ShouldGridControlsWithSpacingComplicated()
	{
		var target = new Grid
		{
			Width = 200,
			Height = 200,
			RowSpacing = 10,
			ColumnSpacing = 10,
			RowDefinitions = RowDefinitions.Parse("50,*,2*,Auto"),
			ColumnDefinitions = ColumnDefinitions.Parse("50,*,2*,Auto"),
			Children =
			{
				new Border(),
				new Border { [Grid.RowProperty] = 1, [Grid.ColumnProperty] = 1 },
				new Border { [Grid.RowProperty] = 2, [Grid.ColumnProperty] = 2 },
				new Border { [Grid.RowProperty] = 3, [Grid.ColumnProperty] = 3, Width = 30, Height = 30 }
			}
		};
		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Rect(0, 0, 200, 200), target.Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 0, 50, 50), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(60, 60, 30, 30), target.Children[1].Bounds);
		CornerstoneTest.AreEqual(new Rect(100, 100, 60, 60), target.Children[2].Bounds);
		CornerstoneTest.AreEqual(new Rect(170, 170, 30, 30), target.Children[3].Bounds);
	}

	[PresentationTestMethod]
	public void ShouldGridControlsWithSpacingOverflow()
	{
		var target = new Grid
		{
			Width = 100,
			Height = 100,
			ColumnSpacing = 20,
			RowSpacing = 20,
			ColumnDefinitions = ColumnDefinitions.Parse("30,*,*,Auto"),
			RowDefinitions = RowDefinitions.Parse("30,*,*,Auto"),
			Children =
			{
				new Border(),
				new Border { [Grid.RowProperty] = 1, [Grid.ColumnProperty] = 1 },
				new Border { [Grid.RowProperty] = 2, [Grid.ColumnProperty] = 2 },
				new Border { [Grid.RowProperty] = 3, [Grid.ColumnProperty] = 3, Width = 30, Height = 30 }
			}
		};
		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), target.Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 0, 30, 30), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(50, 50, 0, 0), target.Children[1].Bounds);
		CornerstoneTest.AreEqual(new Rect(70, 70, 0, 0), target.Children[2].Bounds);
		CornerstoneTest.AreEqual(new Rect(90, 90, 30, 30), target.Children[3].Bounds);
	}

	[PresentationTestMethod]
	public void ShouldGridControlsWithSpacingOverflow2()
	{
		var target = new Grid
		{
			Height = 100,
			ColumnSpacing = 20,
			ColumnDefinitions = ColumnDefinitions.Parse("*,Auto"),
			Children =
			{
				new Border { Width = 60 },
				new Border { [Grid.ColumnProperty] = 1, Width = 60 }
			}
		};
		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Rect(0, 0, 100, 100), target.Bounds);
		CornerstoneTest.AreEqual(new Rect(-20, 0, 60, 100), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(40, 0, 60, 100), target.Children[1].Bounds);
	}

	[PresentationTestMethod]
	public void SizeGroupChangesAreTracked()
	{
		var grids = new[]
		{
			CreateGrid((null, new GridLength(0, GridUnitType.Auto)), (null, new GridLength())),
			CreateGrid(("A", new GridLength(30)), (null, new GridLength()))
		};
		var scope = new Grid();
		foreach (var xgrids in grids)
		{
			scope.Children.Add(xgrids);
		}

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		root.Measure(new Size(50, 50));
		root.Arrange(new Rect(new Point(), new Point(50, 50)));
		PrintColumnDefinitions(grids[0]);
		CornerstoneTest.AreEqual(0, grids[0].ColumnDefinitions[0].ActualWidth);

		grids[0].ColumnDefinitions[0].SharedSizeGroup = "A";

		root.Measure(new Size(51, 51));
		root.Arrange(new Rect(new Point(), new Point(51, 51)));
		PrintColumnDefinitions(grids[0]);
		CornerstoneTest.AreEqual(30, grids[0].ColumnDefinitions[0].ActualWidth);

		grids[0].ColumnDefinitions[0].SharedSizeGroup = null;

		root.Measure(new Size(52, 52));
		root.Arrange(new Rect(new Point(), new Point(52, 52)));
		PrintColumnDefinitions(grids[0]);
		CornerstoneTest.AreEqual(0, grids[0].ColumnDefinitions[0].ActualWidth);
	}

	[PresentationTestMethod]
	public void SizeGroupDefinitionResizesAreTracked()
	{
		var grids = new[]
		{
			CreateGrid(("A", new GridLength(5, GridUnitType.Pixel)), (null, new GridLength())),
			CreateGrid(("A", new GridLength(5, GridUnitType.Pixel)), (null, new GridLength()))
		};
		var scope = new Grid();
		foreach (var xgrids in grids)
		{
			scope.Children.Add(xgrids);
		}

		var rootGrid = new Grid();
		rootGrid.UseLayoutRounding = false;
		rootGrid.SetValue(Grid.IsSharedSizeScopeProperty, true);
		rootGrid.Children.Add(scope);

		var root = new TestRoot(rootGrid)
		{
			Width = 50,
			Height = 50
		};

		root.LayoutManager.ExecuteInitialLayoutPass();

		PrintColumnDefinitions(grids[0]);
		CornerstoneTest.AreEqual(5, grids[0].ColumnDefinitions[0].ActualWidth);
		CornerstoneTest.AreEqual(5, grids[1].ColumnDefinitions[0].ActualWidth);

		grids[0].ColumnDefinitions[0].Width = new GridLength(10, GridUnitType.Pixel);

		foreach (var grid in grids)
		{
			grid.Measure(new Size(50, 50));
			grid.Arrange(new Rect(new Point(), new Point(50, 50)));
		}

		PrintColumnDefinitions(grids[0]);
		CornerstoneTest.AreEqual(10, grids[0].ColumnDefinitions[0].ActualWidth);
		CornerstoneTest.AreEqual(10, grids[1].ColumnDefinitions[0].ActualWidth);
	}

	[PresentationTestMethod]
	public void SizePrioritiesAreMaintained()
	{
		var sizers = new List<Control>();
		var grid = CreateGrid(
			("A", new GridLength(20)),
			("A", new GridLength(20, GridUnitType.Auto)),
			("A", new GridLength(1, GridUnitType.Star)),
			("A", new GridLength(1, GridUnitType.Star)),
			(null, new GridLength()));
		for (var i = 0; i < 3; i++)
		{
			sizers.Add(AddSizer(grid, i, 6 + (i * 6)));
		}
		var scope = new Grid();
		scope.Children.Add(grid);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(scope);

		grid.Measure(new Size(100, 100));
		grid.Arrange(new Rect(new Point(), new Point(100, 100)));
		PrintColumnDefinitions(grid);

		// all in group are equal to the first fixed column
		CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(20, cd.ActualWidth));

		grid.ColumnDefinitions[0].SharedSizeGroup = null;

		grid.Measure(new Size(100, 100));
		grid.Arrange(new Rect(new Point(), new Point(100, 100)));
		PrintColumnDefinitions(grid);

		// NOTE: THIS IS BROKEN IN WPF
		// CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(6 + 2 * 6, cd.ActualWidth));
		// grid.ColumnDefinitions[1].SharedSizeGroup = null;

		// grid.Measure(new Size(100, 100));
		// grid.Arrange(new Rect(new Point(), new Point(100, 100)));
		// PrintColumnDefinitions(grid);

		// NOTE: THIS IS BROKEN IN WPF
		// all in group are equal to width (MinWidth) of the sizer in the second column
		// CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(6 + 1 * 6, cd.ActualWidth));

		// NOTE: THIS IS BROKEN IN WPF
		// grid.ColumnDefinitions[2].SharedSizeGroup = null;

		// NOTE: THIS IS BROKEN IN WPF
		// grid.Measure(new Size(double.PositiveInfinity, 100));
		// grid.Arrange(new Rect(new Point(), new Point(100, 100)));
		// PrintColumnDefinitions(grid);
		// with no constraint star columns default to the MinWidth of the sizer in the column
		// CornerstoneTest.All(grid.ColumnDefinitions.Where(cd => cd.SharedSizeGroup == "A"), cd => CornerstoneTest.AreEqual(0, cd.ActualWidth));
	}

	[PresentationTestMethod]
	public void SizePropagationIsConstrainedToInnermostScope()
	{
		var grids = new[] { CreateGrid(("A", new GridLength())), CreateGrid(("A", new GridLength(30)), (null, new GridLength())) };
		var innerScope = new Grid();

		foreach (var grid in grids)
		{
			innerScope.Children.Add(grid);
		}

		innerScope.SetValue(Grid.IsSharedSizeScopeProperty, true);

		var outerGrid = CreateGrid(("A", new GridLength(0)));
		var outerScope = new Grid();
		outerScope.Children.Add(outerGrid);
		outerScope.Children.Add(innerScope);

		var root = new Grid();
		root.UseLayoutRounding = false;
		root.SetValue(Grid.IsSharedSizeScopeProperty, true);
		root.Children.Add(outerScope);

		root.Measure(new Size(50, 50));
		root.Arrange(new Rect(new Point(), new Point(50, 50)));
		CornerstoneTest.AreEqual(0, outerGrid.ColumnDefinitions[0].ActualWidth);
	}

	private Control AddSizer(Grid grid, int column, double size = 30)
	{
		var ctrl = new Control { MinWidth = size, MinHeight = size };
		ctrl.SetValue(Grid.ColumnProperty, column);
		grid.Children.Add(ctrl);
		output.WriteLine($"[AddSizer] Column: {column} MinWidth: {size} MinHeight: {size}");
		return ctrl;
	}

	private static void ChangePropertyAndVerifyMeasureRequested(Grid grid, Action change)
	{
		grid.Measure(new Size(100, 100));
		grid.Arrange(new Rect(grid.DesiredSize));

		CornerstoneTest.IsTrue(grid.IsMeasureValid);
		CornerstoneTest.IsTrue(grid.IsArrangeValid);

		change();

		CornerstoneTest.IsFalse(grid.IsMeasureValid);
		CornerstoneTest.IsFalse(grid.IsArrangeValid);
	}

	private static Grid CreateGrid(params (string name, GridLength width)[] columns)
	{
		return CreateGrid(columns.Select(c =>
			(c.name, c.width, ColumnDefinition.MinWidthProperty.GetDefaultValue(typeof(ColumnDefinition)))).ToArray());
	}

	private static Grid CreateGrid(params (string name, GridLength width, double minWidth)[] columns)
	{
		return CreateGrid(columns.Select(c =>
			(c.name, c.width, c.minWidth, ColumnDefinition.MaxWidthProperty.GetDefaultValue(typeof(ColumnDefinition)))).ToArray());
	}

	private static Grid CreateGrid(params (string name, GridLength width, double minWidth, double maxWidth)[] columns)
	{
		var grid = new Grid();
		foreach (var k in columns.Select(c => new ColumnDefinition
				{
					SharedSizeGroup = c.name,
					Width = c.width,
					MinWidth = c.minWidth,
					MaxWidth = c.maxWidth
				}))
		{
			grid.ColumnDefinitions.Add(k);
		}

		return grid;
	}

	private void PrintColumnDefinitions(Grid grid)
	{
		output.WriteLine($"[Grid] ActualWidth: {grid.Bounds.Width} ActualHeight: {grid.Bounds.Width}");
		output.WriteLine("[ColumnDefinitions]");
		for (var i = 0; i < grid.ColumnDefinitions.Count; i++)
		{
			var cd = grid.ColumnDefinitions[i];
			output.WriteLine($"[{i}] ActualWidth: {cd.ActualWidth} SharedSizeGroup: {cd.SharedSizeGroup}");
		}
	}

	#endregion

	#region Classes

	private class TestControl : Control
	{
		#region Properties

		public Size MeasureSize { get; set; }

		#endregion

		#region Methods

		protected override Size MeasureOverride(Size availableSize)
		{
			return MeasureSize;
		}

		#endregion
	}

	#endregion
}