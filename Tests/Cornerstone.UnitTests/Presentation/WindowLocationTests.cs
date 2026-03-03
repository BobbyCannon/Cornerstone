#region References

using Cornerstone.Presentation;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests.Presentation;

public class WindowLocationTests : CornerstoneUnitTest
{
	#region Methods

	[Test]
	public void IsDefaultLocation()
	{
		var location = new WindowLocation();
		AreEqual(true, location.IsDefaultLocation());
		location.Top = 0;
		AreEqual(false, location.IsDefaultLocation());
	}

	#endregion
}