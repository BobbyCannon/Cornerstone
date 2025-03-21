#region References

using System.Linq;
using System.Threading.Tasks;
using Cornerstone.Collections;
using Cornerstone.Generators;
using Cornerstone.Testing;
using Cornerstone.Threading;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.PerformanceTests.Collections;

[TestClass]
public class SpeedyListTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void MaximumReaderTest()
	{
		// todo: Check that you cannot have 1000000 readers
	}

	[TestMethod]
	public void ParallelShouldWork()
	{
		var list = new SpeedyList<int>();
		var options = new ParallelOptions
		{
			MaxDegreeOfParallelism = 32
		};

		var add = 0;
		var removeAt = 0;
		var indexOf = 0;
		var clear = 0;
		var indexer = 0;

		Parallel.For(0, 1000, options, i =>
		{
			for (var j = 0; j < 100; ++j)
			{
				//Debug.WriteLine($"+++ i: {i}, j: {j}");

				list.Add((i * 100) + j);
				ThreadSafe.Increment(ref add);

				if ((list.Count > 0) && ((add % 10) == 0))
				{
					try
					{
						list.EnterUpgradeableReadLock();

						if (list.Count > 0)
						{
							//Debug.WriteLine($"--- i: {i}, j: {j}");
							list.RemoveAt(RandomGenerator.NextInteger(0, list.Count / 2));
							ThreadSafe.Increment(ref removeAt);
						}
					}
					finally
					{
						list.ExitUpgradeableReadLock();
					}
				}

				if ((list.Count > 0) && ((RandomGenerator.NextInteger(0, 100) % 3) == 0))
				{
					list.Order();
				}

				if ((list.Count > 0) && ((RandomGenerator.NextInteger(0, 100) % 3) == 0))
				{
					_ = list.IndexOf(j);
					ThreadSafe.Increment(ref indexOf);
				}

				if ((list.Count > 0) && ((RandomGenerator.NextInteger(0, 100) % 3) == 0))
				{
					list.Clear();
					ThreadSafe.Increment(ref clear);
				}

				if ((list.Count > 0) && ((RandomGenerator.NextInteger(0, 100) % 3) == 0))
				{
					try
					{
						list.EnterUpgradeableReadLock();
						if (list.Count > 0)
						{
							var index = RandomGenerator.NextInteger(0, list.Count / 2);
							var _ = list[index];
							list[index] = index;
							ThreadSafe.Increment(ref indexer);
						}
					}
					finally
					{
						list.ExitUpgradeableReadLock();
					}
				}
			}
		});

		$"Add {add}, RemoveAt {removeAt}, IndexOf {indexOf}, Clear {clear}, Indexer {indexer}".Dump();
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