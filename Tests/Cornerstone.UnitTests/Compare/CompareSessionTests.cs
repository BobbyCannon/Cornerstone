#region References

using System;
using Cornerstone.Compare;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Compare;

[TestClass]
public class CompareSessionTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AddDifferenceShouldIncludePathWhenAvailable()
	{
		var session = new CompareSession();
		session.Path.Push("Root");
		session.Path.Push("Child");
		session.AddDifference("Value mismatch.");

		var result = session.Differences.ToString();

		// Path entries are joined with "."
		IsTrue(result.Contains("Root") || result.Contains("Child"));
		IsTrue(result.Contains("Value mismatch."));
	}

	[TestMethod]
	public void AddDifferenceWithExpectedAndActualShouldSetNotEqual()
	{
		var session = new CompareSession();
		session.AddDifference("expected", "actual", true);

		AreEqual(CompareResult.NotEqual, session.Result);
		var diff = session.Differences.ToString();
		IsTrue(diff.Contains("expected"));
		IsTrue(diff.Contains("actual"));
		IsTrue(diff.Contains("!="));
	}

	[TestMethod]
	public void AddDifferenceWithShouldNotEqualShouldContainMessage()
	{
		var session = new CompareSession();
		session.AddDifference("expected", "actual", false);

		AreEqual(CompareResult.NotEqual, session.Result);
		var diff = session.Differences.ToString();
		IsTrue(diff.Contains("Should not have equaled"));
	}

	[TestMethod]
	public void AppendObjectTypes()
	{
		var session = new CompareSession();
		session.AddDifference('C', 'B', true);
		AreEqual("C\r\n **** != ****\r\nB\r\n", session.Differences.ToString());
	}

	[TestMethod]
	public void CompareBothNullShouldBeEqual()
	{
		var settings = new ComparerSettings();
		var session = new CompareSession<string, string>(null, null, settings);
		session.Compare();

		AreEqual(CompareResult.AreEqual, session.Result);
	}

	[TestMethod]
	public void CompareExpectedNotNullActualNullShouldBeNotEqual()
	{
		var settings = new ComparerSettings();
		var session = new CompareSession<string, string>("value", null, settings);
		session.Compare();

		AreEqual(CompareResult.NotEqual, session.Result);
	}

	[TestMethod]
	public void CompareExpectedNullActualNotNullShouldBeNotEqual()
	{
		var settings = new ComparerSettings();
		var session = new CompareSession<string, string>(null, "value", settings);
		session.Compare();

		AreEqual(CompareResult.NotEqual, session.Result);
	}

	[TestMethod]
	public void ConstructorWithSettingsShouldApplySettings()
	{
		var settings = new ComparerSettings
		{
			IgnoreMissingProperties = true,
			MaxDepth = 5,
			StringComparison = StringComparison.OrdinalIgnoreCase
		};
		var session = new CompareSession(settings);

		IsTrue(session.Settings.IgnoreMissingProperties);
		AreEqual(5, session.Settings.MaxDepth);
		AreEqual(StringComparison.OrdinalIgnoreCase, session.Settings.StringComparison);
	}

	[TestMethod]
	public void DefaultConstructorShouldInitializeDefaults()
	{
		var session = new CompareSession();
		AreEqual(CompareResult.Inconclusive, session.Result);
		AreEqual(string.Empty, session.Differences.ToString());
		AreEqual(0, session.Path.Count);
		IsNotNull(session.Settings);
	}

	[TestMethod]
	public void GenericSessionAssertShouldIncludePrefixInException()
	{
		var settings = new ComparerSettings();
		var session = new CompareSession<string, string>("a", "b", settings);
		session.Compare();

		ExpectedException<CompareException>(
			() => session.Assert(CompareResult.AreEqual, () => "Custom prefix"),
			"Custom prefix"
		);
	}

	[TestMethod]
	public void GenericSessionAssertShouldNotThrowWhenResultMatches()
	{
		var settings = new ComparerSettings();
		var session = new CompareSession<int, int>(42, 42, settings);
		session.Compare();

		// Should not throw
		session.Assert(CompareResult.AreEqual);
	}

	[TestMethod]
	public void GenericSessionAssertShouldThrowWhenResultDoesNotMatch()
	{
		var settings = new ComparerSettings();
		var session = new CompareSession<string, string>("a", "b", settings);
		session.Compare();

		ExpectedException<CompareException>(() => session.Assert(CompareResult.AreEqual)
		);
	}

	[TestMethod]
	public void GenericSessionCompareShouldBeEqualForMatchingValues()
	{
		var settings = new ComparerSettings();
		var session = new CompareSession<string, string>("hello", "hello", settings);
		session.Compare();

		AreEqual(CompareResult.AreEqual, session.Result);
		AreEqual("hello", session.Expected);
		AreEqual("hello", session.Actual);
	}

	[TestMethod]
	public void GenericSessionCompareShouldBeNotEqualForDifferentValues()
	{
		var settings = new ComparerSettings();
		var session = new CompareSession<string, string>("hello", "world", settings);
		session.Compare();

		AreEqual(CompareResult.NotEqual, session.Result);
	}

	[TestMethod]
	public void MultipleDifferencesShouldAccumulate()
	{
		var session = new CompareSession();
		session.AddDifference("First issue.");
		session.AddDifference("Second issue.");

		var diff = session.Differences.ToString();
		IsTrue(diff.Contains("First issue."));
		IsTrue(diff.Contains("Second issue."));
	}

	[TestMethod]
	public void SessionToString()
	{
		var session = new CompareSession();
		session.AddDifference("This is different.");
		AreEqual("This is different.\r\n", session.Differences.ToString());
	}

	[TestMethod]
	public void UpdateResultShouldChangeResult()
	{
		var session = new CompareSession();
		AreEqual(CompareResult.Inconclusive, session.Result);

		session.UpdateResult(CompareResult.AreEqual);
		AreEqual(CompareResult.AreEqual, session.Result);

		session.UpdateResult(CompareResult.NotEqual);
		AreEqual(CompareResult.NotEqual, session.Result);
	}

	#endregion
}