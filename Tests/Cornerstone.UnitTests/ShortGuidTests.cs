#region References

using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests;

[TestClass]
[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class ShortGuidTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void CompareTo()
	{
		var expected = Guid.Parse("B2751797-F420-4FFD-A9B9-722C0A3C4FDF");
		var guid1 = ShortGuid.Parse("B2751797-F420-4FFD-A9B9-722C0A3C4FDF");
		var guid2 = new ShortGuid(expected);

		AreEqual(0, guid1.CompareTo(guid2));
		AreEqual(0, guid1.CompareTo(expected));
		AreEqual(0, guid2.CompareTo(expected));
		AreEqual(0, expected.CompareTo(guid1));
		AreEqual(0, expected.CompareTo(guid2));

		guid1 = ShortGuid.Parse("D8DECDB8-341D-4E69-95CE-2580B2AEC271");

		AreEqual(1, guid1.CompareTo(guid2));
		AreEqual(-1, guid2.CompareTo(guid1));
	}

	[TestMethod]
	[SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
	public void Equals()
	{
		var expected = Guid.Parse("B2751797-F420-4FFD-A9B9-722C0A3C4FDF");
		var guid1 = ShortGuid.Parse("B2751797-F420-4FFD-A9B9-722C0A3C4FDF");
		var guid2 = new ShortGuid(expected);

		Assert.IsTrue(guid1 == guid2);
		Assert.IsTrue(guid1.Equals(guid2));
		Assert.IsTrue(Equals(guid1, guid2));
		Assert.IsTrue(Equals(guid1, expected));
		Assert.IsTrue(Equals(guid1, "lxd1siD0_U-puXIsCjxP3w"));

		// Not equal but using Equals method
		Assert.IsFalse(guid1.Equals(ShortGuid.Empty));
		Assert.IsFalse(guid1.Equals(null));
		Assert.IsFalse(guid1.Equals(21));
		Assert.IsFalse(guid1.Equals(true));
	}

	[TestMethod]
	public void EqualsNot()
	{
		var guid1 = ShortGuid.Empty;
		var guid2 = new ShortGuid(Guid.Parse("B2751797-F420-4FFD-A9B9-722C0A3C4FDF"));

		Assert.IsTrue(guid1 != guid2);
	}

	[TestMethod]
	public void GetHashCodeTest()
	{
		var guid1 = ShortGuid.Empty;
		var guid2 = new ShortGuid(Guid.Parse("B2751797-F420-4FFD-A9B9-722C0A3C4FDF"));

		AreEqual(0, guid1.GetHashCode());
		AreEqual(246769172, guid2.GetHashCode());
	}

	[TestMethod]
	public void ImplicitConversions()
	{
		var expectedGuid = Guid.Parse("B2751797-F420-4FFD-A9B9-722C0A3C4FDF");
		var expectedShortGuid = expectedGuid.ToShortGuid();
		var expectedShortGuidString = expectedShortGuid.Value;

		ShortGuid actual1 = "lxd1siD0_U-puXIsCjxP3w";
		string actual2 = expectedShortGuid;
		Guid actual3 = expectedShortGuid;
		ShortGuid actual4 = expectedGuid;

		AreEqual(expectedShortGuid, actual1);
		AreEqual(expectedShortGuidString, actual2);
		AreEqual(expectedGuid, actual3);
		AreEqual(expectedShortGuid, actual4);
	}

	[TestMethod]
	public void NewGuid()
	{
		var actual = ShortGuid.NewGuid();
		Assert.AreNotEqual(ShortGuid.Empty, actual);
		Assert.AreNotEqual(0, actual.GetHashCode());
	}

	[TestMethod]
	public void ParseGuid()
	{
		var actual = ShortGuid.Parse("B2751797-F420-4FFD-A9B9-722C0A3C4FDF");
		AreEqual("lxd1siD0_U-puXIsCjxP3w", actual.ToString());
		AreEqual("b2751797-f420-4ffd-a9b9-722c0a3c4fdf", actual.Guid.ToString());

		actual = ShortGuid.Parse("lxd1siD0_U-puXIsCjxP3w");
		AreEqual("lxd1siD0_U-puXIsCjxP3w", actual.ToString());
		AreEqual("b2751797-f420-4ffd-a9b9-722c0a3c4fdf", actual.Guid.ToString());
	}

	[TestMethod]
	public void TryParse()
	{
		var result = ShortGuid.TryParse("B2751797-F420-4FFD-A9B9-722C0A3C4FDF", out var actual);
		AreEqual(true, result);
		AreEqual("lxd1siD0_U-puXIsCjxP3w", actual.ToString());
		AreEqual("b2751797-f420-4ffd-a9b9-722c0a3c4fdf", actual.Guid.ToString());

		IsFalse(ShortGuid.TryParse("aoeu", out actual));
		AreEqual(ShortGuid.Empty, actual);
	}

	[TestMethod]
	public void ToStringLength()
	{
		for (var i = 0; i < 20; i++)
		{
			var guid = ShortGuid.NewGuid();
			AreEqual(22, guid.ToString()?.Length);
		}
	}

	#endregion
}