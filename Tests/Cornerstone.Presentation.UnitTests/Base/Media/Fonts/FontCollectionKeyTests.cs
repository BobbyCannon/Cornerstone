#region References

using System;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Fonts;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media.Fonts;

[TestClass]
public class FontCollectionKeyTests
{
	#region Methods

	[PresentationTestMethod]
	public void ArraySortProducesLexicographicOrder()
	{
		var keys = new[]
		{
			new FontCollectionKey(FontStyle.Italic, FontWeight.Normal, FontStretch.Normal),
			new FontCollectionKey(FontStyle.Normal, FontWeight.Bold, FontStretch.Normal),
			new FontCollectionKey(FontStyle.Normal, FontWeight.Normal, FontStretch.Expanded),
			new FontCollectionKey(FontStyle.Normal, FontWeight.Normal, FontStretch.Normal)
		};

		Array.Sort(keys);

		CornerstoneTest.AreEqual(new[]
		{
			new FontCollectionKey(FontStyle.Normal, FontWeight.Normal, FontStretch.Normal),
			new FontCollectionKey(FontStyle.Normal, FontWeight.Normal, FontStretch.Expanded),
			new FontCollectionKey(FontStyle.Normal, FontWeight.Bold, FontStretch.Normal),
			new FontCollectionKey(FontStyle.Italic, FontWeight.Normal, FontStretch.Normal)
		}, keys);
	}

	[PresentationTestMethod]
	public void CompareToIsConsistentWithEquality()
	{
		var a = new FontCollectionKey(FontStyle.Oblique, FontWeight.Medium, FontStretch.SemiExpanded);
		var b = new FontCollectionKey(FontStyle.Oblique, FontWeight.Medium, FontStretch.SemiExpanded);
		var c = new FontCollectionKey(FontStyle.Oblique, FontWeight.Medium, FontStretch.SemiCondensed);

		CornerstoneTest.IsTrue(a.Equals(b));
		CornerstoneTest.AreEqual(0, a.CompareTo(b));

		CornerstoneTest.IsFalse(a.Equals(c));
		CornerstoneTest.AreNotEqual(0, a.CompareTo(c));
	}

	[PresentationTestMethod]
	public void EqualKeysCompareEqual()
	{
		var a = new FontCollectionKey(FontStyle.Italic, FontWeight.Bold, FontStretch.Condensed);
		var b = new FontCollectionKey(FontStyle.Italic, FontWeight.Bold, FontStretch.Condensed);

		CornerstoneTest.AreEqual(0, a.CompareTo(b));
		CornerstoneTest.IsFalse(a < b);
		CornerstoneTest.IsFalse(a > b);
		CornerstoneTest.IsTrue(a <= b);
		CornerstoneTest.IsTrue(a >= b);
	}

	[PresentationTestMethod]
	public void NonGenericCompareToThrowsForWrongType()
	{
		var key = new FontCollectionKey(FontStyle.Normal, FontWeight.Normal, FontStretch.Normal);

		Assert.Throws<ArgumentException>(() => key.CompareTo("not a key"));
	}

	[PresentationTestMethod]
	public void NonGenericCompareToTreatsNullAsSmaller()
	{
		var key = new FontCollectionKey(FontStyle.Normal, FontWeight.Normal, FontStretch.Normal);

		CornerstoneTest.AreEqual(1, key.CompareTo(null));
	}

	[PresentationTestMethod]
	public void StretchIsTheTertiarySortKeyWhenStyleAndWeightMatch()
	{
		var condensed = new FontCollectionKey(FontStyle.Normal, FontWeight.Normal, FontStretch.Condensed);
		var expanded = new FontCollectionKey(FontStyle.Normal, FontWeight.Normal, FontStretch.Expanded);

		CornerstoneTest.IsTrue(condensed.CompareTo(expanded) < 0);
		CornerstoneTest.IsTrue(condensed < expanded);
	}

	[PresentationTestMethod]
	public void StyleIsThePrimarySortKey()
	{
		// Normal style, max weight/stretch
		var normal = new FontCollectionKey(FontStyle.Normal, FontWeight.Black, FontStretch.UltraExpanded);

		// Italic style, min weight/stretch
		var italic = new FontCollectionKey(FontStyle.Italic, FontWeight.Thin, FontStretch.UltraCondensed);

		CornerstoneTest.IsTrue(normal.CompareTo(italic) < 0);
		CornerstoneTest.IsTrue(italic.CompareTo(normal) > 0);
		CornerstoneTest.IsTrue(normal < italic);
	}

	[PresentationTestMethod]
	public void WeightIsTheSecondarySortKeyWhenStyleMatches()
	{
		var light = new FontCollectionKey(FontStyle.Normal, FontWeight.Light, FontStretch.UltraExpanded);
		var bold = new FontCollectionKey(FontStyle.Normal, FontWeight.Bold, FontStretch.UltraCondensed);

		CornerstoneTest.IsTrue(light.CompareTo(bold) < 0);
		CornerstoneTest.IsTrue(light < bold);
	}

	#endregion
}