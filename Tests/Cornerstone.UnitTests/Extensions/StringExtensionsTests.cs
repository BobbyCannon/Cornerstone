#region References

using Cornerstone.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Extensions;

[TestClass]
public class StringExtensionsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CamelCaseMatch()
	{
		IsTrue("theQuickBrown".CamelCaseMatch("tqb"));
		IsTrue("TheQuickBrown".CamelCaseMatch("tqb"));
		IsTrue("The Quick Brown".CamelCaseMatch("tqb"));
	}

	[TestMethod]
	public void EndsWithStartOf()
	{
		IsTrue("ABC".EndsWithStartOf("BC", out var match, false), match);
		IsTrue("ABC".EndsWithStartOf("bc", out match, true), match);
		IsTrue("ABC-123".EndsWithStartOf("bc-123", out match, true), match);
		IsTrue("ABC-123".EndsWithStartOf("bC-123", out match, true), match);
		IsTrue("ABC-123".EndsWithStartOf("Bc-123", out match, true), match);

		IsTrue("cd 'C:\\Program Files\\Win".EndsWithStartOf("'C:\\Program Files\\Windows Defender'", out match, false));
		AreEqual("'C:\\Program Files\\Win", match);

		// These should not match and should not exception
		((string) null).EndsWithStartOf(null, out _, false);
	}

	[TestMethod]
	public void IndexOfAny()
	{
		//           0123 4 567
		var value = "123\r\n456";
		AreEqual(-1, value.IndexOfAny([' '], 2, 2));
		AreEqual(3, value.IndexOfAny(['\r'], 3, 5));
		AreEqual(3, value.IndexOfAny(['\r']));
		AreEqual(7, value.IndexOfAny(['6'], 7, 1));
	}

	[TestMethod]
	public void IndexOfAnyReverse()
	{
		//           0123 4 567
		var value = "123\r\n456";
		AreEqual(-1, value.IndexOfAnyReverse(['6'], 8));
		AreEqual(7, value.IndexOfAnyReverse(['6'], 7));
		AreEqual(3, value.IndexOfAnyReverse(['\r'], 5));
		AreEqual(3, value.IndexOfAnyReverse(['\r'], 3));

		AreEqual(-1, string.Empty.IndexOfAnyReverse([' '], 0));
		AreEqual(-1, string.Empty.IndexOfAnyReverse([' '], 10));
	}

	[TestMethod]
	public void IsQueryString()
	{
		IsTrue("?".IsQueryString());
	}

	#endregion
}