#region References

using System;
using Cornerstone.Presentation.Media.Fonts;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media.Fonts;

[TestClass]
public class FamilyNameCollectionTests
{
	#region Methods

	[PresentationTestMethod]
	public void ExceptionShouldBeThrownIfNamesIsNull()
	{
		Assert.Throws<ArgumentNullException>(() => new FamilyNameCollection(null!));
	}

	[PresentationTestMethod]
	public void ShouldBeEqual()
	{
		var familyNames = new FamilyNameCollection("Arial, Times New Roman");

		CornerstoneTest.AreEqual(new FamilyNameCollection("Arial, Times New Roman"), familyNames);
	}

	#endregion
}