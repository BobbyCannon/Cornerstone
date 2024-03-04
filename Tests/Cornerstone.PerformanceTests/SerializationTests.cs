#region References

using System.Collections.Generic;
using Cornerstone.Newtonsoft;
using Cornerstone.Serialization.Json;
using Cornerstone.Testing;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.PerformanceTests;

[TestClass]
public class SerializationTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void DateTimeTests()
	{
		foreach (var type in Activator.DateTypes)
		{
			var values = GetValuesForTesting(type);
		}
	}

	public void SerializeSingleObjectForCornerstone(object value, int loopCount = 10000)
	{
		var cornerstone = new JsonSerializer();

		var c = Time(() =>
		{
			for (var i = 0; i < loopCount; i++)
			{
				cornerstone.ToJson(value);
			}
		});

		$"\tC: {c.TotalMilliseconds} ms".Dump();
	}

	public void TestSingleObjectForNewtonsoft(object value, int loopCount = 10000)
	{
		var newtonsoft = new NewtonsoftJsonSerializer();

		var n = Time(() =>
		{
			for (var i = 0; i < loopCount; i++)
			{
				newtonsoft.ToJson(value);
			}
		});

		$"\tN: {n.TotalMilliseconds} ms".Dump();
	}

	private void WarmupSerializers(List<SerializationScenario> scenarios, params IJsonSerializer[] providers)
	{
		foreach (var scenario in scenarios)
		{
			foreach (var provider in providers)
			{
				provider.ToJson(scenario.Value);
				provider.FromJson(scenario.Expected, scenario.Type);
			}
		}
	}

	#endregion
}