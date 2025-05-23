﻿#region References

using System;
using Cornerstone.Protocols.Osc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Protocols.Osc;

[TestClass]
public class OscRgbaTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Equals()
	{
		var expected = new OscRgba { A = 0x01, B = 0x02, G = 0x03, R = 0x04 };
		var actual = new OscRgba { A = 0x01, B = 0x02, G = 0x03, R = 0x04 };
		IsTrue(expected == actual);
		IsTrue(expected.Equals(actual));
		IsTrue(expected.Equals(new byte[] { 0x04, 0x03, 0x02, 0x01 }));
		AreEqual(250755124, expected.GetHashCode());
		AreEqual(250755124, actual.GetHashCode());
	}

	[TestMethod]
	public void GetHashCodeShouldSucceed()
	{
		var rgba = new OscRgba();
		AreEqual(0, rgba.GetHashCode());
		rgba.R = 1;
		AreEqual(62570773, rgba.GetHashCode());
		rgba.R = 0;
		rgba.G = 1;
		AreEqual(157609, rgba.GetHashCode());
		rgba.G = 0;
		rgba.B = 1;
		AreEqual(397, rgba.GetHashCode());
		rgba.B = 0;
		rgba.A = 1;
		AreEqual(1, rgba.GetHashCode());
		rgba.A = 0;
		AreEqual(0, rgba.GetHashCode());
	}

	[TestMethod]
	public void NotEquals()
	{
		var notExpected = new OscRgba { A = 0x04, B = 0x03, G = 0x02, R = 0x01 };
		var actual = new OscRgba { A = 0x01, B = 0x02, G = 0x03, R = 0x04 };
		AreEqual(250755124, actual.GetHashCode());
		IsTrue(notExpected != actual);
		// ReSharper disable once SuspiciousTypeConversion.Global
		IsFalse(actual.Equals(true));
	}

	[TestMethod]
	public void ParseException()
	{
		ExpectedException<Exception>(() => OscRgba.Parse("FooBar"), "Invalid color 'FooBar'");
	}

	#endregion
}