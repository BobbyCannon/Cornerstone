#region References

using System;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Fonts;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

/// <summary>
/// The user-space variation settings value: parse round-trips, order-independent
/// structural equality, last-wins duplicate handling, and the lookups — the contract
/// that lets the type serve as a style value and a cache key.
/// </summary>
[TestClass]
public class FontVariationSettingsTests
{
	#region Fields

	private static readonly OpenTypeTag sopsz = OpenTypeTag.Parse("opsz");
	private static readonly OpenTypeTag swdth = OpenTypeTag.Parse("wdth");
	private static readonly OpenTypeTag swght = OpenTypeTag.Parse("wght");

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void ConstructorRejectsNaNAndNull()
	{
		Assert.Throws<ArgumentException>(() =>
			new FontVariationSettings(new[] { new FontVariation(swght, double.NaN) }));
		Assert.Throws<ArgumentNullException>(() => new FontVariationSettings(null!));
	}

	[PresentationTestMethod]
	public void DuplicateTagsCollapseToTheLastValue()
	{
		// CSS font-variation-settings behavior: the last occurrence wins.
		var settings = FontVariationSettings.Parse("wght=400,wght=700");

		CornerstoneTest.AreEqual(1, settings.Variations.Length);
		CornerstoneTest.IsTrue(settings.TryGetValue(swght, out var wght));
		CornerstoneTest.AreEqual(700, wght);
	}

	[PresentationTestMethod]
	public void EmptyIsEmptyAndRoundTrips()
	{
		CornerstoneTest.IsTrue(FontVariationSettings.Empty.IsEmpty);
		CornerstoneTest.Empty(FontVariationSettings.Empty.Variations);
		CornerstoneTest.AreEqual(string.Empty, FontVariationSettings.Empty.ToString());
		CornerstoneTest.AreEqual(FontVariationSettings.Empty, FontVariationSettings.Parse(""));
		CornerstoneTest.AreEqual(FontVariationSettings.Empty, FontVariationSettings.Parse("   "));
	}

	[PresentationTestMethod]
	public void EqualityIsOrderIndependentWithMatchingHashes()
	{
		var a = new FontVariationSettings(new[]
		{
			new FontVariation(swght, 700),
			new FontVariation(sopsz, 36)
		});
		var b = new FontVariationSettings(new[]
		{
			new FontVariation(sopsz, 36),
			new FontVariation(swght, 700)
		});

		CornerstoneTest.AreEqual(a, b);
		CornerstoneTest.AreEqual(a.GetHashCode(), b.GetHashCode());
		CornerstoneTest.AreNotEqual(a, FontVariationSettings.Parse("wght=700"));
		CornerstoneTest.IsFalse(a.Equals(null));
	}

	[PresentationTestMethod]
	public void InfiniteValuesAreAcceptedAndRoundTrip()
	{
		// Infinities clamp to the axis range when applied, like any out-of-range value.
		var settings = new FontVariationSettings(new[]
		{
			new FontVariation(swght, double.PositiveInfinity),
			new FontVariation(sopsz, double.NegativeInfinity)
		});

		CornerstoneTest.IsTrue(settings.TryGetValue(swght, out var wght));
		CornerstoneTest.AreEqual(double.PositiveInfinity, wght);

		var roundTripped = FontVariationSettings.Parse(settings.ToString());

		CornerstoneTest.AreEqual(settings, roundTripped);
		CornerstoneTest.IsTrue(roundTripped.TryGetValue(sopsz, out var opsz));
		CornerstoneTest.AreEqual(double.NegativeInfinity, opsz);
	}

	[PresentationTestMethod]
	public void ParseReadsCommaSeparatedTagValuePairs()
	{
		var settings = FontVariationSettings.Parse(" wght = 700 , wdth=85.5 ");

		CornerstoneTest.AreEqual(2, settings.Variations.Length);
		CornerstoneTest.IsTrue(settings.TryGetValue(swght, out var wght));
		CornerstoneTest.AreEqual(700, wght);
		CornerstoneTest.IsTrue(settings.TryGetValue(swdth, out var wdth));
		CornerstoneTest.AreEqual(85.5, wdth);
	}

	[PresentationTestMethod]
	[DataRow("wght")]
	[DataRow("wght=")]
	[DataRow("=700")]
	[DataRow("weight=700")]
	[DataRow("wght=seven")]
	[DataRow("wght=NaN")]
	public void ParseRejectsMalformedInput(string input)
	{
		Assert.Throws<FormatException>(() => FontVariationSettings.Parse(input));
	}

	[PresentationTestMethod]
	public void ToStringRoundTripsThroughParse()
	{
		var settings = FontVariationSettings.Parse("opsz=14.25,wght=650");
		var roundTripped = FontVariationSettings.Parse(settings.ToString());

		CornerstoneTest.AreEqual(settings, roundTripped);
		CornerstoneTest.AreEqual("opsz=14.25,wght=650", settings.ToString());
	}

	[PresentationTestMethod]
	public void TryGetValueMissesReportFalseAndZero()
	{
		var settings = FontVariationSettings.Parse("wght=700");

		CornerstoneTest.IsFalse(settings.TryGetValue(sopsz, out var value));
		CornerstoneTest.AreEqual(0, value);
	}

	[PresentationTestMethod]
	public void VariationToStringIsThePairForm()
	{
		CornerstoneTest.AreEqual("wght=700", new FontVariation(swght, 700).ToString());
		CornerstoneTest.AreEqual("opsz=14.25", new FontVariation(sopsz, 14.25).ToString());
	}

	[PresentationTestMethod]
	public void VariationsAreSortedByTag()
	{
		var settings = FontVariationSettings.Parse("wght=700,opsz=14,wdth=85");

		CornerstoneTest.AreEqual(sopsz, settings.Variations[0].Tag);
		CornerstoneTest.AreEqual(swdth, settings.Variations[1].Tag);
		CornerstoneTest.AreEqual(swght, settings.Variations[2].Tag);
	}

	#endregion
}