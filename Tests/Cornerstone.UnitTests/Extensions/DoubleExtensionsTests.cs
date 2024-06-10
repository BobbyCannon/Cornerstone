#region References

using System;
using Cornerstone.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class DoubleExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Decrement()
	{
		AreEqual(0.0, double.Epsilon.Decrement());
		AreEqual(-1.2345, 0.0.Decrement(1.2345));
		AreEqual(1.0345, 1.2345.Decrement(0.2));

		var random = new Random();
		var first = random.NextDouble();
		var increment = random.NextDouble();

		AreEqual(first - increment, first.Decrement(increment));
		AreEqual(first, first.Decrement(double.NaN));
		AreEqual(first, first.Decrement(double.PositiveInfinity));
		AreEqual(first, first.Decrement(double.NegativeInfinity));
	}

	[TestMethod]
	public void Increment()
	{
		AreEqual(double.Epsilon, 0.0.Increment());
		AreEqual(1.2345, 0.0.Increment(1.2345));
		AreEqual(3.4345, 2.2.Increment(1.2345));

		var random = new Random();
		var first = random.NextDouble();
		var increment = random.NextDouble();

		AreEqual(first + increment, first.Increment(increment));
		AreEqual(first, first.Increment(double.NaN));
		AreEqual(first, first.Increment(double.PositiveInfinity));
		AreEqual(first, first.Increment(double.NegativeInfinity));

		// Increment negative values
		AreEqual(double.Epsilon * -1, 0.0.Increment(double.Epsilon * -1.0));
	}

	#endregion
}