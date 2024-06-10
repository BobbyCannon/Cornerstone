#region References

using System;
using Cornerstone.Protocols.Osc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Protocols.Osc;

[TestClass]
public class OscMidiTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void Equals()
	{
		var expected = new OscMidi(1, 2, 3, 4);
		var actual = new OscMidi([1, 2, 3, 4]);
		Assert.IsTrue(expected == actual);
		Assert.IsTrue(expected.Equals(actual));
		Assert.IsTrue(expected.Equals(new byte[] { 1, 2, 3, 4 }));
		AreEqual(62884804, expected.GetHashCode());
		AreEqual(62884804, actual.GetHashCode());
	}

	[TestMethod]
	public void GetHashCodeShouldSucceed()
	{
		var midi = new OscMidi();
		AreEqual(0, midi.GetHashCode());
		midi.Port = 1;
		AreEqual(62570773, midi.GetHashCode());
		midi.Port = 0;
		midi.Status = 1;
		AreEqual(157609, midi.GetHashCode());
		midi.Status = 0;
		midi.Data1 = 1;
		AreEqual(397, midi.GetHashCode());
		midi.Data1 = 0;
		midi.Data2 = 1;
		AreEqual(1, midi.GetHashCode());
		midi.Data2 = 0;
		AreEqual(0, midi.GetHashCode());
	}

	[TestMethod]
	public void NotEquals()
	{
		var notExpected = new OscMidi(1, 2, 3, 4);
		var actual = new OscMidi([4, 3, 2, 1]);
		AreEqual(250755124, actual.GetHashCode());
		Assert.IsTrue(notExpected != actual);
		// ReSharper disable once SuspiciousTypeConversion.Global
		Assert.IsFalse(actual.Equals(true));
	}

	[TestMethod]
	public void ParseException()
	{
		ExpectedException<Exception>(() => OscMidi.Parse("0"), "Not a midi message '0'");
	}

	#endregion
}