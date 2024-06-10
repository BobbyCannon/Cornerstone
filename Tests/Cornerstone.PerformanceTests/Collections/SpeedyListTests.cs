#region References

using System.Linq;
using System.Threading.Tasks;
using Cornerstone.Collections;
using Cornerstone.UnitTests.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.PerformanceTests.Collections;

[TestClass]
public class SpeedyListTests : BaseCollectionTests
{
	#region Methods

	[TestMethod]
	public virtual void ParallelShouldWork()
	{
		var list = new SpeedyList<int>();
		TestParallelOperations(list);
	}

	[TestMethod]
	public void ProcessThenOrderWithParallelAction()
	{
		var list = new SpeedyList<int>();
		list.OrderBy = [new OrderBy<int>(x => x)];

		var expected = Enumerable.Range(1, 99).ToArray();
		list.InitializeProfiler();

		AreEqual(0, list.Profiler.OrderCount.Value);

		list.ProcessThenOrder(() =>
			Parallel.For(1, 100, x => { list.ProcessThenOrder(() => list.Add(x)); })
		);

		AreEqual(expected, list.ToArray());
		AreEqual(1, list.Profiler.OrderCount.Value);
	}

	[TestMethod]
	public void ThreadSafeOrderedCollection()
	{
		var count = 1000;
		var collection = new SpeedyList<int> { OrderBy = [new OrderBy<int>(x => x)] };
		var options = new ParallelOptions { MaxDegreeOfParallelism = 32 };

		Parallel.For(0, 1000, options, (x, _) => { collection.Add(x); });

		var expected = Enumerable.Range(0, count).ToArray();
		var actual = collection.ToArray();

		AreEqual(expected, actual);
	}

	#endregion
}