#region References

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Compare;

[TestClass]
public class EqualsComparerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void UriEqualsIgnoresAbsoluteUriStringMismatch()
	{
		var withFragment = new Uri("resm:Cornerstone.Presentation.Visuals.UnitTests#MyFont");
		var withoutFragment = new Uri("resm:Cornerstone.Presentation.Visuals.UnitTests");

		IsTrue(withFragment.Equals(withoutFragment));
		AreEqual(withoutFragment, withFragment);
	}

	#endregion
}