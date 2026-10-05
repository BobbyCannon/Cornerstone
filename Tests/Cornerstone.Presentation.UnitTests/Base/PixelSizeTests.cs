#region References

using System;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class PixelSizeTests
{
	#region Methods

	[PresentationTestMethod]
	[DataRow(10, 1.0, 10)]
	[DataRow(10, 1.25, 13)]
	[DataRow(10, 1.5, 15)]
	[DataRow(10, 1.75, 18)]
	[DataRow(10, 2.0, 20)]
	[DataRow(10, 1.125, 12)]
	[DataRow(8, 1.5, 12)]
	[DataRow(0, 1.5, 0)]
	[DataRow(1, 2.5, 3)]
	public void FromSizeCeilingComputesExpectedPixels(int logical, double scale, int expected)
	{
		var pixel = PixelSize.FromSizeCeiling(new Size(logical, logical), scale);
		CornerstoneTest.AreEqual(expected, pixel.Width);
		CornerstoneTest.AreEqual(expected, pixel.Height);
	}

	[PresentationTestMethod]
	[DataRow(1.5)]
	[DataRow(2.0)]
	[DataRow(3.0)]
	public void FromSizeCeilingSnapsWhenWithinEpsilon(double scale)
	{
		// Pick a logical size where logical * scale is an exact integer; perturbing it by a tiny
		// amount in either direction must still produce that integer (no spurious +1 from ceiling).
		const int logical = 10;
		var exact = logical * scale;
		var below = exact - 1e-9;
		var above = exact + 1e-9;
		var roundedBelow = below / scale;
		var roundedAbove = above / scale;

		var p1 = PixelSize.FromSizeCeiling(new Size(roundedBelow, roundedBelow), scale);
		var p2 = PixelSize.FromSizeCeiling(new Size(roundedAbove, roundedAbove), scale);
		CornerstoneTest.AreEqual((int) exact, p1.Width);
		CornerstoneTest.AreEqual((int) exact, p2.Width);
	}

	[PresentationTestMethod]
	[DataRow("1024,768", 1024, 768, null)]
	[DataRow("1024x768", 0, 0, "Invalid PixelSize.")]
	public void Parse(string source, int expectedWidth, int expectedHeight, string exceptionMessage)
	{
		Exception error = null;
		PixelSize result = default;
		try
		{
			result = PixelSize.Parse(source);
		}
		catch (Exception ex)
		{
			error = ex;
		}
		CornerstoneTest.AreEqual(exceptionMessage, error?.Message);
		CornerstoneTest.AreEqual(new PixelSize(expectedWidth, expectedHeight), result);
	}

	[PresentationTestMethod]
	[DataRow("1024,768", 1024, 768)]
	[DataRow("1024x768", 0, 0)]
	public void TryParse(string source, int expectedWidth, int expectedHeight)
	{
		CornerstoneTest.AreEqual((expectedWidth != 0) || (expectedHeight != 0), PixelSize.TryParse(source, out var result));
		CornerstoneTest.AreEqual(new PixelSize(expectedWidth, expectedHeight), result);
	}

	#endregion
}