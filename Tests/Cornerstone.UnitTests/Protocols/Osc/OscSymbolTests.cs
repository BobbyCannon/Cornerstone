#region References

using Cornerstone.Protocols.Osc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Protocols.Osc;

[TestClass]
public class OscSymbolTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Equals()
	{
		var expected = new OscSymbol { Value = "Foo Bar" };
		var actual = new OscSymbol("Foo Bar");
		Assert.IsTrue(expected == actual);
		Assert.IsTrue(expected.Equals(actual));
		// ReSharper disable once SuspiciousTypeConversion.Global
		Assert.IsTrue(expected.Equals("Foo Bar"));
		Assert.AreEqual(1716311859, expected.GetHashCode());
		Assert.AreEqual(1716311859, actual.GetHashCode());
	}

	[TestMethod]
	public void NotEquals()
	{
		var notExpected = new OscSymbol { Value = "Foo Bar" };
		var actual = new OscSymbol("foo bar");
		Assert.AreEqual(1715228275, actual.GetHashCode());
		Assert.IsTrue(notExpected != actual);
		// ReSharper disable once SuspiciousTypeConversion.Global
		Assert.IsFalse(actual.Equals(true));
	}

	#endregion
}