#region References

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.Shared.Storage.Server;

#endregion

namespace Cornerstone.UnitTests.Internal;

[TestClass]
public class HashCodeCalculatorTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Combine()
	{
		AreEqual(181782913, HashCodeCalculator.Combine("Foo"));
		AreEqual(1113753849, HashCodeCalculator.Combine("Hello World", 1.234));
	}

	#endregion
}