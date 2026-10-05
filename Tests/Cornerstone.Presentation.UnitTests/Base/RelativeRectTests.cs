#region References

using System;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class RelativeRectTests
{
	#region Fields

	private static readonly RelativeRectComparer Compare = new();

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void ParseShouldAcceptAbsoluteValue()
	{
		var result = RelativeRect.Parse("4,5,50,60");

		CornerstoneTest.AreEqual(new RelativeRect(4, 5, 50, 60, RelativeUnit.Absolute), result, Compare);
	}

	[PresentationTestMethod]
	public void ParseShouldAcceptRelativeValue()
	{
		var result = RelativeRect.Parse("10%, 20%, 40%, 70%");

		CornerstoneTest.AreEqual(new RelativeRect(0.1, 0.2, 0.4, 0.7, RelativeUnit.Relative), result, Compare);
	}

	[PresentationTestMethod]
	public void ParseShouldThrowMixedValues()
	{
		Assert.Throws<FormatException>(() =>
			RelativeRect.Parse("10%, 20%, 40, 70%"));
	}

	#endregion
}