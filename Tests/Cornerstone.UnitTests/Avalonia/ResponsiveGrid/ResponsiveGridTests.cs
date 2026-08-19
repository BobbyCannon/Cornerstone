#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Cornerstone.Avalonia.ResponsiveGrid;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Controls;

[TestClass]
public class ResponsiveGridTests : CornerstoneAvaloniaUnitTest
{
	#region Methods

	[TestMethod]
	public void AttachedPropertyNamesMatchXaml()
	{
		AreEqual("LG", ResponsiveGrid.LGProperty.Name);
		AreEqual("MD", ResponsiveGrid.MDProperty.Name);
		AreEqual("SM", ResponsiveGrid.SMProperty.Name);
		AreEqual("XS", ResponsiveGrid.XSProperty.Name);
		AreEqual("MD_Offset", ResponsiveGrid.MD_OffsetProperty.Name);
		AreEqual("SM_Offset", ResponsiveGrid.SM_OffsetProperty.Name);
		IsFalse(ReferenceEquals(ResponsiveGrid.MD_OffsetProperty, ResponsiveGrid.SM_OffsetProperty));
	}

	[TestMethod]
	public void ChangingChildSpanInvalidatesParentMeasure()
	{
		RunOnUi(() =>
		{
			var child = CreateChild();
			ResponsiveGrid.SetXS(child, 24);
			var grid = CreateGrid(child);
			grid.Measure(new Size(400, 200));
			IsTrue(grid.IsMeasureValid);

			ResponsiveGrid.SetSM(child, 12);
			IsFalse(grid.IsMeasureValid);
		});
	}

	[TestMethod]
	public void CollapsedChildrenDoNotOccupyARow()
	{
		RunOnUi(() =>
		{
			var first = CreateChild();
			var hidden = CreateChild();
			var third = CreateChild();
			ResponsiveGrid.SetXS(first, 12);
			ResponsiveGrid.SetXS(hidden, 12);
			ResponsiveGrid.SetXS(third, 12);
			hidden.IsVisible = false;

			var grid = CreateGrid(first, hidden, third);
			grid.Measure(new Size(400, 400));

			AreEqual(0, ResponsiveGrid.GetActualRow(first));
			AreEqual(0, ResponsiveGrid.GetActualColumn(first));
			AreEqual(0, ResponsiveGrid.GetActualRow(third));
			AreEqual(12, ResponsiveGrid.GetActualColumn(third));
		});
	}

	[TestMethod]
	public void ArrangeUsesFinalWidthNotMeasureWidth()
	{
		RunOnUi(() =>
		{
			var first = CreateChild();
			var second = CreateChild();
			ResponsiveGrid.SetXS(first, 24);
			ResponsiveGrid.SetSM(first, 12);
			ResponsiveGrid.SetXS(second, 24);
			ResponsiveGrid.SetSM(second, 12);

			var grid = CreateGrid(first, second);
			grid.Measure(new Size(1000, 200));
			grid.Arrange(new Rect(0, 0, 400, 200));

			AreEqual(0, ResponsiveGrid.GetActualRow(first));
			AreEqual(1, ResponsiveGrid.GetActualRow(second));
			AreEqual(0d, second.Bounds.X);
			IsTrue(first.Bounds.Width <= 400);
			IsTrue(second.Bounds.Width <= 400);
			IsTrue(second.Bounds.Y >= first.Bounds.Bottom);
		});
	}

	[TestMethod]
	public void ColumnSpacingDoesNotOverflowNarrowStretchRow()
	{
		RunOnUi(() =>
		{
			var left = CreateChild();
			var right = CreateChild();
			ResponsiveGrid.SetXS(left, 12);
			ResponsiveGrid.SetXS(right, 12);

			var grid = CreateGrid(left, right);
			grid.ColumnSpacing = 10;
			grid.Measure(new Size(200, 100));
			grid.Arrange(new Rect(0, 0, 200, 100));

			IsTrue(left.Bounds.Right <= right.Bounds.X + 0.01);
			IsTrue(right.Bounds.Right <= 200.01);
		});
	}

	[TestMethod]
	public void ColumnSpacingSeparatesAdjacentCells()
	{
		RunOnUi(() =>
		{
			var left = CreateChild();
			var right = CreateChild();
			ResponsiveGrid.SetXS(left, 12);
			ResponsiveGrid.SetXS(right, 12);

			var grid = CreateGrid(left, right);
			grid.ColumnSpacing = 10;
			grid.Measure(new Size(470, 100));
			grid.Arrange(new Rect(0, 0, 470, 100));

			AreEqual(0d, left.Bounds.X);
			AreEqual(240d, right.Bounds.X);
		});
	}

	[TestMethod]
	public void SizeThresholdsConverterParsesThreeValues()
	{
		var converter = new SizeThresholdsTypeConverter();
		var thresholds = (SizeThresholds) converter.ConvertFrom(null, null, "200, 500, 900");
		AreEqual(200d, thresholds.XSmallToSmall);
		AreEqual(500d, thresholds.SmallToMedium);
		AreEqual(900d, thresholds.MediumToLarge);
	}

	[TestMethod]
	public void SizeThresholdsConverterRequiresThreeItems()
	{
		var converter = new SizeThresholdsTypeConverter();
		Assert.ThrowsExactly<ArgumentException>(() => converter.ConvertFrom(null, null, "200,500"));
	}

	[TestMethod]
	public void CompactThresholdsReachSmallInsideANarrowGrid()
	{
		RunOnUi(() =>
		{
			var left = CreateChild();
			var right = CreateChild();
			ResponsiveGrid.SetXS(left, 24);
			ResponsiveGrid.SetSM(left, 12);
			ResponsiveGrid.SetXS(right, 24);
			ResponsiveGrid.SetSM(right, 12);

			var grid = CreateGrid(left, right);
			grid.Thresholds = new SizeThresholds
			{
				XSmallToSmall = 200,
				SmallToMedium = 400,
				MediumToLarge = 600
			};
			grid.Measure(new Size(320, 200));

			AreEqual(0, ResponsiveGrid.GetActualRow(left));
			AreEqual(0, ResponsiveGrid.GetActualRow(right));
			AreEqual(12, ResponsiveGrid.GetActualColumn(right));
		});
	}

	[TestMethod]
	public void MaxDivisionDefaultsTo24()
	{
		RunOnUi(() =>
		{
			var grid = new ResponsiveGrid();
			AreEqual(24, grid.MaxDivision);
		});
	}

	[TestMethod]
	public void NullThresholdsDoesNotThrowOnMeasure()
	{
		RunOnUi(() =>
		{
			var child = CreateChild();
			ResponsiveGrid.SetXS(child, 24);
			var grid = CreateGrid(child);
			grid.SetValue(ResponsiveGrid.ThresholdsProperty, null);
			grid.Measure(new Size(400, 100));
			IsNotNull(grid.Thresholds);
			AreEqual(0, ResponsiveGrid.GetActualRow(child));
		});
	}

	[TestMethod]
	public void PullIsClampedToNonNegativeColumn()
	{
		RunOnUi(() =>
		{
			var child = CreateChild();
			ResponsiveGrid.SetXS(child, 8);
			ResponsiveGrid.SetXS_Pull(child, 20);
			var grid = CreateGrid(child);
			grid.Measure(new Size(400, 100));
			AreEqual(0, ResponsiveGrid.GetActualColumn(child));
		});
	}

	[TestMethod]
	public void UnsetLargerBreakpointFallsBackToXs()
	{
		RunOnUi(() =>
		{
			var first = CreateChild();
			var second = CreateChild();
			ResponsiveGrid.SetXS(first, 12);
			ResponsiveGrid.SetXS(second, 12);

			var grid = CreateGrid(first, second);
			grid.Measure(new Size(1300, 200));

			AreEqual(0, ResponsiveGrid.GetActualRow(first));
			AreEqual(0, ResponsiveGrid.GetActualRow(second));
			AreEqual(12, ResponsiveGrid.GetActualColumn(second));
		});
	}

	[TestMethod]
	public void WrapWhenSpanExceedsMaxDivision()
	{
		RunOnUi(() =>
		{
			var first = CreateChild();
			var second = CreateChild();
			ResponsiveGrid.SetXS(first, 24);
			ResponsiveGrid.SetXS(second, 24);

			var grid = CreateGrid(first, second);
			grid.Measure(new Size(400, 400));

			AreEqual(0, ResponsiveGrid.GetActualRow(first));
			AreEqual(1, ResponsiveGrid.GetActualRow(second));
			AreEqual(0, ResponsiveGrid.GetActualColumn(second));
		});
	}

	private static Border CreateChild()
	{
		return new Border
		{
			MinHeight = 20,
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
	}

	private static ResponsiveGrid CreateGrid(params Control[] children)
	{
		var grid = new ResponsiveGrid();

		foreach (var child in children)
		{
			grid.Children.Add(child);
		}

		return grid;
	}

	#endregion
}
