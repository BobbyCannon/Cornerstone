#region References

using System.Collections.Generic;
using Cornerstone.Data.Bytes;
using Cornerstone.Profiling;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Profiling;

[TestClass]
public class ByteCounterTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ByteCounter()
	{
		var scenarios = new Dictionary<decimal, string>
		{
			{ 1, "1 B" },
			{ 1024, "1 KB" },
			{ 12345, "12.06 KB" },
			{ 1234567, "1.18 MB" },
			{ 12345678901, "11.5 GB" }
		};

		foreach (var scenario in scenarios)
		{
			var counter = new ByteCounter();
			counter.Increment(scenario.Key);
			var actual = counter.Size.Humanize().Dump();
			AreEqual(scenario.Value, actual);
		}
	}

	#endregion
}