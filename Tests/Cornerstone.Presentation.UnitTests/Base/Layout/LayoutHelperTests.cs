#region References

using System;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Layout;

[TestClass]
public class LayoutHelperTests
{
	#region Methods

	[PresentationTestMethod]
	public void RoundLayoutValueWithDPIAware()
	{
		const double dpiScale = 1.25;
		const double value = 42.5;
		var expectedValue = Math.Round(value * dpiScale) / dpiScale;
		var actualValue = LayoutHelper.RoundLayoutValue(value, dpiScale);
		CornerstoneTest.AreEqual(expectedValue, actualValue);
	}

	[PresentationTestMethod]
	public void RoundLayoutValueWithoutDPIAware()
	{
		const double value = 42.5;
		var expectedValue = Math.Round(value);
		var actualValue = LayoutHelper.RoundLayoutValue(value, 1.0);
		CornerstoneTest.AreEqual(expectedValue, actualValue);
	}

	[PresentationTestMethod]
	public void ValidateScalingReturnsExactOneForApproximateOne()
	{
		var result = LayoutHelper.ValidateScaling(1.000000000000001);
		CornerstoneTest.AreEqual(1.0, result);
	}

	[PresentationTestMethod]
	public void ValidateScalingReturnsValidScalingValue()
	{
		const double scaling = 1.5;
		var result = LayoutHelper.ValidateScaling(scaling);
		CornerstoneTest.AreEqual(scaling, result);
	}

	[PresentationTestMethod]
	[DataRow(0.0)]
	[DataRow(-1.5)]
	[DataRow(double.NaN)]
	[DataRow(double.PositiveInfinity)]
	[DataRow(double.NegativeInfinity)]
	public void ValidateScalingThrowsForInvalidValues(double scaling)
	{
		Assert.Throws<InvalidOperationException>(() => LayoutHelper.ValidateScaling(scaling));
	}

	#endregion
}