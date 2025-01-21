#region References

using System.Linq;
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

	[TestMethod]
	public void GetEnumValues()
	{
		AreEqual(new[]
			{
				BrowserType.None,
				BrowserType.Chrome,
				BrowserType.Edge,
				BrowserType.Firefox,
				BrowserType.All
			},
			EnumExtensions.GetEnumValues(typeof(BrowserType)).ToArray()
		);
	}

	[TestMethod]
	public void GetFlagValues()
	{
		AreEqual(new[]
			{
				BrowserType.Chrome,
				BrowserType.Edge,
				BrowserType.Firefox
			},
			typeof(BrowserType).GetFlagValues().ToArray()
		);
	}

	#endregion
}