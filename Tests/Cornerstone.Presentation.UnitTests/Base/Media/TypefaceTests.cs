#region References

using System;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class TypefaceTests
{
	#region Methods

	[PresentationTestMethod]
	public void ExceptionShouldBeThrownIfFontWeightLessThanEqualToZero()
	{
		Assert.Throws<ArgumentException>(() => new Typeface("foo", (FontStyle) 12, 0));
	}

	[PresentationTestMethod]
	public void ShouldBeEqual()
	{
		CornerstoneTest.AreEqual(new Typeface("Font A"), new Typeface("Font A"));
	}

	[DataRow("Hello World 6", "Hello World 6", FontStyle.Normal, FontWeight.Normal)]
	[DataRow("Hello World Italic", "Hello World", FontStyle.Italic, FontWeight.Normal)]
	[DataRow("Hello World Italic Bold", "Hello World", FontStyle.Italic, FontWeight.Bold)]
	[DataRow("FontAwesome 6 Free Regular", "FontAwesome 6 Free", FontStyle.Normal, FontWeight.Normal)]
	[DataRow("FontAwesome 6 Free Solid", "FontAwesome 6 Free", FontStyle.Normal, FontWeight.Solid)]
	[DataRow("FontAwesome 6 Brands", "FontAwesome 6 Brands", FontStyle.Normal, FontWeight.Normal)]
	[PresentationTestMethod]
	public void ShouldGetImplicitTypeface(string input, string familyName, FontStyle style, FontWeight weight)
	{
		var typeface = new Typeface(input);

		var normalizedTypeface = typeface.Normalize(out var normalizedFamilyName);

		CornerstoneTest.AreEqual(familyName, normalizedFamilyName);
		CornerstoneTest.AreEqual(style, normalizedTypeface.Style);
		CornerstoneTest.AreEqual(weight, normalizedTypeface.Weight);
		CornerstoneTest.AreEqual(FontStretch.Normal, normalizedTypeface.Stretch);
	}

	[PresentationTestMethod]
	public void ShouldHaveEqualHash()
	{
		CornerstoneTest.AreEqual(new Typeface("Font A").GetHashCode(), new Typeface("Font A").GetHashCode());
	}

	#endregion
}