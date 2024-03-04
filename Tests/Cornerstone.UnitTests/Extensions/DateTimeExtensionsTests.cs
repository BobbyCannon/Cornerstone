#region References

using System;
using Cornerstone.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class DateTimeExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ToDateOnly()
	{
		AreEqual(new DateOnly(2024, 01, 02), new DateTime(2024, 01, 02, 03, 04, 05, 06, DateTimeKind.Utc).ToDateOnly());
	}

	#endregion
}