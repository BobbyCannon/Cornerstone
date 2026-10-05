#region References

using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Presenters;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Presenters;

[TestClass]
public sealed class TableViewLayoutHelperTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void NeedsActualWidthsReturnsFalseAfterWidthsAreComputed()
	{
		var columns = new OldPresentationList<TableViewColumn>
		{
			new() { Width = new GridLength(1, GridUnitType.Star) },
			new() { Width = new GridLength(1, GridUnitType.Star) }
		};

		TableViewLayoutHelper.UpdateActualWidths(columns, 200, false, 1.0);

		CornerstoneTest.IsFalse(TableViewLayoutHelper.NeedsActualWidths(columns));
	}

	[PresentationTestMethod]
	public void NeedsActualWidthsReturnsFalseForEmptyColumns()
	{
		var columns = new OldPresentationList<TableViewColumn>();

		CornerstoneTest.IsFalse(TableViewLayoutHelper.NeedsActualWidths(columns));
	}

	[PresentationTestMethod]
	public void NeedsActualWidthsReturnsTrueWhenFirstColumnHasNaNWidth()
	{
		var columns = new OldPresentationList<TableViewColumn> { new(), new() };

		CornerstoneTest.IsTrue(TableViewLayoutHelper.NeedsActualWidths(columns));
	}

	[PresentationTestMethod]
	public void ResetActualWidthsSetsAllWidthsToNaN()
	{
		var columns = new OldPresentationList<TableViewColumn>
		{
			new() { Width = new GridLength(50) },
			new() { Width = new GridLength(1, GridUnitType.Star) }
		};

		TableViewLayoutHelper.UpdateActualWidths(columns, 200, false, 1.0);
		TableViewLayoutHelper.ResetActualWidths(columns);

		CornerstoneTest.IsTrue(double.IsNaN(columns[0].ActualWidth));
		CornerstoneTest.IsTrue(double.IsNaN(columns[1].ActualWidth));
	}

	[PresentationTestMethod]
	public void UpdateActualWidthsDistributesStarColumnsProportionally()
	{
		var columns = new OldPresentationList<TableViewColumn>
		{
			new() { Width = new GridLength(1, GridUnitType.Star) },
			new() { Width = new GridLength(3, GridUnitType.Star) }
		};

		TableViewLayoutHelper.UpdateActualWidths(columns, 400, false, 1.0);

		CornerstoneTest.AreEqual(100, columns[0].ActualWidth);
		CornerstoneTest.AreEqual(300, columns[1].ActualWidth);
	}

	[PresentationTestMethod]
	public void UpdateActualWidthsFallsBackTo1000ForInfiniteWidth()
	{
		var columns = new OldPresentationList<TableViewColumn>
		{
			new() { Width = new GridLength(1, GridUnitType.Star) },
			new() { Width = new GridLength(1, GridUnitType.Star) }
		};

		TableViewLayoutHelper.UpdateActualWidths(columns, double.PositiveInfinity, false, 1.0);

		CornerstoneTest.AreEqual(500, columns[0].ActualWidth);
		CornerstoneTest.AreEqual(500, columns[1].ActualWidth);
	}

	[PresentationTestMethod]
	public void UpdateActualWidthsPixelColumnUsesItsFixedWidth()
	{
		var columns = new OldPresentationList<TableViewColumn>
		{
			new() { Width = new GridLength(80) }
		};

		TableViewLayoutHelper.UpdateActualWidths(columns, 500, false, 1.0);

		CornerstoneTest.AreEqual(80, columns[0].ActualWidth);
	}

	[PresentationTestMethod]
	public void UpdateActualWidthsPixelPlusStarSubtractsFixedFromStarBudget()
	{
		var columns = new OldPresentationList<TableViewColumn>
		{
			new() { Width = new GridLength(50) },
			new() { Width = new GridLength(1, GridUnitType.Star) }
		};

		TableViewLayoutHelper.UpdateActualWidths(columns, 200, false, 1.0);

		CornerstoneTest.AreEqual(50, columns[0].ActualWidth);
		CornerstoneTest.AreEqual(150, columns[1].ActualWidth);
	}

	[PresentationTestMethod]
	public void UpdateActualWidthsReturnsFalseWhenNoChange()
	{
		var columns = new OldPresentationList<TableViewColumn>
		{
			new() { Width = new GridLength(1, GridUnitType.Star) },
			new() { Width = new GridLength(50) }
		};

		CornerstoneTest.IsTrue(TableViewLayoutHelper.UpdateActualWidths(columns, 200, false, 1.0));
		CornerstoneTest.IsFalse(TableViewLayoutHelper.UpdateActualWidths(columns, 200, false, 1.0));
	}

	[PresentationTestMethod]
	public void UpdateActualWidthsReturnsTrueWhenWidthChanges()
	{
		var columns = new OldPresentationList<TableViewColumn>
		{
			new() { Width = new GridLength(1, GridUnitType.Star) }
		};

		CornerstoneTest.IsTrue(TableViewLayoutHelper.UpdateActualWidths(columns, 200, false, 1.0));
		CornerstoneTest.IsTrue(TableViewLayoutHelper.UpdateActualWidths(columns, 300, false, 1.0));
		CornerstoneTest.AreEqual(300, columns[0].ActualWidth);
	}

	[PresentationTestMethod]
	public void UpdateActualWidthsRoundsFixedColumnWidthAndStarAbsorbsRemainder()
	{
		var columns = new OldPresentationList<TableViewColumn>
		{
			new() { Width = new GridLength(50.4) },
			new() { Width = new GridLength(1, GridUnitType.Star) },
			new() { Width = new GridLength(2, GridUnitType.Star) }
		};

		TableViewLayoutHelper.UpdateActualWidths(columns, 200, true, 1);

		// The fixed column rounds to a whole pixel (50); the remaining 150px star budget is split
		// 1:2 between the star columns.
		CornerstoneTest.AreEqual(50, columns[0].ActualWidth);
		CornerstoneTest.AreEqual(50, columns[1].ActualWidth);
		CornerstoneTest.AreEqual(100, columns[2].ActualWidth);
	}

	[PresentationTestMethod]
	public void UpdateActualWidthsSingleStarColumnGetsFullWidth()
	{
		var columns = new OldPresentationList<TableViewColumn>
		{
			new() { Width = new GridLength(1, GridUnitType.Star) }
		};

		CornerstoneTest.IsTrue(TableViewLayoutHelper.UpdateActualWidths(columns, 200, false, 1.0));
		CornerstoneTest.AreEqual(200, columns[0].ActualWidth);
	}

	[PresentationTestMethod]
	public void UpdateActualWidthsSpreadsRoundingAcrossStarColumns()
	{
		var columns = new OldPresentationList<TableViewColumn>
		{
			new() { Width = new GridLength(2, GridUnitType.Star) },
			new() { Width = new GridLength(3, GridUnitType.Star) },
			new() { Width = new GridLength(2, GridUnitType.Star) }
		};

		TableViewLayoutHelper.UpdateActualWidths(columns, 100, true, 1);

		// Weights 2:3:2 over 100 give 28.57 / 42.86 / 28.57. Rounding each column independently
		// would produce 29 / 43 / 29 = 101; spreading the remainder keeps the total at 100.
		CornerstoneTest.AreEqual(29, columns[0].ActualWidth);
		CornerstoneTest.AreEqual(42, columns[1].ActualWidth);
		CornerstoneTest.AreEqual(29, columns[2].ActualWidth);
		CornerstoneTest.AreEqual(100, columns[0].ActualWidth + columns[1].ActualWidth + columns[2].ActualWidth);
	}

	[PresentationTestMethod]
	public void UpdateActualWidthsSpreadsRoundingAtFractionalLayoutScale()
	{
		var columns = new OldPresentationList<TableViewColumn>
		{
			new() { Width = new GridLength(3, GridUnitType.Star) },
			new() { Width = new GridLength(1, GridUnitType.Star) },
			new() { Width = new GridLength(3, GridUnitType.Star) }
		};

		TableViewLayoutHelper.UpdateActualWidths(columns, 100, true, 2);

		// At 2x scale widths snap to half-pixels (whole device pixels). Weights 3:1:3 give
		// 42.86 / 14.29 / 42.86; rounding each independently would drift the total to 100.5,
		// while spreading the remainder keeps it at 100.
		CornerstoneTest.AreEqual(43, columns[0].ActualWidth);
		CornerstoneTest.AreEqual(14, columns[1].ActualWidth);
		CornerstoneTest.AreEqual(43, columns[2].ActualWidth);
		CornerstoneTest.AreEqual(100, columns[0].ActualWidth + columns[1].ActualWidth + columns[2].ActualWidth);
	}

	[PresentationTestMethod]
	public void UpdateActualWidthsSpreadsRoundingWithIntertwinedFixedColumns()
	{
		var columns = new OldPresentationList<TableViewColumn>
		{
			new() { Width = new GridLength(50) },
			new() { Width = new GridLength(1, GridUnitType.Star) },
			new() { Width = new GridLength(30) },
			new() { Width = new GridLength(3, GridUnitType.Star) },
			new() { Width = new GridLength(2, GridUnitType.Star) }
		};

		TableViewLayoutHelper.UpdateActualWidths(columns, 201, true, 1);

		// Fixed columns keep their exact size (80 total). The remaining 121px star budget is split
		// 1:3:2 (20.17 / 60.5 / 40.33). Rounding each independently would drop to 200; spreading the
		// remainder gives the extra pixel to the 3* column so everything fills exactly 201.
		CornerstoneTest.AreEqual(50, columns[0].ActualWidth);
		CornerstoneTest.AreEqual(20, columns[1].ActualWidth);
		CornerstoneTest.AreEqual(30, columns[2].ActualWidth);
		CornerstoneTest.AreEqual(61, columns[3].ActualWidth);
		CornerstoneTest.AreEqual(40, columns[4].ActualWidth);
		CornerstoneTest.AreEqual(201, columns[0].ActualWidth + columns[1].ActualWidth + columns[2].ActualWidth +
			columns[3].ActualWidth + columns[4].ActualWidth);
	}

	[PresentationTestMethod]
	public void UpdateActualWidthsStarClampedToZeroWhenPixelExceedsAvailable()
	{
		var columns = new OldPresentationList<TableViewColumn>
		{
			new() { Width = new GridLength(300) },
			new() { Width = new GridLength(1, GridUnitType.Star) }
		};

		TableViewLayoutHelper.UpdateActualWidths(columns, 200, false, 1.0);

		CornerstoneTest.AreEqual(300, columns[0].ActualWidth);
		CornerstoneTest.AreEqual(0, columns[1].ActualWidth);
	}

	[PresentationTestMethod]
	public void UpdateActualWidthsTreatsAutoAsOneStar()
	{
		var columns = new OldPresentationList<TableViewColumn>
		{
			new() { Width = GridLength.Auto },
			new() { Width = new GridLength(1, GridUnitType.Star) }
		};

		TableViewLayoutHelper.UpdateActualWidths(columns, 200, false, 1.0);

		CornerstoneTest.AreEqual(100, columns[0].ActualWidth);
		CornerstoneTest.AreEqual(100, columns[1].ActualWidth);
	}

	[PresentationTestMethod]
	public void UpdateActualWidthsWithNoColumnsReturnsFalse()
	{
		var columns = new OldPresentationList<TableViewColumn>();

		CornerstoneTest.IsFalse(TableViewLayoutHelper.UpdateActualWidths(columns, 100, false, 1.0));
	}

	#endregion
}