#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class FlexBasisTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ParseShouldParseAbsoluteValue()
	{
		var result = FlexBasis.Parse("2");

		CornerstoneTest.AreEqual(new FlexBasis(2, FlexBasisKind.Absolute), result);
	}

	[PresentationTestMethod]
	public void ParseShouldParseAuto()
	{
		var result = FlexBasis.Parse("Auto");

		CornerstoneTest.AreEqual(FlexBasis.Auto, result);
	}

	[PresentationTestMethod]
	public void ParseShouldParseAutoLowercase()
	{
		var result = FlexBasis.Parse("auto");

		CornerstoneTest.AreEqual(FlexBasis.Auto, result);
	}

	[PresentationTestMethod]
	public void ParseShouldParsePercentage()
	{
		var result = FlexBasis.Parse("50%");

		CornerstoneTest.AreEqual(new FlexBasis(0.5, FlexBasisKind.Relative), result);
	}

	[PresentationTestMethod]
	public void ParseShouldThrowArgumentExceptionForInvalidString()
	{
		Assert.Throws<ArgumentException>(() => FlexBasis.Parse("2x"));
	}

	[PresentationTestMethod]
	public async Task ToStringAllCultureAbsoluteShouldPass()
	{
		List<CultureInfo> cultureInfos = [CultureInfo.GetCultureInfo("en-US"), CultureInfo.GetCultureInfo("fr-FR")];
		var length = new FlexBasis(1.2d, FlexBasisKind.Absolute);

		foreach (var culture in cultureInfos)
		{
			await Task.Run(() =>
			{
				CultureInfo.CurrentCulture = culture;
				CornerstoneTest.AreEqual("1.2", length.ToString());
			}, CancellationToken.None);
		}
	}

	#endregion
}