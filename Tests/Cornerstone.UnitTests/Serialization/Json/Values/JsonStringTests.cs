#region References

using System.Collections.Generic;
using Cornerstone.Serialization.Json.Values;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Serialization.Json.Values;

[TestClass]
public class JsonStringTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void EscapeCharacter()
	{
		var builder = new TextBuilder();
		var scenarios = new Dictionary<char, string>
		{
			{ '\"', "\\\"" },
			{ '\\', "\\\\" },
			{ '/', "\\/" },
			{ '\b', "\\b" },
			{ '\f', "\\f" },
			{ '\n', "\\n" },
			{ '\r', "\\r" },
			{ '\t', "\\t" },
			{ '\0', "\\u0000" }
		};

		foreach (var scenario in scenarios)
		{
			builder.Clear();
			JsonString.Escape(scenario.Key, builder);
			AreEqual(scenario.Value, builder);
		}
	}

	[TestMethod]
	public void EscapeString()
	{
		var builder = new TextBuilder();
		var scenarios = new Dictionary<string, string>
		{
			{ "\"\\/\b\f\n\r\t\0", "\\\"\\\\\\/\\b\\f\\n\\r\\t\\u0000" }
		};

		foreach (var scenario in scenarios)
		{
			builder.Clear();
			JsonString.Escape(scenario.Key, builder);
			AreEqual(scenario.Value, builder);
		}
	}

	#endregion
}