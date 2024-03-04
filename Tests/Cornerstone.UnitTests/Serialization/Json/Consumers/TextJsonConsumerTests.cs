#region References

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Serialization;
using Cornerstone.Serialization.Json.Consumers;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Serialization.Json.Consumers;

[TestClass]
public class TextJsonConsumerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Indented()
	{
		var scenarios = new Dictionary<string, ISerializationOptions>
		{
			{
				"{\r\n\t\"Age\": 21,\r\n\t\"Name\": \"John Doe\"\r\n}",
				new SerializationOptions { TextFormat = TextFormat.Indented }
			},
			{
				"{\"Age\":21,\"Name\":\"John Doe\"}",
				new SerializationOptions { TextFormat = TextFormat.None }
			},
			{
				"{\"age\":21,\"name\":\"John Doe\"}",
				new SerializationOptions { NamingConvention = NamingConvention.CamelCase }
			}
		};

		foreach (var scenario in scenarios)
		{
			var consumer = new TextJsonConsumer(scenario.Value);
			var sample = new Sample();
			consumer.WriteObject(sample);
			var actual = consumer.ToString();
			AreEqual(scenario.Key, actual);
		}
	}

	#endregion

	#region Classes

	[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
	private class Sample
	{
		#region Constructors

		public Sample()
		{
			Name = "John Doe";
			Age = 21;
		}

		#endregion

		#region Properties

		public int Age { get; set; }

		public string Name { get; set; }

		#endregion
	}

	#endregion
}