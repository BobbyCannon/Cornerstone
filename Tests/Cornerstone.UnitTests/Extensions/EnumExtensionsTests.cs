#region References

using Cornerstone.Automation.Web;
using Cornerstone.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class EnumExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Count()
	{
		AreEqual(5, EnumExtensions.Count<BrowserType>());
	}

	#endregion
}