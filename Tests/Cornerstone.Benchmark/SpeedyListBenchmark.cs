#region References

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Benchmark;

[MemoryDiagnoser]
[ThreadingDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class SpeedyListBenchmark
{
	#region Fields

	[Params(5000)]
	public int Loops;

	[Params(16)]
	public int MaxDegree;

	private readonly ConcurrentStack<int> _concurrentStack;
	private readonly List<int> _list;
	private readonly SpeedyList<int> _speedyList;

	#endregion

	#region Constructors

	public SpeedyListBenchmark()
	{
		_concurrentStack = new ConcurrentStack<int>();
		_list = new List<int>();
		_speedyList = new SpeedyList<int>();
	}

	#endregion

	#region Methods

	[Benchmark]
	public void AddForConcurrentStack()
	{
		for (var i = 0; i < Loops; i++)
		{
			_concurrentStack.Push(i);
		}
	}

	[Benchmark]
	public void AddForConcurrentStackParallel()
	{
		var options = new ParallelOptions { MaxDegreeOfParallelism = MaxDegree };
		Parallel.For(0, Loops, options, x => _concurrentStack.Push(x));
	}

	[Benchmark]
	public void AddForList()
	{
		for (var i = 0; i < Loops; i++)
		{
			_list.Add(i);
		}
	}

	[Benchmark]
	public void AddForSpeedyList()
	{
		for (var i = 0; i < Loops; i++)
		{
			_speedyList.Add(i);
		}
	}

	[Benchmark]
	public void AddForSpeedyListParallel()
	{
		var options = new ParallelOptions { MaxDegreeOfParallelism = MaxDegree };
		Parallel.For(0, Loops, options, x => _speedyList.Add(x));
	}

	[IterationSetup]
	public void Setup()
	{
		_concurrentStack.Clear();
		_list.Clear();
		_speedyList.Clear();
	}

	#endregion
}