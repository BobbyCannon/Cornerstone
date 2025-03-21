#region References

using System.Collections.Generic;
using Cornerstone.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Collections;

[TestClass]
public class SpeedyDictionaryTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Add()
	{
		var dictionary = new SpeedyDictionary<int, string>();
		AreEqual(0, dictionary.Count);

		dictionary.Add(1, "One");
		AreEqual(1, dictionary.Count);
	}

	[TestMethod]
	public void UpdateWith()
	{
		var list = new SpeedyDictionary<string, int>();
		AreEqual(0, list.Count);
		AreEqual(0, list.Keys.Count);

		var scenarios = new IDictionary<string, int>[]
		{
			new Dictionary<string, int>
			{
				{ "Foo", 1 }, { "Bar", 2 }, { "Hello", 1 }
			}
		};

		foreach (var item in scenarios)
		{
			list.UpdateWith(item);
			AreEqual(item, list);
		}
	}

	#endregion
}