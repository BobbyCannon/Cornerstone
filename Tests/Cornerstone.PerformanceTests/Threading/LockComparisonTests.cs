#region References

using System;
using System.Linq;
using System.Threading.Tasks;
using Cornerstone.Threading;
using Cornerstone.UnitTests.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.PerformanceTests.Threading;

[TestClass]
public class LockComparisonTests : BaseLockableTests
{
	#region Fields

	private static int _num, _collisions;

	#endregion

	#region Methods

	[TestMethod]
	public void TestLocks()
	{
		const int incrementTimes = 5;
		const int iterations = 10_000;

		Console.WriteLine("Executing units: " + Environment.ProcessorCount);

		Test([
				() => ReadUnlocked(iterations, incrementTimes),
				() => WriteUnlocked(iterations, incrementTimes)
			],
			"Unlocked",
			iterations, incrementTimes
		);

		foreach (var lockable in GetLocks(true))
		{
			Test([
					() => Read(lockable, iterations, incrementTimes),
					() => Write(lockable, iterations, incrementTimes)
				],
				lockable.GetType().Name.Replace("ReaderWriterLock", ""),
				iterations, incrementTimes
			);
		}
	}

	private static void Read(IReaderWriterLock readerWriterLock, int iterations, int incrementTimes)
	{
		for (var j = 0; j < iterations; j++)
		{
			readerWriterLock.EnterReadLock();
			try
			{
				if (_num % incrementTimes != 0)
				{
					_collisions++;
				}
			}
			finally
			{
				readerWriterLock.ExitReadLock();
			}
		}
	}

	private static void ReadUnlocked(int iterations, int incrementTimes)
	{
		for (var j = 0; j < iterations; j++)
		{
			if (_num % incrementTimes != 0)
			{
				_collisions++;
			}
		}
	}

	private static void Test(Action[] tasks, string test, int iterations, int incrementTimes)
	{
		_num = _collisions = 0;
		tasks = Enumerable.Repeat(tasks, 10).SelectMany(x => x).ToArray();
		var dt = DateTime.Now;
		Parallel.Invoke(tasks);
		var dt2 = DateTime.Now;
		Console.WriteLine();
		Console.WriteLine(test + ":");
		Console.WriteLine("ms: " + (dt2 - dt).TotalMilliseconds);
		Console.WriteLine("read collisions: " + _collisions);
		Console.WriteLine("result: " + _num);
		Console.WriteLine("expected result: " + iterations * incrementTimes * tasks.Length / 2);
	}

	private static void Write(IReaderWriterLock readerWriterLock, int iterations, int incrementTimes)
	{
		for (var j = 0; j < iterations; j++)
		{
			readerWriterLock.EnterWriteLock();
			try
			{
				for (var i = 0; i < incrementTimes; i++)
				{
					_num++;
				}
			}
			finally
			{
				readerWriterLock.ExitWriteLock();
			}
		}
	}

	private static void WriteUnlocked(int iterations, int incrementTimes)
	{
		for (var j = 0; j < iterations; j++)
		for (var i = 0; i < incrementTimes; i++)
		{
			_num++;
		}
	}

	#endregion
}