#region References

using Cornerstone.Presentation.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation.Controls;

[TestClass]
public class BreadcrumbTrailTests
{
	#region Methods

	[TestMethod]
	public void HiddenPrefixCountKeepsAllWhenTheyFit()
	{
		var hide = BreadcrumbTrail.HiddenPrefixCount([40d, 40d, 40d], 10, 200, 30);
		Assert.AreEqual(0, hide);
	}

	[TestMethod]
	public void HiddenPrefixCountKeepsSingleItem()
	{
		var hide = BreadcrumbTrail.HiddenPrefixCount([400d], 10, 50, 30);
		Assert.AreEqual(0, hide);
	}

	[TestMethod]
	public void HiddenPrefixCountHidesLeadingPrefix()
	{
		var hide = BreadcrumbTrail.HiddenPrefixCount([40d, 40d, 40d, 40d, 40d], 10, 150, 30);
		Assert.AreEqual(3, hide);
	}

	[TestMethod]
	public void HiddenPrefixCountKeepsOnlyTheLastItemWhenNothingElseFits()
	{
		var hide = BreadcrumbTrail.HiddenPrefixCount([40d, 40d, 40d, 40d, 40d], 10, 50, 30);
		Assert.AreEqual(4, hide);
	}

	[TestMethod]
	public void HiddenPrefixCountLeavesAnUnconstrainedRowAlone()
	{
		var hide = BreadcrumbTrail.HiddenPrefixCount([40d, 40d, 40d], 10, double.PositiveInfinity, 30);
		Assert.AreEqual(0, hide);
	}

	#endregion
}
