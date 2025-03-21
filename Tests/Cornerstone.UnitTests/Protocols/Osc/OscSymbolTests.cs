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
		IsTrue(expected == actual);
		IsTrue(expected.Equals(actual));
		// ReSharper disable once SuspiciousTypeConversion.Global
		IsTrue(expected.Equals("Foo Bar"));
		AreEqual(1716311859, expected.GetHashCode());
		AreEqual(1716311859, actual.GetHashCode());
	}

	[TestMethod]
	public void NotEquals()
	{
		var notExpected = new OscSymbol { Value = "Foo Bar" };
		var actual = new OscSymbol("foo bar");
		AreEqual(1715228275, actual.GetHashCode());
		IsTrue(notExpected != actual);
		// ReSharper disable once SuspiciousTypeConversion.Global
		IsFalse(actual.Equals(true));
	}

	#endregion
}