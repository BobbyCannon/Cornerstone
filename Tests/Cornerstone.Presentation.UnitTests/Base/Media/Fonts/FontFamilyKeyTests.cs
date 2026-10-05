#region References

using System;
using Cornerstone.Presentation.Media.Fonts;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media.Fonts;

[TestClass]
public class FontFamilyKeyTests
{
	#region Methods

	[PresentationTestMethod]
	public void ExceptionShouldBeThrownIfSourceIsNull()
	{
		Assert.Throws<ArgumentNullException>(() => new FontFamilyKey(null!));
	}

	[PresentationTestMethod]
	public void ShouldInitializeWithLocation()
	{
		var source = new Uri("resm:Cornerstone.Presentation.Visuals.UnitTests#MyFont");

		var fontFamilyKey = new FontFamilyKey(source);

		CornerstoneTest.AreEqual(new Uri("resm:Cornerstone.Presentation.Visuals.UnitTests"), fontFamilyKey.Source);
	}

	[PresentationTestMethod]
	public void ShouldInitializeWithLocationAndFilename()
	{
		var source = new Uri("resm:Cornerstone.Presentation.Visuals.UnitTests.MyFont.ttf#MyFont");

		var fontFamilyKey = new FontFamilyKey(source);

		CornerstoneTest.AreEqual(new Uri("resm:Cornerstone.Presentation.Visuals.UnitTests.MyFont.ttf"), fontFamilyKey.Source);
	}

	#endregion
}