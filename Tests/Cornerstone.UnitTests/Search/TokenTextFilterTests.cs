#region References

using System.Collections.Generic;
using Cornerstone.Search;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Search;

[TestClass]
public class TokenTextFilterTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void EmptyOrWhitespaceMatchesAll()
	{
		IsTrue(TokenTextFilter.Matches(null, "Oat Milk"));
		IsTrue(TokenTextFilter.Matches(string.Empty, "Oat Milk", "Dairy"));
		IsTrue(TokenTextFilter.Matches("   ", "Oat Milk", "Dairy", "barista blend"));
	}

	[TestMethod]
	public void HaystackListMatchesAnyField()
	{
		IReadOnlyList<string> fields = ["Honeycrisp Apples", "Produce", "for pie"];
		IsTrue(TokenTextFilter.Matches("honey pie", fields));
		IsFalse(TokenTextFilter.Matches("honey missing", fields));
		IsTrue(TokenTextFilter.Matches(string.Empty, fields));
		IsFalse(TokenTextFilter.Matches("honey", (IReadOnlyList<string>) null));
	}

	[TestMethod]
	public void MatchingIsCaseInsensitive()
	{
		IsTrue(TokenTextFilter.Matches("HONEY crisp", "Honeycrisp Apples"));
		IsTrue(TokenTextFilter.Matches("PIE", string.Empty, "for pie"));
	}

	[TestMethod]
	public void MissingTokenFails()
	{
		IsFalse(TokenTextFilter.Matches("honey missing", "Honeycrisp Apples"));
	}

	[TestMethod]
	public void SingleTokenStillMatchesSubstring()
	{
		IsTrue(TokenTextFilter.Matches("honey", "Honeycrisp Apples"));
		IsFalse(TokenTextFilter.Matches("xyz", "Honeycrisp Apples"));
	}

	[TestMethod]
	public void TokensAndOnSameField()
	{
		IsTrue(TokenTextFilter.Matches("honey crisp", "Honeycrisp Apples"));
	}

	[TestMethod]
	public void TokensMayHitDifferentFields()
	{
		IsTrue(TokenTextFilter.Matches(
			"honey pie",
			"Honeycrisp Apples",
			"Produce",
			"for pie"));
		IsTrue(TokenTextFilter.Matches(
			"wheat bakery",
			"Whole Wheat Bread",
			"Bakery",
			"sandwich loaf"));
	}

	#endregion
}