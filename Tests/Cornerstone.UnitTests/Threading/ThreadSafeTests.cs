#region References

using System.Threading.Tasks;
using Cornerstone.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Threading;

[TestClass]
public class ThreadSafeTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ThreadSafeDoubleDecrement()
	{
		double actual = 10000;
		Parallel.For(0, 10000, _ =>
		{
			// The comment will fail
			//tasks.Add(new Task(() => actual--));
			ThreadSafe.Decrement(ref actual, 1.0);
		});
		Assert.AreEqual(0, actual);
	}

	[TestMethod]
	public void ThreadSafeDoubleIncrement()
	{
		var expected = 10000;
		double actual = 0;
		Parallel.For(0, expected, _ =>
		{
			// The comment will fail
			//tasks.Add(new Task(() => actual++));
			ThreadSafe.Increment(ref actual, 1.0);
		});
		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void ThreadSafeFloatDecrement()
	{
		float actual = 10000;
		Parallel.For(0, 10000, _ =>
		{
			// The comment will fail
			//tasks.Add(new Task(() => actual--));
			ThreadSafe.Decrement(ref actual, 1.0f);
		});
		Assert.AreEqual(0, actual);
	}

	[TestMethod]
	public void ThreadSafeFloatIncrement()
	{
		var expected = 10000;
		float actual = 0;
		Parallel.For(0, expected, _ =>
		{
			// The comment will fail
			//tasks.Add(new Task(() => actual++));
			ThreadSafe.Increment(ref actual, 1.0f);
		});
		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void ThreadSafeIntDecrement()
	{
		var actual = 10000;

		Parallel.For(0, 10000, _ =>
		{
			// The comment will fail
			//tasks.Add(new Task(() => actual--));
			ThreadSafe.Decrement(ref actual);
		});

		Assert.AreEqual(0, actual);
	}

	[TestMethod]
	public void ThreadSafeIntIncrement()
	{
		var expected = 10000;
		var actual = 0;

		Parallel.For(0, expected, _ =>
		{
			// The comment will fail
			//tasks.Add(new Task(() => actual--));
			ThreadSafe.Increment(ref actual);
		});

		Assert.AreEqual(expected, actual);
	}

	#endregion
}