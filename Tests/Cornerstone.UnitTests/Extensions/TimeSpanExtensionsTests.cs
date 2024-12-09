#region References

using System;
using System.Collections.Generic;
using Cornerstone.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class TimeSpanExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void PercentOf()
	{
		AreEqual(10.0m, TimeSpan.FromSeconds(1).PercentOf(TimeSpan.FromSeconds(10)));
		AreEqual(1.0m, TimeSpan.FromSeconds(1).PercentOf(TimeSpan.FromSeconds(100)));
		AreEqual(22.22m, TimeSpan.FromSeconds(2).PercentOf(TimeSpan.FromSeconds(9)));
	}

	[TestMethod]
	public void ToMinString()
	{
		var scenarios = new Dictionary<string, TimeSpan>
		{
			{ "00.0000001", TimeSpan.FromTicks(1) },
			{ "00.000001", TimeSpan.FromMicroseconds(1) },
			{ "00.000999", TimeSpan.FromMicroseconds(999) },
			{ "00.001", TimeSpan.FromMilliseconds(1) },
			{ "00.999", TimeSpan.FromMilliseconds(999) },
			{ "00", TimeSpan.Zero }
		};

		foreach (var scenario in scenarios)
		{
			AreEqual(scenario.Key, scenario.Value.ToMinString());
		}
	}

	#endregion
}