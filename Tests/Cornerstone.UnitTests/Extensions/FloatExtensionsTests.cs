#region References

using System;
using Cornerstone.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class FloatExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Decrement()
	{
		AreEqual(0.0f, float.Epsilon.Decrement());
		AreEqual(-1.2345f, 0.0.Decrement(1.2345f));
		AreEqual(1.0345f, 1.2345f.Decrement(0.2f));

		var random = new Random();
		var first = (float) random.NextDouble();
		var increment = (float) random.NextDouble();

		AreEqual(first - increment, first.Decrement(increment));
		AreEqual(first, first.Decrement(float.NaN));
		AreEqual(first, first.Decrement(float.PositiveInfinity));
		AreEqual(first, first.Decrement(float.NegativeInfinity));
	}

	[TestMethod]
	public void Increment()
	{
		AreEqual(float.Epsilon, 0.0f.Increment());
		AreEqual(1.2345f, 0.0f.Increment(1.2345f));
		AreEqual(3.4345002f, 2.2f.Increment(1.2345f));

		var random = new Random();
		var first = (float) random.NextDouble();
		var increment = (float) random.NextDouble();

		AreEqual(first + increment, first.Increment(increment));
		AreEqual(first, first.Increment(float.NaN));
		AreEqual(first, first.Increment(float.PositiveInfinity));
		AreEqual(first, first.Increment(float.NegativeInfinity));

		// Increment negative values
		AreEqual(float.Epsilon * -1, 0.0f.Increment(float.Epsilon * -1.0f));
	}

	#endregion
}