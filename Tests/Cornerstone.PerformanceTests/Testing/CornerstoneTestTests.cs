#region References

using System;
using System.Threading;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.PerformanceTests.Testing;

[TestClass]
public class CornerstoneTestTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AreEqual()
	{
		ValidatePerformance("String.Equals(Ordinal)",
			() => _ = string.Equals("1", "2", StringComparison.Ordinal), 40, 30);

		ValidatePerformance("String.Equals(OrdinalIgnoreCase)",
			() => _ = string.Equals("1", "2", StringComparison.OrdinalIgnoreCase), 40, 30);
	}

	[TestMethod]
	public void ValidateExceptions()
	{
		ExpectedException<Exception>(() => ValidatePerformance("Time Failed", () => Thread.Sleep(1), 0, int.MaxValue, 1, 10), "Timing failed:");
		ExpectedException<Exception>(() => ValidatePerformance("Allocation Failed", () => _ = new int[1024], int.MaxValue, 0, 1, 10), "Allocation failed:");
	}

	#endregion
}