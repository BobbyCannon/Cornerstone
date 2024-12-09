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
public class SpeedyDictionaryBenchmark
{
	#region Fields

	[Params(10000)]
	public int Loops;
	
	[Params(16)]
	public int MaxDegree;

	private readonly ConcurrentDictionary<int, int> _concurrentDictionary;
	private readonly Dictionary<int, int> _dictionary;
	private readonly SpeedyDictionary<int, int> _speedyDictionary;

	#endregion

	#region Constructors

	public SpeedyDictionaryBenchmark()
	{
		_concurrentDictionary = new ConcurrentDictionary<int, int>();
		_dictionary = new Dictionary<int, int>();
		_speedyDictionary = new SpeedyDictionary<int, int>();
	}

	#endregion

	#region Methods

	[Benchmark]
	public void AddRangeForConcurrentDictionary()
	{
		for (var i = 0; i < Loops; i++)
		{
			_concurrentDictionary.TryAdd(i, i);
		}
	}

	[Benchmark]
	public void AddRangeForConcurrentDictionaryParallel()
	{
		var options = new ParallelOptions { MaxDegreeOfParallelism = MaxDegree };
		Parallel.For(0, Loops, options, x => _concurrentDictionary.TryAdd(x, x));
	}

	[Benchmark]
	public void AddRangeForDictionary()
	{
		for (var i = 0; i < Loops; i++)
		{
			_dictionary.Add(i, i);
		}
	}

	[Benchmark]
	public void AddRangeForSpeedyDictionary()
	{
		for (var i = 0; i < Loops; i++)
		{
			_speedyDictionary.Add(i, i);
		}
	}

	[Benchmark]
	public void AddRangeForSpeedyDictionaryParallel()
	{
		var options = new ParallelOptions { MaxDegreeOfParallelism = MaxDegree };
		Parallel.For(0, Loops, options, x => _speedyDictionary.Add(x, x));
	}

	[IterationSetup]
	public void Setup()
	{
		_concurrentDictionary.Clear();
		_dictionary.Clear();
		_speedyDictionary.Clear();
	}

	#endregion
}