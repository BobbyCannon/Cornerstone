#region References

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cornerstone.Collections;
using Cornerstone.Testing;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.PerformanceTests.Collections;

[TestClass]
public class GapBufferTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void InitialAllocations()
	{
		ValidatePerformance("GapBuffer<char>", () => _ = new GapBuffer<char>(), int.MaxValue, 2600, 1, 1);
		ValidatePerformance("GapBuffer<char> 1024", () => _ = new GapBuffer<char>(1024), int.MaxValue, 2600, 1, 1);

		// System
		ValidatePerformance("List<char> GapDefault", () => _ = new List<char>(GapBuffer<char>.DefaultCapacity), int.MaxValue, 2600, 1, 1);
		ValidatePerformance("List<char> 1024", () => _ = new List<char>(1024), int.MaxValue, 2600, 1, 1);
	}

	[TestMethod]
	public void LoadMillion()
	{
		var buffer = new GapBuffer<char>();
		var data = Enumerable.Range(0, 1_000_000).Select(i => (char) (' ' + (i % 95))).ToArray();

		ValidatePerformance($"GapBuffer<char> {data.Length:N0} characters",
			() => buffer.Add(data),
			int.MaxValue, 1696, 1, 1,
			() => buffer.Clear()
		);

		AreEqual(1000000, buffer.Count);
		var w = Stopwatch.StartNew();
		var actual = buffer.ToArray();
		w.Stop();
		$"ToArray: {w.Elapsed}".Dump();
		AreEqual(data, actual);
	}

	#endregion
}