#region References

using System.Collections.Generic;
using System.Diagnostics;
using Cornerstone.Collections;
using Cornerstone.Testing;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.PerformanceTests.Collections;

[TestClass]
public class BufferTests : CornerstoneUnitTest
{
	#region Constants

	private const int _endOffset = _startOffset + _perfTestSize / 2;
	private const int _perfTestSize = 100_000;
	private const int _startOffset = _perfTestSize / 10;

	#endregion

	#region Methods

	[TestMethod]
	public void Add()
	{
		var scenarios = GetScenarios();

		foreach (var scenario in scenarios)
		{
			var watch = StartTest(scenario);
			for (var i = _startOffset; i < _endOffset; i++)
			{
				scenario.Add('*');
			}
			watch.Stop();
			watch.Elapsed.Dump($"{scenario.GetType().Name}: ");
		}
	}

	[TestMethod]
	public void Insert()
	{
		var scenarios = GetScenarios();

		foreach (var scenario in scenarios)
		{
			var watch = StartTest(scenario);
			for (var i = _startOffset; i < _endOffset; i++)
			{
				scenario.Insert(i, '*');
			}
			watch.Stop();
			watch.Elapsed.Dump($"{scenario.GetType().Name}: ");
		}
	}

	[TestMethod]
	public void Remove()
	{
		var scenarios = GetScenarios();

		foreach (var scenario in scenarios)
		{
			var watch = StartTest(scenario);
			for (var i = _startOffset; i < _endOffset; i++)
			{
				scenario.RemoveAt(_startOffset);
			}
			watch.Stop();
			watch.Elapsed.Dump($"{scenario.GetType().Name}: ");
		}
	}

	private static IList<char>[] GetScenarios()
	{
		return
		[
			new GapBuffer<char>(),
			new List<char>()
		];
	}

	private Stopwatch StartTest<T>(T list) where T : IList<char>
	{
		for (var i = 0; i < _perfTestSize; i++)
		{
			list.Add('*');
		}
		return Stopwatch.StartNew();
	}

	#endregion
}