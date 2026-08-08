#region References

using System;
using Avalonia;
using Cornerstone.Avalonia.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Controls;

[TestClass]
public class LayoutGridSettingsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ClampShareDefaultsInvalidAndClampsEdges()
	{
		AreEqual(LayoutGridSettings.DefaultFirstShare, LayoutGridSettings.ClampShare(0));
		AreEqual(LayoutGridSettings.DefaultFirstShare, LayoutGridSettings.ClampShare(1));
		AreEqual(LayoutGridSettings.DefaultFirstShare, LayoutGridSettings.ClampShare(double.NaN));
		AreEqual(LayoutGridSettings.MinFirstShare, LayoutGridSettings.ClampShare(0.01));
		AreEqual(LayoutGridSettings.MaxFirstShare, LayoutGridSettings.ClampShare(0.99));
		AreEqual(0.33, LayoutGridSettings.ClampShare(0.33));
	}

	[TestMethod]
	public void UpdateActiveShareOnlyTouchesCurrentOrientation()
	{
		var settings = new LayoutGridSettings
		{
			IsHorizontal = true,
			HorizontalFirstShare = 0.5,
			VerticalFirstShare = 0.7
		};

		var grid = new LayoutGrid { IsHorizontal = true };
		grid.Measure(new Size(400, 200));
		grid.Arrange(new Rect(0, 0, 400, 200));

		settings.UpdateActiveShareFromFirstPane(grid, new Size(120, 200));
		// 120/400 = 0.3 — vertical share must stay 0.7
		IsTrue(Near(0.3, settings.HorizontalFirstShare));
		AreEqual(0.7, settings.VerticalFirstShare);

		settings.IsHorizontal = false;
		grid.IsHorizontal = false;
		settings.UpdateActiveShareFromFirstPane(grid, new Size(400, 50));
		// 50/200 = 0.25 — horizontal share must stay 0.3
		IsTrue(Near(0.25, settings.VerticalFirstShare));
		IsTrue(Near(0.3, settings.HorizontalFirstShare));
	}

	[TestMethod]
	public void RestoreSizeAppliesBothAxesIndependently()
	{
		var grid = new LayoutGrid();
		grid.RestoreSize(0.25, 0.4);
		IsTrue(Near(0.25, grid.GetFirstRowShare()));
		IsTrue(Near(0.4, grid.GetFirstColumnShare()));
	}

	private static bool Near(double expected, double actual, double epsilon = 0.001)
	{
		return Math.Abs(expected - actual) <= epsilon;
	}

	#endregion
}