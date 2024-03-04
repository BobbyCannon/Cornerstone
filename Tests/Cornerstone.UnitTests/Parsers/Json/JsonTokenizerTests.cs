#region References

using System.Collections.Generic;
using System.IO;
using Cornerstone.Parsers.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Parsers.Json;

[TestClass]
public class JsonTokenizerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void SimpleTests()
	{
		var scenarios = new Dictionary<string, JsonTokenData[]>
		{
			{
				"{}",
				[
					new JsonTokenData { Position = 0 },
					new JsonTokenData { Position = 1 },
				]
			}
		};

		foreach (var scenario in scenarios)
		{
			using var r = new StringReader(scenario.Key);
			var t = new JsonTokenizer(r);

			foreach (var data in scenario.Value)
			{
				AreEqual(t.CurrentToken.Position, data.Position);
				t.MoveNext();
			}
		}
	}

	#endregion
}