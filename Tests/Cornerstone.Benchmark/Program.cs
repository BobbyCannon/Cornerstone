#region References

using BenchmarkDotNet.Running;

#endregion

namespace Cornerstone.Benchmark;

public class Program
{
	#region Methods

	private static void Main(string[] args)
	{
		BenchmarkRunner.Run<SpeedyDictionaryBenchmark>(args: args);
		//BenchmarkRunner.Run<SpeedyListBenchmark>(args: args);
	}

	#endregion
}