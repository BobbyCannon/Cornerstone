#region References

using Cornerstone.Automation.Desktop;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.AutomationTests;

[TestClass]
public class DesktopAutomationTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void InstanceIsCached()
	{
		var first = DesktopAutomation.Instance;
		var second = DesktopAutomation.Instance;

		IsNotNull(first);
		IsTrue(ReferenceEquals(first, second));
	}

	#endregion
}
