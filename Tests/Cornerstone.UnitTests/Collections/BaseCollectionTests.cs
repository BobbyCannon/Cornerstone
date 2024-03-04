#region References

using System.Collections.Generic;
using System.Threading.Tasks;
using Cornerstone.Generators;
using Cornerstone.Threading;

#endregion

namespace Cornerstone.UnitTests.Collections;

public abstract class BaseCollectionTests : CornerstoneUnitTest
{
	#region Methods

	protected void TestParallelOperations(IList<int> list)
	{
		var options = new ParallelOptions
		{
			MaxDegreeOfParallelism = 32
		};

		var total = 0;
		var removals = 0;

		Parallel.For(0, 1000, options, i =>
		{
			for (var j = 0; j < 100; ++j)
			{
				list.Add((i * 100) + j);
				ThreadSafe.Increment(ref total);

				if ((total > 0) && ((total % 10) == 0))
				{
					list.RemoveAt(RandomGenerator.NextInteger(0, list.Count / 2));
					ThreadSafe.Increment(ref removals);
				}

				if ((total > 0) && ((RandomGenerator.NextInteger(0, 100) % 3) == 0))
				{
					// todo: fix, this is not thread safe
					var index = RandomGenerator.NextInteger(0, list.Count / 2);
					var _ = list[index];
					list[index] = index;
				}
			}
		});

		AreEqual(100000, total);
		AreEqual(total - removals, list.Count);
	}

	#endregion
}