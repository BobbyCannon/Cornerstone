#region References

using Cornerstone.Automation;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.AutomationTests;

[TestClass]
public class ResourceServiceTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void GetTestScriptIsCachedAndIncludesNativeFind()
	{
		var first = ResourceService.GetTestScript();
		var second = ResourceService.GetTestScript();

		IsFalse(string.IsNullOrWhiteSpace(first));
		IsTrue(ReferenceEquals(first, second));
		IsTrue(first.Contains("findElements"));
		IsTrue(first.Contains("elementToItem"));
		IsTrue(first.Contains("ensureId"));
		IsFalse(first.Contains("speedyResult"));
	}

	#endregion
}
