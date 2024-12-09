#region References

using Cornerstone.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Collections;

[TestClass]
public class SpeedyDictionaryTests : BaseCollectionTests
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

	#endregion
}