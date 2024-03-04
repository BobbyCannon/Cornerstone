#region References

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security;
using Cornerstone.Extensions;
using Cornerstone.Generators;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Generators;

[TestClass]
[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class RandomGeneratorTests : CornerstoneUnitTest
{
	#region Constants

	public const byte LoopCount = 20;

	#endregion

	#region Methods

	[TestMethod]
	public void DefaultNextDecimal()
	{
		for (var i = 0; i < LoopCount; i++)
		{
			RandomGenerator.NextDecimal(0, 20, scale: 6).Dump();
		}
	}

	[TestMethod]
	public void GetBytes()
	{
		for (var i = 0; i < LoopCount; i++)
		{
			var actual = RandomGenerator.GetBytes(13);
			actual.DumpJson();
			Assert.AreEqual(13, actual.Length);
		}
	}

	[TestMethod]
	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	public void GetItem()
	{
		var items = new[] { 1, 2, 3, 4, 5, 6 };

		for (var i = 0; i < items.Length; i++)
		{
			var actual = RandomGenerator.GetItem(items);
			actual.Dump();
			IsTrue(items.Contains(actual));
		}

		var enumerable = items.AsEnumerable();

		for (var i = 0; i < items.Length; i++)
		{
			var actual = RandomGenerator.GetItem(enumerable);
			actual.Dump();
			IsTrue(items.Contains(actual));
		}
	}

	[TestMethod]
	public void GetPassword()
	{
		var actual = RandomGenerator.GetPassword(24, false);
		actual.ToUnsecureString().Dump();

		for (var i = 0; i < LoopCount; i++)
		{
			actual = RandomGenerator.GetPassword(24);
			actual.ToUnsecureString().Dump();
			Assert.AreEqual(24, actual.Length);
		}
	}

	[TestMethod]
	public void GetPhoneNumber()
	{
		for (var i = 0; i < LoopCount; i++)
		{
			RandomGenerator.GetPhoneNumber().Dump();
			RandomGenerator.GetPhoneNumber(true).Dump();
		}
	}

	[TestMethod]
	public void LoremIpsum()
	{
		for (var i = 0; i < LoopCount; i++)
		{
			RandomGenerator.LoremIpsum().Dump();
		}

		var actual = RandomGenerator.LoremIpsum(prefix: "foo");
		IsTrue(actual.StartsWith("foo"));
		actual.Dump();
	}

	[TestMethod]
	public void NextBool()
	{
		for (var i = 0; i < LoopCount; i++)
		{
			RandomGenerator.NextBool().Dump();
		}
	}

	[TestMethod]
	public void NextDateTime()
	{
		for (var i = 0; i < LoopCount; i++)
		{
			RandomGenerator.NextDateTime(DateTime.MinValue, DateTime.MaxValue);
			var actual = RandomGenerator.NextDateTime(DateTime.MinValue, DateTime.MinValue);
			actual.Dump();
			Assert.AreEqual(DateTime.MinValue, actual);
			actual = RandomGenerator.NextDateTime(DateTime.MaxValue, DateTime.MaxValue);
			actual.Dump();
			Assert.AreEqual(DateTime.MaxValue, actual);
			actual = RandomGenerator.NextDateTime(new DateTime(1970, 01, 01), new DateTime(2024, 01, 01));
			actual.Dump();
		}
	}

	[TestMethod]
	public void NextDecimal()
	{
		for (var i = 0; i < LoopCount; i++)
		{
			var actual = RandomGenerator.NextDecimal();
			actual.Dump();

			actual = RandomGenerator.NextDecimal(scale: 6);
			actual.Dump();
		}
	}

	[TestMethod]
	public void NextDecimalWithSameMinimumAndMaximum()
	{
		for (var i = 0; i < LoopCount; i++)
		{
			var actual = RandomGenerator.NextDecimal(64, 64);
			actual.Dump();
			Assert.AreEqual(64, actual);
			actual = RandomGenerator.NextDecimal(-46, -46);
			actual.Dump();
			Assert.AreEqual(-46, actual);
		}
	}

	[TestMethod]
	public void NextDouble()
	{
		byte scale = 3;

		for (var i = 0; i < LoopCount; i++)
		{
			var actual = RandomGenerator.NextDouble(scale: scale++);
			actual.Dump();
			IsFalse(double.IsInfinity(actual), "Double was infinite and should not have been.");

			if (scale > 10)
			{
				scale = 3;
			}
		}
	}

	[TestMethod]
	public void NextDoubleWithRange()
	{
		var min = -100000.0d;
		var max = -10.0d;
		byte scale = 3;

		for (var i = 0; i < LoopCount; i++)
		{
			var actual = RandomGenerator.NextDouble(min, max, scale++);
			actual.Dump();
			IsTrue(actual >= min, "Value is less than min...");
			IsTrue(actual <= max, "Value is greater than max...");

			if (scale > 10)
			{
				scale = 2;
			}
		}
	}

	[TestMethod]
	public void NextDoubleWithSameMinimumMaximum()
	{
		for (var i = 0; i < LoopCount; i++)
		{
			var actual = RandomGenerator.NextDouble(10.0, 10.0);
			actual.Dump();
			Assert.AreEqual(10.0, actual);
		}
	}

	[TestMethod]
	public void NextInteger()
	{
		for (var i = 0; i < LoopCount; i++)
		{
			RandomGenerator.NextInteger().Dump();
		}
	}

	[TestMethod]
	public void NextLong()
	{
		for (var i = 0; i < LoopCount; i++)
		{
			RandomGenerator.NextLong().Dump();
		}
	}

	[TestMethod]
	public void NextLongWithSameMinimumMaximum()
	{
		for (var i = 0; i < LoopCount; i++)
		{
			var actual = RandomGenerator.NextLong(48, 48);
			actual.Dump();
			Assert.AreEqual(48, actual);
		}
	}

	[TestMethod]
	public void NextString()
	{
		for (var i = 0; i < LoopCount; i++)
		{
			RandomGenerator.NextString(12).Dump();
		}
	}

	[TestMethod]
	public void NextStringWithProvidedCharacters()
	{
		for (var i = 0; i < LoopCount; i++)
		{
			var actual = RandomGenerator.NextString(4, "a");
			actual.Dump();
			Assert.AreEqual("aaaa", actual);
		}
	}

	[TestMethod]
	public void NextUnsignedInteger()
	{
		for (var i = 0; i < LoopCount; i++)
		{
			RandomGenerator.NextUnsignedInteger().Dump();
		}
	}

	[TestMethod]
	public void Populate()
	{
		for (var i = 0; i < LoopCount; i++)
		{
			var actual = new char[12];
			RandomGenerator.Populate(ref actual);
			actual.DumpJson();
		}
	}

	[TestMethod]
	public void PopulateUniqueItems()
	{
		var actual = new char[40];
		RandomGenerator.PopulateUnique(ref actual, RandomGenerator.AlphabetAndNumbersCharacters);
		var distinct = actual.Distinct();
		actual.DumpJson();
		AreEqual(distinct, actual);
	}

	[TestMethod]
	public void SetPassword()
	{
		for (var i = 0; i < LoopCount; i++)
		{
			var actual = new SecureString();
			Assert.AreEqual(0, actual.Length);
			RandomGenerator.SetPassword(actual);
			Assert.AreEqual(16, actual.Length);
			actual.ToUnsecureString().Dump();

			actual = new SecureString();
			RandomGenerator.SetPassword(actual, 14, false);
			Assert.AreEqual(14, actual.Length);
			actual.ToUnsecureString().Dump();
		}
	}

	[TestMethod]
	public void Shuffle()
	{
		for (var i = 0; i < LoopCount; i++)
		{
			var expected = new[] { 0, 1, 2, 3, 4, 5, 6 };
			var actual = new[] { 0, 1, 2, 3, 4, 5, 6 }.Shuffle();

			actual.DumpJson();

			AreNotEqual(expected, actual);
		}
	}

	#endregion
}