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
		IsTrue("cd 'C:\\Program Files\\Win".EndsWithStartOf("'C:\\Program Files\\Windows Defender'", out var match, false));
		AreEqual("'C:\\Program Files\\Win", match);

		IsTrue("ABC".EndsWithStartOf("BCD", out match, false), match);
		IsTrue("ABC".EndsWithStartOf("bcd", out match, true), match);
		IsTrue("ABC-123".EndsWithStartOf("bc-1234", out match, true), match);
		IsTrue("ABC-123".EndsWithStartOf("bC-1234", out match, true), match);
		IsTrue("ABC-123".EndsWithStartOf("Bc-1234", out match, true), match);

		// These should not match and should not exception
		((string) null).EndsWithStartOf(null, out _, false);

		IsFalse("CD".EndsWithStartOf("BCD", out match, false), match);
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

	[TestMethod]
	public void ToMd5HashHexString()
	{
		var data = "7142AA46-BC9E-4527-86AC-D627DDE6D437";
		var value = data.ToMd5HashHexString();
		AreEqual("9122aad21df449f09bfa00c808c47be9", value);
	}

	#endregion
}