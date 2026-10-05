#region References

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class GridLengthTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ParseLengthsAcceptsCommaSeparators()
	{
		var result = GridLength.ParseLengths("*,Auto,2*,4").ToList();

		CornerstoneTest.AreEqual(new[]
		{
			new GridLength(1, GridUnitType.Star),
			GridLength.Auto,
			new GridLength(2, GridUnitType.Star),
			new GridLength(4, GridUnitType.Pixel)
		}, result);
	}

	[PresentationTestMethod]
	public void ParseLengthsAcceptsCommaSeparatorsWithSpaces()
	{
		var result = GridLength.ParseLengths("*, Auto, 2* ,4").ToList();

		CornerstoneTest.AreEqual(new[]
		{
			new GridLength(1, GridUnitType.Star),
			GridLength.Auto,
			new GridLength(2, GridUnitType.Star),
			new GridLength(4, GridUnitType.Pixel)
		}, result);
	}

	[PresentationTestMethod]
	public void ParseLengthsAcceptsSpaceSeparators()
	{
		var result = GridLength.ParseLengths("* Auto 2* 4").ToList();

		CornerstoneTest.AreEqual(new[]
		{
			new GridLength(1, GridUnitType.Star),
			GridLength.Auto,
			new GridLength(2, GridUnitType.Star),
			new GridLength(4, GridUnitType.Pixel)
		}, result);
	}

	[PresentationTestMethod]
	public void ParseShouldParseAuto()
	{
		var result = GridLength.Parse("Auto");

		CornerstoneTest.AreEqual(GridLength.Auto, result);
	}

	[PresentationTestMethod]
	public void ParseShouldParseAutoLowercase()
	{
		var result = GridLength.Parse("auto");

		CornerstoneTest.AreEqual(GridLength.Auto, result);
	}

	[PresentationTestMethod]
	public void ParseShouldParsePixelValue()
	{
		var result = GridLength.Parse("2");

		CornerstoneTest.AreEqual(new GridLength(2, GridUnitType.Pixel), result);
	}

	[PresentationTestMethod]
	public void ParseShouldParseStar()
	{
		var result = GridLength.Parse("*");

		CornerstoneTest.AreEqual(new GridLength(1, GridUnitType.Star), result);
	}

	[PresentationTestMethod]
	public void ParseShouldParseStarValue()
	{
		var result = GridLength.Parse("2*");

		CornerstoneTest.AreEqual(new GridLength(2, GridUnitType.Star), result);
	}

	[PresentationTestMethod]
	public void ParseShouldThrowFormatExceptionForInvalidString()
	{
		Assert.Throws<FormatException>(() => GridLength.Parse("2x"));
	}

	[PresentationTestMethod]
	[DataRow(1.2d, GridUnitType.Pixel, "1.2")]
	[DataRow(1.2d, GridUnitType.Star, "1.2*")]
	[DataRow(1.2d, GridUnitType.Auto, "Auto")]
	public async Task ToStringAllCultureShouldPass(double d, GridUnitType type, string result)
	{
		var cultureInfos = CultureInfo.GetCultures(CultureTypes.AllCultures).ToList();
		var length = new GridLength(d, type);
		foreach (var culture in cultureInfos)
		{
			await Task.Run(() =>
			{
				CultureInfo.CurrentCulture = culture;
				CornerstoneTest.AreEqual(result, length.ToString());
			}, CancellationToken.None);
		}
	}

	#endregion
}