#region References

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Cornerstone.Compare;
using Cornerstone.Compare.Comparers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Compare.Comparers;

[TestClass]
public class DictionaryComparerTests : ComparerTests
{
	#region Methods

	[TestMethod]
	public void CompareDictionaries()
	{
		var actual = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };
		var expected = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };
		var notExpected = new Dictionary<string, int> { { "A", 1 }, { "B", 2 } };
		var notExpected2 = new Dictionary<string, int> { { "a", 2 }, { "b", 1 } };
		var session = new CompareSession();
		var comparer = new DictionaryComparer();

		AreEqual(CompareResult.AreEqual, comparer.Compare(session, expected, actual));
		AreEqual(CompareResult.AreEqual, comparer.Compare(session, expected, new ReadOnlyDictionary<string, int>(actual)));
		AreEqual(CompareResult.AreEqual, comparer.Compare(session, new ReadOnlyDictionary<string, int>(expected), actual));
		AreEqual(CompareResult.AreEqual, comparer.Compare(session, expected, actual));
		AreEqual(CompareResult.AreEqual, comparer.Compare(session, expected, new ReadOnlyDictionary<string, int>(actual)));
		AreEqual(CompareResult.AreEqual, comparer.Compare(session, new ReadOnlyDictionary<string, int>(expected), actual));
		AreEqual(CompareResult.NotEqual, comparer.Compare(session, notExpected, actual));
		AreEqual(CompareResult.NotEqual, comparer.Compare(session, notExpected, new ReadOnlyDictionary<string, int>(actual)));
		AreEqual(CompareResult.NotEqual, comparer.Compare(session, new ReadOnlyDictionary<string, int>(notExpected), actual));
		AreEqual(CompareResult.NotEqual, comparer.Compare(session, notExpected2, actual));
		AreEqual(CompareResult.NotEqual, comparer.Compare(session, notExpected2, new ReadOnlyDictionary<string, int>(actual)));
		AreEqual(CompareResult.NotEqual, comparer.Compare(session, new ReadOnlyDictionary<string, int>(notExpected2), actual));
	}

	[TestMethod]
	public void LengthsNotEqual()
	{
		var actual = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };
		var expected = new Dictionary<string, int> { { "a", 1 } };
		var session = new CompareSession();
		var comparer = new DictionaryComparer();

		AreEqual(CompareResult.NotEqual, comparer.Compare(session, expected, actual));
		AreEqual("The dictionary lengths are different. 1 != 2", session.Differences.ToString());

		session = new CompareSession();
		AreEqual(CompareResult.NotEqual, comparer.Compare(session, actual, expected));
		AreEqual("The dictionary lengths are different. 2 != 1", session.Differences.ToString());
	}

	#endregion
}