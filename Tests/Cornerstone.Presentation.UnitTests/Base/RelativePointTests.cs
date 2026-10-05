#region References

using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class RelativePointTests
{
	#region Methods

	[PresentationTestMethod]
	public void ParseShouldAcceptAbsoluteValue()
	{
		var result = RelativePoint.Parse("4,5");

		CornerstoneTest.AreEqual(new RelativePoint(4, 5, RelativeUnit.Absolute), result);
	}

	[PresentationTestMethod]
	public void ParseShouldAcceptRelativeValue()
	{
		var result = RelativePoint.Parse("25%, 50%");

		CornerstoneTest.AreEqual(new RelativePoint(0.25, 0.5, RelativeUnit.Relative), result);
	}

	#endregion
}