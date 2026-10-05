#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Collections;
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
		var data = Enumerable.Range(0, 1_000_000).Select(i => (char) (' ' + (i % 95))).ToArray();

		// Default-capacity load of 1M chars grows once to ~4 MB; 5 MB is slack, not a noise-sensitive micro-budget.
		ValidatePerformance($"GapBuffer<char> {data.Length:N0} characters",
			() =>
			{
				var buffer = new GapBuffer<char>();
				buffer.Add(data);
			},
			int.MaxValue, 5_000_000, 5, 10
		);

		var loaded = new GapBuffer<char>();
		loaded.Add(data);
		AreEqual(1_000_000, loaded.Count);
		IsTrue(data.AsSpan().SequenceEqual(loaded.ToArray()));
	}

	#endregion
}