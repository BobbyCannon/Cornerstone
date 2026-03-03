#region References

using System;
using Cornerstone.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Runtime;

[TestClass]
public class ApplicationArgumentsTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ConvertToBooleanInvalidValueReturnsDefault()
	{
		var args = new ApplicationArguments();
		args.Parse(["-key1", "invalid"]);
		var result = args.GetValue<bool>("key1");
		IsFalse(result);
	}

	[TestMethod]
	public void ConvertToBooleanValidValueReturnsParsedValue()
	{
		var args = new ApplicationArguments();
		args.Parse(["-key1", "true"]);

		var result = args.GetValue<bool>("key1");

		IsTrue(result);
	}

	[TestMethod]
	public void ConvertToEnumInvalidValueReturnsDefault()
	{
		var args = new ApplicationArguments();
		args.Parse(["-key1", "invalid"]);

		var result = args.GetValue<DayOfWeek>("key1");

		AreEqual(default, result);
	}

	[TestMethod]
	public void ConvertToEnumValidValueReturnsParsedValue()
	{
		var args = new ApplicationArguments();
		args.Parse(["-key1", "Monday"]);

		var result = args.GetValue<DayOfWeek>("key1");

		AreEqual(DayOfWeek.Monday, result);
	}

	[TestMethod]
	public void ConvertToInt32InvalidValueReturnsDefault()
	{
		var args = new ApplicationArguments();
		args.Parse(["-key1", "invalid"]);

		var result = args.GetValue<int>("key1");

		AreEqual(0, result);
	}

	[TestMethod]
	public void ConvertToInt32ValidValueReturnsParsedValue()
	{
		var args = new ApplicationArguments();
		args.Parse(["-key1", "42"]);

		var result = args.GetValue<int>("key1");

		AreEqual(42, result);
	}

	[TestMethod]
	public void ConvertToNonExistentKeyReturnsDefault()
	{
		var args = new ApplicationArguments();

		var result = args.GetValue<int>("nonexistent");

		AreEqual(0, result);
	}

	[TestMethod]
	public void ConvertToUInt32InvalidValueReturnsDefault()
	{
		var args = new ApplicationArguments();
		args.Parse(["-key1", "invalid"]);

		var result = args.GetValue<uint>("key1");

		AreEqual(0u, result);
	}

	[TestMethod]
	public void ConvertToUInt32ValidValueReturnsParsedValue()
	{
		var args = new ApplicationArguments();
		args.Parse(["-key1", "42"]);

		var result = args.GetValue<uint>("key1");

		AreEqual(42u, result);
	}

	[TestMethod]
	public void IndexerGetNonExistentKeyReturnsNull()
	{
		var args = new ApplicationArguments();
		AreEqual(null, args["nonexistent"]);
	}

	[TestMethod]
	public void IndexerGetReturnsValueForKey()
	{
		var args = new ApplicationArguments();
		args.Parse(["-key1", "value1"]);
		AreEqual("value1", args["key1"]);
	}

	[TestMethod]
	public void ParseEmptyStringArgumentsSkipsEmpty()
	{
		var args = new ApplicationArguments();
		string[] input = ["", "-key1", "value1"];

		args.Parse(input);

		AreEqual("value1", args["key1"]);
	}

	[TestMethod]
	public void ParseMultiplePrefixesParsesCorrectly()
	{
		var args = new ApplicationArguments();
		string[] input = ["--key1", "value1", "--key2"];

		args.Parse(input, "--");

		AreEqual("value1", args["key1"]);
		IsNull(args["key2"]);
	}

	[TestMethod]
	public void ParseNullArgumentSkipsNull()
	{
		var args = new ApplicationArguments();
		string[] input = ["-key1", null, "-key2", "value2"];

		args.Parse(input);

		IsNull(args["key1"]);
		AreEqual("value2", args["key2"]);
	}

	[TestMethod]
	public void ParseOnlyPrefixAddsKeyWithNullValue()
	{
		var args = new ApplicationArguments();
		string[] input = ["-"];

		args.Parse(input);

		IsNull(args[""]);
	}

	[TestMethod]
	public void ParseWithArgumentsParsesCorrectly()
	{
		var args = new ApplicationArguments();
		string[] input = ["-key1", "value1", "other", "-key2"];

		args.Parse(input);

		AreEqual("value1", args["key1"]);
		IsNull(args["key2"]);
	}

	[TestMethod]
	public void ParseWithCustomPrefixParsesCorrectly()
	{
		var args = new ApplicationArguments();
		string[] input = ["--key1", "value1", "other"];

		args.Parse(input, "--");

		AreEqual("value1", args["key1"]);
	}

	[TestMethod]
	public void ToStringReturnsRawArgumentsJoined()
	{
		var args = new ApplicationArguments();
		args.Parse(["-key1", "value1", "other"]);
		var result = args.ToString();
		AreEqual("-key1 value1 other", result);
	}

	[TestMethod]
	public void TryGetArgumentExistingKeyReturnsTrueAndValue()
	{
		var args = new ApplicationArguments();
		args.Parse(["-key1", "value1"]);
		var result = args.TryGetValue<string>("key1", out var value);
		IsTrue(result);
		AreEqual("value1", value);
	}

	[TestMethod]
	public void TryGetArgumentNonExistentKeyReturnsFalseAndNull()
	{
		var args = new ApplicationArguments();
		var result = args.TryGetValue<string>("nonexistent", out var value);
		IsFalse(result);
		IsNull(value);
	}

	#endregion
}