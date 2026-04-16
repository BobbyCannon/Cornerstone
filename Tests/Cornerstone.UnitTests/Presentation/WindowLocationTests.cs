#region References

using Cornerstone.Presentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Presentation;

[TestClass]
public class WindowLocationTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void IsDefaultLocation()
	{
		var location = new WindowLocation();
		AreEqual(true, location.IsDefaultLocation());
		location.Top = 0;
		AreEqual(false, location.IsDefaultLocation());
	}

	#endregion
}